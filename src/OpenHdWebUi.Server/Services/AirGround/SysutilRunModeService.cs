using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.AirGround;

public class SysutilRunModeService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(2);

    public async Task<RunModeInfoDto> GetRunModeAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return new RunModeInfoDto(false, "unknown");
        }

        if (!File.Exists(SocketPath))
        {
            return new RunModeInfoDto(false, "unknown");
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
            var payload = Encoding.UTF8.GetBytes("{\"type\":\"sysutil.settings.request\"}\n");
            await stream.WriteAsync(payload, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, leaveOpen: true);
            string? line;
            using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                readCts.CancelAfter(ReadTimeout);
                line = await reader.ReadLineAsync().WaitAsync(readCts.Token);
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                return new RunModeInfoDto(false, "unknown");
            }

            var payloadData = JsonSerializer.Deserialize<SysutilSettingsPayload>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payloadData == null || !payloadData.Ok)
            {
                return new RunModeInfoDto(false, "unknown");
            }

            var mode = payloadData.HasRunMode ? payloadData.RunMode : "unknown";
            return new RunModeInfoDto(true, mode);
        }
        catch
        {
            return new RunModeInfoDto(false, "unknown");
        }
    }

    public async Task<RunModeUpdateResponseDto> SetRunModeAsync(string mode, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return new RunModeUpdateResponseDto(false, "Unsupported platform.", null);
        }

        if (mode != "air" && mode != "ground")
        {
            return new RunModeUpdateResponseDto(false, "Invalid run mode.", null);
        }

        if (!File.Exists(SocketPath))
        {
            return new RunModeUpdateResponseDto(false, "Sysutils socket not available.", null);
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
            var payload = Encoding.UTF8.GetBytes(
                $"{{\"type\":\"sysutil.settings.update\",\"run_mode\":\"{mode}\"}}\n");
            await stream.WriteAsync(payload, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, leaveOpen: true);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCts.CancelAfter(ReadTimeout);
            var line = await reader.ReadLineAsync().WaitAsync(readCts.Token);

            if (string.IsNullOrWhiteSpace(line))
            {
                return new RunModeUpdateResponseDto(false, "No response from sysutils.", null);
            }

            var payloadData = JsonSerializer.Deserialize<SysutilUpdatePayload>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payloadData == null || !payloadData.Ok)
            {
                return new RunModeUpdateResponseDto(false, "Sysutils rejected the update.", null);
            }

            return new RunModeUpdateResponseDto(true, null, mode);
        }
        catch
        {
            return new RunModeUpdateResponseDto(false, "Failed to contact sysutils.", null);
        }
    }

    private sealed record SysutilSettingsPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("has_run_mode")] bool HasRunMode,
        [property: JsonPropertyName("run_mode")] string RunMode);

    private sealed record SysutilUpdatePayload(
        [property: JsonPropertyName("ok")] bool Ok);
}
