using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Models;

namespace OpenHdWebUi.Server.Services.Settings;

public class SettingsService
{
    private static readonly string[] ExcludedDirectories =
    {
        "web-ui"
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly string[] _settingsRoots;

    public SettingsService(ILogger<SettingsService> logger, IOptions<ServiceConfiguration> configuration)
    {
        _logger = logger;
        _settingsRoots = configuration.Value.SettingsDirectories?
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => Path.GetFullPath(directory))
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        if (_settingsRoots.Length == 0)
        {
            _logger.LogInformation("No settings directories configured or available.");
        }
        else
        {
            foreach (var root in _settingsRoots)
            {
                _logger.LogInformation("Settings directory registered: {SettingsDirectory}", root);
            }
        }
    }

    public IReadOnlyCollection<SettingFileSummaryDto> GetSettingFiles()
    {
        var summaries = new List<SettingFileSummaryDto>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in _settingsRoots)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
                {
                    var fullPath = Path.GetFullPath(file);
                    if (!IsPathInRoot(fullPath) || !seenFiles.Add(fullPath))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(root, fullPath);
                    if (IsExcluded(relativePath))
                    {
                        continue;
                    }

                    var normalizedRelativePath = NormalizeSeparators(relativePath);
                    var category = ExtractCategory(normalizedRelativePath);
                    var id = EncodePath(fullPath);
                    summaries.Add(new SettingFileSummaryDto(id, Path.GetFileName(fullPath), normalizedRelativePath, category));
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to enumerate settings directory {SettingsDirectory}", root);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while enumerating settings directory {SettingsDirectory}", root);
            }
        }

        return summaries
            .OrderBy(summary => summary.Category ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SettingFileDto? GetSettingFile(string id)
    {
        var fullPath = DecodeAndValidate(id);
        if (fullPath == null || !File.Exists(fullPath))
        {
            return null;
        }

        var root = ResolveRoot(fullPath);
        var relativePath = root != null ? Path.GetRelativePath(root, fullPath) : Path.GetFileName(fullPath);
        if (root != null && IsExcluded(relativePath))
        {
            return null;
        }

        var normalizedRelativePath = NormalizeSeparators(relativePath);
        var category = ExtractCategory(normalizedRelativePath);
        try
        {
            var content = File.ReadAllText(fullPath);
            return new SettingFileDto(id, Path.GetFileName(fullPath), normalizedRelativePath, category, content);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read settings file {SettingsFile}", fullPath);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while reading settings file {SettingsFile}", fullPath);
            return null;
        }
    }

    public bool TrySaveSettingFile(string id, string content, out SettingFileDto? updated, out bool notFound, out bool invalidJson)
    {
        updated = null;
        notFound = false;
        invalidJson = false;
        var fullPath = DecodeAndValidate(id);
        if (fullPath == null || !File.Exists(fullPath))
        {
            notFound = true;
            return false;
        }

        var root = ResolveRoot(fullPath);
        var relativePath = root != null ? Path.GetRelativePath(root, fullPath) : null;
        if (relativePath != null && IsExcluded(relativePath))
        {
            notFound = true;
            return false;
        }

        if (!IsValidJson(content))
        {
            invalidJson = true;
            _logger.LogWarning("Attempted to write invalid JSON to settings file {SettingsFile}", fullPath);
            return false;
        }

        try
        {
            File.WriteAllText(fullPath, content);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to write settings file {SettingsFile}", fullPath);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while writing settings file {SettingsFile}", fullPath);
            return false;
        }

        updated = GetSettingFile(id);
        return updated != null;
    }

    private static string? ExtractCategory(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = NormalizeSeparators(relativePath);
        var separatorIndex = normalized.IndexOf('/');
        return separatorIndex > 0 ? normalized[..separatorIndex] : null;
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/');
    }

    private string? DecodeAndValidate(string id)
    {
        try
        {
            var pathBytes = WebEncoders.Base64UrlDecode(id);
            var decodedPath = Encoding.UTF8.GetString(pathBytes);
            var fullPath = Path.GetFullPath(decodedPath);
            return IsPathInRoot(fullPath) ? fullPath : null;
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to decode settings file id: {Id}", id);
            return null;
        }
    }

    private bool IsPathInRoot(string fullPath)
    {
        return _settingsRoots.Any(root => IsPathUnderRoot(fullPath, root));
    }

    private string EncodePath(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(path));
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private string? ResolveRoot(string fullPath)
    {
        return _settingsRoots.FirstOrDefault(root => IsPathUnderRoot(fullPath, root));
    }

    private static bool IsExcluded(string relativePath)
    {
        var segments = NormalizeSeparators(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => ExcludedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsPathUnderRoot(string fullPath, string root)
    {
        var relative = Path.GetRelativePath(root, fullPath);
        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

    private static bool IsValidJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind != JsonValueKind.Undefined;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
