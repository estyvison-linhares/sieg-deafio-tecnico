namespace FiscalDocAPI.Domain.Events;

public class DocumentProcessedEvent
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentKey { get; set; } = string.Empty;
    public string EmitterCnpj { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
