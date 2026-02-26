namespace SWS.Desktop.Services;

/// <summary>
/// Small wrapper around Properties.Settings to keep UI code clean.
/// </summary>
public sealed class AppSettingsService
{
    private readonly Properties.Settings _s = Properties.Settings.Default;

    public AppTheme Theme
    {
        get => AppThemeService.ParseTheme(_s.AppTheme);    // AppTheme is stored as string
        set => _s.AppTheme = value.ToString();             // persist as string
    }

    public string EngineeringLogoPath
    {
        get => _s.EngineeringLogoPath ?? string.Empty;
        set => _s.EngineeringLogoPath = value;
    }

    public string ClientLogoPath
    {
        get => _s.ClientLogoPath ?? string.Empty;
        set => _s.ClientLogoPath = value;
    }

    public string ConnectionString
    {
        get => _s.SwsConnectionString ?? string.Empty;
        set => _s.SwsConnectionString = value;
    }

    public void Save() => _s.Save();
}