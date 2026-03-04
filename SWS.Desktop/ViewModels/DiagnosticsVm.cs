using System.Collections.ObjectModel;
using System.Linq;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Grouped diagnostics container.
/// UI can bind to AlarmGroups and StateGroups and show them as sections.
/// </summary>
public sealed class DiagnosticsVm
{
    public ObservableCollection<DiagnosticsGroupVm> AlarmGroups { get; } = new();
    public ObservableCollection<DiagnosticsGroupVm> StateGroups { get; } = new();

    public void SetAlarmGroups(IEnumerable<(string Title, IEnumerable<string> Items)> groups)
        => SetGroups(AlarmGroups, groups, emptyFallback: "OK");

    public void SetStateGroups(IEnumerable<(string Title, IEnumerable<string> Items)> groups)
        => SetGroups(StateGroups, groups, emptyFallback: "—");

    private static void SetGroups(
        ObservableCollection<DiagnosticsGroupVm> target,
        IEnumerable<(string Title, IEnumerable<string> Items)> groups,
        string emptyFallback)
    {
        target.Clear();

        foreach (var (title, items) in groups)
        {
            var g = new DiagnosticsGroupVm(title);

            foreach (var line in items.Where(x => !string.IsNullOrWhiteSpace(x)))
                g.Items.Add(line);

            // Only add groups that have something meaningful
            if (g.Items.Count > 0)
                target.Add(g);
        }

        // If nothing at all, add a single fallback group so UI doesn't look broken
        if (target.Count == 0)
        {
            var g = new DiagnosticsGroupVm("Status");
            g.Items.Add(emptyFallback);
            target.Add(g);
        }
    }
}