using Dragonmark.Core;
using Dragonmark.Infrastructure;
using Xunit;

namespace Dragonmark.Tests;

public sealed class InfrastructureTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Dragonmark-tests-" + Guid.NewGuid().ToString("N"));
    private LocalData Data => new(_root);
    private string FilePath => Path.Combine(_root, "preset.json");
    [Fact]
    public void SettingsRoundTripAndMalformedFallback()
    {
        var data = Data; Assert.False(data.LoadSettings().AdvancedMode); data.SaveSettings(new(true)); Assert.True(data.LoadSettings().AdvancedMode);
        File.WriteAllText(Path.Combine(_root, "settings.json"), "{"); Assert.False(data.LoadSettings().AdvancedMode); Assert.Contains("defaults", data.ReadHistory());
    }
    [Fact]
    public void PresetsValidateAndDeduplicate()
    {
        var data = Data; data.SavePreset(FilePath, ["A", "A", "B"]); Assert.Equal(["A", "B"], data.LoadPreset(FilePath, ["A", "B"]).ApiIds);
        Assert.Throws<InvalidDataException>(() => data.LoadPreset(FilePath, ["A"]));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"Version\":2,\"AppId\":489830,\"ApiIds\":[]}")]
    [InlineData("{\"Version\":1,\"AppId\":480,\"ApiIds\":[]}")]
    [InlineData("{\"Version\":1,\"AppId\":489830,\"ApiIds\":null}")]
    [InlineData("{\"Version\":1,\"AppId\":489830,\"ApiIds\":[null]}")]
    public void MalformedPresetsAreRejected(string json) { var data = Data; File.WriteAllText(FilePath, json); Assert.Throws<InvalidDataException>(() => data.LoadPreset(FilePath, [])); }
    [Fact] public void OversizedFilesAreRejected() { var data = Data; File.WriteAllText(FilePath, new string(' ', 262145)); Assert.Throws<InvalidDataException>(() => data.LoadPreset(FilePath, [])); }
    [Fact]
    public void SnapshotsAreScopedByAccountAndMode()
    {
        var data = Data; var info = new ProviderInfo("DEMO", true, true, "demo"); data.SaveSnapshot(info, MockAchievementProvider.CreateFixtures());
        Assert.Equal(8, data.LoadSnapshot(info)!.Achievements.Length); Assert.Null(data.LoadSnapshot(info with { IsDemo = false })); Assert.Null(data.LoadSnapshot(info with { AccountKey = "different" }));
    }
    [Fact]
    public void CsvEscapesFormulasQuotesAndNewlines()
    {
        _ = Data; LocalData.ExportCsv(FilePath, [new("A", "=cmd", "A,\"B\"\nC", false)]);
        string csv = File.ReadAllText(FilePath); Assert.Contains("\"'=cmd\"", csv); Assert.Contains("\"A,\"\"B\"\"\nC\"", csv);
    }
    [Fact]
    public void LogsRedactSteamIdsAndHomeDirectory()
    {
        var data = Data; data.Log("User 76561198000000000 " + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        string log = data.ReadHistory(); Assert.DoesNotContain("76561198000000000", log); Assert.Contains("[account redacted]", log); Assert.Contains("[user]", log);
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
