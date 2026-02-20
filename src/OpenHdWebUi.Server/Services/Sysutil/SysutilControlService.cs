using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Sysutil;

public class SysutilControlService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(2);

    public async Task<SysutilDebugDto> GetDebugAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.debug.request\"}\n", ReadTimeout, cancellationToken);
        if (response == null)
        {
            return new SysutilDebugDto(false, false);
        }

        var payload = JsonSerializer.Deserialize<SysutilDebugPayload>(response, JsonOptions);
        if (payload == null)
        {
            return new SysutilDebugDto(false, false);
        }

        return new SysutilDebugDto(true, payload.Debug);
    }

    public async Task<SysutilDebugUpdateResponseDto> SetDebugAsync(bool enabled, CancellationToken cancellationToken)
    {
        var payload = $"{{\"type\":\"sysutil.debug.update\",\"debug\":{(enabled ? "true" : "false")}}}\n";
        var response = await SendRequestAsync(payload, ReadTimeout, cancellationToken);
        if (response == null)
        {
            return new SysutilDebugUpdateResponseDto(false, enabled, "Sysutils socket not available.");
        }

        var payloadData = JsonSerializer.Deserialize<SysutilDebugUpdatePayload>(response, JsonOptions);
        if (payloadData == null || !payloadData.Ok)
        {
            return new SysutilDebugUpdateResponseDto(false, enabled, "Sysutils rejected the update.");
        }

        return new SysutilDebugUpdateResponseDto(true, payloadData.Debug, null);
    }

    public async Task<SysutilPlatformInfoDto> GetPlatformAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.platform.request\"}\n", ReadTimeout, cancellationToken);
        if (response == null)
        {
            return new SysutilPlatformInfoDto(false, 0, "unknown");
        }

        var payload = JsonSerializer.Deserialize<SysutilPlatformPayload>(response, JsonOptions);
        if (payload == null)
        {
            return new SysutilPlatformInfoDto(false, 0, "unknown");
        }

        return new SysutilPlatformInfoDto(true, payload.PlatformType, payload.PlatformName ?? "unknown");
    }

    public async Task<SysutilPlatformUpdateResponseDto> UpdatePlatformAsync(SysutilPlatformUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var payloadObject = new
        {
            type = "sysutil.platform.update",
            action = request.Action,
            platform_type = request.PlatformType,
            platform_name = request.PlatformName
        };

        var payload = JsonSerializer.Serialize(payloadObject, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var response = await SendRequestAsync(payload + "\n", ReadTimeout, cancellationToken);
        if (response == null)
        {
            return new SysutilPlatformUpdateResponseDto(false, 0, "unknown", "Sysutils socket not available.");
        }

        var payloadData = JsonSerializer.Deserialize<SysutilPlatformUpdatePayload>(response, JsonOptions);
        if (payloadData == null || !payloadData.Ok)
        {
            return new SysutilPlatformUpdateResponseDto(false, payloadData?.PlatformType ?? 0,
                payloadData?.PlatformName ?? "unknown", "Sysutils rejected the update.");
        }

        return new SysutilPlatformUpdateResponseDto(true, payloadData.PlatformType, payloadData.PlatformName ?? "unknown", null);
    }

    public async Task<SysutilVideoResponseDto> RestartVideoAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.video.request\",\"action\":\"restart\"}\n", ReadTimeout, cancellationToken);
        if (response == null)
        {
            return new SysutilVideoResponseDto(false, null, "Sysutils socket not available.");
        }

        var payloadData = JsonSerializer.Deserialize<SysutilVideoPayload>(response, JsonOptions);
        if (payloadData == null || !payloadData.Ok)
        {
            return new SysutilVideoResponseDto(false, payloadData?.Pipeline, "Sysutils rejected the video restart.");
        }

        return new SysutilVideoResponseDto(true, payloadData.Pipeline, null);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private async Task<string?> SendRequestAsync(string payload, TimeSpan readTimeout, CancellationToken cancellationToken)
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
            var bytes = Encoding.UTF8.GetBytes(payload);
            await stream.WriteAsync(bytes, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, leaveOpen: true);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCts.CancelAfter(readTimeout);
            return await reader.ReadLineAsync().WaitAsync(readCts.Token);
        }
        catch
        {
            return null;
        }
    }

    private sealed record SysutilDebugPayload(
        [property: JsonPropertyName("debug")] bool Debug);

    private sealed record SysutilDebugUpdatePayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("debug")] bool Debug);

    private sealed record SysutilPlatformPayload(
        [property: JsonPropertyName("platform_type")] int PlatformType,
        [property: JsonPropertyName("platform_name")] string? PlatformName);

    private sealed record SysutilPlatformUpdatePayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("platform_type")] int PlatformType,
        [property: JsonPropertyName("platform_name")] string? PlatformName);

    private sealed record SysutilVideoPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("pipeline")] string? Pipeline);
}
