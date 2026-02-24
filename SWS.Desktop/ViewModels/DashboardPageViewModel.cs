using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

public partial class DashboardPageViewModel : ObservableObject
{
    private readonly ConfigDataService _data;

    public ObservableCollection<LatestReadingRow> Rows { get; } = new();

    public DashboardPageViewModel(ConfigDataService data)
    {
        _data = data;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var list = await _data.GetLatestReadingsAsync(CancellationToken.None);

        Rows.Clear();
        foreach (var r in list)
            Rows.Add(r);
    }
}