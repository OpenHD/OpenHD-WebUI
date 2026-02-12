using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Settings;

namespace OpenHdWebUi.Server.Services.Hardware;

public class HotspotSettingsService
{
    private const string NetworkingSettingsPath = "interface/networking_settings.json";
    private readonly SettingsService _settingsService;

    public HotspotSettingsService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public HotspotSettingsDto GetHotspotSettings()
    {
        var file = LoadNetworkingSettingsFile(out _);
        return file == null ? HotspotSettingsDto.Unavailable() : BuildDto(file, null);
    }

    public HotspotSettingsDto UpdateHotspotSettings(HotspotSettingsUpdateRequest request)
    {
        var file = LoadNetworkingSettingsFile(out var summary);
        if (file == null || summary == null)
        {
            return HotspotSettingsDto.Unavailable();
        }

        var action = string.IsNullOrWhiteSpace(request.Action) ? "set" : request.Action.Trim().ToLowerInvariant();
        if (action == "refresh")
        {
            return BuildDto(file, action);
        }

        JsonObject root;
        try
        {
            root = JsonNode.Parse(file.Content) as JsonObject ?? new JsonObject();
        }
        catch
        {
            return HotspotSettingsDto.Unavailable();
        }

        if (action == "clear")
        {
            root["wifi_hotspot_mode"] = 0;
            root["wifi_hotspot_ssid"] = string.Empty;
            root["wifi_hotspot_password"] = string.Empty;
            root["wifi_hotspot_interface_override"] = string.Empty;
        }
        else if (action == "set")
        {
            if (request.HotspotMode.HasValue)
            {
                root["wifi_hotspot_mode"] = request.HotspotMode.Value;
            }
            if (request.HotspotSsid != null)
            {
                root["wifi_hotspot_ssid"] = request.HotspotSsid;
            }
            if (request.HotspotPassword != null)
            {
                root["wifi_hotspot_password"] = request.HotspotPassword;
            }
            if (request.HotspotInterfaceOverride != null)
            {
                root["wifi_hotspot_interface_override"] = request.HotspotInterfaceOverride;
            }
        }
        else
        {
            return BuildDto(file, action);
        }

        var updatedJson = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        if (!_settingsService.TrySaveSettingFile(summary.Id, updatedJson, out var updatedFile, out _, out _) ||
            updatedFile == null)
        {
            return HotspotSettingsDto.Unavailable();
        }

        return BuildDto(updatedFile, action);
    }

    private SettingFileDto? LoadNetworkingSettingsFile(out SettingFileSummaryDto? summary)
    {
        summary = _settingsService.GetSettingFiles()
            .FirstOrDefault(file => string.Equals(file.RelativePath, NetworkingSettingsPath, StringComparison.OrdinalIgnoreCase));
        if (summary == null)
        {
            return null;
        }

        return _settingsService.GetSettingFile(summary.Id);
    }

    private static HotspotSettingsDto BuildDto(SettingFileDto file, string? action)
    {
        try
        {
            using var document = JsonDocument.Parse(file.Content);
            var root = document.RootElement;
            var mode = ReadInt(root, "wifi_hotspot_mode", 0);
            var ssid = ReadString(root, "wifi_hotspot_ssid") ?? string.Empty;
            var password = ReadString(root, "wifi_hotspot_password") ?? string.Empty;
            var iface = ReadString(root, "wifi_hotspot_interface_override") ?? string.Empty;
            return new HotspotSettingsDto(true, mode, ssid, password, iface, action);
        }
        catch
        {
            return HotspotSettingsDto.Unavailable();
        }
    }

    private static int ReadInt(JsonElement root, string name, int fallback)
    {
        return root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;
    }

    private static string? ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) ? value.GetString() : null;
    }
}
