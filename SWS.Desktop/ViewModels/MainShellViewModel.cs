using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SWS.Desktop.ViewModels;

public partial class MainShellViewModel : ObservableObject
{
    public INavigationService Navigation { get; }
    private readonly AppSettingsService _settings;

    [ObservableProperty]
    private AppPageKey _currentPage = AppPageKey.Dashboard;

    [ObservableProperty]
    private ImageSource? _engineeringLogoImage;

    [ObservableProperty]
    private ImageSource? _clientLogoImage;

    public MainShellViewModel(INavigationService navigation, AppSettingsService settings)
    {
        Navigation = navigation;
        _settings = settings;

        // Initial load
        RefreshLogos();

        // Auto-apply when settings change
        _settings.SettingsChanged += (_, __) => RefreshLogos();

        _ = Navigation.NavigateToAsync(CurrentPage);
    }

    partial void OnCurrentPageChanged(AppPageKey value)
    {
        _ = Navigation.NavigateToAsync(value);
    }

    private void RefreshLogos()
    {
        EngineeringLogoImage = LoadImageOrNull(_settings.EngineeringLogoPath);
        ClientLogoImage = LoadImageOrNull(_settings.ClientLogoPath);
    }

    private static ImageSource? LoadImageOrNull(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        // Important: CacheOption.OnLoad so we don't lock the file on disk
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path, UriKind.Absolute);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    [RelayCommand] private async Task GoDashboardAsync() => await Navigation.NavigateToAsync(AppPageKey.Dashboard);
    [RelayCommand] private async Task GoDevicesAsync() => await Navigation.NavigateToAsync(AppPageKey.Devices);
    [RelayCommand] private async Task GoPointsAsync() => await Navigation.NavigateToAsync(AppPageKey.Points);
    [RelayCommand] private void GoSettings() => _ = Navigation.NavigateToAsync(AppPageKey.Settings);
}