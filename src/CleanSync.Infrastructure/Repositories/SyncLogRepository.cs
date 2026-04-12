using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;
using CleanSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanSync.Infrastructure.Repositories;

public class SyncLogRepository : ISyncLogRepository
{
    private readonly CleanSyncDbContext _context;

    public SyncLogRepository(CleanSyncDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SyncLog>> GetAllAsync(int limit = 50)
    {
        return await _context.SyncLogs
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<SyncLog?> GetByIdAsync(int id)
    {
        return await _context.SyncLogs.FindAsync(id);
    }

    public async Task AddAsync(SyncLog log)
    {
        await _context.SyncLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SyncLog log)
    {
        _context.SyncLogs.Update(log);
        await _context.SaveChangesAsync();
    }
}