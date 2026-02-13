using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Hardware;

public class SysutilHardwareService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
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
}
