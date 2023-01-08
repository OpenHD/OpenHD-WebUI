using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;

using static Nuke.Common.IO.HttpTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.CompressionTasks;

partial class Build
{
    readonly List<FfmpegBinDescription> FfmpegBinDescriptions = new()
    {
        new()
        {
            Ulr = "https://dl.cloudsmith.io/public/openhd/ffmpeg/raw/files/ffmpeg-5.1.1-armhf.zip",
            Rid = "linux-arm",
            FileName = "ffmpeg-armhf.tar.xz",
            UncompressedDirectoryName = "ffmpeg-5.1.1-armhf"
        },
        new ()
        {
            Ulr = "https://dl.cloudsmith.io/public/openhd/ffmpeg/raw/files/ffmpeg-5.1.1-arm64.zip",
            Rid = "linux-arm64",
            FileName = "ffmpeg-arm64.tar.xz",
            UncompressedDirectoryName = "ffmpeg-5.1.1-arm64"
        },
        new()
        {
            Ulr = "https://dl.cloudsmith.io/public/openhd/ffmpeg/raw/files/ffmpeg-5.1.1-amd64.zip",
            Rid = "linux-x64",
            FileName = "ffmpeg-amd64.tar.xz",
            UncompressedDirectoryName = "ffmpeg-5.1.1-amd64"
        }
    };

    readonly AbsolutePath DownloadsPath;

    Target DownloadFfmpeg => _ => _
        .After(Clean)
        .Executes(() =>
        {
            EnsureExistingDirectory(DownloadsPath);
            foreach (var binDescription in FfmpegBinDescriptions)
            {
                var fileName = DownloadsPath / binDescription.FileName;
                HttpDownloadFile(binDescription.Ulr, fileName, clientConfigurator: settings =>
                {
                    settings.Timeout = TimeSpan.FromSeconds(60);
                    return settings;
                });
            }
        });

    Target UncompressFfmpeg => _ => _
        .DependsOn(DownloadFfmpeg)
        .Executes(() =>
        {
            EnsureExistingDirectory(DownloadsPath);
            foreach (var binDescription in FfmpegBinDescriptions)
            {
                var fileName = DownloadsPath / binDescription.FileName;
                UncompressZip(fileName, DownloadsPath);
            }
        });
}

class FfmpegBinDescription
{
    public required string Ulr { get; init; }

    public required string Rid { get; init; }

    public required string FileName { get; init; }

    public required string UncompressedDirectoryName { get; init; }
}