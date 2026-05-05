using System.ComponentModel.DataAnnotations;
using TimeTracker.Core.Models;

namespace TimeTracker.Web.Client.Models;

public class InvoiceDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public List<InvoiceLineDto> InvoiceLines { get; set; } = [];
}

public class InvoiceLineDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public RateType RateType { get; set; }
    public decimal LineTotal { get; set; }
}

public class GenerateInvoiceFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Client is required")]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "Period start is required")]
    public DateTime PeriodStart { get; set; }

    [Required(ErrorMessage = "Period end is required")]
    public DateTime PeriodEnd { get; set; }
}

public class VoidInvoiceFormModel
{
    [Required(ErrorMessage = "Reason is required")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
