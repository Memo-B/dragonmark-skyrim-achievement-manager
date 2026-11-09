using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dragonmark.Core;
using Dragonmark.Infrastructure;
using Dragonmark.Steam;
using Microsoft.Win32;

namespace Dragonmark.App;

public sealed class MainViewModel : Bindable, IAsyncDisposable
{
    private readonly LocalData _data;
    private readonly SteamAchievementProvider _steam;
    private readonly MockAchievementProvider _demo = new();
    private IAchievementProvider? _provider;
    private IReadOnlyList<Achievement> _items = [];
    private readonly Selection _selection = new();
    private CancellationTokenSource? _operation;
    private Snapshot? _previous;
    private AppSettings _settings;
    private string _search = "", _page = "Dashboard", _message = "Choose Connect Steam to inspect your account, or Explore demo to preview the app.";
    private bool _busy;
    private ConnectionState _state;
    private AchievementFilter _filter;
    private AchievementSort _sort;
    private AchievementRow? _detail;
    private ImageSource? _icon;
    private BatchResult? _lastBatch;
    private readonly Dictionary<(string Id, bool Unlocked), ImageSource> _iconCache = [];
    private CancellationTokenSource? _iconLoading;
    private Task _iconTask = Task.CompletedTask;
    private ImageSource? _themeArtwork;
    private bool _closing;
    private OperationPhase _phase;
    public ObservableCollection<AchievementRow> Rows { get; } = [];
    public string[] Pages { get; } = ["Dashboard", "All Achievements", "Locked", "Unlocked", "Presets", "Activity History", "Settings", "Diagnostics", "About"];
    public Array Filters { get; } = Enum.GetValues<AchievementFilter>();
    public Array Sorts { get; } = Enum.GetValues<AchievementSort>();
    public MainViewModel(LocalData data)
    {
        _data = data; _settings = data.LoadSettings();
        Motion.Reduced = _settings.ReduceMotion;
        SystemParameters.StaticPropertyChanged += OnSystemPreferences;
        if (_settings.ThemeArtwork is { } artwork) TryFile(() => LoadTheme(artwork));
        _steam = new(new SteamApi(Log), Log);
        _steam.Progress += OnProgress; _demo.Progress += OnProgress;
        ConnectCommand = new ActionCommand(() => _ = ConnectSteamAsync(), () => !Busy);
        DemoCommand = new ActionCommand(() => _ = ConnectDemoAsync(), () => !Busy);
        RefreshCommand = new ActionCommand(() => _ = RefreshAsync(), () => !Busy && _items.Count > 0);
        SelectCommand = new ActionCommand(() => { foreach (var row in Rows.Where(r => !r.Item.Unlocked)) row.Selected = true; SelectionChanged(); }, () => !Busy && Rows.Count > 0);
        ClearSelectionCommand = new ActionCommand(() => { _selection.Clear(); Rebuild(); }, () => !Busy && _selection.Ids.Count > 0);
        UnlockCommand = new ActionCommand(() => _ = ApplyAsync(ChangeKind.Unlock), () => !Busy && _selection.Ids.Count > 0);
        UnlockAllCommand = new ActionCommand(() => _ = UnlockAllAsync(), () => !Busy && _provider is not null && Locked > 0);
        ResultsCommand = new ActionCommand(ShowResults, () => !Busy && _lastBatch is not null);
        ClearAchievementsCommand = new ActionCommand(() => _ = ApplyAsync(ChangeKind.Clear), () => !Busy && AdvancedMode && _provider?.Info.CanClear == true && _selection.Ids.Count > 0);
        SavePresetCommand = new ActionCommand(SavePreset, () => !Busy && _selection.Ids.Count > 0);
        LoadPresetCommand = new ActionCommand(LoadPreset, () => !Busy && _items.Count > 0);
        ExportCommand = new ActionCommand(Export, () => !Busy && _items.Count > 0);
        CancelCommand = new ActionCommand(() => _operation?.Cancel(), () => Busy && State is not (ConnectionState.ApplyingChanges or ConnectionState.Verifying));
        ChooseArtworkCommand = new ActionCommand(ChooseArtwork, () => !Busy);
        ResetArtworkCommand = new ActionCommand(() => { ThemeArtwork = null; _settings = _settings with { ThemeArtwork = null }; TryFile(() => _data.SaveSettings(_settings)); Notify(nameof(ThemeDescription)); }, () => !Busy);
        PreviewDetailCommand = new ActionCommand(() => { if (Detail is not null) _ = PreviewOneAsync(Detail.Item); }, () => !Busy && Detail?.CanUnlock == true);
    }
    public ICommand ConnectCommand { get; }
    public ICommand DemoCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand UnlockCommand { get; }
    public ICommand UnlockAllCommand { get; }
    public ICommand ResultsCommand { get; }
    public ICommand ClearAchievementsCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand LoadPresetCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ChooseArtworkCommand { get; }
    public ICommand ResetArtworkCommand { get; }
    public ICommand PreviewDetailCommand { get; }
    public bool ReduceMotion { get => _settings.ReduceMotion; set { _settings = _settings with { ReduceMotion = value }; Motion.Reduced = value; TryFile(() => _data.SaveSettings(_settings)); Notify(); Notify(nameof(AnimateProgress)); } }
    public bool AnimateProgress => Busy && Motion.Allowed;
    public ImageSource? ThemeArtwork { get => _themeArtwork; private set { _themeArtwork = value; Notify(); } }
    public string ThemeDescription => ThemeArtwork is null ? "Original mountain artwork · No game assets required" : "Your local artwork · Stored as a local path, never uploaded";
    private string DataLocation => _data.Root.Contains("Dragonmark-screenshots", StringComparison.Ordinal) ? "Isolated temporary demo profile" : "%LOCALAPPDATA%\\Dragonmark";
    private void OnSystemPreferences(object? sender, PropertyChangedEventArgs args) => Application.Current.Dispatcher.Invoke(() => Notify(nameof(AnimateProgress)));
    public bool IsDemo => _provider?.Info.IsDemo == true;
    public string PhaseTitle => State == ConnectionState.Failed ? "FAILED · Inspect status and diagnostics" : _phase switch { OperationPhase.Pending => "PENDING · Preparing confirmed targets", OperationPhase.AcceptedLocally => "ACCEPTED LOCALLY · Not yet persisted", OperationPhase.Submitted => "SUBMITTED · Awaiting Steam acknowledgement", OperationPhase.Verifying => "VERIFYING · Checking refreshed states", OperationPhase.Completed => "RESULTS · Review verified / failed / unverified", _ => Busy ? "LOADING · Please wait" : Total == 0 ? "DISCONNECTED · Changes unavailable" : "READY · Changes require confirmation" };
    public string Search { get => _search; set { _search = value; Notify(); Rebuild(); } }
    public AchievementFilter Filter { get => _filter; set { _filter = value; Notify(); Rebuild(); } }
    public AchievementSort Sort { get => _sort; set { _sort = value; Notify(); Rebuild(); } }
    public string Page
    {
        get => _page;
        set
        {
            _page = value; Notify(); Notify(nameof(IsDashboard)); Notify(nameof(IsLockedPage)); Notify(nameof(IsCollection)); Notify(nameof(IsInformation)); Notify(nameof(IsSettings)); Notify(nameof(IsAbout)); Notify(nameof(Information)); Notify(nameof(PageSubtitle));
            Filter = value switch { "Locked" => AchievementFilter.Locked, "Unlocked" => AchievementFilter.Unlocked, _ => AchievementFilter.All };
        }
    }
    public bool IsCollection => Page is "Dashboard" or "All Achievements" or "Locked" or "Unlocked";
    public bool IsDashboard => Page == "Dashboard";
    public bool IsLockedPage => Page == "Locked";
    public string ProductFooter => "Unofficial · Original open source\nv" +
        (typeof(MainViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "development") +
        " · Stable";
    public string BatchSummary => _lastBatch is null ? "No changes requested this session" :
        $"{(_lastBatch.IsDemo ? "DEMO · " : "LIVE · ")}{_lastBatch.VerifiedCount} successful · {_lastBatch.FailedCount} failed · {_lastBatch.UnverifiedCount} unverified";
    public bool IsInformation => !IsCollection;
    public bool IsSettings => Page == "Settings";
    public bool IsAbout => Page == "About";
    public string PageSubtitle => Page switch { "Dashboard" => "Every deed leaves a mark.", "Presets" => "A selection remembered. Every change reviewed.", "Settings" => "Shape your companion.", "Diagnostics" => "Clear evidence. No assumptions.", "Activity History" => "A record of requests and their outcomes.", "About" => "Original craft. Open source. Independent.", _ => "The Skyrim collection · Special / Anniversary Edition" };
    public bool Busy { get => _busy; private set { _busy = value; Notify(); Notify(nameof(Idle)); Notify(nameof(AnimateProgress)); Notify(nameof(PhaseTitle)); CommandManager.InvalidateRequerySuggested(); } }
    public bool Idle => !Busy;
    public ConnectionState State { get => _state; private set { _state = value; Notify(); Notify(nameof(PhaseTitle)); CommandManager.InvalidateRequerySuggested(); } }
    public string Message { get => _message; private set { _message = value; Notify(); } }
    public string Mode => IsDemo ? "DEMO MODE · Fictional achievements · No Steam account changes" : _provider?.Info.Mode ?? "DISCONNECTED · Connect Steam or explore Demo Mode";
    public int Total => _items.Count;
    public int Unlocked => _items.Count(a => a.Unlocked);
    public int Locked => Total - Unlocked;
    public double Completion => Total == 0 ? 0 : Unlocked * 100d / Total;
    public string SelectionSummary => $"{_selection.Ids.Count} selected · {Rows.Count} shown";
    public string Delta => _previous is null ? "No previous snapshot" : $"{Unlocked - _previous.Achievements.Count(a => a.Unlocked):+0;-0;0} unlocked since previous snapshot";
    public string RaritySummary => $"{_items.Count(a => a.GlobalPercent is < 10)} rare (<10%) · {_items.Count(a => a.GlobalPercent is >= 10 and < 50)} uncommon · {_items.Count(a => a.GlobalPercent is >= 50)} common · {_items.Count(a => a.GlobalPercent is null)} unknown";
    public string Recent => string.Join(" · ", _items.Where(a => a.UnlockedAt is not null).OrderByDescending(a => a.UnlockedAt).Take(3).Select(a => a.Name)) is { Length: > 0 } recent ? "Recent: " + recent : "No dated unlocks available";
    public bool Empty => Rows.Count == 0;
    public string EmptyText => Total == 0 ? "Your collection starts here.\nConnect Steam or explore the clearly labelled demo." : "No achievements match these filters.\nTry another search or choose All.";
    public bool AdvancedMode { get => _settings.AdvancedMode; set { _settings = _settings with { AdvancedMode = value }; TryFile(() => _data.SaveSettings(_settings)); Notify(); Notify(nameof(ClearExplanation)); CommandManager.InvalidateRequerySuggested(); } }
    public string ClearExplanation => _provider?.Info.CanClear == true ? "Individual clearing is simulated in demo mode. Advanced Mode is required." : "Live clearing is disabled until compatibility has been independently verified.";
    public AchievementRow? Detail { get => _detail; set { _detail = value; Icon = value?.Icon; Notify(); Notify(nameof(HasDetail)); CommandManager.InvalidateRequerySuggested(); } }
    public bool HasDetail => Detail is not null;
    public ImageSource? Icon { get => _icon; private set { _icon = value; Notify(); } }
    public string Information => Page switch
    {
        "Presets" => "SELECTION PRESETS\n\nUse Save preset to export your selected API identifiers. Load preset validates the game, version, size and every identifier against the connected collection. Loading only changes your selection.\n\nUse Preview unlock to review the exact targets and confirm an operation. Presets never execute automatically.\n\nSNAPSHOTS & REPORTS\n\nA local snapshot is saved after successful reads. The dashboard compares it with the preceding snapshot for the same account and mode. Export report saves JSON or CSV. Snapshots are informational and cannot roll back Steam changes.",
        "Activity History" => _data.ReadHistory(),
        "About" => "DRAGONMARK · SKYRIM ACHIEVEMENT MANAGER\n\n" + ProductFooter + "\n\nAn independent companion for Skyrim Special Edition / Anniversary Edition on Steam. Original geometric artwork, cold mountain silhouettes and restrained metalwork.\n\nOriginal code and artwork: MIT license. Cinzel by the Cinzel Project Authors: SIL Open Font License 1.1. Steamworks.NET: MIT. Steam achievement artwork is retrieved at runtime and is not bundled.\n\nNot affiliated with Valve, Steam, Bethesda or Microsoft. No official Skyrim logos, fonts, textures or music are included.\n\nSteam reading and icons were verified locally. Individual unlock and Unlock All were reported working by the owner. Broader compatibility testing is ongoing; live clearing is disabled.\n\nRepository: github.com/Memo-B/dragonmark-skyrim-achievement-manager",
        "Settings" => "LANGUAGE & PRIVACY\n\nApplication language: English. Steam controls achievement metadata language; Windows controls file-dialog language.\n\nNo telemetry, credential collection, game extraction or save modification. Local artwork is optional and stays on your device. Choose only images you have permission to use.\n\nAdvanced Mode enables individual clearing only in the fictional demo. Live clearing remains disabled.\n\nMotion automatically respects Windows animation preferences. The Reduce motion setting additionally disables application fades.\n\nLocal settings, snapshots and logs: " + DataLocation,
        _ => BuildDiagnostics()
    };
    private string BuildDiagnostics()
    {
        using var process = Process.GetCurrentProcess();
        var steamProcesses = Process.GetProcessesByName("steam");
        bool running = steamProcesses.Length > 0;
        foreach (var p in steamProcesses) p.Dispose();
        return $"CONNECTION DIAGNOSTICS\n\nMode: {Mode}\nState: {State}\nExpected App ID: {Game.AppId}\n64-bit process: {Environment.Is64BitProcess}\nSteam process running: {running}\nNative x64 library present: {File.Exists(Path.Combine(AppContext.BaseDirectory, "steam_api64.dll"))}\nApp ID file present: {File.Exists(Path.Combine(AppContext.BaseDirectory, "steam_appid.txt"))}\n\nTROUBLESHOOTING\n\n1. Start Steam, sign in, and confirm your account owns Skyrim Special Edition.\n2. Run Dragonmark as the same Windows user and elevation level as Steam.\n3. If the native library is missing, follow docs/STEAM_INTEGRATION.md. Only use the matching official Steamworks.NET release.\n4. Connect Steam to check the actual App ID, license and callback response.\n5. A timeout or failed write is not a rollback. Steam can persist staged changes later, including on exit. Inspect Steam before retrying.\n\nStandalone Steamworks compatibility depends on the active Steam client and account. No SKSE bridge is included; runtime and SKSE checks are not applicable. Game installation is checked after initialization and logged. No save files are modified.\n\nLogs: {DataLocation}\n\nLast status: {Message}";
    }
    private void OnProgress(ProviderProgress progress) => Application.Current.Dispatcher.Invoke(() => { State = progress.State; Message = progress.Message; _phase = progress.Phase; Notify(nameof(PhaseTitle)); Notify(nameof(Information)); });
    private void Log(string message)
    {
        try { _data.Log(message); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Application.Current.Dispatcher.BeginInvoke(() => Message = "Diagnostic log could not be written: " + e.Message); }
    }
    private async Task Work(Func<CancellationToken, Task> action)
    {
        if (Busy) return;
        Busy = true; _operation = new();
        try { await StopIconsAsync(); await action(_operation.Token); }
        catch (OperationCanceledException) { Message = "Operation cancelled. No additional changes were requested."; }
        catch (Exception error) { Message = error.Message; State = ConnectionState.Failed; Log(error.GetType().Name + ": " + error.Message); }
        finally { _operation.Dispose(); _operation = null; Busy = false; Notify(nameof(Information)); StartIcons(); }
    }
    public Task ConnectSteamAsync() => ConnectAsync(_steam);
    public Task ConnectDemoAsync() => ConnectAsync(_demo);
    private Task ConnectAsync(IAchievementProvider provider) => Work(async token =>
    {
        _provider = provider; _iconCache.Clear(); _selection.Clear(); _items = []; _previous = null; _lastBatch = null; Notify(nameof(BatchSummary)); Rebuild(); Notify(nameof(Mode)); Notify(nameof(IsDemo)); Notify(nameof(ClearExplanation));
        var items = await provider.ConnectAsync(token);
        UpdateItems(items, true);
    });
    public Task RefreshAsync() => Work(async token => { if (_provider is not null) UpdateItems(await _provider.RefreshAsync(token), true); });
    private void UpdateItems(IReadOnlyList<Achievement> items, bool capturePrevious)
    {
        _items = items;
        if (_provider is not null)
        {
            TryFile(() => { if (capturePrevious) _previous = _data.LoadSnapshot(_provider.Info); _data.SaveSnapshot(_provider.Info, items); if (!_provider.Info.IsDemo) _data.CacheMetadata(items); });
        }
        _selection.Retain(_items); Rebuild();
    }
    private void Rebuild()
    {
        string? detailId = _detail?.ApiId;
        Rows.Clear();
        foreach (var item in AchievementQuery.Apply(_items, Search, Filter, Sort))
        {
            var row = new AchievementRow(item, _selection, SelectionChanged, IsDemo, achievement => _ = PreviewOneAsync(achievement));
            if (_iconCache.TryGetValue((item.ApiId, item.Unlocked), out var image)) row.Icon = image;
            Rows.Add(row);
        }
        _detail = Rows.FirstOrDefault(r => r.ApiId == detailId); Icon = null;
        foreach (string name in new[] { nameof(Total), nameof(Unlocked), nameof(Locked), nameof(Completion), nameof(Delta), nameof(RaritySummary), nameof(Recent), nameof(Empty), nameof(EmptyText), nameof(Detail), nameof(HasDetail) }) Notify(name);
        SelectionChanged();
    }
    private void SelectionChanged() { Notify(nameof(SelectionSummary)); CommandManager.InvalidateRequerySuggested(); }
    private async Task ApplyAsync(ChangeKind kind)
    {
        if (_provider is null || Busy) return;
        var plan = OperationPlanner.Create(_items, _selection.Ids, kind);
        await ConfirmAndApplyAsync(plan, false);
    }
    private Task PreviewOneAsync(Achievement item) => Busy ? Task.CompletedTask : ConfirmAndApplyAsync(OperationPlanner.Create(_items, [item.ApiId], ChangeKind.Unlock), false);
    public async Task UnlockAllAsync()
    {
        if (_provider is null || Busy) return;
        ChangePlan? plan = null;
        // Re-enumerate before showing the one confirmation. Never use the filtered Rows
        // or the manual Selection as the source of an Unlock All batch.
        await Work(async token =>
        {
            UpdateItems(await _provider.RefreshAsync(token), true);
            plan = OperationPlanner.UnlockAll(_items);
        });
        if (plan is not null) await ConfirmAndApplyAsync(plan, true);
    }
    private async Task ConfirmAndApplyAsync(ChangePlan plan, bool unlockAll)
    {
        if (_provider is null || Busy) return;
        if (plan.Targets.Count == 0) { Message = unlockAll ? "All available achievements are already unlocked. No changes were requested." : "No selected achievements need this change."; return; }
        Busy = true;
        bool confirmed;
        try
        {
            await StopIconsAsync();
            var dialog = CreateConfirmation(plan, unlockAll);
            confirmed = dialog.ShowDialog() == true;
        }
        finally { Busy = false; }
        if (!confirmed) { Message = "Preview dismissed. No changes requested."; StartIcons(); return; }
        await Work(async token =>
        {
            var result = await _provider.ApplyAsync(plan, true, token);
            _lastBatch = result; Notify(nameof(BatchSummary));
            foreach (var outcome in result.Outcomes) { Log($"{(_provider.Info.IsDemo ? "DEMO" : "LIVE")} {outcome.ApiId}: accepted={outcome.Accepted}; persistenceAcknowledged={outcome.PersistenceAcknowledged}; verified={outcome.Verified}. {outcome.Message}"); if (outcome.Verified) _selection.Set(outcome.ApiId, false); }
            UpdateItems(result.Achievements, false);
            Message = BatchSummary + ". " + (result.IsDemo ? "No Steam data changed." : result.UnverifiedCount > 0 ? "Persistence is uncertain. Further writes are blocked; Steam may save pending changes later. View results for details." : "Successful means Steam acknowledged persistence and refreshed state matched. View results for details.");
        });
    }
    internal ConfirmationWindow CreateConfirmation(ChangePlan plan, bool unlockAll)
    {
        string targets = string.Join("\n\n", plan.Targets.Select(id => _items.First(a => a.ApiId == id)).Select(a => $"{a.Name}\n{a.ApiId}"));
        string warning = IsDemo ? "DEMO MODE · Fictional achievements. No Steam account will be changed." : "LIVE STEAM · This changes your own account. Staged changes may persist after a timeout. No rollback is guaranteed. Review the results and verify changes in Steam.";
        return new ConfirmationWindow(unlockAll ? "Unlock the missing marks" : "Review your selection", warning + $"\n\n{plan.Targets.Count} achievements will be requested. Already in the requested state: {plan.Skipped}." + (unlockAll ? "\nIncludes all locked achievements, independent of search and filters." : ""), targets, plan.Targets.Count) { Owner = Application.Current.MainWindow };
    }
    internal ConfirmationWindow CreateDemoCaptureConfirmation()
    {
        if (!IsDemo) throw new InvalidOperationException("Screenshot previews require Demo Mode.");
        return CreateConfirmation(OperationPlanner.UnlockAll(_items), true);
    }
    private void ShowResults()
    {
        if (_lastBatch is null) return;
        new BatchResultsWindow(_lastBatch) { Owner = Application.Current.MainWindow }.ShowDialog();
    }
    private void StartIcons()
    {
        if (_closing || _provider is null || IsDemo || !_iconTask.IsCompleted) return;
        _iconLoading = new();
        _iconTask = LoadIconsAsync(_provider, _items.ToArray(), _iconLoading.Token);
    }
    private async Task StopIconsAsync()
    {
        _iconLoading?.Cancel();
        await _iconTask;
        _iconLoading?.Dispose(); _iconLoading = null;
    }
    private async Task LoadIconsAsync(IAchievementProvider provider, Achievement[] items, CancellationToken token)
    {
        try
        {
            // A second pass allows late native icon callbacks to populate missing artwork.
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var item in items)
                {
                    token.ThrowIfCancellationRequested();
                    if (_iconCache.ContainsKey((item.ApiId, item.Unlocked))) continue;
                    var icon = await provider.GetIconAsync(item.ApiId, token);
                    if (icon is null) continue;
                    var pixels = icon.Rgba.ToArray();
                    for (int i = 0; i < pixels.Length; i += 4) (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
                    var bitmap = BitmapSource.Create(icon.Width, icon.Height, 96, 96, PixelFormats.Bgra32, null, pixels, icon.Width * 4);
                    bitmap.Freeze(); _iconCache[(item.ApiId, item.Unlocked)] = bitmap;
                    foreach (var row in Rows.Where(r => r.ApiId == item.ApiId && r.Item.Unlocked == item.Unlocked)) row.Icon = bitmap;
                    if (Detail?.ApiId == item.ApiId) Icon = bitmap;
                }
                await Task.Delay(700, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { Log("Artwork unavailable: " + e.Message); }
    }
    private void LoadTheme(string path)
    {
        if (!Path.IsPathFullyQualified(path) || new Uri(path).IsUnc || !File.Exists(path)) throw new InvalidDataException("Choose an existing local image file.");
        var info = new FileInfo(path);
        if (info.Length > 16 * 1024 * 1024 || info.Length == 0) throw new InvalidDataException("Theme artwork must be a non-empty PNG or JPEG smaller than 16 MB.");
        if (Path.GetExtension(path).ToLowerInvariant() is not (".png" or ".jpg" or ".jpeg")) throw new InvalidDataException("Use a PNG or JPEG image.");
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        var frame = decoder.Frames[0];
        if (frame.PixelWidth is <= 0 or > 16384 || frame.PixelHeight is <= 0 or > 16384) throw new InvalidDataException("Theme artwork dimensions must be at most 16384 pixels per side.");
        double scale = Math.Min(1, Math.Min(1920d / frame.PixelWidth, 1080d / frame.PixelHeight));
        int decodeWidth = Math.Max(1, (int)(frame.PixelWidth * scale));
        stream.Position = 0;
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = decodeWidth; image.StreamSource = stream; image.EndInit(); image.Freeze();
        ThemeArtwork = image;
    }
    private void ChooseArtwork()
    {
        var dialog = new OpenFileDialog { Title = "Choose local artwork you have permission to use", Filter = "Local artwork (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog() != true) return;
        TryFile(() => { LoadTheme(dialog.FileName); _settings = _settings with { ThemeArtwork = dialog.FileName }; _data.SaveSettings(_settings); Notify(nameof(ThemeDescription)); });
    }
    private void TryFile(Action action)
    {
        try { action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException or NotSupportedException or ArgumentException or FormatException or System.Runtime.InteropServices.COMException)
        { Message = "Local file error: " + e.Message; Log(Message); }
    }
    private void SavePreset()
    {
        var dialog = new SaveFileDialog { Title = "Save selection preset", Filter = "Dragonmark selection (*.json)|*.json", FileName = "dragonmark-selection.json" };
        if (dialog.ShowDialog() == true) TryFile(() => { _data.SavePreset(dialog.FileName, _selection.Ids); Message = "Selection preset saved."; });
    }
    private void LoadPreset()
    {
        var dialog = new OpenFileDialog { Title = "Load selection preset", Filter = "Dragonmark selection (*.json)|*.json" };
        if (dialog.ShowDialog() == true) TryFile(() => { var preset = _data.LoadPreset(dialog.FileName, _items.Select(a => a.ApiId)); _selection.Clear(); foreach (string id in preset.ApiIds) _selection.Set(id, true); Rebuild(); Message = "Preset loaded as a selection. Preview and confirm before applying changes."; });
    }
    private void Export()
    {
        if (_provider is null) return;
        var dialog = new SaveFileDialog { Title = "Export achievement report", Filter = "JSON report (*.json)|*.json|CSV report (*.csv)|*.csv", FileName = "dragonmark-report" };
        if (dialog.ShowDialog() == true) TryFile(() => { if (dialog.FilterIndex == 2) LocalData.ExportCsv(dialog.FileName, _items); else LocalData.ExportJson(dialog.FileName, _provider.Info, _items); Message = "Achievement report exported."; });
    }
    public async ValueTask DisposeAsync() { _closing = true; SystemParameters.StaticPropertyChanged -= OnSystemPreferences; await StopIconsAsync(); await _steam.DisposeAsync(); await _demo.DisposeAsync(); }
}
