using Jupiter.UI;
using UniversalVideoDownloader.ViewModels;

namespace UniversalVideoDownloader.App.Services;

internal sealed class AppearancePreferences(SettingsService settings) : IAccentPreferences, IThemePreferences
{
    public bool UseCustomAccent { get => settings.Current.UseCustomAccent; set => settings.Current.UseCustomAccent = value; }
    public string? CustomAccent { get => settings.Current.CustomAccent; set => settings.Current.CustomAccent = value; }
    public string? Theme { get => settings.Current.Theme; set => settings.Current.Theme = value; }
    public void Save()
    {
        if (!settings.Save(out var error))
            App.GetService<NotificationViewModel>().Show($"Appearance could not be saved: {error}", NotificationLevel.Error);
    }
}
