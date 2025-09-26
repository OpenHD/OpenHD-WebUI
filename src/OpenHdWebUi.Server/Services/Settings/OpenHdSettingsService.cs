using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Services.Settings;

public class OpenHdSettingsService
{
    private readonly ILogger<OpenHdSettingsService> _logger;
    private readonly IReadOnlyCollection<string> _settingsDirectories;

    public OpenHdSettingsService(IOptions<ServiceConfiguration> options, ILogger<OpenHdSettingsService> logger)
    {
        _logger = logger;
        _settingsDirectories = (options.Value.SettingsDirectories ?? new List<string>())
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => Path.GetFullPath(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<SettingFileInfo> DiscoverFiles()
    {
        var files = new ConcurrentDictionary<string, SettingFileInfo>(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(_settingsDirectories, directory =>
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    _logger.LogDebug("Settings directory {Directory} not found", directory);
                    return;
                }

                foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
                {
                    var fullPath = Path.GetFullPath(file);
                    if (!IsPathAllowed(fullPath))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(directory, fullPath);
                    var info = new SettingFileInfo(
                        BuildId(fullPath),
                        Path.GetFileName(fullPath),
                        fullPath,
                        directory,
                        relativePath);

                    files[info.Id] = info;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate settings files in {Directory}", directory);
            }
        });

        return files.Values
            .OrderBy(file => file.RootDirectory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetFileInfo(string id, out SettingFileInfo? fileInfo)
    {
        fileInfo = null;
        if (!TryDecodeId(id, out var fullPath))
        {
            return false;
        }

        fileInfo = DiscoverFiles().FirstOrDefault(file =>
            string.Equals(file.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));

        return fileInfo != null;
    }

    public async Task<(bool Found, string? Content)> TryReadAsync(string id)
    {
        if (!TryGetFileInfo(id, out var fileInfo))
        {
            return (false, null);
        }

        try
        {
            var text = await File.ReadAllTextAsync(fileInfo!.FullPath);
            return (true, TryFormatJson(text));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read settings file {File}", fileInfo!.FullPath);
            return (false, null);
        }
    }

    public async Task<(bool Updated, string? Content, string? Error)> TryUpdateAsync(string id, string rawContent)
    {
        if (!TryGetFileInfo(id, out var fileInfo))
        {
            return (false, null, "Settings file not found.");
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(rawContent);
            var formatted = JsonSerializer.Serialize(jsonDocument.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(fileInfo!.FullPath, formatted + Environment.NewLine);
            return (true, formatted, null);
        }
        catch (JsonException jsonException)
        {
            _logger.LogWarning(jsonException, "Invalid JSON provided for {File}", fileInfo!.FullPath);
            return (false, null, "The provided content is not valid JSON.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings file {File}", fileInfo!.FullPath);
            return (false, null, "Failed to save the settings file.");
        }
    }

    private bool TryDecodeId(string id, out string fullPath)
    {
        fullPath = string.Empty;
        try
        {
            var decodedBytes = WebEncoders.Base64UrlDecode(id);
            var path = Encoding.UTF8.GetString(decodedBytes);
            var absolutePath = Path.GetFullPath(path);

            if (!IsPathAllowed(absolutePath) || !File.Exists(absolutePath))
            {
                return false;
            }

            fullPath = absolutePath;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsPathAllowed(string path)
    {
        foreach (var directory in _settingsDirectories)
        {
            try
            {
                var relative = Path.GetRelativePath(directory, path);
                if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
                {
                    return true;
                }
            }
            catch
            {
                // ignored - we'll keep checking other directories
            }
        }

        return false;
    }

    private static string TryFormatJson(string content)
    {
        try
        {
            using var jsonDocument = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(jsonDocument.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return content;
        }
    }

    public static string BuildId(string fullPath)
    {
        var bytes = Encoding.UTF8.GetBytes(fullPath);
        return WebEncoders.Base64UrlEncode(bytes);
    }
}

public record SettingFileInfo(string Id, string Name, string FullPath, string RootDirectory, string RelativePath);
