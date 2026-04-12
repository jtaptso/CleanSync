namespace CleanSync.Application.DTOs;

public class EcommerceCustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public EcommerceAddressDto? DefaultAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Source { get; set; } = "Shopify";
}

public class EcommerceAddressDto
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
}