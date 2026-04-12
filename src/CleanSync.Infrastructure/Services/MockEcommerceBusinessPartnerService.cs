using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;

namespace CleanSync.Infrastructure.Services;

public class MockEcommerceBusinessPartnerService : IEcommerceBusinessPartnerService
{
    private readonly List<EcommerceCustomerDto> _customers = new()
    {
        new EcommerceCustomerDto
        {
            Id = "shopify_001",
            Email = "john.doe@shopifydemo.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "+1-555-0201",
            Company = "Doe Consulting LLC",
            DefaultAddress = new EcommerceAddressDto
            {
                Address1 = "456 Oak Avenue",
                Address2 = "Suite 100",
                City = "Los Angeles",
                Province = "CA",
                Country = "US",
                Zip = "90001",
                Phone = "+1-555-0201"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Source = "Shopify"
        },
        new EcommerceCustomerDto
        {
            Id = "shopify_002",
            Email = "jane.smith@shopifydemo.com",
            FirstName = "Jane",
            LastName = "Smith",
            Phone = "+1-555-0202",
            Company = "Smith Industries",
            DefaultAddress = new EcommerceAddressDto
            {
                Address1 = "789 Pine Street",
                City = "San Francisco",
                Province = "CA",
                Country = "US",
                Zip = "94102"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            Source = "Shopify"
        },
        new EcommerceCustomerDto
        {
            Id = "amazon_001",
            Email = "bob.wilson@amazon-demo.com",
            FirstName = "Bob",
            LastName = "Wilson",
            Phone = "+1-555-0203",
            Company = "Wilson Retail",
            DefaultAddress = new EcommerceAddressDto
            {
                Address1 = "321 Maple Drive",
                City = "Seattle",
                Province = "WA",
                Country = "US",
                Zip = "98101"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            Source = "Amazon"
        }
    };

    public Task<IEnumerable<EcommerceCustomerDto>> GetCustomersAsync()
    {
        return Task.FromResult<IEnumerable<EcommerceCustomerDto>>(_customers);
    }

    public Task<EcommerceCustomerDto?> GetCustomerByIdAsync(string id)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(customer);
    }

    public Task<EcommerceCustomerDto?> GetCustomerByEmailAsync(string email)
    {
        var customer = _customers.FirstOrDefault(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(customer);
    }
}