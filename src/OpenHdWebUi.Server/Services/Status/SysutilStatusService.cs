using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Status;

public class SysutilStatusService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(700);

    public async Task<OpenHdStatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return Unavailable();
        }

        if (!File.Exists(SocketPath))
        {
            return Unavailable();
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
            var payload = Encoding.UTF8.GetBytes("{\"type\":\"sysutil.status.request\"}\n");
            await stream.WriteAsync(payload, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
            string? line;
            using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                readCts.CancelAfter(ReadTimeout);
                line = await reader.ReadLineAsync().WaitAsync(readCts.Token);
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                return Unavailable();
            }

            var payloadData = JsonSerializer.Deserialize<SysutilStatusPayload>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payloadData == null)
            {
                return Unavailable();
            }

            return new OpenHdStatusDto(
                true,
                payloadData.HasData,
                payloadData.HasError,
                payloadData.State,
                payloadData.Description,
                payloadData.Message,
                payloadData.Severity,
                payloadData.UpdatedMs);
        }
        catch
        {
            return Unavailable();
        }
    }

    private static OpenHdStatusDto Unavailable()
    {
        return new OpenHdStatusDto(false, false, false, null, null, null, 0, 0);
    }

    private sealed record SysutilStatusPayload(
        [property: JsonPropertyName("has_data")] bool HasData,
        [property: JsonPropertyName("has_error")] bool HasError,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("severity")] int Severity,
        [property: JsonPropertyName("updated_ms")] long UpdatedMs);
}
