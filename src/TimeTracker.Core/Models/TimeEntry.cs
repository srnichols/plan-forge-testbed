namespace TimeTracker.Core.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
