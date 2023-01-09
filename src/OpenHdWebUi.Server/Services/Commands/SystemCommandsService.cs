using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Services.AirGround;

namespace OpenHdWebUi.Server.Services.Commands;

public class SystemCommandsService
{
    private readonly Dictionary<string, SystemCommand> _commands;

    public SystemCommandsService(
        IOptions<ServiceConfiguration> options,
        AirGroundService airGroundService)
    {
        var configuration = options.Value;

        _commands = configuration.SystemCommands
            .Where(airGroundService.IsItemVisible)
            .Select(c => new SystemCommand(c.Id, c.DisplayName, c.Command))
            .ToDictionary(c => c.Id);
    }

    public IReadOnlyCollection<SystemCommand> GetAllCommands()
    {
        return _commands.Values;
    }

    public async Task TryRunCommandAsync(string commandId)
    {
        if (_commands.TryGetValue(commandId, out var command))
        {
            await command.ExecuteAsync();
        }
    }
}