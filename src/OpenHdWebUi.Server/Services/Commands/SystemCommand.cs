using Cysharp.Diagnostics;

namespace OpenHdWebUi.Server.Services.Commands;

public class SystemCommand
{
    private readonly string _command;

    public SystemCommand(string id, string displayName, string command)
    {
        _command = command;
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public async Task ExecuteAsync()
    {
        await ProcessX.StartAsync(_command).WaitAsync();
    }
}