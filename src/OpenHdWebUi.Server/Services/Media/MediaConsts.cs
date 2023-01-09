namespace OpenHdWebUi.Server.Services.Media;

public static class MediaConsts
{
    private static readonly string PreviewsString = "previews";

    public static string PreviewsFsPath => Path.GetFullPath($"{PreviewsString}/");

    public static string PreviewsWebPath => $"{PreviewsString}";
}