using CleanSync.Domain.Entities;

namespace CleanSync.Domain.Interfaces;

public interface IBusinessPartnerRepository
{
    Task<IEnumerable<BusinessPartner>> GetAllAsync();
    Task<BusinessPartner?> GetByIdAsync(int id);
    Task<BusinessPartner?> GetByCardCodeAsync(string cardCode);
    Task<BusinessPartner?> GetByExternalIdAsync(string externalId, string source);
    Task<IEnumerable<BusinessPartner>> GetPendingSyncAsync();
    Task AddAsync(BusinessPartner partner);
    Task UpdateAsync(BusinessPartner partner);
    Task<bool> ExistsAsync(string cardCode);
}