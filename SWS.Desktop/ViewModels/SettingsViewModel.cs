using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;
using System;
using Microsoft.Win32;
using System.Collections.ObjectModel;

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
            Status = "Client logo selected.";
        }
    }
}