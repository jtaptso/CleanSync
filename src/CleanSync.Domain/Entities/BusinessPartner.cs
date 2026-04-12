namespace CleanSync.Domain.Entities;

public class BusinessPartner
{
    public int Id { get; set; }
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
    public string Source { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? SyncError { get; set; }
}

public enum SyncStatus
{
    Pending,
    Synced,
    Failed,
    InProgress
}