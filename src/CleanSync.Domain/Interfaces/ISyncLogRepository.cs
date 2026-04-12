using CleanSync.Domain.Entities;

namespace CleanSync.Domain.Interfaces;

public interface ISyncLogRepository
{
    Task<IEnumerable<SyncLog>> GetAllAsync(int limit = 50);
    Task<SyncLog?> GetByIdAsync(int id);
    Task AddAsync(SyncLog log);
    Task UpdateAsync(SyncLog log);
}