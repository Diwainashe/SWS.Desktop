using System;
using System.Linq;
using System.Windows;

namespace SWS.Desktop.Services;

/// <summary>
/// Controls swapping the active theme dictionary at runtime.
/// The trick: all controls use DynamicResource keys, and the theme file only defines brushes.
/// </summary>
public sealed class AppThemeService
{
    private const string ThemeFolder = "Themes";
    private static readonly Uri DarkUri = new Uri($"{ThemeFolder}/Dark.xaml", UriKind.Relative);
    private static readonly Uri LightUri = new Uri($"{ThemeFolder}/Light.xaml", UriKind.Relative);

    /// <summary>
    /// Apply theme by enum.
    /// </summary>
    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null) return;

        // Find the currently loaded theme dictionary (Dark.xaml or Light.xaml)
        var dictionaries = app.Resources.MergedDictionaries;

        ResourceDictionary? currentTheme = dictionaries
            .FirstOrDefault(d => d.Source is not null &&
                                 d.Source.OriginalString.StartsWith($"{ThemeFolder}/", StringComparison.OrdinalIgnoreCase) &&
                                 (d.Source.OriginalString.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                                  d.Source.OriginalString.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase)));

        var newThemeDict = new ResourceDictionary
        {
            Source = theme == AppTheme.Light ? LightUri : DarkUri
        };

        if (currentTheme is null)
        {
            // If none found, just add it
            dictionaries.Add(newThemeDict);
            return;
        }

        // Replace in-place (keeps bindings stable)
        var index = dictionaries.IndexOf(currentTheme);
        dictionaries[index] = newThemeDict;
    }

    /// <summary>
    /// Helper: parse from persisted string safely.
    /// </summary>
    public static AppTheme ParseTheme(string? value)
        => Enum.TryParse<AppTheme>(value, ignoreCase: true, out var t) ? t : AppTheme.Dark;
}

/// <summary>
/// Theme values stored in Settings (as string) but used as enum in code.
/// </summary>
public enum AppTheme
{
    Dark,
    Light
}