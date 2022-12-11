namespace OpenHdWebUi.Server.Services.Files;

public class FileSystemFile : IFile
{
    private readonly string _path;

    public FileSystemFile(string id, string displayName, string path)
    {
        _path = Path.GetFullPath(path);
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; set; }

    public async Task<(bool Found, byte[]? Content)> TryGetContentAsync()
    {
        if (!Path.Exists(_path))
        {
            return (false, null);
        }

        var content = await File.ReadAllBytesAsync(_path);
        return (true, content);
    }
}