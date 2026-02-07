namespace FiscalDocAPI.Application.DTOs;

public class UploadXmlRequest
{
    public required Stream XmlStream { get; set; }
    public required string FileName { get; set; }
}

public class UploadXmlResponse
{
    public Guid DocumentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsNewDocument { get; set; }
}

public class DocumentListRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Cnpj { get; set; }
    public string? UF { get; set; }
    public string? DocumentType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class UpdateDocumentRequest
{
    public string? EmitterName { get; set; }
    public string? RecipientName { get; set; }
    public string? ProcessingStatus { get; set; }
    public string? AdditionalData { get; set; }
}

public class DocumentSummaryDto
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string EmitterCnpj { get; set; } = string.Empty;
    public string EmitterName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentDetailDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentKey { get; set; } = string.Empty;
    public string EmitterCnpj { get; set; } = string.Empty;
    public string EmitterName { get; set; } = string.Empty;
    public string EmitterUF { get; set; } = string.Empty;
    public string RecipientCnpj { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ProcessingStatus { get; set; }
    public string? AdditionalData { get; set; }
}
