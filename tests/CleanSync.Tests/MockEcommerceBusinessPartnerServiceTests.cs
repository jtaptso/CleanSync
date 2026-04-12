using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;
using CleanSync.Infrastructure.Services;
using Xunit;

namespace CleanSync.Tests;

public class MockEcommerceBusinessPartnerServiceTests
{
    private readonly MockEcommerceBusinessPartnerService _service;

    public MockEcommerceBusinessPartnerServiceTests()
    {
        _service = new MockEcommerceBusinessPartnerService();
    }

    [Fact]
    public void Constructor_InitializesWithThreeCustomers()
    {
        // Act
        var result = _service.GetCustomersAsync().Result;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetCustomersAsync_ReturnsAllCustomers()
    {
        // Act
        var result = await _service.GetCustomersAsync();

        // Assert
        var customerList = result.ToList();
        Assert.Contains(customerList, c => c.Id == "shopify_001");
        Assert.Contains(customerList, c => c.Id == "shopify_002");
        Assert.Contains(customerList, c => c.Id == "amazon_001");
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ExistingId_ReturnsCustomer()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("shopify_001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("shopify_001", result.Id);
        Assert.Equal("john.doe@shopifydemo.com", result.Email);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("non_existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_ExistingEmail_ReturnsCustomer()
    {
        // Act
        var result = await _service.GetCustomerByEmailAsync("john.doe@shopifydemo.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_CaseInsensitive_ReturnsCustomer()
    {
        // Act
        var result = await _service.GetCustomerByEmailAsync("JOHN.DOE@SHOPIFYDEMO.COM");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("shopify_001", result.Id);
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _service.GetCustomerByEmailAsync("notexist@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomersAsync_ShopifyCustomer_HasAddress()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("shopify_001");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DefaultAddress);
        Assert.Equal("456 Oak Avenue", result.DefaultAddress.Address1);
        Assert.Equal("Los Angeles", result.DefaultAddress.City);
        Assert.Equal("CA", result.DefaultAddress.Province);
        Assert.Equal("US", result.DefaultAddress.Country);
        Assert.Equal("90001", result.DefaultAddress.Zip);
    }

    [Fact]
    public async Task GetCustomersAsync_AmazonCustomer_HasSource()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("amazon_001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Amazon", result.Source);
    }

    [Fact]
    public async Task GetCustomersAsync_AllHaveRequiredFields()
    {
        // Act
        var result = await _service.GetCustomersAsync();

        // Assert
        foreach (var customer in result)
        {
            Assert.False(string.IsNullOrEmpty(customer.Id), $"Customer {customer.Id} has empty Id");
            Assert.False(string.IsNullOrEmpty(customer.Email), $"Customer {customer.Id} has empty Email");
            Assert.False(string.IsNullOrEmpty(customer.Source), $"Customer {customer.Id} has empty Source");
        }
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ShopifyCustomer_HasCompany()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("shopify_001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Doe Consulting LLC", result.Company);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ShopifyCustomer_HasPhone()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("shopify_001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("+1-555-0201", result.Phone);
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_SecondShopifyCustomer()
    {
        // Act
        var result = await _service.GetCustomerByEmailAsync("jane.smith@shopifydemo.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("shopify_002", result.Id);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("Smith Industries", result.Company);
    }
}
