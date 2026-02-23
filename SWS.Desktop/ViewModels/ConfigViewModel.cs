using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;
using SWS.Data;
using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Configuration screen VM.
/// IMPORTANT RULE: Do NOT store SwsDbContext as a field.
/// Use IDbContextFactory to create a fresh context per operation.
/// </summary>
public partial class ConfigViewModel : ObservableObject
{
    private readonly IDbContextFactory<SwsDbContext> _dbFactory;

    public ObservableCollection<PointConfig> Points { get; } = new();

    [ObservableProperty] private string _status = "Idle";

    public ConfigViewModel(IDbContextFactory<SwsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task InitializeAsync()
    {
        await RefreshPointsAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await RefreshPointsAsync();
    }

    private async Task RefreshPointsAsync()
    {
        Status = "Loading...";

        // ✅ Fresh DbContext instance every time
        await using var db = await _dbFactory.CreateDbContextAsync();

        var points = await db.PointConfigs
            .AsNoTracking()
            .OrderBy(p => p.DeviceConfigId)
            .ThenBy(p => p.Key)
            .ToListAsync();

        Points.Clear();
        foreach (var p in points)
            Points.Add(p);

        Status = $"Loaded {Points.Count} points.";
    }
}