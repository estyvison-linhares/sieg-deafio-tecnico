using System.ComponentModel.DataAnnotations;
using FiscalDocAPI.Domain.Constants;

namespace FiscalDocAPI.Domain.Entities;

public class FiscalDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string DocumentKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(14)]
    public string EmitterCnpj { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string EmitterName { get; set; } = string.Empty;
    
    [MaxLength(2)]
    public string EmitterUF { get; set; } = string.Empty;
    
    [MaxLength(14)]
    public string RecipientCnpj { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string RecipientName { get; set; } = string.Empty;
    
    public decimal TotalValue { get; set; }
    
    public DateTime IssueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [Required]
    public string XmlContent { get; set; } = string.Empty;
    
    [MaxLength(64)]
    public string XmlHash { get; set; } = string.Empty;
    
    public string? ProcessingStatus { get; set; }
    
    public string? AdditionalData { get; set; }

    public static FiscalDocument Create(
        string documentType,
        string documentKey,
        string emitterCnpj,
        string emitterName,
        string emitterUF,
        string recipientCnpj,
        string recipientName,
        decimal totalValue,
        DateTime issueDate,
        string xmlContent,
        string xmlHash)
    {
        var document = new FiscalDocument
        {
            DocumentType = documentType,
            DocumentKey = documentKey,
            EmitterCnpj = emitterCnpj,
            EmitterName = emitterName,
            EmitterUF = emitterUF,
            RecipientCnpj = recipientCnpj,
            RecipientName = recipientName,
            TotalValue = totalValue,
            IssueDate = issueDate,
            XmlContent = xmlContent,
            XmlHash = xmlHash,
            ProcessingStatus = AppConstants.ProcessingStatus.Pending
        };

        return document;
    }

    public void Update(string? emitterName, string? recipientName, string? processingStatus, string? additionalData)
    {
        if (!string.IsNullOrWhiteSpace(emitterName))
            EmitterName = emitterName;

        if (!string.IsNullOrWhiteSpace(recipientName))
            RecipientName = recipientName;

        if (!string.IsNullOrWhiteSpace(processingStatus))
            ProcessingStatus = processingStatus;

        if (!string.IsNullOrWhiteSpace(additionalData))
            AdditionalData = additionalData;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessingStatus = "Processed";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsError()
    {
        ProcessingStatus = "Error";
        UpdatedAt = DateTime.UtcNow;
    }
}
