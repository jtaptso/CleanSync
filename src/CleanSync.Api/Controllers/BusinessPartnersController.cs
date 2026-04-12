using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessPartnersController : ControllerBase
{
    private readonly IBusinessPartnerRepository _repository;
    private readonly ISyncLogRepository _syncLogRepository;

    public BusinessPartnersController(IBusinessPartnerRepository repository, ISyncLogRepository syncLogRepository)
    {
        _repository = repository;
        _syncLogRepository = syncLogRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusinessPartner>>> GetAll()
    {
        var partners = await _repository.GetAllAsync();
        return Ok(partners);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BusinessPartner>> GetById(int id)
    {
        var partner = await _repository.GetByIdAsync(id);
        if (partner == null)
            return NotFound();
        return Ok(partner);
    }

    [HttpGet("sync-logs")]
    public async Task<ActionResult<IEnumerable<SyncLog>>> GetSyncLogs()
    {
        var logs = await _syncLogRepository.GetAllAsync();
        return Ok(logs);
    }
}