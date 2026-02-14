using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Status;

public class SysutilRfControlService
{
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(1000);

    public async Task<RfControlResponse> ApplyAsync(RfControlRequest request, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return new RfControlResponse { Ok = false, Message = "RF control is only available on Linux targets." };
        }

        if (!File.Exists(SocketPath))
        {
            return new RfControlResponse { Ok = false, Message = "Sysutils socket is not available." };
        }

        var payload = new Dictionary<string, object?>
        {
            ["type"] = "sysutil.link.control"
        };

        if (!string.IsNullOrWhiteSpace(request.InterfaceName))
        {
            payload["interface"] = request.InterfaceName;
        }
        if (request.FrequencyMhz.HasValue)
        {
            payload["frequency_mhz"] = request.FrequencyMhz.Value;
        }
        if (request.ChannelWidthMhz.HasValue)
        {
            payload["channel_width_mhz"] = request.ChannelWidthMhz.Value;
        }
        if (request.McsIndex.HasValue)
        {
            payload["mcs_index"] = request.McsIndex.Value;
        }
        if (request.TxPowerMw.HasValue)
        {
            payload["tx_power_mw"] = request.TxPowerMw.Value;
        }
        if (request.TxPowerIndex.HasValue)
        {
            payload["tx_power_index"] = request.TxPowerIndex.Value;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var response = await SendRequestAsync($"{json}\n", cancellationToken);
        if (string.IsNullOrWhiteSpace(response))
        {
            return new RfControlResponse { Ok = false, Message = "No response from sysutils." };
        }

        try
        {
            var payloadData = JsonSerializer.Deserialize<SysutilRfControlPayload>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payloadData == null)
            {
                return new RfControlResponse { Ok = false, Message = "Invalid sysutils response." };
            }

            return new RfControlResponse
            {
                Ok = payloadData.Ok,
                Message = payloadData.Message
            };
        }
        catch
        {
            return new RfControlResponse { Ok = false, Message = "Unable to parse sysutils response." };
        }
    }

    private static async Task<string?> SendRequestAsync(string payload, CancellationToken cancellationToken)
    {
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

    private sealed record SysutilRfControlPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("message")] string? Message);
}
