using CleanSync.Application.DTOs;
using CleanSync.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CleanSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly BusinessPartnerSyncService _syncService;

    public SyncController(BusinessPartnerSyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("business-partners")]
    public async Task<ActionResult<SyncResultDto>> SyncBusinessPartners()
    {
        var result = await _syncService.SyncFromEcommerceToSapAsync();
        return Ok(result);
    }
}