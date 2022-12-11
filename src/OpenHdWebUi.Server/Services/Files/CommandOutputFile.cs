using System.Text;
using Cysharp.Diagnostics;

namespace OpenHdWebUi.Server.Services.Files;

class CommandOutputFile : IFile
{
    private readonly string _command;

    public CommandOutputFile(string id, string displayName, string command)
    {
        _command = command;
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public async Task<(bool Found, byte[]? Content)> TryGetContentAsync()
    {
        var output = await ProcessX.StartAsync(_command).ToTask();
        if (output == null || output.Length == 0)
        {
            return (false, null);
        }

        var fullContentString = string.Join(Environment.NewLine, output);
        return (true, Encoding.UTF8.GetBytes(fullContentString));
    }
}