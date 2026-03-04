using System;
using System.IO;
using System.Text.Json;
using SWS.Core.Models;

namespace SWS.Desktop.Templates;

/// <summary>
/// Loads controller templates from JSON files.
/// JSON uses "manual-style" addresses exactly (e.g. 40007).
/// </summary>
public sealed class DeviceTemplateStore
{
    private readonly string _basePath;

    public DeviceTemplateStore()
    {
        // Templates folder lives alongside the app binaries
        // e.g. bin/Debug/net8.0-windows/Templates/
        _basePath = Path.Combine(AppContext.BaseDirectory, "Templates");
    }

    public DeviceTemplateDto? TryLoad(DeviceType deviceType)
    {
        if (deviceType == DeviceType.Generic)
            return null;

        // Map enum -> filename
        string fileName = deviceType switch
        {
            DeviceType.GM9907_L5 => "gm9907_l5.template.json",
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string path = Path.Combine(_basePath, fileName);

        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<DeviceTemplateDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }
}