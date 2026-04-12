using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;
using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;

namespace CleanSync.Application.Services;

public class BusinessPartnerSyncService
{
    private readonly IEcommerceBusinessPartnerService _ecommerceService;
    private readonly ISapBusinessPartnerService _sapService;
    private readonly IBusinessPartnerRepository _repository;
    private readonly ISyncLogRepository _syncLogRepository;

    public BusinessPartnerSyncService(
        IEcommerceBusinessPartnerService ecommerceService,
        ISapBusinessPartnerService sapService,
        IBusinessPartnerRepository repository,
        ISyncLogRepository syncLogRepository)
    {
        _ecommerceService = ecommerceService;
        _sapService = sapService;
        _repository = repository;
        _syncLogRepository = syncLogRepository;
    }

    public async Task<SyncResultDto> SyncFromEcommerceToSapAsync()
    {
        var result = new SyncResultDto
        {
            StartedAt = DateTime.UtcNow
        };

        var syncLog = new SyncLog
        {
            EntityType = "BusinessPartner",
            Direction = "ToSap",
            StartedAt = result.StartedAt,
            Status = SyncStatus.InProgress
        };

        try
        {
            await _syncLogRepository.AddAsync(syncLog);

            var customers = await _ecommerceService.GetCustomersAsync();
            result.TotalProcessed = customers.Count();
            syncLog.EntityCount = customers.Count();

            foreach (var customer in customers)
            {
                try
                {
                    var existingPartner = await _repository.GetByExternalIdAsync(customer.Id, customer.Source);
                    
                    if (existingPartner != null)
                    {
                        var sapPartner = MapToSapDto(customer);
                        await _sapService.UpdateAsync(existingPartner.CardCode, sapPartner);
                        existingPartner.LastSyncedAt = DateTime.UtcNow;
                        existingPartner.SyncStatus = SyncStatus.Synced;
                        await _repository.UpdateAsync(existingPartner);
                    }
                    else
                    {
                        var cardCode = await GenerateUniqueCardCodeAsync(customer);
                        var sapPartner = MapToSapDto(customer);
                        sapPartner.CardCode = cardCode;
                        var created = await _sapService.CreateAsync(sapPartner);

                        var newPartner = new BusinessPartner
                        {
                            CardCode = created.CardCode,
                            CardName = created.CardName,
                            CardType = created.CardType,
                            FederalTaxId = created.FederalTaxId,
                            Phone1 = created.Phone1,
                            Email = created.Email,
                            Address = created.Address,
                            City = created.City,
                            Country = created.Country,
                            ZipCode = created.ZipCode,
                            Source = customer.Source,
                            ExternalId = customer.Id,
                            SyncStatus = SyncStatus.Synced,
                            LastSyncedAt = DateTime.UtcNow
                        };
                        await _repository.AddAsync(newPartner);
                    }
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new SyncErrorDto
                    {
                        EntityId = customer.Id,
                        ErrorMessage = ex.Message
                    });
                }
            }

            result.Success = result.FailureCount == 0;
            result.CompletedAt = DateTime.UtcNow;
            result.Message = $"Synced {result.SuccessCount} of {result.TotalProcessed} customers";

            syncLog.CompletedAt = result.CompletedAt;
            syncLog.SuccessCount = result.SuccessCount;
            syncLog.FailureCount = result.FailureCount;
            syncLog.Status = result.Success ? SyncStatus.Synced : SyncStatus.Failed;
            if (result.Errors.Any())
            {
                syncLog.ErrorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.CompletedAt = DateTime.UtcNow;
            result.Message = $"Sync failed: {ex.Message}";
            syncLog.Status = SyncStatus.Failed;
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = result.CompletedAt;
        }

        await _syncLogRepository.UpdateAsync(syncLog);
        return result;
    }

    private async Task<string> GenerateUniqueCardCodeAsync(EcommerceCustomerDto customer)
    {
        var prefix = customer.Source == "Amazon" ? "AMZ" : "SH";
        var baseCode = $"{prefix}{customer.Email[..Math.Min(8, customer.Email.Length)].ToUpper().Replace("@", "").Replace(".", "")}";
        
        var code = baseCode;
        var counter = 1;
        
        while (await _repository.ExistsAsync(code))
        {
            code = $"{baseCode}{counter}";
            counter++;
        }
        
        return code;
    }

    private SapBusinessPartnerDto MapToSapDto(EcommerceCustomerDto customer)
    {
        var name = $"{customer.FirstName} {customer.LastName}".Trim();
        if (string.IsNullOrEmpty(name))
            name = customer.Email;

        return new SapBusinessPartnerDto
        {
            CardName = name,
            CardType = "cCustomer",
            FederalTaxId = customer.Company,
            Phone1 = customer.Phone ?? customer.DefaultAddress?.Phone,
            Email = customer.Email,
            Website = null,
            Address = customer.DefaultAddress?.Address1,
            City = customer.DefaultAddress?.City,
            Country = customer.DefaultAddress?.Country,
            ZipCode = customer.DefaultAddress?.Zip,
            GroupCode = 100
        };
    }
}