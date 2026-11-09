namespace Dragonmark.Core;

public static class Game { public const uint AppId = 489830; }
public enum ConnectionState { Disconnected, Connecting, Loading, Ready, ApplyingChanges, Verifying, Completed, PartialFailure, Failed }
public enum ChangeKind { Unlock, Clear }
public enum OutcomeStatus { Successful, Failed, Unverified }
public enum AchievementFilter { All, Locked, Unlocked, Hidden, Rare }
public enum AchievementSort { Name, UnlockDate, Rarity }
public sealed record Achievement(string ApiId, string Name, string Description, bool Unlocked,
    DateTimeOffset? UnlockedAt = null, bool Hidden = false, double? GlobalPercent = null);
public sealed record AchievementIcon(int Width, int Height, byte[] Rgba);
public sealed record ProviderInfo(string Mode, bool IsDemo, bool CanClear, string AccountKey);
public sealed record ChangePlan(ChangeKind Kind, IReadOnlyList<string> Targets, int Skipped);
public sealed record ChangeOutcome(string ApiId, bool Accepted, bool PersistenceAcknowledged, bool Verified, string Message)
{
    public OutcomeStatus Status => Verified ? OutcomeStatus.Successful : Accepted ? OutcomeStatus.Unverified : OutcomeStatus.Failed;
}
public sealed record BatchResult(IReadOnlyList<ChangeOutcome> Outcomes, IReadOnlyList<Achievement> Achievements, bool IsDemo)
{
    public int VerifiedCount => Outcomes.Count(x => x.Verified);
    public int FailedCount => Outcomes.Count(x => x.Status == OutcomeStatus.Failed);
    public int UnverifiedCount => Outcomes.Count(x => x.Status == OutcomeStatus.Unverified);
    public bool Complete => Outcomes.Count > 0 && Outcomes.All(x => x.Verified);
}
public enum OperationPhase { Idle, Pending, AcceptedLocally, Submitted, Verifying, Completed }
public sealed record ProviderProgress(ConnectionState State, string Message, OperationPhase Phase = OperationPhase.Idle, int Accepted = 0, int Total = 0);
public sealed class ProviderException(string code, string message, Exception? inner = null) : Exception(message, inner)
{ public string Code { get; } = code; }

public interface IAchievementProvider : IAsyncDisposable
{
    ProviderInfo Info { get; }
    event Action<ProviderProgress>? Progress;
    Task<IReadOnlyList<Achievement>> ConnectAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Achievement>> RefreshAsync(CancellationToken cancellationToken);
    Task<BatchResult> ApplyAsync(ChangePlan plan, bool confirmed, CancellationToken cancellationToken);
    Task<AchievementIcon?> GetIconAsync(string apiId, CancellationToken cancellationToken);
}

public static class AchievementQuery
{
    public static IReadOnlyList<Achievement> Apply(IEnumerable<Achievement> source, string search = "",
        AchievementFilter filter = AchievementFilter.All, AchievementSort sort = AchievementSort.Name)
    {
        var query = source.Where(a => (a.Name + "\n" + a.Description + "\n" + a.ApiId)
            .Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        query = filter switch
        {
            AchievementFilter.Locked => query.Where(a => !a.Unlocked),
            AchievementFilter.Unlocked => query.Where(a => a.Unlocked),
            AchievementFilter.Hidden => query.Where(a => a.Hidden),
            AchievementFilter.Rare => query.Where(a => a.GlobalPercent is < 10),
            _ => query
        };
        return (sort switch
        {
            AchievementSort.UnlockDate => query.OrderByDescending(a => a.UnlockedAt).ThenBy(a => a.ApiId),
            AchievementSort.Rarity => query.OrderBy(a => a.GlobalPercent ?? double.MaxValue).ThenBy(a => a.ApiId),
            _ => query.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ThenBy(a => a.ApiId)
        }).ToArray();
    }
}

public sealed class Selection
{
    private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> Ids => _ids.ToArray();
    public bool Contains(string id) => _ids.Contains(id);
    public void Set(string id, bool selected) { if (selected) _ids.Add(id); else _ids.Remove(id); }
    public void Clear() => _ids.Clear();
    public void Retain(IEnumerable<Achievement> items) => _ids.IntersectWith(items.Select(a => a.ApiId));
}

public static class OperationPlanner
{
    public static ChangePlan UnlockAll(IEnumerable<Achievement> achievements)
    {
        var items = achievements.ToArray();
        return Create(items, items.Select(a => a.ApiId), ChangeKind.Unlock);
    }
    public static ChangePlan Create(IEnumerable<Achievement> achievements, IEnumerable<string> selection, ChangeKind kind)
    {
        var map = achievements.ToDictionary(a => a.ApiId, StringComparer.Ordinal);
        var ids = selection.Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Any(id => !map.ContainsKey(id))) throw new ArgumentException("Selection contains unknown achievement identifiers.");
        var targets = ids.Where(id => map[id].Unlocked == (kind == ChangeKind.Clear)).ToArray();
        return new(kind, targets, ids.Length - targets.Length);
    }
}
