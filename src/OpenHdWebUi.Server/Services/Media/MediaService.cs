using System.Resources;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;

using Xabe.FFmpeg;

namespace OpenHdWebUi.Server.Services.Media;

public class MediaService
{
    public MediaService(IOptions<ServiceConfiguration> configuration)
    {
        MediaDirectoryFullPath = Path.GetFullPath(configuration.Value.FilesFolder);
    }

    public string MediaDirectoryFullPath { get; }

    public string PreviewsDirectoryFullPath => MediaConsts.PreviewsFsPath;

    public string[] GetMediaFilesPaths()
    {
        return Directory.GetFiles(MediaDirectoryFullPath, "*.mkv");
    }

    public async Task EnsurePreviewsCreatedAsync()
    {
        var files = GetMediaFilesPaths();
        foreach (var file in files)
        {
            //IConversion conversion = await FFmpeg.Conversions.New().;
            //IConversionResult result = await conversion.Start();
        }
    }
}