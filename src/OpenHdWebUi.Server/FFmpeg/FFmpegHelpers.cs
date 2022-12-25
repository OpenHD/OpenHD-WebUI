using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg.Exceptions;

namespace OpenHdWebUi.Server.FFmpeg;

public static class FFmpegHelpers
{
    public static async Task EnsureFFmpegAvailableAsync()
    {
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

        if (isNeedToDownload)
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        }
    }

    private class FFmpegBinProbe : Xabe.FFmpeg.FFmpeg
    {
        public string Path => FFmpegPath;
    }
}