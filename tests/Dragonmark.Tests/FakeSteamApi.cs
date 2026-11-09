using Dragonmark.Core;
using Dragonmark.Steam;

namespace Dragonmark.Tests;

internal sealed class FakeSteamApi : ISteamApi
{
    public bool InitResult = true, SendStats = true, StatsSuccess = true, StoreAccepted = true, SendStore = true, StoreSuccess = true, MatchRefresh = true;
    public Exception? InitError;
    public uint AppId { get; set; } = Game.AppId;
    public ulong UserId { get; set; } = 123;
    public bool LoggedOn { get; set; } = true;
    public bool OwnsGame { get; set; } = true;
    public bool GameInstalled => true;
    public bool PercentagesReady => true;
    public bool Disposed;
    public int StoreCalls, SetCalls, ClearCalls;
    public bool ForeignCallbackOnly;
    public Action? OnSet;
    public Action? AfterStatsReply;
    public Action? OnRead;
    public Action? AfterPump;
    public readonly List<ulong> WriteUsers = [];
    public readonly List<ulong> StoreUsers = [];
    public HashSet<string> Rejected { get; } = [];
    public HashSet<string> Unmatched { get; } = [];
    public bool FailRefreshAfterStore;
    public List<Achievement> Items = MockAchievementProvider.CreateFixtures();
    private readonly Queue<Action> _events = new();
    private readonly HashSet<string> _staged = [];
    public event Action<StatsReply>? StatsReceived;
    public event Action<StoreReply>? StatsStored;
    public bool Initialize() { if (InitError is not null) throw InitError; return InitResult; }
    public bool RequestCurrentStats() => RequestUserStats();
    public bool RequestUserStats()
    {
        ulong requestedUser = UserId;
        if (SendStats) _events.Enqueue(() => { StatsReceived?.Invoke(new(ForeignCallbackOnly ? 1u : Game.AppId, requestedUser, StatsSuccess, StatsSuccess ? "OK" : "Fail")); AfterStatsReply?.Invoke(); });
        return true;
    }
    public bool RequestPercentages() => true;
    public void Pump() { if (_events.TryDequeue(out var action)) action(); AfterPump?.Invoke(); }
    public IReadOnlyList<Achievement> ReadAchievements(bool userCache) { var result = Items.ToArray(); OnRead?.Invoke(); return result; }
    public bool SetAchievement(string id)
    {
        WriteUsers.Add(UserId); SetCalls++; OnSet?.Invoke();
        if (Rejected.Contains(id)) return false;
        _staged.Add(id); return true;
    }
    public bool ClearAchievement(string id) { ClearCalls++; return false; }
    public bool StoreStats()
    {
        StoreCalls++;
        StoreUsers.Add(UserId);
        if (!StoreAccepted) return false;
        if (SendStore) _events.Enqueue(() =>
        {
            if (StoreSuccess && MatchRefresh) Items = Items.Select(a => _staged.Contains(a.ApiId) && !Unmatched.Contains(a.ApiId) ? a with { Unlocked = true, UnlockedAt = DateTimeOffset.UtcNow } : a).ToList();
            if (FailRefreshAfterStore) SendStats = false;
            StatsStored?.Invoke(new(ForeignCallbackOnly ? 1u : Game.AppId, StoreSuccess, StoreSuccess ? "OK" : "InvalidParam"));
        });
        return true;
    }
    public readonly List<(string Id, bool Unlocked)> IconRequests = [];
    public AchievementIcon? Icon;
    public AchievementIcon? ReadIcon(string id, bool unlocked) { IconRequests.Add((id, unlocked)); return Icon; }
    public void Dispose() => Disposed = true;
}
