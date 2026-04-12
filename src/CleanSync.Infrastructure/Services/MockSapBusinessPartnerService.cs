using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;

namespace CleanSync.Infrastructure.Services;

public class MockSapBusinessPartnerService : ISapBusinessPartnerService
{
    private readonly List<SapBusinessPartnerDto> _partners = new();
    private int _cardCodeCounter = 1;

    public MockSapBusinessPartnerService()
    {
        _partners.Add(new SapBusinessPartnerDto
        {
            CardCode = "C00001",
            CardName = "Demo Customer Inc.",
            CardType = "cCustomer",
            FederalTaxId = "12-3456789",
            Phone1 = "+1-555-0100",
            Email = "demo@example.com",
            Address = "123 Demo Street",
            City = "New York",
            Country = "US",
            ZipCode = "10001",
            GroupCode = 100
        });
    }

    public Task<SapBusinessPartnerDto?> GetByCardCodeAsync(string cardCode)
    {
        var partner = _partners.FirstOrDefault(p => p.CardCode == cardCode);
        return Task.FromResult(partner);
    }

    public Task<IEnumerable<SapBusinessPartnerDto>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<SapBusinessPartnerDto>>(_partners);
    }

    public Task<SapBusinessPartnerDto> CreateAsync(SapBusinessPartnerDto partner)
    {
        partner.CardCode = $"C{_cardCodeCounter:D5}";
        _cardCodeCounter++;
        _partners.Add(partner);
        return Task.FromResult(partner);
    }

    public Task<SapBusinessPartnerDto> UpdateAsync(string cardCode, SapBusinessPartnerDto partner)
    {
        var existing = _partners.FirstOrDefault(p => p.CardCode == cardCode);
        if (existing != null)
        {
            existing.CardName = partner.CardName;
            existing.FederalTaxId = partner.FederalTaxId;
            existing.Phone1 = partner.Phone1;
            existing.Email = partner.Email;
            existing.Address = partner.Address;
            existing.City = partner.City;
            existing.Country = partner.Country;
            existing.ZipCode = partner.ZipCode;
        }
        return Task.FromResult(existing ?? partner);
    }

    public Task<bool> ExistsAsync(string cardCode)
    {
        return Task.FromResult(_partners.Any(p => p.CardCode == cardCode));
    }

    public Task<bool> TestConnectionAsync()
    {
        return Task.FromResult(true);
    }
}