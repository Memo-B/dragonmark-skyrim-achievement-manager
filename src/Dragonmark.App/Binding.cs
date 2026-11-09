using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Dragonmark.Core;

namespace Dragonmark.App;

public abstract class Bindable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Notify([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
}
public sealed class ActionCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
}
public sealed class AchievementRow(Achievement achievement, Selection selection, Action changed, bool demo, Action<Achievement> preview) : Bindable
{
    private ImageSource? _icon = demo ? DemoArtwork.Create(achievement.ApiId) : null;
    public Achievement Item { get; } = achievement;
    public string Name => Item.Name;
    public string Description => Item.Description;
    public string ApiId => Item.ApiId;
    public string Status => Item.Unlocked ? "UNLOCKED" : "LOCKED";
    public string Rarity => Item.GlobalPercent is { } p ? p.ToString("0.0", CultureInfo.InvariantCulture) + "% of players" : "Rarity unavailable";
    public string Date => Item.UnlockedAt?.ToLocalTime().ToString("MMM d, yyyy · HH:mm", CultureInfo.GetCultureInfo("en-US")) ?? (Item.Unlocked ? "Unlock date unavailable" : "Not unlocked");
    public bool Rare => Item.GlobalPercent is < 10;
    public bool CanUnlock => !Item.Unlocked;
    public string Accent => Item.Unlocked ? "#D5BD86" : "#657D8D";
    public string ArtworkLabel => demo ? "Original demo artwork" : Icon is not null ? "Steam artwork · " + Status.ToLowerInvariant() : "Steam artwork unavailable";
    public ImageSource? Icon { get => _icon; set { _icon = value; Notify(); Notify(nameof(ArtworkLabel)); } }
    public ICommand PreviewCommand { get; } = new ActionCommand(() => preview(achievement), () => !achievement.Unlocked);
    public string Hidden => Item.Hidden ? "Hidden achievement" : "Standard achievement";
    public string Symbol => Item.Unlocked ? "◆" : "◇";
    public bool Selected { get => selection.Contains(ApiId); set { selection.Set(ApiId, value); Notify(); changed(); } }
}
