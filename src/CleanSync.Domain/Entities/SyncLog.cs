namespace CleanSync.Domain.Entities;

public class SyncLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public int EntityCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public SyncStatus Status { get; set; }
}

public enum SyncDirection
{
    ToSap,
    FromSap,
    Bidirectional
}