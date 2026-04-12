namespace CleanSync.Application.DTOs;

public class SyncResultDto
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<SyncErrorDto> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SyncErrorDto
{
    public string EntityId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}