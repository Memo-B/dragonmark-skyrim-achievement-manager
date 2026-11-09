using Dragonmark.Core;
using Dragonmark.Steam;
using Xunit;

namespace Dragonmark.Tests;

public sealed class ArtworkAndProgressTests
{
    [Theory]
    [InlineData("DEMO_FIRST_LIGHT", true)]
    [InlineData("DEMO_IRON_OATH", false)]
    public async Task ArtworkRequestsUseTheEnumeratedIdentifierAndState(string id, bool unlocked)
    {
        var api = new FakeSteamApi { Icon = new(1, 1, [1, 2, 3, 255]) };
        await using var provider = new SteamAchievementProvider(api, _ => { });
        await provider.ConnectAsync(default);
        Assert.Same(api.Icon, await provider.GetIconAsync(id, default));
        Assert.Equal([(id, unlocked)], api.IconRequests);
        Assert.Equal(0, api.SetCalls + api.ClearCalls + api.StoreCalls);
    }
    [Fact]
    public async Task UnknownArtworkCannotReachTheNativeBoundary()
    {
        var api = new FakeSteamApi();
        await using var provider = new SteamAchievementProvider(api, _ => { });
        await provider.ConnectAsync(default);
        Assert.Null(await provider.GetIconAsync("UNKNOWN", default));
        Assert.Empty(api.IconRequests);
    }
    [Fact]
    public async Task ArtworkStateChangesOnlyAfterARefreshedAchievementChange()
    {
        var api = new FakeSteamApi { Icon = new(1, 1, [1, 2, 3, 255]) };
        await using var provider = new SteamAchievementProvider(api, _ => { }, storeInterval: TimeSpan.Zero);
        await provider.ConnectAsync(default);
        await provider.GetIconAsync("DEMO_IRON_OATH", default);
        var result = await provider.ApplyAsync(new(ChangeKind.Unlock, ["DEMO_IRON_OATH"], 0), true, default);
        Assert.True(result.Complete);
        await provider.GetIconAsync("DEMO_IRON_OATH", default);
        Assert.Equal([("DEMO_IRON_OATH", false), ("DEMO_IRON_OATH", true)], api.IconRequests);
    }
    [Fact]
    public async Task SubmissionIsReportedOnlyAfterStoreAcceptsTheBatch()
    {
        var api = new FakeSteamApi { StoreAccepted = false };
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        await provider.ConnectAsync(default);
        var phases = new List<OperationPhase>(); provider.Progress += p => phases.Add(p.Phase);
        var result = await provider.ApplyAsync(new(ChangeKind.Unlock, ["DEMO_IRON_OATH"], 0), true, default);
        Assert.Contains(OperationPhase.AcceptedLocally, phases);
        Assert.DoesNotContain(OperationPhase.Submitted, phases);
        Assert.Equal(0, result.VerifiedCount);
        Assert.Equal(1, result.UnverifiedCount);
    }
    [Fact]
    public async Task SuccessfulProgressSeparatesAcceptanceSubmissionAndVerification()
    {
        var api = new FakeSteamApi();
        await using var provider = new SteamAchievementProvider(api, _ => { }, storeInterval: TimeSpan.Zero);
        await provider.ConnectAsync(default);
        var phases = new List<OperationPhase>(); provider.Progress += p => phases.Add(p.Phase);
        await provider.ApplyAsync(new(ChangeKind.Unlock, ["DEMO_IRON_OATH"], 0), true, default);
        Assert.Equal([OperationPhase.Pending, OperationPhase.AcceptedLocally, OperationPhase.Submitted, OperationPhase.Verifying, OperationPhase.Completed], phases);
    }
}
