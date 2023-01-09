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
                dirInfo.UnixFileMode = Consts.Mode0777;
            }

            return;
        }

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            Directory.CreateDirectory(fullPath, Consts.Mode0777);
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
        var exeFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        return Path.GetDirectoryName(exeFileName);
    }
}