using CleanSync.Application.DTOs;

namespace CleanSync.Application.Interfaces;

public interface IEcommerceBusinessPartnerService
{
    Task<IEnumerable<EcommerceCustomerDto>> GetCustomersAsync();
    Task<EcommerceCustomerDto?> GetCustomerByIdAsync(string id);
    Task<EcommerceCustomerDto?> GetCustomerByEmailAsync(string email);
}