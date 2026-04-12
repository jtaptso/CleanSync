using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;
using CleanSync.Infrastructure.Data;
using CleanSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleanSync.Tests;

public class SyncLogRepositoryTests
{
    private CleanSyncDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CleanSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CleanSyncDbContext(options);
    }

    [Fact]
    public async Task AddAsync_NewLog_AddsToDatabase()
    {
        // Arrange
        using var context = CreateContext();
        ISyncLogRepository repository = new SyncLogRepository(context);
        var log = new SyncLog
        {
            EntityType = "BusinessPartner",
            Direction = "ToSap",
            EntityCount = 10,
            SuccessCount = 9,
            FailureCount = 1,
            Status = SyncStatus.Synced,
            StartedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(log);

        // Assert
        var allLogs = await repository.GetAllAsync();
        Assert.Single(allLogs);
        Assert.Equal("BusinessPartner", allLogs.First().EntityType);
    }

    [Fact]
    public async Task GetAllAsync_ExistingLogs_ReturnsOrderedByStartedAt()
    {
        // Arrange
        using var context = CreateContext();
        context.SyncLogs.AddRange(
            new SyncLog { EntityType = "BusinessPartner", StartedAt = DateTime.UtcNow.AddDays(-1), Status = SyncStatus.Synced },
            new SyncLog { EntityType = "BusinessPartner", StartedAt = DateTime.UtcNow, Status = SyncStatus.Synced },
            new SyncLog { EntityType = "BusinessPartner", StartedAt = DateTime.UtcNow.AddHours(-12), Status = SyncStatus.Synced }
        );
        await context.SaveChangesAsync();
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
        Assert.True((DateTime.UtcNow - result.First().StartedAt).TotalSeconds < 1); // Most recent first
    }

    [Fact]
    public async Task GetAllAsync_LimitParameter_RespectsLimit()
    {
        // Arrange
        using var context = CreateContext();
        for (int i = 1; i <= 20; i++)
        {
            context.SyncLogs.Add(new SyncLog 
            { 
                EntityType = "BusinessPartner", 
                StartedAt = DateTime.UtcNow.AddMinutes(-i),
                Status = SyncStatus.Synced 
            });
        }
        await context.SaveChangesAsync();
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        var result = await repository.GetAllAsync(limit: 5);

        // Assert
        Assert.Equal(5, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_DefaultLimit_Returns50()
    {
        // Arrange
        using var context = CreateContext();
        for (int i = 1; i <= 60; i++)
        {
            context.SyncLogs.Add(new SyncLog 
            { 
                EntityType = "BusinessPartner", 
                StartedAt = DateTime.UtcNow.AddMinutes(-i),
                Status = SyncStatus.Synced 
            });
        }
        await context.SaveChangesAsync();
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(50, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsLog()
    {
        // Arrange
        using var context = CreateContext();
        context.SyncLogs.Add(new SyncLog { EntityType = "BusinessPartner", Status = SyncStatus.Synced, StartedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var logId = context.SyncLogs.First().Id;
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        var result = await repository.GetByIdAsync(logId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BusinessPartner", result.EntityType);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingLog_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateContext();
        var log = new SyncLog
        {
            EntityType = "BusinessPartner",
            Status = SyncStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };
        context.SyncLogs.Add(log);
        await context.SaveChangesAsync();
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        log.Status = SyncStatus.Synced;
        log.CompletedAt = DateTime.UtcNow;
        log.SuccessCount = 10;
        log.FailureCount = 0;
        await repository.UpdateAsync(log);

        // Assert
        var result = await repository.GetByIdAsync(log.Id);
        Assert.NotNull(result);
        Assert.Equal(SyncStatus.Synced, result.Status);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();
        ISyncLogRepository repository = new SyncLogRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_LogWithErrorMessage_StoresErrorMessage()
    {
        // Arrange
        using var context = CreateContext();
        ISyncLogRepository repository = new SyncLogRepository(context);
        var log = new SyncLog
        {
            EntityType = "BusinessPartner",
            Status = SyncStatus.Failed,
            ErrorMessage = "Connection timeout to SAP",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(log);

        // Assert
        var result = await repository.GetByIdAsync(log.Id);
        Assert.NotNull(result);
        Assert.Equal("Connection timeout to SAP", result.ErrorMessage);
    }
}
