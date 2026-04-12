using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;
using CleanSync.Infrastructure.Data;
using CleanSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleanSync.Tests;

public class BusinessPartnerRepositoryTests
{
    private CleanSyncDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CleanSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CleanSyncDbContext(options);
    }

    [Fact]
    public async Task AddAsync_NewPartner_AddsToDatabase()
    {
        // Arrange
        using var context = CreateContext();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);
        var partner = new BusinessPartner
        {
            CardCode = "C00001",
            CardName = "Test Customer",
            Email = "test@example.com",
            Source = "Shopify",
            ExternalId = "shopify_001",
            SyncStatus = SyncStatus.Pending
        };

        // Act
        await repository.AddAsync(partner);

        // Assert
        var allPartners = await repository.GetAllAsync();
        Assert.Single(allPartners);
        Assert.Equal("C00001", allPartners.First().CardCode);
    }

    [Fact]
    public async Task GetAllAsync_ExistingPartners_ReturnsOrderedByLastSynced()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.AddRange(
            new BusinessPartner { CardCode = "C00001", CardName = "Old Partner", LastSyncedAt = DateTime.UtcNow.AddDays(-1), Source = "Shopify", ExternalId = "1" },
            new BusinessPartner { CardCode = "C00002", CardName = "New Partner", LastSyncedAt = DateTime.UtcNow, Source = "Shopify", ExternalId = "2" },
            new BusinessPartner { CardCode = "C00003", CardName = "Middle Partner", LastSyncedAt = DateTime.UtcNow.AddHours(-12), Source = "Shopify", ExternalId = "3" }
        );
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
        Assert.Equal("C00002", result.First().CardCode); // Most recent first
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsPartner()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.Add(new BusinessPartner { CardCode = "C00001", CardName = "Test", Source = "Shopify", ExternalId = "1" });
        await context.SaveChangesAsync();
        var partnerId = context.BusinessPartners.First().Id;
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetByIdAsync(partnerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("C00001", result.CardCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCardCodeAsync_ExistingCode_ReturnsPartner()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.Add(new BusinessPartner { CardCode = "SH00001", CardName = "Test", Source = "Shopify", ExternalId = "1" });
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetByCardCodeAsync("SH00001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SH00001", result.CardCode);
    }

    [Fact]
    public async Task GetByCardCodeAsync_NonExistingCode_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetByCardCodeAsync("NONEXISTENT");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByExternalIdAsync_MatchingExternalIdAndSource_ReturnsPartner()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.Add(new BusinessPartner 
        { 
            CardCode = "SH00001", 
            CardName = "Test", 
            Source = "Shopify", 
            ExternalId = "shopify_123" 
        });
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetByExternalIdAsync("shopify_123", "Shopify");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("shopify_123", result.ExternalId);
        Assert.Equal("Shopify", result.Source);
    }

    [Fact]
    public async Task GetByExternalIdAsync_DifferentSource_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.Add(new BusinessPartner 
        { 
            CardCode = "SH00001", 
            CardName = "Test", 
            Source = "Shopify", 
            ExternalId = "shopify_123" 
        });
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetByExternalIdAsync("shopify_123", "Amazon");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingSyncAsync_OnlyPendingPartners_ReturnsPending()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.AddRange(
            new BusinessPartner { CardCode = "C00001", SyncStatus = SyncStatus.Pending, Source = "Shopify", ExternalId = "1" },
            new BusinessPartner { CardCode = "C00002", SyncStatus = SyncStatus.Synced, Source = "Shopify", ExternalId = "2" },
            new BusinessPartner { CardCode = "C00003", SyncStatus = SyncStatus.Pending, Source = "Shopify", ExternalId = "3" },
            new BusinessPartner { CardCode = "C00004", SyncStatus = SyncStatus.Failed, Source = "Shopify", ExternalId = "4" }
        );
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.GetPendingSyncAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(SyncStatus.Pending, p.SyncStatus));
    }

    [Fact]
    public async Task UpdateAsync_ExistingPartner_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateContext();
        var partner = new BusinessPartner { CardCode = "C00001", CardName = "Original", Source = "Shopify", ExternalId = "1" };
        context.BusinessPartners.Add(partner);
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        partner.CardName = "Updated Name";
        partner.SyncStatus = SyncStatus.Synced;
        await repository.UpdateAsync(partner);

        // Assert
        var result = await repository.GetByCardCodeAsync("C00001");
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.CardName);
        Assert.Equal(SyncStatus.Synced, result.SyncStatus);
    }

    [Fact]
    public async Task ExistsAsync_ExistingCode_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        context.BusinessPartners.Add(new BusinessPartner { CardCode = "SH00001", Source = "Shopify", ExternalId = "1" });
        await context.SaveChangesAsync();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.ExistsAsync("SH00001");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingCode_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        IBusinessPartnerRepository repository = new BusinessPartnerRepository(context);

        // Act
        var result = await repository.ExistsAsync("NONEXISTENT");

        // Assert
        Assert.False(result);
    }
}
