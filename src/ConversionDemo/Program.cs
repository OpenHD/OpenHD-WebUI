using OpenHdWebUi.FFmpeg;
using OpenHdWebUi.FileSystem;

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

        var file = Directory.GetFiles(mediaFolder, "*.mkv").First();
        var previewCreator = new PreviewCreator(file, previewFolder);
        await previewCreator.StartAsync();
        Console.WriteLine("Ready");
    }

    private static async Task PrestartAsync()
    {
        FileSystemHelpers.EnsureCurrentDirectoryIsBinaryDirectory();
        await FFmpegHelpers.EnsureFFmpegAvailableAsync();
    }
}