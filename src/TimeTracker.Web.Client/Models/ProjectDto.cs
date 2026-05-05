namespace TimeTracker.Web.Client.Models;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ClientId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
