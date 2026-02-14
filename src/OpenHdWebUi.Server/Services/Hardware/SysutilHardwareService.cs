using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Hardware;

public class SysutilHardwareService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private const string DefaultNwEthernetCard = "RPI_ETHERNET_ONLY";
    private const string DefaultMicrohardUsername = "admin";
    private const string DefaultMicrohardPassword = "qwertz1";
    private const int DefaultVideoPort = 5000;
    private const int DefaultTelemetryPort = 5600;
    private const int DefaultMicrohardVideoPort = 5910;
    private const int DefaultMicrohardTelemetryPort = 5920;
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(1000);

    public async Task<PlatformInfoDto> GetPlatformAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.platform.request\"}\n", cancellationToken);
        return ParsePlatformResponse(response);
    }

    public async Task<PlatformInfoDto> UpdatePlatformAsync(PlatformUpdateRequest request, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "sysutil.platform.update",
            ["action"] = string.IsNullOrWhiteSpace(request.Action) ? "refresh" : request.Action
        };
        if (request.PlatformType.HasValue)
        {
            payload["platform_type"] = request.PlatformType.Value;
        }
        if (!string.IsNullOrWhiteSpace(request.PlatformName))
        {
            payload["platform_name"] = request.PlatformName;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var response = await SendRequestAsync($"{json}\n", cancellationToken);
        return ParsePlatformResponse(response);
    }

    public async Task<WifiInfoDto> GetWifiAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.wifi.request\"}\n", cancellationToken);
        return ParseWifiResponse(response);
    }

    public async Task<WifiInfoDto> UpdateWifiAsync(WifiUpdateRequest request, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "sysutil.wifi.update",
            ["action"] = string.IsNullOrWhiteSpace(request.Action) ? "refresh" : request.Action
        };
        if (!string.IsNullOrWhiteSpace(request.Interface))
        {
            payload["interface"] = request.Interface;
        }
        if (!string.IsNullOrWhiteSpace(request.OverrideType))
        {
            payload["override_type"] = request.OverrideType;
        }
        if (request.TxPower is not null)
        {
            payload["tx_power"] = request.TxPower;
        }
        if (request.TxPowerHigh is not null)
        {
            payload["tx_power_high"] = request.TxPowerHigh;
        }
        if (request.TxPowerLow is not null)
        {
            payload["tx_power_low"] = request.TxPowerLow;
        }
        if (request.CardName is not null)
        {
            payload["card_name"] = request.CardName;
        }
        if (request.PowerLevel is not null)
        {
            payload["power_level"] = request.PowerLevel;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var response = await SendRequestAsync($"{json}\n", cancellationToken);
        return ParseWifiResponse(response);
    }

    public async Task<HardwareConfigDto> GetHardwareConfigAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.settings.request\"}\n", cancellationToken);
        return ParseHardwareConfigResponse(response);
    }

    public async Task<HardwareConfigDto> UpdateHardwareConfigAsync(HardwareConfigUpdateRequest request, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "sysutil.settings.update"
        };
        if (request.WifiEnableAutodetect.HasValue)
        {
            payload["wifi_enable_autodetect"] = request.WifiEnableAutodetect.Value;
        }
        if (request.WifiWbLinkCards is not null)
        {
            payload["wifi_wb_link_cards"] = request.WifiWbLinkCards;
        }
        if (request.WifiHotspotCard is not null)
        {
            payload["wifi_hotspot_card"] = request.WifiHotspotCard;
        }
        if (request.WifiMonitorCardEmulate.HasValue)
        {
            payload["wifi_monitor_card_emulate"] = request.WifiMonitorCardEmulate.Value;
        }
        if (request.WifiForceNoLinkButHotspot.HasValue)
        {
            payload["wifi_force_no_link_but_hotspot"] = request.WifiForceNoLinkButHotspot.Value;
        }
        if (request.WifiLocalNetworkEnable.HasValue)
        {
            payload["wifi_local_network_enable"] = request.WifiLocalNetworkEnable.Value;
        }
        if (request.WifiLocalNetworkSsid is not null)
        {
            payload["wifi_local_network_ssid"] = request.WifiLocalNetworkSsid;
        }
        if (request.WifiLocalNetworkPassword is not null)
        {
            payload["wifi_local_network_password"] = request.WifiLocalNetworkPassword;
        }
        if (request.NwEthernetCard is not null)
        {
            payload["nw_ethernet_card"] = request.NwEthernetCard;
        }
        if (request.NwManualForwardingIps is not null)
        {
            payload["nw_manual_forwarding_ips"] = request.NwManualForwardingIps;
        }
        if (request.NwForwardToLocalhost58xx.HasValue)
        {
            payload["nw_forward_to_localhost_58xx"] = request.NwForwardToLocalhost58xx.Value;
        }
        if (request.GenEnableLastKnownPosition.HasValue)
        {
            payload["gen_enable_last_known_position"] = request.GenEnableLastKnownPosition.Value;
        }
        if (request.GenRfMetricsLevel.HasValue)
        {
            payload["gen_rf_metrics_level"] = request.GenRfMetricsLevel.Value;
        }
        if (request.GroundUnitIp is not null)
        {
            payload["ground_unit_ip"] = request.GroundUnitIp;
        }
        if (request.AirUnitIp is not null)
        {
            payload["air_unit_ip"] = request.AirUnitIp;
        }
        if (request.VideoPort.HasValue)
        {
            payload["video_port"] = request.VideoPort.Value;
        }
        if (request.TelemetryPort.HasValue)
        {
            payload["telemetry_port"] = request.TelemetryPort.Value;
        }
        if (request.DisableMicrohardDetection.HasValue)
        {
            payload["disable_microhard_detection"] = request.DisableMicrohardDetection.Value;
        }
        if (request.ForceMicrohard.HasValue)
        {
            payload["force_microhard"] = request.ForceMicrohard.Value;
        }
        if (request.MicrohardUsername is not null)
        {
            payload["microhard_username"] = request.MicrohardUsername;
        }
        if (request.MicrohardPassword is not null)
        {
            payload["microhard_password"] = request.MicrohardPassword;
        }
        if (request.MicrohardIpAir is not null)
        {
            payload["microhard_ip_air"] = request.MicrohardIpAir;
        }
        if (request.MicrohardIpGround is not null)
        {
            payload["microhard_ip_ground"] = request.MicrohardIpGround;
        }
        if (request.MicrohardIpRange is not null)
        {
            payload["microhard_ip_range"] = request.MicrohardIpRange;
        }
        if (request.MicrohardVideoPort.HasValue)
        {
            payload["microhard_video_port"] = request.MicrohardVideoPort.Value;
        }
        if (request.MicrohardTelemetryPort.HasValue)
        {
            payload["microhard_telemetry_port"] = request.MicrohardTelemetryPort.Value;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var response = await SendRequestAsync($"{json}\n", cancellationToken);
        if (!IsSettingsUpdateOk(response))
        {
            return HardwareConfigDto.Unavailable();
        }
        return await GetHardwareConfigAsync(cancellationToken);
    }

    private static PlatformInfoDto ParsePlatformResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return PlatformInfoDto.Unavailable();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SysutilPlatformPayload>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload == null)
            {
                return PlatformInfoDto.Unavailable();
            }

            if (payload.Ok.HasValue && !payload.Ok.Value)
            {
                return PlatformInfoDto.Unavailable();
            }

            return new PlatformInfoDto(true, payload.PlatformType, payload.PlatformName ?? "Unknown", payload.Action);
        }
        catch
        {
            return PlatformInfoDto.Unavailable();
        }
    }

    private static WifiInfoDto ParseWifiResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return WifiInfoDto.Unavailable();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SysutilWifiPayload>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload == null)
            {
                return WifiInfoDto.Unavailable();
            }

            if (payload.Ok.HasValue && !payload.Ok.Value)
            {
                return WifiInfoDto.Unavailable();
            }

            var cards = payload.Cards?.Select(card => new WifiCardInfoDto(
                card.Interface ?? string.Empty,
                card.Driver ?? string.Empty,
                card.Mac ?? string.Empty,
                card.PhyIndex,
                card.VendorId ?? string.Empty,
                card.DeviceId ?? string.Empty,
                card.DetectedType ?? string.Empty,
                card.OverrideType ?? string.Empty,
                card.Type ?? string.Empty,
                card.Disabled,
                card.TxPower ?? string.Empty,
                card.TxPowerHigh ?? string.Empty,
                card.TxPowerLow ?? string.Empty,
                card.CardName ?? string.Empty,
                card.PowerLevel ?? string.Empty,
                card.PowerLowest ?? string.Empty,
                card.PowerLow ?? string.Empty,
                card.PowerMid ?? string.Empty,
                card.PowerHigh ?? string.Empty,
                card.PowerMin ?? string.Empty,
                card.PowerMax ?? string.Empty))
                .ToArray() ?? Array.Empty<WifiCardInfoDto>();

            return new WifiInfoDto(true, cards, payload.Action);
        }
        catch
        {
            return WifiInfoDto.Unavailable();
        }
    }

    private static HardwareConfigDto ParseHardwareConfigResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return HardwareConfigDto.Unavailable();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SysutilSettingsPayload>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload == null)
            {
                return HardwareConfigDto.Unavailable();
            }
            if (payload.Ok.HasValue && !payload.Ok.Value)
            {
                return HardwareConfigDto.Unavailable();
            }

            return new HardwareConfigDto(
                true,
                payload.WifiEnableAutodetect ?? true,
                payload.WifiWbLinkCards ?? string.Empty,
                payload.WifiHotspotCard ?? string.Empty,
                payload.WifiMonitorCardEmulate ?? false,
                payload.WifiForceNoLinkButHotspot ?? false,
                payload.WifiLocalNetworkEnable ?? false,
                payload.WifiLocalNetworkSsid ?? string.Empty,
                payload.WifiLocalNetworkPassword ?? string.Empty,
                payload.NwEthernetCard ?? DefaultNwEthernetCard,
                payload.NwManualForwardingIps ?? string.Empty,
                payload.NwForwardToLocalhost58xx ?? false,
                payload.GenEnableLastKnownPosition ?? false,
                payload.GenRfMetricsLevel ?? 0,
                payload.GroundUnitIp ?? string.Empty,
                payload.AirUnitIp ?? string.Empty,
                payload.VideoPort ?? DefaultVideoPort,
                payload.TelemetryPort ?? DefaultTelemetryPort,
                payload.DisableMicrohardDetection ?? false,
                payload.ForceMicrohard ?? false,
                payload.MicrohardUsername ?? DefaultMicrohardUsername,
                payload.MicrohardPassword ?? DefaultMicrohardPassword,
                payload.MicrohardIpAir ?? string.Empty,
                payload.MicrohardIpGround ?? string.Empty,
                payload.MicrohardIpRange ?? string.Empty,
                payload.MicrohardVideoPort ?? DefaultMicrohardVideoPort,
                payload.MicrohardTelemetryPort ?? DefaultMicrohardTelemetryPort);
        }
        catch
        {
            return HardwareConfigDto.Unavailable();
        }
    }

    private static bool IsSettingsUpdateOk(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var type) ||
                type.GetString() != "sysutil.settings.update.response")
            {
                return false;
            }
            if (root.TryGetProperty("ok", out var ok))
            {
                return ok.ValueKind != JsonValueKind.False;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> SendRequestAsync(string payload, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return null;
        }

        if (!File.Exists(SocketPath))
        {
            return null;
        }

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(SocketPath);

            using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                connectCts.CancelAfter(ConnectTimeout);
                await socket.ConnectAsync(endpoint, connectCts.Token);
            }

            using var stream = new NetworkStream(socket, ownsSocket: true);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            await stream.WriteAsync(payloadBytes, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
            string? line;
            using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                readCts.CancelAfter(ReadTimeout);
                line = await reader.ReadLineAsync().WaitAsync(readCts.Token);
            }

            return line;
        }
        catch
        {
            return null;
        }
    }

    private sealed record SysutilPlatformPayload(
        [property: JsonPropertyName("ok")] bool? Ok,
        [property: JsonPropertyName("platform_type")] int PlatformType,
        [property: JsonPropertyName("platform_name")] string? PlatformName,
        [property: JsonPropertyName("action")] string? Action);

    private sealed record SysutilWifiPayload(
        [property: JsonPropertyName("ok")] bool? Ok,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("cards")] List<SysutilWifiCardPayload>? Cards);

    private sealed record SysutilWifiCardPayload(
        [property: JsonPropertyName("interface")] string? Interface,
        [property: JsonPropertyName("driver")] string? Driver,
        [property: JsonPropertyName("mac")] string? Mac,
        [property: JsonPropertyName("phy_index")] int PhyIndex,
        [property: JsonPropertyName("vendor_id")] string? VendorId,
        [property: JsonPropertyName("device_id")] string? DeviceId,
        [property: JsonPropertyName("detected_type")] string? DetectedType,
        [property: JsonPropertyName("override_type")] string? OverrideType,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("disabled")] bool Disabled,
        [property: JsonPropertyName("tx_power")] string? TxPower,
        [property: JsonPropertyName("tx_power_high")] string? TxPowerHigh,
        [property: JsonPropertyName("tx_power_low")] string? TxPowerLow,
        [property: JsonPropertyName("card_name")] string? CardName,
        [property: JsonPropertyName("power_level")] string? PowerLevel,
        [property: JsonPropertyName("power_lowest")] string? PowerLowest,
        [property: JsonPropertyName("power_low")] string? PowerLow,
        [property: JsonPropertyName("power_mid")] string? PowerMid,
        [property: JsonPropertyName("power_high")] string? PowerHigh,
        [property: JsonPropertyName("power_min")] string? PowerMin,
        [property: JsonPropertyName("power_max")] string? PowerMax);

    private sealed record SysutilSettingsPayload(
        [property: JsonPropertyName("ok")] bool? Ok,
        [property: JsonPropertyName("wifi_enable_autodetect")] bool? WifiEnableAutodetect,
        [property: JsonPropertyName("wifi_wb_link_cards")] string? WifiWbLinkCards,
        [property: JsonPropertyName("wifi_hotspot_card")] string? WifiHotspotCard,
        [property: JsonPropertyName("wifi_monitor_card_emulate")] bool? WifiMonitorCardEmulate,
        [property: JsonPropertyName("wifi_force_no_link_but_hotspot")] bool? WifiForceNoLinkButHotspot,
        [property: JsonPropertyName("wifi_local_network_enable")] bool? WifiLocalNetworkEnable,
        [property: JsonPropertyName("wifi_local_network_ssid")] string? WifiLocalNetworkSsid,
        [property: JsonPropertyName("wifi_local_network_password")] string? WifiLocalNetworkPassword,
        [property: JsonPropertyName("nw_ethernet_card")] string? NwEthernetCard,
        [property: JsonPropertyName("nw_manual_forwarding_ips")] string? NwManualForwardingIps,
        [property: JsonPropertyName("nw_forward_to_localhost_58xx")] bool? NwForwardToLocalhost58xx,
        [property: JsonPropertyName("gen_enable_last_known_position")] bool? GenEnableLastKnownPosition,
        [property: JsonPropertyName("gen_rf_metrics_level")] int? GenRfMetricsLevel,
        [property: JsonPropertyName("ground_unit_ip")] string? GroundUnitIp,
        [property: JsonPropertyName("air_unit_ip")] string? AirUnitIp,
        [property: JsonPropertyName("video_port")] int? VideoPort,
        [property: JsonPropertyName("telemetry_port")] int? TelemetryPort,
        [property: JsonPropertyName("disable_microhard_detection")] bool? DisableMicrohardDetection,
        [property: JsonPropertyName("force_microhard")] bool? ForceMicrohard,
        [property: JsonPropertyName("microhard_username")] string? MicrohardUsername,
        [property: JsonPropertyName("microhard_password")] string? MicrohardPassword,
        [property: JsonPropertyName("microhard_ip_air")] string? MicrohardIpAir,
        [property: JsonPropertyName("microhard_ip_ground")] string? MicrohardIpGround,
        [property: JsonPropertyName("microhard_ip_range")] string? MicrohardIpRange,
        [property: JsonPropertyName("microhard_video_port")] int? MicrohardVideoPort,
        [property: JsonPropertyName("microhard_telemetry_port")] int? MicrohardTelemetryPort);
}
