using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Status;

public class SysutilRfControlService
{
    private readonly ILogger<SysutilRfControlService> _logger;
    private const string SocketPath = "/run/openhd/openhd_sys.sock";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(5000);

    public SysutilRfControlService(ILogger<SysutilRfControlService> logger)
    {
        _logger = logger;
    }

    public async Task<RfControlResponse> ApplyAsync(RfControlRequest request, CancellationToken cancellationToken)
    {
        var debug = new RfControlDebugInfo
        {
            SocketAvailable = File.Exists(SocketPath)
        };
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("RF control request: interface={Interface} freq={Frequency} width={Width} mcs={Mcs} powerLevel={PowerLevel} txMw={TxMw} txIndex={TxIndex}",
            request.InterfaceName ?? string.Empty,
            request.FrequencyMhz,
            request.ChannelWidthMhz,
            request.McsIndex,
            request.PowerLevel ?? string.Empty,
            request.TxPowerMw,
            request.TxPowerIndex);

        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            _logger.LogWarning("RF control blocked: unsupported OS.");
            debug.ElapsedMs = stopwatch.ElapsedMilliseconds;
            return new RfControlResponse
            {
                Ok = false,
                Message = "RF control is only available on Linux targets.",
                Debug = debug
            };
        }

        if (!File.Exists(SocketPath))
        {
            _logger.LogWarning("RF control blocked: sysutils socket not found at {SocketPath}.", SocketPath);
            debug.ElapsedMs = stopwatch.ElapsedMilliseconds;
            return new RfControlResponse
            {
                Ok = false,
                Message = "Sysutils socket is not available.",
                Debug = debug
            };
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
        if (!string.IsNullOrWhiteSpace(request.PowerLevel))
        {
            payload["power_level"] = request.PowerLevel;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        debug.RequestPayload = json;
        var attempts = 0;
        var response = await SendRequestAsync($"{json}\n", cancellationToken);
        attempts++;
        if (string.IsNullOrWhiteSpace(response))
        {
            // Retry once in case sysutils was busy.
            await Task.Delay(200, cancellationToken);
            response = await SendRequestAsync($"{json}\n", cancellationToken);
            attempts++;
        }
        debug.Attempts = attempts;
        debug.ResponsePayload = response;
        debug.ElapsedMs = stopwatch.ElapsedMilliseconds;
        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogWarning("RF control timeout: no response from sysutils.");
            return new RfControlResponse
            {
                Ok = false,
                Message = "No response from sysutils. Ensure openhd_sys_utils is updated and running.",
                Debug = debug
            };
        }

        try
        {
            _logger.LogDebug("RF control sysutils response: {Response}", response);
            var payloadData = JsonSerializer.Deserialize<SysutilRfControlPayload>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payloadData == null)
            {
                _logger.LogWarning("RF control failed: unable to deserialize response.");
                return new RfControlResponse { Ok = false, Message = "Invalid sysutils response." };
            }

            return new RfControlResponse
            {
                Ok = payloadData.Ok,
                Message = payloadData.Message,
                Debug = debug
            };
        }
        catch
        {
            _logger.LogWarning("RF control failed: exception while parsing response.");
            return new RfControlResponse
            {
                Ok = false,
                Message = "Unable to parse sysutils response.",
                Debug = debug
            };
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
