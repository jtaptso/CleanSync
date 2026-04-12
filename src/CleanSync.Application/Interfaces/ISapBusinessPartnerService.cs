using CleanSync.Application.DTOs;

namespace CleanSync.Application.Interfaces;

public interface ISapBusinessPartnerService
{
    Task<SapBusinessPartnerDto?> GetByCardCodeAsync(string cardCode);
    Task<IEnumerable<SapBusinessPartnerDto>> GetAllAsync();
    Task<SapBusinessPartnerDto> CreateAsync(SapBusinessPartnerDto partner);
    Task<SapBusinessPartnerDto> UpdateAsync(string cardCode, SapBusinessPartnerDto partner);
    Task<bool> ExistsAsync(string cardCode);
    Task<bool> TestConnectionAsync();
}