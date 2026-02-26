using System;
using System.IO;

namespace SWS.Desktop.Services;

/// <summary>
/// Small wrapper around Properties.Settings to keep UI code clean.
/// Adds change notifications so the UI can auto-refresh (logos/theme) without restart.
/// </summary>
public sealed class AppSettingsService
{
    private readonly Properties.Settings _s = Properties.Settings.Default;

    /// <summary>
    /// Fires whenever any setting is changed (including after Save()).
    /// Your shell can listen to this to refresh logos / re-apply theme automatically.
    /// </summary>
    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Theme is persisted as a string in Settings.settings.
    /// </summary>
    public AppTheme Theme
    {
        get => AppThemeService.ParseTheme(_s.AppTheme);
        set
        {
            var next = value.ToString();

            if (!string.Equals(_s.AppTheme, next, StringComparison.Ordinal))
            {
                _s.AppTheme = next;
                OnSettingsChanged();
            }
        }
    }

    public string EngineeringLogoPath
    {
        get => _s.EngineeringLogoPath ?? string.Empty;
        set
        {
            value ??= string.Empty;
            if (!string.Equals(_s.EngineeringLogoPath, value, StringComparison.Ordinal))
            {
                _s.EngineeringLogoPath = value;
                OnSettingsChanged();
            }
        }
    }

    public string ClientLogoPath
    {
        get => _s.ClientLogoPath ?? string.Empty;
        set
        {
            value ??= string.Empty;
            if (!string.Equals(_s.ClientLogoPath, value, StringComparison.Ordinal))
            {
                _s.ClientLogoPath = value;
                OnSettingsChanged();
            }
        }
    }

    public string ConnectionString
    {
        get => _s.SwsConnectionString ?? string.Empty;
        set
        {
            value ??= string.Empty;
            if (!string.Equals(_s.SwsConnectionString, value, StringComparison.Ordinal))
            {
                _s.SwsConnectionString = value;
                OnSettingsChanged();
            }
        }
    }

    /// <summary>
    /// Optional helper for your Settings UI: only accept existing logo files.
    /// </summary>
    public bool FileExists(string? path)
        => !string.IsNullOrWhiteSpace(path) && File.Exists(path);

    /// <summary>
    /// Persist to disk. We also raise SettingsChanged so listeners can refresh.
    /// </summary>
    public void Save()
    {
        _s.Save();
        OnSettingsChanged();
    }

    private void OnSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}