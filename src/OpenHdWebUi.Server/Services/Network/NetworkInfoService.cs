using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Network;

public class NetworkInfoService
{
    public NetworkInfoDto GetNetworkInfo()
    {
        var wifi = GetWifiInterfaces();
        var ethernet = GetEthernetInterfaces(wifi.Select(w => w.Name));
        return new NetworkInfoDto(wifi, ethernet);
    }

    public async Task SetEthernetIpAsync(string iface, string ip, string netmask)
    {
        var command = $"ifconfig {iface} {ip} netmask {netmask}";
        await RunCommandAsync(command);
    }

    private static WifiInterfaceDto[] GetWifiInterfaces()
    {
        var output = RunCommand("iwconfig 2>/dev/null");
        var result = new List<WifiInterfaceDto>();
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Contains("no wireless extensions")) continue;
            if (!char.IsWhiteSpace(line[0]))
            {
                var iface = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
                var driverPath = RunCommand($"readlink -f /sys/class/net/{iface}/device/driver 2>/dev/null").Trim();
                var driver = Path.GetFileName(driverPath);
                result.Add(new WifiInterfaceDto(iface, driver));
            }
        }
        return result.ToArray();
    }

    private static EthernetInterfaceDto[] GetEthernetInterfaces(IEnumerable<string> wifiNames)
    {
        var wifiSet = new HashSet<string>(wifiNames);
        var output = RunCommand("ifconfig 2>/dev/null");
        var blocks = output.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<EthernetInterfaceDto>();
        foreach (var block in blocks)
        {
            var lines = block.Split('\n');
            if (lines.Length == 0) continue;
            var name = lines[0].Split(':')[0];
            if (ShouldIgnoreInterface(name))
            {
                continue;
            }
            if (wifiSet.Contains(name)) continue;
            var ipMatch = Regex.Match(block, @"inet (addr:)?(?<ip>\d+\.\d+\.\d+\.\d+)");
            var maskMatch = Regex.Match(block, @"netmask (?<mask>\d+\.\d+\.\d+\.\d+)|Mask:(?<mask>\d+\.\d+\.\d+\.\d+)");
            var ip = ipMatch.Success ? ipMatch.Groups["ip"].Value : string.Empty;
            var mask = maskMatch.Success ? maskMatch.Groups["mask"].Value : string.Empty;
            result.Add(new EthernetInterfaceDto(name, ip, mask));
        }
        return result.ToArray();
    }

    private static bool ShouldIgnoreInterface(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        name = name.Trim();

        if (name.Equals("lo", StringComparison.OrdinalIgnoreCase) || name.StartsWith("lo@", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var ignoredPrefixes = new[] { "docker", "br-", "veth", "tun", "tap" };
        foreach (var prefix in ignoredPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task RunCommandAsync(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            ArgumentList = { "-c", command },
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var process = Process.Start(psi);
        if (process == null) return;
        await process.StandardOutput.ReadToEndAsync();
        await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
    }

    private static string RunCommand(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            ArgumentList = { "-c", command },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(psi);
        if (process == null) return string.Empty;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return output + error;
    }
}
