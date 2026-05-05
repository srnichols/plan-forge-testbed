namespace TimeTracker.Web.Client.Models;

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
