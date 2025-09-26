using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Settings;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly OpenHdSettingsService _settingsService;

    public SettingsController(OpenHdSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("files")]
    public IReadOnlyCollection<SettingFileSummaryDto> GetFiles()
    {
        return _settingsService.DiscoverFiles()
            .Select(file => new SettingFileSummaryDto(file.Id, file.Name, file.RootDirectory, file.RelativePath))
            .ToArray();
    }

    [HttpGet("file")]
    public async Task<ActionResult<SettingFileContentDto>> GetFile([FromQuery] string id)
    {
        var (found, content) = await _settingsService.TryReadAsync(id);
        if (!found || content is null)
        {
            return NotFound();
        }

        if (!_settingsService.TryGetFileInfo(id, out var fileInfo))
        {
            return NotFound();
        }

        return new SettingFileContentDto(id, fileInfo!.Name, content);
    }

    [HttpPut("file")]
    public async Task<ActionResult<SettingFileContentDto>> UpdateFile([FromBody] UpdateSettingFileRequest request)
    {
        var (updated, content, error) = await _settingsService.TryUpdateAsync(request.Id, request.Content);
        if (!updated)
        {
            return BadRequest(new { error });
        }

        if (!_settingsService.TryGetFileInfo(request.Id, out var fileInfo))
        {
            return NotFound();
        }

        return new SettingFileContentDto(request.Id, fileInfo!.Name, content!);
    }
}
