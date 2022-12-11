namespace OpenHdWebUi.Server.Services.Files;

public interface IFile
{
    public string Id { get; }

    public string DisplayName { get; }

    Task<(bool Found, byte[]? Content)> TryGetContentAsync();
}