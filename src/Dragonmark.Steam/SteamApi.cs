using Dragonmark.Core;
using Steamworks;

namespace Dragonmark.Steam;

public sealed class SteamApi : ISteamApi
{
    private bool _initialized;
    private Callback<UserStatsReceived_t>? _received;
    private Callback<UserStatsStored_t>? _stored;
    private Callback<UserAchievementStored_t>? _achievementStored;
    private Callback<UserAchievementIconFetched_t>? _iconFetched;
    private readonly Dictionary<(string Id, bool Unlocked), AchievementIcon> _icons = [];
    private readonly HashSet<string> _knownIds = new(StringComparer.Ordinal);
    private CallResult<UserStatsReceived_t>? _userRequest;
    private CallResult<GlobalAchievementPercentagesReady_t>? _percentages;
    private readonly Action<string> _log;
    public SteamApi(Action<string> log) => _log = log;
    public event Action<StatsReply>? StatsReceived;
    public event Action<StoreReply>? StatsStored;
    public uint AppId => SteamUtils.GetAppID().m_AppId;
    public ulong UserId => SteamUser.GetSteamID().m_SteamID;
    public bool LoggedOn => SteamUser.BLoggedOn();
    public bool OwnsGame => SteamApps.BIsSubscribedApp(new(Game.AppId));
    public bool GameInstalled => SteamApps.BIsAppInstalled(new(Game.AppId));
    public bool PercentagesReady { get; private set; }
    public bool Initialize()
    {
        if (_initialized) return true;
        _initialized = SteamAPI.Init();
        if (!_initialized) return false;
        _received = Callback<UserStatsReceived_t>.Create(r => StatsReceived?.Invoke(new(r.m_nGameID, r.m_steamIDUser.m_SteamID, r.m_eResult == EResult.k_EResultOK, r.m_eResult.ToString())));
        _stored = Callback<UserStatsStored_t>.Create(r => StatsStored?.Invoke(new(r.m_nGameID, r.m_eResult == EResult.k_EResultOK, r.m_eResult.ToString())));
        // This is diagnostic evidence only, never a substitute for the StoreStats callback.
        _achievementStored = Callback<UserAchievementStored_t>.Create(r =>
        {
            if (r.m_nGameID == Game.AppId) _log($"Achievement callback: {r.m_rgchAchievementName}; progress {r.m_nCurProgress}/{r.m_nMaxProgress}.");
        });
        _iconFetched = Callback<UserAchievementIconFetched_t>.Create(r =>
        {
            // The callback identifies both the API name and the actual Steam variant.
            // Never paint or relabel the other variant to simulate a state change.
            if (r.m_nGameID.m_GameID != Game.AppId || !_knownIds.Contains(r.m_rgchAchievementName)) return;
            var image = DecodeIcon(r.m_nIconHandle);
            if (image is not null) _icons[(r.m_rgchAchievementName, r.m_bAchieved)] = image;
        });
        _userRequest = CallResult<UserStatsReceived_t>.Create((r, failure) => StatsReceived?.Invoke(new(r.m_nGameID, r.m_steamIDUser.m_SteamID, !failure && r.m_eResult == EResult.k_EResultOK, failure ? "I/O failure" : r.m_eResult.ToString())));
        _percentages = CallResult<GlobalAchievementPercentagesReady_t>.Create((r, failure) =>
        {
            PercentagesReady = !failure && r.m_nGameID == Game.AppId && r.m_eResult == EResult.k_EResultOK;
            if (!PercentagesReady) _log("Global percentages unavailable: " + r.m_eResult);
        });
        return true;
    }
    public bool RequestCurrentStats() => SteamUserStats.RequestCurrentStats();
    public bool RequestUserStats()
    {
        var handle = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
        if (handle == SteamAPICall_t.Invalid) return false;
        _userRequest!.Set(handle);
        return true;
    }
    public bool RequestPercentages()
    {
        var handle = SteamUserStats.RequestGlobalAchievementPercentages();
        if (handle == SteamAPICall_t.Invalid) return false;
        _percentages!.Set(handle);
        return true;
    }
    public void Pump() => SteamAPI.RunCallbacks();
    public IReadOnlyList<Achievement> ReadAchievements(bool userCache)
    {
        uint count = SteamUserStats.GetNumAchievements();
        if (count > 10000) throw new ProviderException("metadata", "Steam returned an unreasonable achievement count.");
        var result = new List<Achievement>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (uint index = 0; index < count; index++)
        {
            string id = SteamUserStats.GetAchievementName(index);
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new ProviderException("metadata", "Steam returned a missing or duplicate achievement identifier.");
            bool unlocked; uint time;
            bool read = userCache
                ? SteamUserStats.GetUserAchievementAndUnlockTime(SteamUser.GetSteamID(), id, out unlocked, out time)
                : SteamUserStats.GetAchievementAndUnlockTime(id, out unlocked, out time);
            if (!read) throw new ProviderException("read", $"Steam could not read {id}. No incomplete state will be used for changes.");
            string name = SteamUserStats.GetAchievementDisplayAttribute(id, "name");
            string description = SteamUserStats.GetAchievementDisplayAttribute(id, "desc");
            bool hidden = SteamUserStats.GetAchievementDisplayAttribute(id, "hidden") == "1";
            double? percent = PercentagesReady && SteamUserStats.GetAchievementAchievedPercent(id, out float p) && float.IsFinite(p) && p is >= 0 and <= 100 ? p : null;
            result.Add(new(id, string.IsNullOrWhiteSpace(name) ? id : name, description ?? "", unlocked,
                unlocked && time != 0 ? DateTimeOffset.FromUnixTimeSeconds(time) : null, hidden, percent));
        }
        _knownIds.Clear();
        _knownIds.UnionWith(ids);
        return result;
    }
    public bool SetAchievement(string id) => SteamUserStats.SetAchievement(id);
    public bool ClearAchievement(string id) => SteamUserStats.ClearAchievement(id);
    public bool StoreStats() => SteamUserStats.StoreStats();
    public AchievementIcon? ReadIcon(string id, bool unlocked)
    {
        if (!_knownIds.Contains(id)) return null;
        if (_icons.TryGetValue((id, unlocked), out var cached)) return cached;
        // GetAchievementIcon exposes the current variant, not an arbitrary-state API.
        // During an uncertain staged write it may disagree with the verified row state.
        if (!SteamUserStats.GetAchievement(id, out bool current) || current != unlocked) return null;
        int handle = SteamUserStats.GetAchievementIcon(id);
        var image = DecodeIcon(handle);
        if (image is not null) _icons[(id, unlocked)] = image;
        return image;
    }
    private static AchievementIcon? DecodeIcon(int handle)
    {
        if (handle <= 0 || !SteamUtils.GetImageSize(handle, out uint width, out uint height) || width is 0 or > 512 || height is 0 or > 512) return null;
        var pixels = new byte[checked((int)(width * height * 4))];
        return SteamUtils.GetImageRGBA(handle, pixels, pixels.Length) ? new((int)width, (int)height, pixels) : null;
    }
    public void Dispose()
    {
        _received?.Dispose(); _stored?.Dispose(); _achievementStored?.Dispose(); _iconFetched?.Dispose(); _userRequest?.Dispose(); _percentages?.Dispose();
        _icons.Clear(); _knownIds.Clear();
        if (_initialized) SteamAPI.Shutdown();
        _initialized = false;
        PercentagesReady = false;
    }
}
