using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;
using CleanSync.Infrastructure.Services;
using Xunit;

namespace CleanSync.Tests;

public class MockSapBusinessPartnerServiceTests
{
    private MockSapBusinessPartnerService CreateService() => new MockSapBusinessPartnerService();

    [Fact]
    public void Constructor_InitializesWithDefaultCustomer()
    {
        // Act
        var service = CreateService();
        var result = service.GetAllAsync().Result;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var customer = result.First();
        Assert.Equal("C00001", customer.CardCode);
        Assert.Equal("Demo Customer Inc.", customer.CardName);
    }

    [Fact]
    public async Task GetByCardCodeAsync_ExistingCode_ReturnsPartner()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetByCardCodeAsync("C00001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("C00001", result.CardCode);
    }

    [Fact]
    public async Task GetByCardCodeAsync_NonExistingCode_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetByCardCodeAsync("NONEXISTENT");

        // Assert - should return null for non-existing
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPartners()
    {
        // Arrange
        var service = CreateService();
        await service.CreateAsync(new SapBusinessPartnerDto { CardName = "New Partner" });
        await service.CreateAsync(new SapBusinessPartnerDto { CardName = "Another Partner" });

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueCardCode()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result1 = await service.CreateAsync(new SapBusinessPartnerDto { CardName = "Partner 1" });
        var result2 = await service.CreateAsync(new SapBusinessPartnerDto { CardName = "Partner 2" });

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.CardCode, result2.CardCode);
        Assert.StartsWith("C", result1.CardCode);
        Assert.StartsWith("C", result2.CardCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedPartner()
    {
        // Arrange
        var service = CreateService();
        var newPartner = new SapBusinessPartnerDto
        {
            CardName = "Test Partner",
            CardType = "cCustomer",
            FederalTaxId = "12-3456789",
            Email = "test@example.com"
        };

        // Act
        var result = await service.CreateAsync(newPartner);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Partner", result.CardName);
        Assert.Equal("cCustomer", result.CardType);
        Assert.NotNull(result.CardCode);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingPartner()
    {
        // Arrange
        var service = CreateService();
        var existingPartner = await service.GetByCardCodeAsync("C00001");
        Assert.NotNull(existingPartner);

        // Act
        var updateDto = new SapBusinessPartnerDto
        {
            CardName = "Updated Name",
            Email = "updated@example.com",
            Phone1 = "+1-555-9999"
        };
        var result = await service.UpdateAsync("C00001", updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.CardName);
        Assert.Equal("updated@example.com", result.Email);
        Assert.Equal("+1-555-9999", result.Phone1);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingCode_ReturnsInputPartner()
    {
        // Arrange
        var service = CreateService();
        var inputPartner = new SapBusinessPartnerDto { CardName = "New" };

        // Act
        var result = await service.UpdateAsync("NONEXISTENT", inputPartner);

        // Assert - mock returns the input when not found
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingCode_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ExistsAsync("C00001");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingCode_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ExistsAsync("NONEXISTENT");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.TestConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreateAsync_PreservesOriginalProperties()
    {
        // Arrange
        var service = CreateService();
        var newPartner = new SapBusinessPartnerDto
        {
            CardName = "Test Corp",
            FederalTaxId = "99-9999999",
            Phone1 = "+1-555-1234",
            Email = "billing@testcorp.com",
            Address = "123 Business Rd",
            City = "Atlanta",
            Country = "US",
            ZipCode = "30301",
            GroupCode = 100
        };

        // Act
        var result = await service.CreateAsync(newPartner);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Corp", result.CardName);
        Assert.Equal("99-9999999", result.FederalTaxId);
        Assert.Equal("+1-555-1234", result.Phone1);
        Assert.Equal("billing@testcorp.com", result.Email);
        Assert.Equal("123 Business Rd", result.Address);
        Assert.Equal("Atlanta", result.City);
        Assert.Equal("US", result.Country);
        Assert.Equal("30301", result.ZipCode);
        Assert.Equal(100, result.GroupCode);
    }
}
