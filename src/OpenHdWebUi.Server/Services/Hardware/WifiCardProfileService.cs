using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Hardware;

public class WifiCardProfileService
{
    private const string WifiCardsPath = "/usr/local/share/OpenHD/SysUtils/wifi_cards.json";

    private static readonly IReadOnlyCollection<WifiCardProfileDto> DefaultProfiles = new[]
    {
        new WifiCardProfileDto("0x02D0", "0xA9A6", "Raspberry Internal", 25, 1000, 25, 100, 200, 500),
        new WifiCardProfileDto("0x0BDA", "0xA81A", "LB-Link 8812eu", 0, 1000, 25, 100, 500, 1000)
    };

    public WifiCardProfilesDto GetProfiles()
    {
        var profiles = LoadProfiles(out var exists);
        return new WifiCardProfilesDto(exists, profiles, "load");
    }

    public WifiCardProfilesDto UpdateProfile(WifiCardProfileUpdateRequest request)
    {
        var normalizedVendor = NormalizeId(request.VendorId);
        var normalizedDevice = NormalizeId(request.DeviceId);

        var profiles = LoadProfiles(out var exists).ToList();
        var current = profiles.FirstOrDefault(profile =>
            string.Equals(profile.VendorId, normalizedVendor, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(profile.DeviceId, normalizedDevice, StringComparison.OrdinalIgnoreCase));

        var minMw = current?.MinMw ?? (request.Lowest > 0 ? request.Lowest : 0);
        var maxMw = current?.MaxMw ?? (request.High > 0 ? request.High : 0);

        var updated = new WifiCardProfileDto(
            normalizedVendor,
            normalizedDevice,
            string.IsNullOrWhiteSpace(request.Name) ? current?.Name ?? string.Empty : request.Name.Trim(),
            minMw,
            maxMw,
            request.Lowest,
            request.Low,
            request.Mid,
            request.High);

        if (current == null)
        {
            profiles.Add(updated);
        }
        else
        {
            var index = profiles.IndexOf(current);
            profiles[index] = updated;
        }

        if (!TryWriteProfiles(profiles))
        {
            return new WifiCardProfilesDto(false, profiles, "save");
        }

        var refreshed = LoadProfiles(out var refreshedExists);
        return new WifiCardProfilesDto(refreshedExists, refreshed, "save");
    }

    private static IReadOnlyCollection<WifiCardProfileDto> LoadProfiles(out bool exists)
    {
        exists = File.Exists(WifiCardsPath);
        if (!exists)
        {
            return DefaultProfiles;
        }

        try
        {
            var content = File.ReadAllText(WifiCardsPath);
            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("cards", out var cards) ||
                cards.ValueKind != JsonValueKind.Array)
            {
                return DefaultProfiles;
            }

            var profiles = new List<WifiCardProfileDto>();
            foreach (var card in cards.EnumerateArray())
            {
                var vendor = NormalizeId(ReadString(card, "vendor_id") ?? string.Empty);
                var device = NormalizeId(ReadString(card, "device_id") ?? string.Empty);
                if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(device))
                {
                    continue;
                }

                var name = ReadString(card, "name") ?? string.Empty;
                var hasMin = TryReadInt(card, "min_mw", out var minMw);
                var hasMax = TryReadInt(card, "max_mw", out var maxMw);

                var levels = card.TryGetProperty("levels_mw", out var levelsNode) &&
                             levelsNode.ValueKind == JsonValueKind.Object
                    ? levelsNode
                    : card;

                var lowest = ReadInt(levels, "lowest");
                var low = ReadInt(levels, "low");
                var mid = ReadInt(levels, "mid");
                var high = ReadInt(levels, "high");

                if (!hasMin && lowest > 0)
                {
                    minMw = lowest;
                }
                if (!hasMax && high > 0)
                {
                    maxMw = high;
                }

                profiles.Add(new WifiCardProfileDto(vendor, device, name, minMw, maxMw, lowest, low, mid, high));
            }

            return profiles.Count == 0 ? DefaultProfiles : profiles;
        }
        catch
        {
            return DefaultProfiles;
        }
    }

    private static bool TryWriteProfiles(IReadOnlyCollection<WifiCardProfileDto> profiles)
    {
        try
        {
            var dir = Path.GetDirectoryName(WifiCardsPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var array = new JsonArray();
            foreach (var profile in profiles)
            {
                var levels = new JsonObject
                {
                    ["lowest"] = profile.Lowest,
                    ["low"] = profile.Low,
                    ["mid"] = profile.Mid,
                    ["high"] = profile.High
                };
                var card = new JsonObject
                {
                    ["vendor_id"] = profile.VendorId,
                    ["device_id"] = profile.DeviceId,
                    ["name"] = profile.Name,
                    ["min_mw"] = profile.MinMw,
                    ["max_mw"] = profile.MaxMw,
                    ["levels_mw"] = levels
                };
                array.Add(card);
            }

            var root = new JsonObject
            {
                ["cards"] = array
            };

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(WifiCardsPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeId(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return "0x" + trimmed[2..].ToUpperInvariant();
        }
        return "0x" + trimmed.ToUpperInvariant();
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) ? value.GetString() : null;
    }

    private static bool TryReadInt(JsonElement element, string name, out int result)
    {
        if (element.TryGetProperty(name, out var value) && value.TryGetInt32(out result))
        {
            return true;
        }
        result = 0;
        return false;
    }

    private static int ReadInt(JsonElement element, string name)
    {
        return TryReadInt(element, name, out var result) ? result : 0;
    }
}
