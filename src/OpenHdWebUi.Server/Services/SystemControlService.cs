using System.Diagnostics;
using System.Text;

namespace OpenHdWebUi.Server.Services;

public class SystemControlService
{
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