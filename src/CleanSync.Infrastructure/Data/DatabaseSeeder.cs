using CleanSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanSync.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(CleanSyncDbContext context, ILogger logger)
    {
        if (await context.BusinessPartners.AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding database with test data...");

        var now = DateTime.UtcNow;

        var businessPartners = new List<BusinessPartner>
        {
            new()
            {
                CardCode = "C0001",
                CardName = "Acme Corporation",
                CardType = "cCustomer",
                FederalTaxId = "12-3456789",
                Phone1 = "+1-555-100-0001",
                Email = "contact@acmecorp.com",
                Website = "https://www.acmecorp.com",
                Address = "123 Main Street",
                City = "New York",
                Country = "US",
                ZipCode = "10001",
                GroupCode = 100,
                Source = "SAP",
                ExternalId = "WEB-001",
                CreatedAt = now.AddDays(-30),
                LastSyncedAt = now.AddHours(-2),
                SyncStatus = SyncStatus.Synced,
            },
            new()
            {
                CardCode = "C0002",
                CardName = "Global Tech Solutions",
                CardType = "cCustomer",
                FederalTaxId = "98-7654321",
                Phone1 = "+1-555-200-0002",
                Email = "info@globaltechsolutions.com",
                Address = "456 Innovation Ave",
                City = "San Francisco",
                Country = "US",
                ZipCode = "94105",
                GroupCode = 100,
                Source = "Ecommerce",
                ExternalId = "WEB-002",
                CreatedAt = now.AddDays(-20),
                LastSyncedAt = now.AddHours(-5),
                SyncStatus = SyncStatus.Synced,
            },
            new()
            {
                CardCode = "C0003",
                CardName = "Sunrise Retail Group",
                CardType = "cCustomer",
                Phone1 = "+1-555-300-0003",
                Email = "orders@sunriseretail.com",
                Address = "789 Commerce Blvd",
                City = "Chicago",
                Country = "US",
                ZipCode = "60601",
                GroupCode = 101,
                Source = "Ecommerce",
                ExternalId = "WEB-003",
                CreatedAt = now.AddDays(-10),
                SyncStatus = SyncStatus.Pending,
            },
            new()
            {
                CardCode = "C0004",
                CardName = "Northern Logistics Ltd",
                CardType = "cCustomer",
                FederalTaxId = "55-4433221",
                Phone1 = "+1-555-400-0004",
                Email = "logistics@northernlogistics.com",
                Address = "321 Harbor Road",
                City = "Boston",
                Country = "US",
                ZipCode = "02101",
                GroupCode = 102,
                Source = "SAP",
                ExternalId = "WEB-004",
                CreatedAt = now.AddDays(-5),
                LastSyncedAt = now.AddDays(-1),
                SyncStatus = SyncStatus.Failed,
                SyncError = "Connection timeout during last sync attempt.",
            },
            new()
            {
                CardCode = "V0001",
                CardName = "Premier Supplies Inc",
                CardType = "cSupplier",
                FederalTaxId = "11-2233445",
                Phone1 = "+1-555-500-0005",
                Email = "sales@premiersupplies.com",
                Address = "654 Industrial Park",
                City = "Detroit",
                Country = "US",
                ZipCode = "48201",
                GroupCode = 200,
                Source = "SAP",
                ExternalId = "SAP-V001",
                CreatedAt = now.AddDays(-60),
                LastSyncedAt = now.AddHours(-1),
                SyncStatus = SyncStatus.Synced,
            },
        };

        context.BusinessPartners.AddRange(businessPartners);

        var syncLogs = new List<SyncLog>
        {
            new()
            {
                EntityType = "BusinessPartner",
                Direction = "ToSap",
                EntityCount = 5,
                SuccessCount = 4,
                FailureCount = 1,
                StartedAt = now.AddHours(-5),
                CompletedAt = now.AddHours(-5).AddSeconds(12),
                Status = SyncStatus.Synced,
            },
            new()
            {
                EntityType = "BusinessPartner",
                Direction = "FromSap",
                EntityCount = 3,
                SuccessCount = 3,
                FailureCount = 0,
                StartedAt = now.AddHours(-2),
                CompletedAt = now.AddHours(-2).AddSeconds(8),
                Status = SyncStatus.Synced,
            },
            new()
            {
                EntityType = "BusinessPartner",
                Direction = "ToSap",
                EntityCount = 2,
                SuccessCount = 1,
                FailureCount = 1,
                StartedAt = now.AddDays(-1),
                CompletedAt = now.AddDays(-1).AddSeconds(30),
                ErrorMessage = "SAP Service Layer returned 503 for 1 record.",
                Status = SyncStatus.Failed,
            },
        };

        context.SyncLogs.AddRange(syncLogs);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded {BpCount} business partners and {LogCount} sync logs.",
            businessPartners.Count,
            syncLogs.Count);
    }
}
