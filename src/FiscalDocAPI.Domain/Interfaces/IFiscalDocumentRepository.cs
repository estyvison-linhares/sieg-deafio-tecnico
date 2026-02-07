using FiscalDocAPI.Domain.Entities;

namespace FiscalDocAPI.Domain.Interfaces;

public interface IFiscalDocumentRepository
{
    Task<FiscalDocument?> GetByIdAsync(Guid id);
    Task<FiscalDocument?> GetByHashAsync(string xmlHash);
    Task<FiscalDocument?> GetByDocumentKeyAsync(string documentKey);
    Task<(List<FiscalDocument> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? cnpj = null,
        string? uf = null,
        string? documentType = null);
    Task AddAsync(FiscalDocument document);
    Task UpdateAsync(FiscalDocument document);
    Task DeleteAsync(FiscalDocument document);
    Task<int> SaveChangesAsync();
}
