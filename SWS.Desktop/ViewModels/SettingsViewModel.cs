using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;
using System;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SWS.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettingsService _settings;
    private readonly AppThemeService _theme;

    // ✅ This is what your SettingsView ComboBox is binding to.
    public ObservableCollection<AppTheme> Themes { get; } =
        new ObservableCollection<AppTheme>((AppTheme[])Enum.GetValues(typeof(AppTheme)));

    [ObservableProperty] private AppTheme _selectedTheme;
    [ObservableProperty] private string _engineeringLogoPath = string.Empty;
    [ObservableProperty] private string _clientLogoPath = string.Empty;
    [ObservableProperty] private string _connectionString = string.Empty;
    [ObservableProperty] private string _status = "Ready";
    [ObservableProperty] private ImageSource? _engineeringLogoPreview;
    [ObservableProperty] private ImageSource? _clientLogoPreview;

    private static ImageSource? LoadPreview(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path, UriKind.Absolute);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private void RefreshPreviews()
    {
        EngineeringLogoPreview = LoadPreview(EngineeringLogoPath);
        ClientLogoPreview = LoadPreview(ClientLogoPath);
    }
    public SettingsViewModel(AppSettingsService settings, AppThemeService theme)
    {
        _settings = settings;
        _theme = theme;

        // Load from persisted settings
        SelectedTheme = _settings.Theme;
        EngineeringLogoPath = _settings.EngineeringLogoPath;
        ClientLogoPath = _settings.ClientLogoPath;
        ConnectionString = _settings.ConnectionString;
    }

    [RelayCommand]
    private void ApplyTheme()
    {
        _theme.ApplyTheme(SelectedTheme);
        Status = $"Theme applied: {SelectedTheme}";
    }

    [RelayCommand]
    private void Save()
    {
        // Validate logo paths
        if (!string.IsNullOrWhiteSpace(EngineeringLogoPath) && !File.Exists(EngineeringLogoPath))
        {
            Status = "Engineering logo path is invalid (file not found).";
            return;
        }

        if (!string.IsNullOrWhiteSpace(ClientLogoPath) && !File.Exists(ClientLogoPath))
        {
            Status = "Client logo path is invalid (file not found).";
            return;
        }

        _settings.Theme = SelectedTheme;
        _settings.EngineeringLogoPath = EngineeringLogoPath;
        _settings.ClientLogoPath = ClientLogoPath;
        _settings.ConnectionString = ConnectionString;

        _settings.Save();
        Status = "Saved.";
    }
    [RelayCommand]
    private void BrowseEngineeringLogo()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Engineering Logo",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            EngineeringLogoPath = dialog.FileName;
            RefreshPreviews();

            // Auto-apply immediately (no restart)
            _settings.EngineeringLogoPath = EngineeringLogoPath;

            Status = "Engineering logo selected.";
        }
    }

    [RelayCommand]
    private void BrowseClientLogo()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Client Logo",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            ClientLogoPath = dialog.FileName;
            RefreshPreviews();

            // Auto-apply immediately (no restart)
            _settings.ClientLogoPath = ClientLogoPath;

            Status = "Client logo selected.";
        }
    }
}