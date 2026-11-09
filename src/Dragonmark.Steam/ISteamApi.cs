using Dragonmark.Core;

namespace Dragonmark.Steam;

public sealed record StatsReply(ulong GameId, ulong UserId, bool Success, string Result);
public sealed record StoreReply(ulong GameId, bool Success, string Result);

/// <summary>Small native boundary, replaceable by a fake in account-safe tests.</summary>
public interface ISteamApi : IDisposable
{
    bool Initialize();
    uint AppId { get; }
    ulong UserId { get; }
    bool LoggedOn { get; }
    bool OwnsGame { get; }
    bool GameInstalled { get; }
    event Action<StatsReply>? StatsReceived;
    event Action<StoreReply>? StatsStored;
    bool RequestCurrentStats();
    bool RequestUserStats();
    bool RequestPercentages();
    bool PercentagesReady { get; }
    void Pump();
    IReadOnlyList<Achievement> ReadAchievements(bool userCache);
    bool SetAchievement(string id);
    bool ClearAchievement(string id);
    bool StoreStats();
    AchievementIcon? ReadIcon(string id, bool unlocked);
}
