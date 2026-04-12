namespace CleanSync.Application.DTOs;

public class SapConnectionSettings
{
    public string ServiceLayerUrl { get; set; } = string.Empty;
    public string CompanyDb { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int SessionTimeoutMinutes { get; set; } = 30;
}
