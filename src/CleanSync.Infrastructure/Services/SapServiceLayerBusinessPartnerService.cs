using System.Net;
using System.Text;
using System.Text.Json;
using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanSync.Infrastructure.Services;

public class SapServiceLayerBusinessPartnerService : ISapBusinessPartnerService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SapConnectionSettings _settings;
    private string? _sessionCookie;
    private DateTime _sessionExpiry;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly ILogger<SapServiceLayerBusinessPartnerService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SapServiceLayerBusinessPartnerService(
        HttpClient httpClient,
        SapConnectionSettings settings,
        ILogger<SapServiceLayerBusinessPartnerService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.ServiceLayerUrl.TrimEnd('/') + "/");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await EnsureSessionAsync();
            var response = await _httpClient.GetAsync("BusinessPartners?$top=1");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP connection test failed");
            return false;
        }
    }

    public async Task<SapBusinessPartnerDto?> GetByCardCodeAsync(string cardCode)
    {
        await EnsureSessionAsync();
        
        var url = string.Format("BusinessPartners('{0}')", Uri.EscapeDataString(cardCode));
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            
            throw new HttpRequestException(string.Format("Failed to get BusinessPartner: {0}", response.StatusCode));
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SapBusinessPartnerDto>(json, JsonOptions);
    }

    public async Task<IEnumerable<SapBusinessPartnerDto>> GetAllAsync()
    {
        await EnsureSessionAsync();
        
        var response = await _httpClient.GetAsync("BusinessPartners?$filter=CardType eq 'cCustomer'");
        
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(string.Format("Failed to get BusinessPartners: {0}", response.StatusCode));

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ODataResponse<SapBusinessPartnerDto>>(json, JsonOptions);
        
        return result?.Value ?? Enumerable.Empty<SapBusinessPartnerDto>();
    }

    public async Task<SapBusinessPartnerDto> CreateAsync(SapBusinessPartnerDto partner)
    {
        await EnsureSessionAsync();
        
        var json = JsonSerializer.Serialize(partner, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("BusinessPartners", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(string.Format("Failed to create BusinessPartner: {0} - {1}", response.StatusCode, errorContent));
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SapBusinessPartnerDto>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize created BusinessPartner");
    }

    public async Task<SapBusinessPartnerDto> UpdateAsync(string cardCode, SapBusinessPartnerDto partner)
    {
        await EnsureSessionAsync();
        
        var json = JsonSerializer.Serialize(partner, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var url = string.Format("BusinessPartners('{0}')", Uri.EscapeDataString(cardCode));
        var response = await _httpClient.PatchAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(string.Format("Failed to update BusinessPartner: {0} - {1}", response.StatusCode, errorContent));
        }

        return await GetByCardCodeAsync(cardCode) ?? partner;
    }

    public async Task<bool> ExistsAsync(string cardCode)
    {
        var partner = await GetByCardCodeAsync(cardCode);
        return partner != null;
    }

    private async Task EnsureSessionAsync()
    {
        if (_sessionCookie != null && DateTime.UtcNow < _sessionExpiry)
            return;

        await _sessionLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_sessionCookie != null && DateTime.UtcNow < _sessionExpiry)
                return;

            await LogoutAsync();
            
            var loginRequest = new
            {
                CompanyDB = _settings.CompanyDb,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            var json = JsonSerializer.Serialize(loginRequest, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("Login", content);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(string.Format("SAP Login failed: {0}", response.StatusCode));
            }

            // Extract B1SESSION cookie
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.Contains("B1SESSION"))
                    {
                        var sessionPart = cookie.Split(';').First();
                        _sessionCookie = sessionPart;
                        _sessionExpiry = DateTime.UtcNow.AddMinutes(_settings.SessionTimeoutMinutes - 5);
                        _logger.LogInformation("SAP session established successfully");
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(_sessionCookie))
            {
                throw new InvalidOperationException("Failed to obtain B1SESSION cookie from SAP");
            }
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private async Task LogoutAsync()
    {
        if (string.IsNullOrEmpty(_sessionCookie))
            return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "Logout");
            request.Headers.Add("Cookie", _sessionCookie);
            await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAP logout failed");
        }
        finally
        {
            _sessionCookie = null;
        }
    }

    public void Dispose()
    {
        LogoutAsync().GetAwaiter().GetResult();
        _sessionLock.Dispose();
    }

    private class ODataResponse<T>
    {
        public List<T> Value { get; set; } = new();
    }
}
