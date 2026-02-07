using FiscalDocAPI.Application.DTOs;

namespace FiscalDocAPI.Application.Interfaces;

public interface IDocumentService
{
    Task<UploadXmlResponse> ProcessXmlUploadAsync(Stream xmlStream, string fileName);
    Task<PagedResult<DocumentSummaryDto>> GetDocumentsAsync(DocumentListRequest request);
    Task<DocumentDetailDto?> GetDocumentByIdAsync(Guid id);
    Task<bool> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request);
    Task<bool> DeleteDocumentAsync(Guid id);
}
