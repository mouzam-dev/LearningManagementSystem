using LMS.WebAPI.Services.Sunnah;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// SuperAdmin-only control of the hadith data: trigger a fresh harvest from the
/// source APIs and poll its progress. The harvest runs in the background so the
/// request returns immediately.
/// </summary>
[ApiController]
[Route("api/admin/hadith")]
[Authorize(Roles = "SuperAdmin")]
public class AdminHadithController : ControllerBase
{
    private readonly HadithHarvestService _harvest;
    private readonly HadithHarvestStatus _status;

    public AdminHadithController(HadithHarvestService harvest, HadithHarvestStatus status)
    {
        _harvest = harvest;
        _status = status;
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        var started = _harvest.Start();
        return started
            ? Accepted(_status.Snapshot())
            : Conflict(new { message = "A refresh is already running.", status = _status.Snapshot() });
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(_status.Snapshot());
}
