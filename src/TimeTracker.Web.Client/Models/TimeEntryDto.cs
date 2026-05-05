namespace TimeTracker.Web.Client.Models;

public class TimeEntryDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; }
    public DateTime CreatedAt { get; set; }
}
