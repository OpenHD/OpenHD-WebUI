﻿using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Services.Commands;

namespace OpenHdWebUi.Server.Services.Files;

public class SystemFilesService
{
    private readonly Dictionary<string, IFile> _files;

    public SystemFilesService(IOptions<ServiceConfiguration> options)
    {
        var configuration = options.Value;

        _files = configuration.SystemFiles
            .Select(c => new FileSystemFile(c.Id, c.DisplayName, c.Path))
            .ToDictionary(c => c.Id, file => (IFile)file);
    }

    public IReadOnlyCollection<IFile> GetAllCommands()
    {
        return _files.Values;
    }

    public async Task<(bool Found, byte[]? Content)> TryGetFileContentAsync(string fileId)
    {
        if (_files.TryGetValue(fileId, out var file))
        {
            var (found, content) = await file.TryGetContentAsync();
            if (found)
            {
                return (true, content);
            }
        }

        return (false, null);
    }
}