using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Partitions;

public class SysutilPartitionService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(900);

    public async Task<PartitionReportDto> GetPartitionsAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return new PartitionReportDto(Array.Empty<PartitionDiskDto>(), null);
        }

        if (!File.Exists(SocketPath))
        {
            return new PartitionReportDto(Array.Empty<PartitionDiskDto>(), null);
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
            var payload = Encoding.UTF8.GetBytes("{\"type\":\"sysutil.partitions.request\"}\n");
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
                return new PartitionReportDto(Array.Empty<PartitionDiskDto>(), null);
            }

            var payloadData = JsonSerializer.Deserialize<PartitionReportPayload>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payloadData?.Disks == null)
            {
                return new PartitionReportDto(Array.Empty<PartitionDiskDto>(), null);
            }

            var disks = payloadData.Disks.Select(d => new PartitionDiskDto(
                d.Name ?? string.Empty,
                d.SizeBytes,
                d.Segments?.Select(s => new PartitionSegmentDto(
                    s.Kind ?? "unknown",
                    s.Device,
                    s.Mountpoint,
                    s.Fstype,
                    s.Label,
                    s.StartBytes,
                    s.SizeBytes)).ToArray() ?? Array.Empty<PartitionSegmentDto>(),
                d.Partitions?.Select(p => new PartitionEntryDto(
                    p.Device ?? string.Empty,
                    p.Mountpoint,
                    p.Fstype,
                    p.Label,
                    p.SizeBytes,
                    p.StartBytes)).ToArray() ?? Array.Empty<PartitionEntryDto>()
            )).ToArray();

            PartitionResizableDto? resizable = null;
            if (payloadData.Resizable != null &&
                !string.IsNullOrWhiteSpace(payloadData.Resizable.Device))
            {
                resizable = new PartitionResizableDto(
                    payloadData.Resizable.Device,
                    payloadData.Resizable.Label,
                    payloadData.Resizable.Fstype,
                    payloadData.Resizable.FreeBytes);
            }

            return new PartitionReportDto(disks, resizable);
        }
        catch
        {
            return new PartitionReportDto(Array.Empty<PartitionDiskDto>(), null);
        }
    }

    public async Task<bool> SendResizeRequestAsync(bool resize, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return false;
        }

        if (!File.Exists(SocketPath))
        {
            return false;
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
            var choice = resize ? "yes" : "no";
            var payload = Encoding.UTF8.GetBytes(
                $"{{\"type\":\"sysutil.partition.resize.request\",\"choice\":\"{choice}\"}}\n");
            await stream.WriteAsync(payload, cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, leaveOpen: true);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCts.CancelAfter(ReadTimeout);
            _ = await reader.ReadLineAsync().WaitAsync(readCts.Token);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record PartitionReportPayload(
        [property: JsonPropertyName("disks")] PartitionDiskPayload[]? Disks,
        [property: JsonPropertyName("resizable")] PartitionResizablePayload? Resizable);

    private sealed record PartitionDiskPayload(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("sizeBytes")] long SizeBytes,
        [property: JsonPropertyName("segments")] PartitionSegmentPayload[]? Segments,
        [property: JsonPropertyName("partitions")] PartitionEntryPayload[]? Partitions);

    private sealed record PartitionSegmentPayload(
        [property: JsonPropertyName("kind")] string? Kind,
        [property: JsonPropertyName("device")] string? Device,
        [property: JsonPropertyName("mountpoint")] string? Mountpoint,
        [property: JsonPropertyName("fstype")] string? Fstype,
        [property: JsonPropertyName("label")] string? Label,
        [property: JsonPropertyName("startBytes")] long StartBytes,
        [property: JsonPropertyName("sizeBytes")] long SizeBytes);

    private sealed record PartitionEntryPayload(
        [property: JsonPropertyName("device")] string? Device,
        [property: JsonPropertyName("mountpoint")] string? Mountpoint,
        [property: JsonPropertyName("fstype")] string? Fstype,
        [property: JsonPropertyName("label")] string? Label,
        [property: JsonPropertyName("sizeBytes")] long SizeBytes,
        [property: JsonPropertyName("startBytes")] long StartBytes);

    private sealed record PartitionResizablePayload(
        [property: JsonPropertyName("device")] string? Device,
        [property: JsonPropertyName("label")] string? Label,
        [property: JsonPropertyName("fstype")] string? Fstype,
        [property: JsonPropertyName("freeBytes")] long FreeBytes);
}
