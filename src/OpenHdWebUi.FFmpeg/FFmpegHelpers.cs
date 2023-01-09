using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg.Exceptions;

namespace OpenHdWebUi.FFmpeg;

public static class FFmpegHelpers
{
    public static async Task EnsureFFmpegAvailableAsync()
    {
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg"));

        bool isNeedToDownload = false;
        try
        {
            var probe = new FFmpegBinProbe();
            Console.WriteLine(probe.Path);
        }
        catch (FFmpegNotFoundException)
        {
            isNeedToDownload = true;
        }

        if (isNeedToDownload || Environment.OSVersion.Platform != PlatformID.Unix)
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        }
    }

    private class FFmpegBinProbe : Xabe.FFmpeg.FFmpeg
    {
        public string Path => FFmpegPath;
    }
}