using System.Diagnostics;
using Dragonmark.Core;
using Dragonmark.Infrastructure;

namespace Dragonmark.Steam;

public sealed class SteamAchievementProvider : IAchievementProvider
{
    private readonly ISteamApi _api;
    private readonly Action<string> _log;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _storeInterval;
    private int _busy;
    private bool _initialized, _ready, _uncertain, _disposed;
    private ulong _user;
    private DateTimeOffset _nextStore;
    private IReadOnlyList<Achievement> _items = [];
    public ProviderInfo Info { get; private set; } = new("LIVE · Steam · Experimental writes", false, false, "disconnected");
    public event Action<ProviderProgress>? Progress;
    public SteamAchievementProvider(ISteamApi api, Action<string> log, TimeSpan? timeout = null, TimeSpan? storeInterval = null)
    {
        _api = api; _log = log; _timeout = timeout ?? TimeSpan.FromSeconds(25);
        _storeInterval = storeInterval ?? TimeSpan.FromMinutes(2);
    }
    private void Report(ConnectionState state, string message, OperationPhase phase = OperationPhase.Idle, int accepted = 0, int total = 0) { _log($"{state}: {message}"); Progress?.Invoke(new(state, message, phase, accepted, total)); }
    private Task<T> Run<T>(Func<T> action, CancellationToken token, bool quietCancellation = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) throw new ProviderException("busy", "Another Steam operation is still running.");
        return Task.Run(() =>
        {
            try { token.ThrowIfCancellationRequested(); return action(); }
            catch (OperationCanceledException) { if (!quietCancellation) Report(_ready ? ConnectionState.Ready : ConnectionState.Disconnected, "Operation cancelled before any new changes were staged."); throw; }
            catch (Exception e)
            {
                Report(ConnectionState.Failed, e is DllNotFoundException ? "Missing steam_api64.dll. See Diagnostics for official setup instructions." : e.Message);
                if (e is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
                    throw new ProviderException("native-library", "Steam native library is missing or incompatible. Use the official Steamworks.NET 2024.8.0 x64 library; see docs/STEAM_INTEGRATION.md.", e);
                throw;
            }
            finally { Volatile.Write(ref _busy, 0); }
        });
    }
    public Task<IReadOnlyList<Achievement>> ConnectAsync(CancellationToken token) => Run(() =>
    {
        _ready = false;
        Report(ConnectionState.Connecting, "Initializing the documented Steamworks API.");
        if (!_initialized)
        {
            if (!_api.Initialize()) throw new ProviderException("init", "Steam initialization failed. Check Steam is running under the same Windows user, the App ID file, and your Skyrim license. Standalone compatibility is not guaranteed.");
            _initialized = true;
        }
        ValidateContext(false);
        _user = _api.UserId;
        Info = Info with { AccountKey = LocalData.AccountKey(_user) };
        if (!_api.GameInstalled) _log("Skyrim is not reported as installed. Installation alone does not establish Steamworks compatibility.");
        Report(ConnectionState.Loading, "Requesting current-user achievement data.");
        RequestStats(false, token);
        if (_api.RequestPercentages())
        {
            var timer = Stopwatch.StartNew();
            while (!_api.PercentagesReady && timer.Elapsed < TimeSpan.FromSeconds(2)) { token.ThrowIfCancellationRequested(); _api.Pump(); Thread.Sleep(20); }
        }
        _items = _api.ReadAchievements(false);
        if (_items.Count == 0) throw new ProviderException("empty", "Steam exposed no achievements in this context. Live changes are disabled.");
        _ready = true;
        Report(ConnectionState.Ready, "Live Steam data loaded. Achievement changes have not been validated on this installation.");
        return _items;
    }, token);

