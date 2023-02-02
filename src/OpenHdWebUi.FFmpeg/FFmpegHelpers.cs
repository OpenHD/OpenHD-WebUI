using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg.Exceptions;

namespace OpenHdWebUi.FFmpeg;

public static class FFmpegHelpers
{
    public static async Task EnsureFFmpegAvailableAsync()
    {
        var testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(testDirectory);

        bool isNeedToDownload = false;
        try
        {
            var probe = new FFmpegBinProbe();
            Console.WriteLine(probe.Path);
        }
        catch (DirectoryNotFoundException)
        {
            isNeedToDownload = true;
            Directory.CreateDirectory(testDirectory);
        }
        catch (FFmpegNotFoundException)
        {
            isNeedToDownload = true;
        }

        if (isNeedToDownload && Environment.OSVersion.Platform != PlatformID.Unix)
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, testDirectory);
        }
    }

    private class FFmpegBinProbe : Xabe.FFmpeg.FFmpeg
    {
        public string Path => FFmpegPath;
    }
}