using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Update;

public class SysutilUpdateService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan InfoReadTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CommandReadTimeout = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SysutilUpdateInfoDto> GetInfoAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.update.info.request\"}\n", InfoReadTimeout, cancellationToken);
        if (string.IsNullOrWhiteSpace(response))
        {
            return SysutilUpdateInfoDto.Unavailable();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SysutilUpdateInfoPayload>(response, JsonOptions);
            if (payload == null)
            {
                return SysutilUpdateInfoDto.Unavailable();
            }

            var message = payload.IsUpdating ? "Update is running." : "Idle";
            if (!payload.Ok)
            {
                message = "Unable to read update state.";
            }

            return new SysutilUpdateInfoDto(payload.Ok, payload.IsUpdating, message);
        }
        catch
        {
            return SysutilUpdateInfoDto.Unavailable();
        }
    }

    public async Task<SysutilUpdateRunResponseDto> RunUpdateAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("{\"type\":\"sysutil.update.request\"}\n", CommandReadTimeout, cancellationToken);
        if (string.IsNullOrWhiteSpace(response))
        {
            return new SysutilUpdateRunResponseDto(false, "Sysutils socket not available.");
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SysutilUpdateRunPayload>(response, JsonOptions);
            if (payload == null)
            {
                return new SysutilUpdateRunResponseDto(false, "Invalid response from sysutils.");
            }

            if (!payload.Accepted)
            {
                return new SysutilUpdateRunResponseDto(false, "Update request was rejected.");
            }

            return new SysutilUpdateRunResponseDto(true, "Update request accepted.");
        }
        catch
        {
            return new SysutilUpdateRunResponseDto(false, "Invalid response from sysutils.");
        }
    }

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

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCts.CancelAfter(readTimeout);
            return await reader.ReadLineAsync().WaitAsync(readCts.Token);
        }
        catch
        {
            return null;
        }
    }

    private sealed record SysutilUpdateInfoPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("is_updating")] bool IsUpdating);

    private sealed record SysutilUpdateRunPayload(
        [property: JsonPropertyName("accepted")] bool Accepted);
}
