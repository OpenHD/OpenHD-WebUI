using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Settings;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<SettingFileSummaryDto>> GetSettings()
    {
        var summaries = _settingsService.GetSettingFiles();
        return Ok(summaries);
    }

    [HttpGet("{id}")]
    public ActionResult<SettingFileDto> GetSetting(string id)
    {
        var file = _settingsService.GetSettingFile(id);
        if (file == null)
        {
            return NotFound();
        }

        return Ok(file);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateSetting(string id, [FromBody] UpdateSettingFileRequest request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var updated = _settingsService.TrySaveSettingFile(id, request.Content, out var file, out var notFound, out var invalidJson);
        if (!updated)
        {
            if (notFound)
            {
                return NotFound();
            }

            if (invalidJson)
            {
                return BadRequest("The provided settings file is not valid JSON.");
            }

            return Problem("Unable to save the requested settings file.");
        }

        if (file == null)
        {
            return Problem("Unable to reload the settings file after saving.");
        }

        return Ok(file);
    }
}
