using Xabe.FFmpeg;

namespace OpenHdWebUi.FFmpeg;

public class PreviewCreator
{
    private const VideoSize DefaultVideoSize = VideoSize.Nhd;

    private readonly string _originalFullPath;
    private readonly string _previewFolderPath;

    public PreviewCreator(string originalFullPath, string previewFolderPath)
    {
        _originalFullPath = originalFullPath;
        _previewFolderPath = previewFolderPath;
    }

    public async Task StartAsync()
    {
        var outputPath = Path.Combine(_previewFolderPath, Path.GetFileNameWithoutExtension(_originalFullPath)) + ".webp";

        if (File.Exists(outputPath))
        {
            return;
        }

        //return await ToWebP(_originalFullPath, outputPath);
        var conversion = await TakeSnapshot(_originalFullPath, outputPath);
        if (conversion == null)
        {
            return;
        }

        await conversion.Start();
    }

    /// <summary>
    /// For future animated preview
    /// </summary>
    private async Task<IConversion> ToWebP(string inputPath, string outputPath)
    {
        IMediaInfo info = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(inputPath);

        IStream videoStream = info.VideoStreams
            .First()
            .SetCodec(VideoCodec.webp)
            .SetFramerate(0.2)
            .SetLoop(0)
            .SetOutputFramesCount(10)
            .SetSize(DefaultVideoSize);

        return Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream(videoStream)
            .SetOutput(outputPath);
    }

    private async Task<IConversion?> TakeSnapshot(string inputPath, string outputPath)
    {
        var info = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(inputPath);

        var videoStream = info.VideoStreams.FirstOrDefault();
        if (videoStream == null)
        {
            return null;
        }

        videoStream = videoStream
            .SetOutputFramesCount(1)
            .SetSize(DefaultVideoSize);

        // OHD can stop writing without end and duration will be 00:00:00
        if (videoStream.Duration != TimeSpan.Zero)
        {
            videoStream.SetSeek(videoStream.Duration / 3);
        }

        return Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream(videoStream)
            .SetOutputFormat(Format.webp)
            .SetOverwriteOutput(true)
            .SetOutput(outputPath);
    }
}
