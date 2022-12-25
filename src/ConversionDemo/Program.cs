using OpenHdWebUi.FileSystem;
using OpenHdWebUi.Server.FFmpeg;

namespace ConversionDemo;

internal class Program
{
    static async Task Main(string[] args)
    {
        await PrestartAsync();

        var mediaFolder = "C:\\testData\\media";
        FileSystemHelpers.EnsureFolderCreated(mediaFolder);

        var previewFolder = "C:\\testData\\previews";
        FileSystemHelpers.EnsureFolderCreated(previewFolder);
    }

    private static async Task PrestartAsync()
    {
        FileSystemHelpers.EnsureCurrentDirectoryIsBinaryDirectory();
        await FFmpegHelpers.EnsureFFmpegAvailableAsync();
    }
}