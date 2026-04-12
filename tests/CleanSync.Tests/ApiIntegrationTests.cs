using System.Net;
using System.Net.Http.Json;
using CleanSync.Application.DTOs;
using CleanSync.Domain.Entities;
using CleanSync.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CleanSync.Tests;

public class ApiIntegrationTests : IClassFixture<CleanSyncWebApplicationFactory>
{
    private readonly CleanSyncWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(CleanSyncWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private CleanSyncDbContext GetDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CleanSyncDbContext>();
    }

    [Fact]
    public async Task GetBusinessPartners_EmptyDatabase_ReturnsEmptyArray()
    {
        // Arrange - clear database
        using var context = GetDbContext();
        context.BusinessPartners.RemoveRange(context.BusinessPartners);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/BusinessPartners");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var partners = await response.Content.ReadFromJsonAsync<List<BusinessPartner>>();
        Assert.NotNull(partners);
        Assert.Empty(partners);
    }

    [Fact]
    public async Task GetBusinessPartners_WithData_ReturnsPartners()
    {
        // Arrange - seed data
        using var context = GetDbContext();
        context.BusinessPartners.RemoveRange(context.BusinessPartners);
        context.BusinessPartners.Add(new BusinessPartner
        {
            CardCode = "SH00001",
            CardName = "Test Customer",
            Email = "test@example.com",
            Source = "Shopify",
            ExternalId = "shopify_001",
            SyncStatus = SyncStatus.Synced
        });
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/BusinessPartners");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var partners = await response.Content.ReadFromJsonAsync<List<BusinessPartner>>();
        Assert.NotNull(partners);
        Assert.Single(partners);
        Assert.Equal("SH00001", partners[0].CardCode);
    }

    [Fact]
    public async Task GetBusinessPartnerById_ExistingId_ReturnsPartner()
    {
        // Arrange - seed data
        using var context = GetDbContext();
        context.BusinessPartners.RemoveRange(context.BusinessPartners);
        var partner = new BusinessPartner
        {
            CardCode = "SH00001",
            CardName = "Test Customer",
            Source = "Shopify",
            ExternalId = "shopify_001",
            SyncStatus = SyncStatus.Synced
        };
        context.BusinessPartners.Add(partner);
        await context.SaveChangesAsync();
        var partnerId = partner.Id;

        // Act
        var response = await _client.GetAsync($"/api/BusinessPartners/{partnerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BusinessPartner>();
        Assert.NotNull(result);
        Assert.Equal(partnerId, result.Id);
        Assert.Equal("SH00001", result.CardCode);
    }

    [Fact]
    public async Task GetBusinessPartnerById_NonExistingId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/BusinessPartners/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSyncLogs_EmptyDatabase_ReturnsEmptyArray()
    {
        // Arrange - clear database
        using var context = GetDbContext();
        context.SyncLogs.RemoveRange(context.SyncLogs);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/BusinessPartners/sync-logs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<SyncLog>>();
        Assert.NotNull(logs);
        Assert.Empty(logs);
    }

    [Fact]
    public async Task GetSyncLogs_WithData_ReturnsLogs()
    {
        // Arrange - seed data
        using var context = GetDbContext();
        context.SyncLogs.RemoveRange(context.SyncLogs);
        context.SyncLogs.Add(new SyncLog
        {
            EntityType = "BusinessPartner",
            Direction = "ToSap",
            EntityCount = 5,
            SuccessCount = 5,
            FailureCount = 0,
            Status = SyncStatus.Synced,
            StartedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/BusinessPartners/sync-logs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<SyncLog>>();
        Assert.NotNull(logs);
        Assert.Single(logs);
        Assert.Equal("BusinessPartner", logs[0].EntityType);
    }

    [Fact]
    public async Task PostSyncBusinessPartners_TriggersSync_ReturnsOk()
    {
        // Act
        var response = await _client.PostAsync("/api/Sync/business-partners", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SyncResultDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalProcessed >= 0);
    }

    [Fact]
    public async Task PostSyncBusinessPartners_SyncCreatesNewPartner()
    {
        // Arrange - clear and ensure empty
        using var context = GetDbContext();
        context.BusinessPartners.RemoveRange(context.BusinessPartners);
        await context.SaveChangesAsync();

        // Act - trigger sync (mock service returns demo customers)
        var syncResponse = await _client.PostAsync("/api/Sync/business-partners", null);
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

        // Assert - verify partner was created
        var partnersResponse = await _client.GetAsync("/api/BusinessPartners");
        var partners = await partnersResponse.Content.ReadFromJsonAsync<List<BusinessPartner>>();
        Assert.NotNull(partners);
        Assert.NotEmpty(partners); // Mock service creates demo customers
    }
}

public class CleanSyncWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string TestDbName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CleanSyncDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing (shared across tests in this fixture)
            services.AddDbContext<CleanSyncDbContext>(options =>
                options.UseInMemoryDatabase(TestDbName));
        });
    }
}
