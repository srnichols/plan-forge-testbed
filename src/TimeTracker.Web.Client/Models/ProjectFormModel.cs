using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Web.Client.Models;

public class ProjectFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Client is required")]
    public int ClientId { get; set; }
}
