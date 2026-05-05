using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Web.Client.Models;

public class TimeEntryFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Project is required")]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Range(0.01, 24.0, ErrorMessage = "Hours must be between 0.01 and 24")]
    public decimal Hours { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsBillable { get; set; } = true;
}
