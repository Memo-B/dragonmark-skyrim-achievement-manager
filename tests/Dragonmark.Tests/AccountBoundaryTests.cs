using Dragonmark.Core;
using Dragonmark.Steam;
using Xunit;

namespace Dragonmark.Tests;

public sealed class AccountBoundaryTests
{
    [Fact]
    public async Task AccountChangeDuringVerificationReadCannotReportSuccess()
    {
        var api = new FakeSteamApi();
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var plan = OperationPlanner.UnlockAll(await provider.ConnectAsync(default));
        int reads = 0;
        api.OnRead = () => { if (++reads == 2) api.UserId++; };
        var result = await provider.ApplyAsync(plan, true, default);
        Assert.Equal(1, api.StoreCalls);
        Assert.Equal(0, result.VerifiedCount);
        Assert.Equal(plan.Targets.Count, result.UnverifiedCount);
        Assert.Equal("uncertain", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(plan, true, default))).Code);
    }

    [Theory]
    [InlineData("reply")]
    [InlineData("read")]
    [InlineData("drain")]
    public async Task AccountChangeAfterInitialValidationCannotStageOrStore(string boundary)
    {
        var api = new FakeSteamApi();
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var plan = OperationPlanner.UnlockAll(await provider.ConnectAsync(default));
        if (boundary == "reply") api.AfterStatsReply = () => api.UserId++;
        if (boundary == "read") api.OnRead = () => api.UserId++;
        if (boundary == "drain") api.OnRead = () => api.AfterPump = () => api.UserId++;

        var error = await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(plan, true, default));
        Assert.Equal("account", error.Code);
        Assert.Equal(0, api.SetCalls);
        Assert.Equal(0, api.StoreCalls);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("offline")]
    [InlineData("license")]
    [InlineData("app")]
    public async Task ContextLossBetweenTargetsStopsFurtherWritesAndStore(string loss)
    {
        var api = new FakeSteamApi();
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var plan = OperationPlanner.UnlockAll(await provider.ConnectAsync(default));
        ulong originalUser = api.UserId;
        api.OnSet = () =>
        {
            if (loss == "account") api.UserId++;
            if (loss == "offline") api.LoggedOn = false;
            if (loss == "license") api.OwnsGame = false;
            if (loss == "app") api.AppId = 480;
        };

        var result = await provider.ApplyAsync(plan, true, default);
        Assert.Equal(1, api.SetCalls);
        Assert.Equal(0, api.StoreCalls);
        Assert.All(api.WriteUsers, id => Assert.Equal(originalUser, id));
        Assert.Equal(0, result.VerifiedCount);
        Assert.Equal(1, result.UnverifiedCount);
        Assert.Equal("uncertain", (await Assert.ThrowsAsync<ProviderException>(() => provider.ApplyAsync(plan, true, default))).Code);
    }

    [Fact]
    public async Task AccountChangeAfterLastSetterCannotStoreOnAnotherAccount()
    {
        var api = new FakeSteamApi();
        await using var provider = new SteamAchievementProvider(api, _ => { }, TimeSpan.FromMilliseconds(100), TimeSpan.Zero);
        var items = await provider.ConnectAsync(default);
        var plan = OperationPlanner.Create(items, ["DEMO_IRON_OATH"], ChangeKind.Unlock);
        api.OnSet = () => api.UserId++;
        var result = await provider.ApplyAsync(plan, true, default);
        Assert.Equal(1, api.SetCalls);
        Assert.Empty(api.StoreUsers);
        Assert.Equal(1, result.UnverifiedCount);
        Assert.Equal(0, result.VerifiedCount);
    }
}
