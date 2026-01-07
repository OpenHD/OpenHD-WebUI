using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Partitions;

public class PartitionService
{
    public PartitionReportDto GetPartitions()
    {
        var rows = ReadLsblkRows();
        var disks = BuildDiskReports(rows);
        return new PartitionReportDto(disks);
    }

    private static List<LsblkRow> ReadLsblkRows()
    {
        var output = RunCommand("lsblk -b -P -o NAME,TYPE,SIZE,START,FSTYPE,MOUNTPOINT,PKNAME");
        var result = new List<LsblkRow>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = ParseKeyValues(line);
            if (!fields.TryGetValue("NAME", out var name) ||
                !fields.TryGetValue("TYPE", out var type))
            {
                continue;
            }

            fields.TryGetValue("SIZE", out var sizeRaw);
            fields.TryGetValue("START", out var startRaw);
            fields.TryGetValue("FSTYPE", out var fstype);
            fields.TryGetValue("MOUNTPOINT", out var mountpoint);
            fields.TryGetValue("PKNAME", out var parent);

            var size = ParseLong(sizeRaw);
            var start = ParseLong(startRaw);

            result.Add(new LsblkRow(
                name.Trim(),
                type.Trim(),
                size,
                start,
                string.IsNullOrWhiteSpace(fstype) ? null : fstype,
                string.IsNullOrWhiteSpace(mountpoint) ? null : mountpoint,
                string.IsNullOrWhiteSpace(parent) ? null : parent));
        }

        return result;
    }

    private static IReadOnlyCollection<PartitionDiskDto> BuildDiskReports(
        List<LsblkRow> rows)
    {
        var diskRows = rows
            .Where(r => r.Type == "disk")
            .ToDictionary(r => r.Name, r => r);

        var partRows = rows.Where(r => r.Type == "part").ToList();
        var disks = new List<PartitionDiskDto>();

        foreach (var disk in diskRows.Values.OrderBy(d => d.Name))
        {
            var parts = partRows
                .Where(p => string.Equals(p.Parent, disk.Name, StringComparison.Ordinal))
                .OrderBy(p => p.StartBytes)
                .ToList();

            var partitions = parts.Select(p => new PartitionEntryDto(
                "/dev/" + p.Name,
                p.Mountpoint,
                p.Fstype,
                p.SizeBytes,
                p.StartBytes)).ToList();

            var segments = BuildSegments(disk.SizeBytes, parts);

            disks.Add(new PartitionDiskDto(
                "/dev/" + disk.Name,
                disk.SizeBytes,
                segments,
                partitions));
        }

        return disks;
    }

    private static List<PartitionSegmentDto> BuildSegments(long diskSize, List<LsblkRow> parts)
    {
        var segments = new List<PartitionSegmentDto>();
        var cursor = 0L;
        foreach (var part in parts.OrderBy(p => p.StartBytes))
        {
            if (part.StartBytes > cursor)
            {
                segments.Add(new PartitionSegmentDto(
                    "free",
                    null,
                    null,
                    null,
                    cursor,
                    part.StartBytes - cursor));
            }

            segments.Add(new PartitionSegmentDto(
                "partition",
                "/dev/" + part.Name,
                part.Mountpoint,
                part.Fstype,
                part.StartBytes,
                part.SizeBytes));

            cursor = part.StartBytes + part.SizeBytes;
        }

        if (diskSize > cursor)
        {
            segments.Add(new PartitionSegmentDto(
                "free",
                null,
                null,
                null,
                cursor,
                diskSize - cursor));
        }

        return segments;
    }

    private static long ParseLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static Dictionary<string, string> ParseKeyValues(string line)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(line, "(?<key>\\w+)=\"(?<value>[^\"]*)\""))
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;
            result[key] = value;
        }
        return result;
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

    private sealed record LsblkRow(
        string Name,
        string Type,
        long SizeBytes,
        long StartBytes,
        string? Fstype,
        string? Mountpoint,
        string? Parent);
}
