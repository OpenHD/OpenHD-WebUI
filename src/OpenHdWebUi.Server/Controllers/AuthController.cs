using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (LinuxPasswordValidator.Validate(request.Username, request.Password))
        {
            return Ok();
        }

        return Unauthorized();
    }
}

static class LinuxPasswordValidator
{
    [DllImport("libc", SetLastError = true)]
    private static extern IntPtr crypt(string key, string salt);

    public static bool Validate(string username, string password)
    {
        try
        {
            foreach (var line in System.IO.File.ReadLines("/etc/shadow"))
            {
                if (!line.StartsWith(username + ":", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line.Split(':');
                var hash = parts.Length > 1 ? parts[1] : string.Empty;
                if (string.IsNullOrEmpty(hash) || hash == "!" || hash == "*")
                {
                    return false;
                }

                var ptr = crypt(password, hash);
                if (ptr == IntPtr.Zero)
                {
                    return false;
                }

                var result = Marshal.PtrToStringAnsi(ptr);
                return string.Equals(result, hash, StringComparison.Ordinal);
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}