    private void ValidateContext(bool sameUser = true)
    {
        if (_api.AppId != Game.AppId) throw new ProviderException("app-id", $"Incorrect Steam App ID: {_api.AppId}. Expected {Game.AppId}; changes are blocked.");
        if (!_api.LoggedOn) throw new ProviderException("offline", "Steam is offline or no user is logged in. Sign in and reconnect.");
        if (!_api.OwnsGame) throw new ProviderException("license", "The active Steam account does not expose a Skyrim Special Edition license.");
        if (_api.UserId == 0 || (sameUser && _api.UserId != _user)) { _ready = false; throw new ProviderException("account", "The active Steam account changed or is invalid. Reconnect before continuing."); }
    }
    private void RequestStats(bool userCache, CancellationToken token)
    {
        // Drain queued callbacks before attaching the request listener. RequestUserStats also uses a native call handle.
        _api.Pump();
        StatsReply? reply = null;
        void OnReply(StatsReply r) { if (r.GameId == Game.AppId && r.UserId == _user) reply = r; }
        _api.StatsReceived += OnReply;
        try
        {
            if (!(userCache ? _api.RequestUserStats() : _api.RequestCurrentStats())) throw new ProviderException("request", "Steam rejected the achievement data request.");
            WaitFor(() => reply is not null, token);
            if (!reply!.Success) throw new ProviderException("stats", "Steam could not refresh achievements: " + reply.Result);
        }
        finally { _api.StatsReceived -= OnReply; }
    }
    private void WaitFor(Func<bool> done, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        while (!done())
        {
            token.ThrowIfCancellationRequested();
            if (timer.Elapsed >= _timeout) throw new ProviderException("timeout", "Steam callback timed out. Persistence is not confirmed; check Steam before retrying.");
            _api.Pump(); Thread.Sleep(20);
        }
    }
    public Task<IReadOnlyList<Achievement>> RefreshAsync(CancellationToken token) => Run(() =>
    {
        if (!_ready) throw new ProviderException("disconnected", "Connect to Steam first.");
        ValidateContext();
        Report(ConnectionState.Loading, "Refreshing achievement state from Steam.");
        RequestStats(true, token);
        _items = _api.ReadAchievements(true);
        Report(ConnectionState.Ready, _uncertain ? "Refreshed. A previous write remains uncertain; further writes are blocked for this session." : "Steam achievement state refreshed.");
        return _items;
    }, token);

