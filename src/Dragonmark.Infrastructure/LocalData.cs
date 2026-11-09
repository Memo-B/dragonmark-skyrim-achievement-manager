using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dragonmark.Core;

namespace Dragonmark.Infrastructure;

public sealed record AppSettings(bool AdvancedMode = false, int Version = 1, bool ReduceMotion = false, string? ThemeArtwork = null);
public sealed record SelectionPreset(int Version, uint AppId, string[] ApiIds);
public sealed record Snapshot(int Version, uint AppId, string AccountKey, bool IsDemo, DateTimeOffset CapturedAt, Achievement[] Achievements);

public sealed class LocalData
{
    public string Root { get; }
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public LocalData(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dragonmark");
        Directory.CreateDirectory(Root);
    }
    public static string AccountKey(ulong id) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString(CultureInfo.InvariantCulture))))[..24];
    public AppSettings LoadSettings()
    {
        var path = Path.Combine(Root, "settings.json");
        if (!File.Exists(path)) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(ReadLimited(path, 4096));
            if (settings is null || settings.Version != 1) throw new InvalidDataException("Unsupported settings version.");
            return settings;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException)
        { Log("Settings could not be loaded; defaults used. " + e.Message); return new(); }
    }
    public void SaveSettings(AppSettings settings) => WriteJson(Path.Combine(Root, "settings.json"), settings);
    public void SavePreset(string path, IEnumerable<string> ids) => WriteJson(path, new SelectionPreset(1, Game.AppId, ids.Distinct(StringComparer.Ordinal).ToArray()));
    public SelectionPreset LoadPreset(string path, IEnumerable<string> knownIds)
    {
        SelectionPreset? preset;
        try { preset = JsonSerializer.Deserialize<SelectionPreset>(ReadLimited(path, 262144)); }
        catch (JsonException e) { throw new InvalidDataException("The selection file is not valid JSON.", e); }
        if (preset is null || preset.Version != 1 || preset.AppId != Game.AppId || preset.ApiIds is null || preset.ApiIds.Length > 4096)
            throw new InvalidDataException("Unsupported preset version, game, or selection size.");
        var known = knownIds.ToHashSet(StringComparer.Ordinal);
        if (preset.ApiIds.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 256 || !known.Contains(id)))
            throw new InvalidDataException("The preset contains unknown or invalid achievement identifiers. Connect to the matching mode first.");
        return preset with { ApiIds = preset.ApiIds.Distinct(StringComparer.Ordinal).ToArray() };
    }
    private string SnapshotPath(ProviderInfo info) => Path.Combine(Root, $"snapshot-{SafeKey(info.AccountKey)}-{info.IsDemo}.json");
    private static string SafeKey(string key) => Regex.IsMatch(key, "^[a-zA-Z0-9]{1,64}$") ? key : throw new InvalidDataException("Invalid account scope.");
    public Snapshot? LoadSnapshot(ProviderInfo info)
    {
        var path = SnapshotPath(info);
        if (!File.Exists(path)) return null;
        var snapshot = JsonSerializer.Deserialize<Snapshot>(ReadLimited(path, 4 * 1024 * 1024));
        if (snapshot is null || snapshot.Version != 1 || snapshot.AppId != Game.AppId || snapshot.AccountKey != info.AccountKey || snapshot.IsDemo != info.IsDemo || snapshot.Achievements is null)
            throw new InvalidDataException("The previous snapshot is incompatible.");
        return snapshot;
    }
    public void SaveSnapshot(ProviderInfo info, IEnumerable<Achievement> items) => WriteJson(SnapshotPath(info), NewSnapshot(info, items));
    public static Snapshot NewSnapshot(ProviderInfo info, IEnumerable<Achievement> items) => new(1, Game.AppId, info.AccountKey, info.IsDemo, DateTimeOffset.UtcNow, items.ToArray());
    public static void ExportJson(string path, ProviderInfo info, IEnumerable<Achievement> items) => WriteJson(path, NewSnapshot(info, items));
    public static void ExportCsv(string path, IEnumerable<Achievement> items)
    {
        static string Cell(string? value)
        {
            value ??= "";
            if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var lines = items.Select(a => string.Join(",", new[] { a.ApiId, a.Name, a.Description, a.Unlocked.ToString(), a.UnlockedAt?.ToString("O"), a.Hidden.ToString(), a.GlobalPercent?.ToString(CultureInfo.InvariantCulture) }.Select(Cell)));
        File.WriteAllLines(path, new[] { "ApiId,Name,Description,Unlocked,UnlockedAt,Hidden,GlobalPercent" }.Concat(lines), new UTF8Encoding(true));
    }
    public void CacheMetadata(IEnumerable<Achievement> items) => WriteJson(Path.Combine(Root, "metadata.json"), items.Select(a => new { a.ApiId, a.Name, a.Description, a.Hidden, a.GlobalPercent }).ToArray());
    public void Log(string message)
    {
        var clean = Regex.Replace(message, @"\b\d{17}\b", "[account redacted]").Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "[user]", StringComparison.OrdinalIgnoreCase);
        var path = Path.Combine(Root, "activity.log");
        lock (_logLock)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024) File.Move(path, path + ".previous", true);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {clean.Replace('\r', ' ').Replace('\n', ' ')}{Environment.NewLine}");
        }
    }
    private readonly object _logLock = new();
    public string ReadHistory() => File.Exists(Path.Combine(Root, "activity.log")) ? File.ReadAllText(Path.Combine(Root, "activity.log")) : "No activity recorded yet.";
    private static string ReadLimited(string path, long maximum)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > maximum) throw new InvalidDataException("The file exceeds the allowed size.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    private static void WriteJson<T>(string path, T data)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(data, Json)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
