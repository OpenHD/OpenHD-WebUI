using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Services.Commands;

public class SystemCommandsService
{
    private Dictionary<string, SystemCommand> _commands;

    public SystemCommandsService(IOptions<ServiceConfiguration> options)
    {
        var configuration = options.Value;

        _commands = configuration.SystemCommands
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

    public void RebootSystem()
    {
        ExecuteSystemctlCommand("reboot");
    }

    public void ShutdownSystem()
    {
        ExecuteSystemctlCommand("poweroff");
    }

    public void RestartQOpenHd()
    {
        ExecuteSystemctlCommand("restart qopenhd");
    }

    public void RestartOpenHd()
    {
        ExecuteSystemctlCommand("restart openhd");
    }

    private string ExecuteSystemctlCommand(string args)
    {
        return Execute("systemctl", args);
    }

    private string Execute(string name, string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = name,
            Arguments = args,
            // WorkingDirectory =
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8
        };

        var process = new Process { StartInfo = startInfo };
        var builder = new StringBuilder();
        process.OutputDataReceived += (sender, eventArgs) => builder.AppendLine(eventArgs.Data);
        process.ErrorDataReceived += (sender, eventArgs) => builder.AppendLine(eventArgs.Data);
        process.Start();
        process.WaitForExit(TimeSpan.FromSeconds(10));
        return builder.ToString();
    }
}