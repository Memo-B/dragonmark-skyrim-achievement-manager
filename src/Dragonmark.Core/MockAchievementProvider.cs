namespace Dragonmark.Core;

/// <summary>Original fictional fixtures. Never connects to Steam.</summary>
public sealed class MockAchievementProvider : IAchievementProvider
{
    private List<Achievement> _items = CreateFixtures();
    private bool _connected;
    public ProviderInfo Info { get; } = new("DEMO · Simulated data", true, true, "demo");
    public event Action<ProviderProgress>? Progress;
    public HashSet<string> RejectIds { get; } = new(StringComparer.Ordinal);
    public static List<Achievement> CreateFixtures() =>
    [
        new("DEMO_FIRST_LIGHT", "First light", "Begin a new journey beyond the mountain pass.", true, new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero), GlobalPercent: 92.4),
        new("DEMO_WAYFARER", "The wayfarer", "Discover the roads less travelled.", true, new(2026, 1, 3, 12, 0, 0, TimeSpan.Zero), GlobalPercent: 63.1),
        new("DEMO_IRON_OATH", "An iron oath", "Stand by an ally when the winter comes.", false, GlobalPercent: 32.7),
        new("DEMO_SILENT_PEAK", "The silent peak", "Reach the summit above the clouds.", false, GlobalPercent: 8.2),
        new("DEMO_LOST_ARCHIVE", "The lost archive", "Recover a forgotten collection of stories.", false, Hidden: true, GlobalPercent: 4.8),
        new("DEMO_HEARTH", "A place by the hearth", "Find a place to call your own.", false, GlobalPercent: 28.3),
        new("DEMO_STAR_FORGED", "Star forged", "Create something that will outlast you.", false, GlobalPercent: 12.6),
        new("DEMO_LAST_EMBER", "The last ember", "See a long journey through to its end.", false, Hidden: true)
    ];
    public Task<IReadOnlyList<Achievement>> ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Progress?.Invoke(new(ConnectionState.Connecting, "Starting the isolated demo provider."));
        Progress?.Invoke(new(ConnectionState.Loading, "Loading fictional demonstration achievements."));
        _connected = true;
        Progress?.Invoke(new(ConnectionState.Ready, "Demo ready. No Steam account will be changed."));
        return Task.FromResult<IReadOnlyList<Achievement>>(_items.ToArray());
    }
    public Task<IReadOnlyList<Achievement>> RefreshAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_connected) throw new ProviderException("disconnected", "Connect to the demo first.");
        return Task.FromResult<IReadOnlyList<Achievement>>(_items.ToArray());
    }
    public Task<BatchResult> ApplyAsync(ChangePlan plan, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_connected) throw new ProviderException("disconnected", "Connect before applying changes.");
        if (!confirmed) throw new ProviderException("confirmation", "Explicit confirmation is required.");
        plan = OperationPlanner.Create(_items, plan.Targets, plan.Kind);
        Progress?.Invoke(new(ConnectionState.ApplyingChanges, "Simulating the selected changes."));
        var outcomes = new List<ChangeOutcome>();
        foreach (var id in plan.Targets)
        {
            bool accepted = !RejectIds.Contains(id);
            if (accepted)
                _items = _items.Select(a => a.ApiId != id ? a : a with
                {
                    Unlocked = plan.Kind == ChangeKind.Unlock,
                    UnlockedAt = plan.Kind == ChangeKind.Unlock ? DateTimeOffset.UtcNow : null
                }).ToList();
            outcomes.Add(new(id, accepted, false, accepted, accepted ? "Simulated; no Steam persistence." : "Simulated rejection."));
        }
        Progress?.Invoke(new(ConnectionState.Verifying, "Checking simulated state."));
        var result = new BatchResult(outcomes, _items.ToArray(), true);
        Progress?.Invoke(new(result.Complete ? ConnectionState.Completed : ConnectionState.PartialFailure, "Demo operation finished."));
        return Task.FromResult(result);
    }
    public Task<AchievementIcon?> GetIconAsync(string apiId, CancellationToken cancellationToken) => Task.FromResult<AchievementIcon?>(null);
    public ValueTask DisposeAsync() { _connected = false; return ValueTask.CompletedTask; }
}
