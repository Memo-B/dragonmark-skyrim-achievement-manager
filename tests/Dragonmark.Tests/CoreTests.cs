using Dragonmark.Core;
using Xunit;

namespace Dragonmark.Tests;

public sealed class CoreTests
{
    [Theory]
    [InlineData("summit", 1)]
    [InlineData("DEMO_LAST", 1)]
    [InlineData(" first LIGHT ", 1)]
    [InlineData("missing", 0)]
    public void SearchMatchesNamesDescriptionsAndIds(string query, int count) => Assert.Equal(count, AchievementQuery.Apply(MockAchievementProvider.CreateFixtures(), query).Count);
    [Theory]
    [InlineData(AchievementFilter.All, 8)]
    [InlineData(AchievementFilter.Locked, 6)]
    [InlineData(AchievementFilter.Unlocked, 2)]
    [InlineData(AchievementFilter.Hidden, 2)]
    [InlineData(AchievementFilter.Rare, 2)]
    public void FiltersOnlyUseKnownData(AchievementFilter filter, int count) => Assert.Equal(count, AchievementQuery.Apply(MockAchievementProvider.CreateFixtures(), filter: filter).Count);
    [Fact]
    public void SortingPutsUnknownRarityLastAndNewestDateFirst()
    {
        var items = MockAchievementProvider.CreateFixtures();
        Assert.Null(AchievementQuery.Apply(items, sort: AchievementSort.Rarity).Last().GlobalPercent);
        Assert.Equal("DEMO_WAYFARER", AchievementQuery.Apply(items, sort: AchievementSort.UnlockDate).First().ApiId);
        Assert.Equal("A place by the hearth", AchievementQuery.Apply(items).First().Name);
    }
    [Fact]
    public void SelectionSurvivesFilteringAndSorting()
    {
        var selection = new Selection(); selection.Set("DEMO_LAST_EMBER", true);
        _ = AchievementQuery.Apply(MockAchievementProvider.CreateFixtures(), "first", sort: AchievementSort.Rarity);
        selection.Retain(MockAchievementProvider.CreateFixtures()); Assert.True(selection.Contains("DEMO_LAST_EMBER"));
        selection.Retain([]); Assert.Empty(selection.Ids);
    }
    [Fact]
    public void PlansDeduplicateAndSkipExistingState()
    {
        var plan = OperationPlanner.Create(MockAchievementProvider.CreateFixtures(), ["DEMO_FIRST_LIGHT", "DEMO_IRON_OATH", "DEMO_IRON_OATH"], ChangeKind.Unlock);
        Assert.Equal(["DEMO_IRON_OATH"], plan.Targets); Assert.Equal(1, plan.Skipped);
    }
    [Fact] public void UnknownTargetsAreRejected() => Assert.Throws<ArgumentException>(() => OperationPlanner.Create([], ["bad"], ChangeKind.Unlock));
    [Fact]
    public async Task MockRequiresConfirmationAndNeverClaimsSteamPersistence()
    {
        await using var mock = new MockAchievementProvider();
        await mock.ConnectAsync(default);
        var plan = new ChangePlan(ChangeKind.Unlock, ["DEMO_IRON_OATH"], 0);
        await Assert.ThrowsAsync<ProviderException>(() => mock.ApplyAsync(plan, false, default));
        Assert.False((await mock.RefreshAsync(default)).First(a => a.ApiId == "DEMO_IRON_OATH").Unlocked);
        var result = await mock.ApplyAsync(plan, true, default);
        Assert.True(result.IsDemo); Assert.True(result.Complete); Assert.False(result.Outcomes.Single().PersistenceAcknowledged);
    }
    [Fact]
    public async Task DemoSupportsClearAndPartialRejection()
    {
        await using var mock = new MockAchievementProvider(); await mock.ConnectAsync(default);
        mock.RejectIds.Add("DEMO_IRON_OATH");
        var partial = await mock.ApplyAsync(new(ChangeKind.Unlock, ["DEMO_IRON_OATH", "DEMO_SILENT_PEAK"], 0), true, default);
        Assert.Equal(1, partial.VerifiedCount); Assert.False(partial.Complete);
        var clear = await mock.ApplyAsync(new(ChangeKind.Clear, ["DEMO_FIRST_LIGHT"], 0), true, default);
        Assert.False(clear.Achievements.First(a => a.ApiId == "DEMO_FIRST_LIGHT").Unlocked);
    }
    [Fact]
    public async Task CancelledMockDoesNotMutate()
    {
        await using var mock = new MockAchievementProvider(); await mock.ConnectAsync(default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mock.ApplyAsync(new(ChangeKind.Unlock, ["DEMO_IRON_OATH"], 0), true, new(true)));
        Assert.False((await mock.RefreshAsync(default)).First(a => a.ApiId == "DEMO_IRON_OATH").Unlocked);
    }
}