    public Task<BatchResult> ApplyAsync(ChangePlan plan, bool confirmed, CancellationToken token) => Run(() =>
    {
        if (!confirmed) throw new ProviderException("confirmation", "Explicit confirmation is required before changing Steam data.");
        if (!_ready) throw new ProviderException("disconnected", "Connect to Steam first.");
        if (_uncertain) throw new ProviderException("uncertain", "An earlier write has an uncertain outcome. Further writes are blocked. Verify your Steam profile before restarting Dragonmark; Steam may persist staged changes on exit.");
        if (plan.Kind == ChangeKind.Clear && !Info.CanClear) throw new ProviderException("unsupported", "Individual clearing has not been validated for this integration and is disabled in live mode.");
        ValidateContext();
        if (DateTimeOffset.UtcNow < _nextStore) throw new ProviderException("rate-limit", $"Please wait {Math.Ceiling((_nextStore - DateTimeOffset.UtcNow).TotalSeconds)} seconds before another batch.");
        RequestStats(true, token);
        _items = _api.ReadAchievements(true);
        plan = OperationPlanner.Create(_items, plan.Targets, plan.Kind);
        token.ThrowIfCancellationRequested();
        if (plan.Targets.Count == 0) return new BatchResult([], _items, false);
        Report(ConnectionState.ApplyingChanges, "Staging the confirmed batch. Cancellation is now disabled until persistence verification finishes.", OperationPhase.Pending, 0, plan.Targets.Count);
        var accepted = new HashSet<string>(StringComparer.Ordinal);
        bool acknowledged = false;
        string failure = "Steam rejected the local change.";
        StoreReply? stored = null;
        void OnStored(StoreReply r) { if (r.GameId == Game.AppId) stored = r; }
        // Steam does not provide a store request ID. Serialize stores, drain old callbacks,
        // and permanently block this provider's writes after an ambiguous outcome.
        _api.Pump();
        _api.StatsStored += OnStored;
        try
        {
            _uncertain = true; // Even an exception after the first native call must not permit a blind retry.
            foreach (string id in plan.Targets)
            {
                if (plan.Kind == ChangeKind.Unlock ? _api.SetAchievement(id) : _api.ClearAchievement(id)) accepted.Add(id);
            }
            Report(ConnectionState.ApplyingChanges, $"{accepted.Count}/{plan.Targets.Count} accepted locally. Persistence is not yet confirmed.", OperationPhase.AcceptedLocally, accepted.Count, plan.Targets.Count);
            if (accepted.Count > 0)
            {
                _nextStore = DateTimeOffset.UtcNow + _storeInterval;
                if (!_api.StoreStats()) throw new ProviderException("store", "StoreStats rejected the batch. Local changes may still persist later or on exit.");
                Report(ConnectionState.ApplyingChanges, "Submitted to Steam. Waiting for the store acknowledgement; this is not verified success.", OperationPhase.Submitted, accepted.Count, plan.Targets.Count);
                WaitFor(() => stored is not null, CancellationToken.None);
                if (!stored!.Success) throw new ProviderException("store", "Steam did not acknowledge persistence: " + stored.Result);
                acknowledged = true;
                Report(ConnectionState.Verifying, "Steam acknowledged persistence. Requesting refreshed achievement state.", OperationPhase.Verifying, accepted.Count, plan.Targets.Count);
                ValidateContext();
                RequestStats(true, CancellationToken.None);
                _items = _api.ReadAchievements(true);
            }
            else
            {
                RequestStats(true, CancellationToken.None);
                _items = _api.ReadAchievements(true);
            }
            _uncertain = false;
        }
        catch (Exception e)
        {
            failure = e.Message;
            _log("Write outcome uncertain: " + e.Message);
            // Refresh the displayed list even on a failed store. A matching read without
            // successful persistence evidence never upgrades an outcome to success.
            try { RequestStats(true, CancellationToken.None); _items = _api.ReadAchievements(true); }
            catch (Exception refreshError) { _log("Post-failure refresh unavailable: " + refreshError.Message); }
            // Do not perform a compensating clear/unlock: that would be another unconfirmed mutation.
        }
        finally { _api.StatsStored -= OnStored; }
        var outcomes = plan.Targets.Select(id =>
        {
            bool verified = accepted.Contains(id) && acknowledged && !_uncertain && _items.Any(a => a.ApiId == id && a.Unlocked == (plan.Kind == ChangeKind.Unlock));
            return new ChangeOutcome(id, accepted.Contains(id), accepted.Contains(id) && acknowledged, verified,
                verified ? "Steam persistence callback acknowledged; refreshed user state matches." : accepted.Contains(id) ? (_uncertain ? failure : "Refreshed state did not match the requested change.") : "Steam rejected the local change.");
        }).ToArray();
        if (outcomes.Any(x => x.Accepted && !x.Verified)) _uncertain = true;
        var result = new BatchResult(outcomes, _items, false);
        Report(result.Complete ? ConnectionState.Completed : ConnectionState.PartialFailure,
            $"{result.VerifiedCount}/{outcomes.Length} changes acknowledged and verified." + (_uncertain ? " Further writes blocked; Steam may persist pending changes later." : ""), OperationPhase.Completed, accepted.Count, plan.Targets.Count);
        return result;
    }, token);
    public Task<AchievementIcon?> GetIconAsync(string apiId, CancellationToken token) => Run(() =>
    {
        if (!_ready) return null;
        ValidateContext();
        var item = _items.FirstOrDefault(a => a.ApiId == apiId);
        if (item is null) return null;
        var image = _api.ReadIcon(apiId, item.Unlocked);
        var timer = Stopwatch.StartNew();
        while (image is null && timer.Elapsed < TimeSpan.FromMilliseconds(500)) { token.ThrowIfCancellationRequested(); _api.Pump(); Thread.Sleep(20); image = _api.ReadIcon(apiId, item.Unlocked); }
        return image;
    }, token, quietCancellation: true);
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        while (Volatile.Read(ref _busy) != 0) await Task.Delay(20).ConfigureAwait(false);
        _api.Dispose(); _initialized = false; _ready = false;
    }
}
