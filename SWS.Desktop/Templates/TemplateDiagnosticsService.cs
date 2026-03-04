using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Core.Models;
using SWS.Desktop.Templates;

namespace SWS.Desktop.Services;

/// <summary>
/// Builds grouped diagnostics from NEW JSON structure:
/// - template.Bitfields[] is the decoder source-of-truth
/// - template.Points[] is used ONLY to find nicer labels (optional)
///
/// Grouping rule (no hardcoding specific register names):
/// - key starts with "Alarm." => alarms
/// - otherwise => states
/// </summary>
public sealed class TemplateDiagnosticsService
{
    private readonly DeviceTemplateStore _store;
    private readonly BitfieldDecoder _decoder;

    public TemplateDiagnosticsService(DeviceTemplateStore store, BitfieldDecoder decoder)
    {
        _store = store;
        _decoder = decoder;
    }

    public (IReadOnlyList<(string Title, IEnumerable<string> Items)> AlarmGroups,
            IReadOnlyList<(string Title, IEnumerable<string> Items)> StateGroups)
        Build(DeviceType deviceType, IReadOnlyList<LatestReadingSnapshot> deviceReadings)
    {
        var template = _store.TryLoad(deviceType);
        if (template == null || template.Bitfields == null || template.Bitfields.Count == 0)
            return (Array.Empty<(string, IEnumerable<string>)>(), Array.Empty<(string, IEnumerable<string>)>());

        // Map point key -> label (nice-to-have)
        var labelMap = (template.Points ?? new List<TemplatePointDto>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key)
            .ToDictionary(g => g.Key, g => g.First().Label ?? "");

        var alarmGroups = new List<(string Title, IEnumerable<string> Items)>();
        var stateGroups = new List<(string Title, IEnumerable<string> Items)>();

        foreach (var bf in template.Bitfields)
        {
            ushort raw = GetU16(deviceReadings, bf.Key);
            var decoded = _decoder.Decode(bf, raw);

            // Title preference: bitfield.label -> point.label -> key
            string title =
                !string.IsNullOrWhiteSpace(bf.Label) ? bf.Label :
                (labelMap.TryGetValue(bf.Key, out var pl) && !string.IsNullOrWhiteSpace(pl)) ? pl :
                bf.Key;

            if (bf.Key.StartsWith("Alarm.", StringComparison.OrdinalIgnoreCase))
                alarmGroups.Add((title, decoded.Count == 0 ? new[] { "OK" } : decoded));
            else
                stateGroups.Add((title, decoded.Count == 0 ? new[] { "—" } : decoded));
        }

        return (alarmGroups, stateGroups);
    }

    private static ushort GetU16(IReadOnlyList<LatestReadingSnapshot> readings, string key)
    {
        var row = readings.FirstOrDefault(x => x.Key == key);

        // Missing / bad quality / null => treat as 0 (prevents false alarms)
        if (row?.ValueNumeric is null) return 0;
        if (row.Quality != ReadingQuality.Good) return 0;

        int v = (int)row.ValueNumeric.Value;
        if (v < 0) v = 0;
        if (v > ushort.MaxValue) v = ushort.MaxValue;
        return (ushort)v;
    }
}