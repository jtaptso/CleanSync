namespace CleanSync.Application.DTOs;

public class SapBusinessPartnerDto
{
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string CardType { get; set; } = "cCustomer";
    public string? FederalTaxId { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public int? GroupCode { get; set; }
}