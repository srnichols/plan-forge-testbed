using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Web.Client.Models;

public class ClientFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be greater than 0")]
    public decimal HourlyRate { get; set; }
}
