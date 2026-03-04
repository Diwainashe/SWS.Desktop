using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// One diagnostics section (e.g. "Operating state", "Alarm Info 1").
/// Contains a title + a list of lines to display.
/// </summary>
public sealed class DiagnosticsGroupVm
{
    public string Title { get; }
    public ObservableCollection<string> Items { get; } = new();

    public DiagnosticsGroupVm(string title) => Title = title;
}