using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;
using CleanSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanSync.Infrastructure.Repositories;

public class BusinessPartnerRepository : IBusinessPartnerRepository
{
    private readonly CleanSyncDbContext _context;

    public BusinessPartnerRepository(CleanSyncDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BusinessPartner>> GetAllAsync()
    {
        return await _context.BusinessPartners.OrderByDescending(b => b.LastSyncedAt).ToListAsync();
    }

    public async Task<BusinessPartner?> GetByIdAsync(int id)
    {
        return await _context.BusinessPartners.FindAsync(id);
    }

    public async Task<BusinessPartner?> GetByCardCodeAsync(string cardCode)
    {
        return await _context.BusinessPartners.FirstOrDefaultAsync(b => b.CardCode == cardCode);
    }

    public async Task<BusinessPartner?> GetByExternalIdAsync(string externalId, string source)
    {
        return await _context.BusinessPartners.FirstOrDefaultAsync(b => b.ExternalId == externalId && b.Source == source);
    }

    public async Task<IEnumerable<BusinessPartner>> GetPendingSyncAsync()
    {
        return await _context.BusinessPartners.Where(b => b.SyncStatus == SyncStatus.Pending).ToListAsync();
    }

    public async Task AddAsync(BusinessPartner partner)
    {
        await _context.BusinessPartners.AddAsync(partner);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BusinessPartner partner)
    {
        _context.BusinessPartners.Update(partner);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string cardCode)
    {
        return await _context.BusinessPartners.AnyAsync(b => b.CardCode == cardCode);
    }
}