using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Camera;

public class SysutilCameraService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(900);

    public async Task<SysutilCameraInfoDto> GetCameraInfoAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return new SysutilCameraInfoDto(false, false, 0);
        }

        if (!File.Exists(SocketPath))
        {
            return new SysutilCameraInfoDto(false, false, 0);
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
                return new SysutilCameraInfoDto(false, false, 0);
            }

            var payloadData = JsonSerializer.Deserialize<SysutilSettingsPayload>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payloadData == null || !payloadData.Ok)
            {
                return new SysutilCameraInfoDto(false, false, 0);
            }

            return new SysutilCameraInfoDto(true, payloadData.HasCameraType, payloadData.CameraType);
        }
        catch
        {
            return new SysutilCameraInfoDto(false, false, 0);
        }
    }

    public async Task<CameraSetupResponseDto> ApplyCameraSetupAsync(int cameraType, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return new CameraSetupResponseDto(false, false, "Unsupported platform.");
        }

        if (!File.Exists(SocketPath))
        {
            return new CameraSetupResponseDto(false, false, "Sysutils socket not available.");
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
                $"{{\"type\":\"sysutil.camera.setup.request\",\"camera_type\":{cameraType}}}\n");
            await stream.WriteAsync(payload, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, leaveOpen: true);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCts.CancelAfter(ReadTimeout);
            var line = await reader.ReadLineAsync().WaitAsync(readCts.Token);

            if (string.IsNullOrWhiteSpace(line))
            {
                return new CameraSetupResponseDto(false, false, "No response from sysutils.");
            }

            var payloadData = JsonSerializer.Deserialize<CameraSetupPayload>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payloadData == null)
            {
                return new CameraSetupResponseDto(false, false, "Invalid response from sysutils.");
            }

            return new CameraSetupResponseDto(
                payloadData.Ok,
                payloadData.Applied,
                payloadData.Message);
        }
        catch
        {
            return new CameraSetupResponseDto(false, false, "Failed to contact sysutils.");
        }
    }

    private sealed record SysutilSettingsPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("has_camera_type")] bool HasCameraType,
        [property: JsonPropertyName("camera_type")] int CameraType);

    private sealed record CameraSetupPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("applied")] bool Applied,
        [property: JsonPropertyName("message")] string? Message);
}
