using Dragonmark.Core;
using Dragonmark.Steam;
using Xunit;

namespace Dragonmark.Tests;

public sealed class UnlockAllTests
{
    [Fact]
    public void AllPlanUsesFullEnumerationAndOnlyLockedTargets()
    {
        var items = MockAchievementProvider.CreateFixtures();
        items.Add(new("NEW_DYNAMIC", "Newly published", "Not hardcoded", false));
        var plan = OperationPlanner.UnlockAll(items);
        Assert.Equal(7, plan.Targets.Count); Assert.Equal(2, plan.Skipped);
        Assert.Contains("NEW_DYNAMIC", plan.Targets);
        Assert.DoesNotContain("DEMO_FIRST_LIGHT", plan.Targets);
        // Manual selection and filtered rows are intentionally absent from this API.
        Assert.Single(AchievementQuery.Apply(items, "summit"));
        Assert.Equal(7, plan.Targets.Count);
    }
    [Fact]
    public void AlreadyCompleteCollectionHasNoTargets()
    {
        Assert.Empty(OperationPlanner.UnlockAll([]).Targets);
        Assert.Empty(OperationPlanner.UnlockAll([new("A", "A", "", true)]).Targets);
    }
    [Fact]
    public async Task AllBatchUsesRealProviderPathAndOneStore()
    {
        var api = new FakeSteamApi(); await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var items = await provider.ConnectAsync(default);
        Assert.Equal(0, api.SetCalls); Assert.Equal(0, api.StoreCalls); // Never auto-executes on connection.
        var result = await provider.ApplyAsync(OperationPlanner.UnlockAll(items), true, default);
        Assert.Equal(6, api.SetCalls); Assert.Equal(1, api.StoreCalls);
        Assert.Equal(6, result.VerifiedCount); Assert.Equal(0, result.FailedCount); Assert.Equal(0, result.UnverifiedCount);
        Assert.All(result.Achievements, a => Assert.True(a.Unlocked));
    }
    [Fact]
    public async Task AllBatchRequiresExplicitConfirmation()
    {
        var api = new FakeSteamApi(); await using var provider = new SteamAchievementProvider(api, _ => { });
        var items = await provider.ConnectAsync(default);
        await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(OperationPlanner.UnlockAll(items), false, default));
        Assert.Equal(0, api.SetCalls); Assert.Equal(0, api.StoreCalls);
    }
    [Fact]
    public async Task AllBatchSeparatesRejectedAndUnverifiedAfterTimeout()
    {
        var api = new FakeSteamApi { SendStore = false }; api.Rejected.Add("DEMO_IRON_OATH");
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var items = await provider.ConnectAsync(default);
        var result = await provider.ApplyAsync(OperationPlanner.UnlockAll(items), true, default);
        Assert.Equal(0, result.VerifiedCount); Assert.Equal(1, result.FailedCount); Assert.Equal(5, result.UnverifiedCount);
        Assert.Equal(6, result.Outcomes.Count); Assert.Equal(1, api.StoreCalls);
    }
    [Fact]
    public async Task AllBatchSeparatesSuccessfulAndRejectedTargets()
    {
        var api = new FakeSteamApi(); api.Rejected.Add("DEMO_IRON_OATH");
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var items = await provider.ConnectAsync(default);
        var result = await provider.ApplyAsync(OperationPlanner.UnlockAll(items), true, default);
        Assert.Equal(5, result.VerifiedCount); Assert.Equal(1, result.FailedCount); Assert.Equal(0, result.UnverifiedCount);
    }
    [Fact]
    public async Task RepeatedAllExecutionCannotOverlap()
    {
        var api = new FakeSteamApi { SendStore = false };
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(150), TimeSpan.Zero);
        var plan = OperationPlanner.UnlockAll(await provider.ConnectAsync(default));
        var first = provider.ApplyAsync(plan, true, default);
        var exception = await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(plan, true, default));
        Assert.Equal("busy", exception.Code); await first; Assert.Equal(1, api.StoreCalls);
    }
    [Fact]
    public async Task AllThreeOutcomeCategoriesCanCoexist()
    {
        var api = new FakeSteamApi(); api.Rejected.Add("DEMO_IRON_OATH"); api.Unmatched.Add("DEMO_SILENT_PEAK");
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var result = await provider.ApplyAsync(OperationPlanner.UnlockAll(await provider.ConnectAsync(default)), true, default);
        Assert.Equal(4, result.VerifiedCount); Assert.Equal(1, result.FailedCount); Assert.Equal(1, result.UnverifiedCount);
        Assert.Equal("uncertain", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(OperationPlanner.UnlockAll(result.Achievements), true, default))).Code);
    }
    [Fact]
    public async Task StoreCallbackWithoutRefreshedStateIsUnverified()
    {
        var api = new FakeSteamApi { FailRefreshAfterStore = true };
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var result = await provider.ApplyAsync(OperationPlanner.UnlockAll(await provider.ConnectAsync(default)), true, default);
        Assert.Equal(0, result.VerifiedCount); Assert.Equal(6, result.UnverifiedCount);
        Assert.All(result.Outcomes, o => Assert.True(o.PersistenceAcknowledged));
    }
}
