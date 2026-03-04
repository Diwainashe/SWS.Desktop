using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Desktop.Templates;

namespace SWS.Desktop.Services;

/// <summary>
/// JSON-driven bitfield decoder.
/// Uses BitfieldDto (NOT TemplatePointDto).
///
/// Rules:
/// - flag: include only when bit == 1
/// - twoState: include exactly one of (whenZero/whenOne) based on bit value
/// </summary>
public sealed class BitfieldDecoder
{
    public IReadOnlyList<string> Decode(BitfieldDto meta, ushort raw)
    {
        if (meta == null) throw new ArgumentNullException(nameof(meta));

        var output = new List<string>();

        foreach (var bitDef in meta.Bits.OrderBy(b => b.Bit))
        {
            // Ignore garbage bit numbers safely
            if (bitDef.Bit < 0 || bitDef.Bit > 15)
                continue;

            bool isOn = IsBitSet(raw, bitDef.Bit);
            string kind = (bitDef.Kind ?? "flag").Trim().ToLowerInvariant();

            if (kind == "twostate")
            {
                // Always emit a result for twoState bits
                var zero = (bitDef.WhenZero ?? "0").Trim();
                var one = (bitDef.WhenOne ?? "1").Trim();

                output.Add(isOn ? one : zero);
            }
            else
            {
                // Default = flag
                if (isOn)
                {
                    var label = (bitDef.Label ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(label))
                        output.Add(label);
                }
            }
        }

        // Keep it clean: remove blanks + duplicates
        return output
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();
    }

    private static bool IsBitSet(ushort value, int bit)
        => (value & (1 << bit)) != 0;
}