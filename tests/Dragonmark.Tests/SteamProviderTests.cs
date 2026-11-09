using Dragonmark.Core;
using Dragonmark.Steam;
using Xunit;

namespace Dragonmark.Tests;

public sealed class SteamProviderTests
{
    private static SteamAchievementProvider Provider(FakeSteamApi api, TimeSpan? interval = null) => new(api, _ => { }, TimeSpan.FromMilliseconds(100), interval ?? TimeSpan.Zero);
    private static ChangePlan Plan(params string[] ids) => new(ChangeKind.Unlock, ids.Length == 0 ? ["DEMO_IRON_OATH"] : ids, 0);
    [Fact]
    public async Task EnumerationIsDynamicAndStatesAreEmitted()
    {
        var api = new FakeSteamApi(); api.Items.Add(new("EXTRA", "Extra", "An additional fixture", false));
        await using var provider = Provider(api); var states = new List<ConnectionState>(); provider.Progress += p => states.Add(p.State);
        Assert.Equal(9, (await provider.ConnectAsync(default)).Count);
        Assert.Equal([ConnectionState.Connecting, ConnectionState.Loading, ConnectionState.Ready], states);
    }
    [Fact]
    public async Task InitFailureIsActionable()
    {
        await using var provider = Provider(new() { InitResult = false });
        Assert.Equal("init", (await Assert.ThrowsAsync<ProviderException>(() => provider.ConnectAsync(default))).Code);
    }
    [Theory]
    [InlineData("dll")]
    [InlineData("architecture")]
    [InlineData("entry")]
    public async Task NativeFailuresAreClassified(string error)
    {
        var api = new FakeSteamApi { InitError = error switch { "dll" => new DllNotFoundException(), "architecture" => new BadImageFormatException(), _ => new EntryPointNotFoundException() } };
        await using var provider = Provider(api);
        Assert.Equal("native-library", (await Assert.ThrowsAsync<ProviderException>(() => provider.ConnectAsync(default))).Code);
    }
    [Theory]
    [InlineData("app-id")]
    [InlineData("offline")]
    [InlineData("license")]
    public async Task InvalidContextBlocksAllWrites(string condition)
    {
        var api = new FakeSteamApi();
        if (condition == "app-id") api.AppId = 480; if (condition == "offline") api.LoggedOn = false; if (condition == "license") api.OwnsGame = false;
        await using var provider = Provider(api);
        Assert.Equal(condition, (await Assert.ThrowsAsync<ProviderException>(() => provider.ConnectAsync(default))).Code);
        Assert.Equal(0, api.SetCalls);
    }
    [Fact]
    public async Task EmptyMetadataIsNotReady()
    {
        var api = new FakeSteamApi { Items = [] }; await using var provider = Provider(api);
        Assert.Equal("empty", (await Assert.ThrowsAsync<ProviderException>(() => provider.ConnectAsync(default))).Code);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrForeignStatsCallbackTimesOut(bool foreign)
    {
        var api = new FakeSteamApi { SendStats = foreign, ForeignCallbackOnly = foreign }; await using var provider = Provider(api);
        Assert.Equal("timeout", (await Assert.ThrowsAsync<ProviderException>(() => provider.ConnectAsync(default))).Code);
    }
    [Fact]
    public async Task FailedStatsCallbackIsRejected()
    {
        await using var provider = Provider(new() { StatsSuccess = false });
        Assert.Equal("stats", (await Assert.ThrowsAsync<ProviderException>(() => provider.ConnectAsync(default))).Code);
    }
    [Fact]
    public async Task SuccessfulBatchNeedsStoreAndRefresh()
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api); await provider.ConnectAsync(default);
        var states = new List<ConnectionState>(); provider.Progress += p => states.Add(p.State);
        var result = await provider.ApplyAsync(Plan(), true, default);
        Assert.True(result.Complete); Assert.True(result.Outcomes.Single().PersistenceAcknowledged); Assert.Equal(1, api.StoreCalls);
        Assert.Equal([ConnectionState.ApplyingChanges, ConnectionState.Verifying, ConnectionState.Completed], states.Distinct());
    }
    [Theory]
    [InlineData("reject-store")]
    [InlineData("timeout")]
    [InlineData("callback-failure")]
    [InlineData("mismatch")]
    public async Task UncertainPersistenceNeverReportsSuccessAndBlocksRetry(string failure)
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api); await provider.ConnectAsync(default);
        api.StoreAccepted = failure != "reject-store"; api.SendStore = failure != "timeout"; api.StoreSuccess = failure != "callback-failure"; api.MatchRefresh = failure != "mismatch";
        var result = await provider.ApplyAsync(Plan(), true, default);
        Assert.False(result.Complete); Assert.True(result.Outcomes.Single().Accepted); Assert.False(result.Outcomes.Single().Verified);
        Assert.Equal("uncertain", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(Plan(), true, default))).Code);
        Assert.Equal(1, api.StoreCalls);
    }
    [Fact]
    public async Task PartialAcceptanceIsReportedPerTarget()
    {
        var api = new FakeSteamApi(); api.Rejected.Add("DEMO_IRON_OATH"); await using var provider = Provider(api); await provider.ConnectAsync(default);
        var result = await provider.ApplyAsync(Plan("DEMO_IRON_OATH", "DEMO_SILENT_PEAK"), true, default);
        Assert.False(result.Complete); Assert.Equal(1, result.VerifiedCount); Assert.Equal(1, api.StoreCalls);
    }
    [Fact]
    public async Task AlreadyUnlockedAndDuplicatesAreSkipped()
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api); await provider.ConnectAsync(default);
        await provider.ApplyAsync(Plan("DEMO_FIRST_LIGHT", "DEMO_IRON_OATH", "DEMO_IRON_OATH"), true, default);
        Assert.Equal(1, api.SetCalls);
    }
    [Fact]
    public async Task RejectedLocalChangesDoNotCallStore()
    {
        var api = new FakeSteamApi(); api.Rejected.Add("DEMO_IRON_OATH"); await using var provider = Provider(api); await provider.ConnectAsync(default);
        var result = await provider.ApplyAsync(Plan(), true, default); Assert.Equal(0, api.StoreCalls); Assert.False(result.Complete);
    }
    [Fact]
    public async Task ConfirmationAndClearCapabilityAreEnforced()
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api); await provider.ConnectAsync(default);
        Assert.Equal("confirmation", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(Plan(), false, default))).Code);
        Assert.Equal("unsupported", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(new(ChangeKind.Clear, ["DEMO_FIRST_LIGHT"], 0), true, default))).Code);
        Assert.Equal(0, api.SetCalls); Assert.Equal(0, api.ClearCalls);
    }
    [Fact]
    public async Task AccountSwitchPreventsWrite()
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api); await provider.ConnectAsync(default); api.UserId++;
        Assert.Equal("account", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(Plan(), true, default))).Code); Assert.Equal(0, api.SetCalls);
    }
    [Fact]
    public async Task RateLimitIsEnforcedBeforeStaging()
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api, TimeSpan.FromMinutes(2)); await provider.ConnectAsync(default);
        await provider.ApplyAsync(Plan(), true, default);
        Assert.Equal("rate-limit", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(Plan("DEMO_SILENT_PEAK"), true, default))).Code);
        Assert.Equal(1, api.SetCalls);
    }
    [Fact]
    public async Task CancellationBeforeStagingDoesNotWrite()
    {
        var api = new FakeSteamApi(); await using var provider = Provider(api); await provider.ConnectAsync(default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ApplyAsync(Plan(), true, new(true))); Assert.Equal(0, api.SetCalls);
    }
    [Fact]
    public async Task CancellationAfterStagingStillWaitsForEvidence()
    {
        var api = new FakeSteamApi(); using var cancellation = new CancellationTokenSource(); api.OnSet = cancellation.Cancel;
        await using var provider = Provider(api); await provider.ConnectAsync(default);
        Assert.True((await provider.ApplyAsync(Plan(), true, cancellation.Token)).Complete);
    }
    [Fact]
    public async Task ConcurrentRequestsAreRejectedAndDisposeWaits()
    {
        var api = new FakeSteamApi { SendStats = false }; var provider = Provider(api);
        var connect = provider.ConnectAsync(default);
        Assert.Equal("busy", (await Assert.ThrowsAsync<ProviderException>(() => provider.RefreshAsync(default))).Code);
        var dispose = provider.DisposeAsync().AsTask(); await Assert.ThrowsAsync<ProviderException>(() => connect); await dispose; Assert.True(api.Disposed);
    }
}
