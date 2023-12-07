namespace OpenHdWebUi.FileSystem;

public static class FileSystemHelpers
{
    public static void EnsureFolderCreated(string fullPath)
    {
        if (Directory.Exists(fullPath))
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var dirInfo = new DirectoryInfo(fullPath);
#pragma warning disable CA1416
                dirInfo.UnixFileMode = Consts.Mode0777;
#pragma warning restore CA1416
            }

            return;
        }

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
#pragma warning disable CA1416
            Directory.CreateDirectory(fullPath, Consts.Mode0777);
#pragma warning restore CA1416
        }
        else
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    public static void EnsureCurrentDirectoryIsBinaryDirectory()
    {
        var currentDir = GetExeDirectory();
        Directory.SetCurrentDirectory(currentDir);
    }

    private static string GetExeDirectory()
    {
        var exeFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
        return Path.GetDirectoryName(exeFileName)!;
    }
}