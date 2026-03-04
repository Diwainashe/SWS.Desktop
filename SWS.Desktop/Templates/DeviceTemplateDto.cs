using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SWS.Desktop.Templates;

/// <summary>
/// Root of the JSON template:
/// {
///   "deviceType": "GM9907_L5",
///   "points": [ ... ],
///   "bitfields": [ ... ]
/// }
/// </summary>
public sealed class DeviceTemplateDto
{
    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = "";

    // Optional defaults block (JSON may omit it - that's fine)
    [JsonPropertyName("defaults")]
    public TemplateDefaultsDto Defaults { get; set; } = new();

    [JsonPropertyName("points")]
    public List<TemplatePointDto> Points { get; set; } = new();

    [JsonPropertyName("bitfields")]
    public List<BitfieldDto> Bitfields { get; set; } = new();
}

/// <summary>
/// Defaults applied when a point doesn't specify something.
/// JSON may omit this entire object.
/// </summary>
public sealed class TemplateDefaultsDto
{
    [JsonPropertyName("pollRateMs")]
    public int PollRateMs { get; set; } = 1000;
}

public sealed class TemplatePointDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    // ✅ Fix: Unit exists because ConfigDataService uses it
    // JSON may omit it; default empty string is fine.
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "";

    [JsonPropertyName("area")]
    public string Area { get; set; } = "";

    [JsonPropertyName("address")]
    public int Address { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; } = 1;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "UInt16";

    [JsonPropertyName("scale")]
    public decimal Scale { get; set; } = 1m;

    [JsonPropertyName("pollRateMs")]
    public int PollRateMs { get; set; } = 0;

    [JsonPropertyName("logToHistory")]
    public bool LogToHistory { get; set; }

    [JsonPropertyName("historyIntervalMs")]
    public int HistoryIntervalMs { get; set; }

    [JsonPropertyName("isEssential")]
    public bool IsEssential { get; set; }
}

public sealed class BitfieldDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("wordSize")]
    public int WordSize { get; set; } = 16;

    [JsonPropertyName("bits")]
    public List<BitDefinitionDto> Bits { get; set; } = new();
}

public sealed class BitDefinitionDto
{
    [JsonPropertyName("bit")]
    public int Bit { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "flag";

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("whenZero")]
    public string? WhenZero { get; set; }

    [JsonPropertyName("whenOne")]
    public string? WhenOne { get; set; }
}