using Microsoft.EntityFrameworkCore;
using FiscalDocAPI.Domain.Entities;
using FiscalDocAPI.Domain.Interfaces;

namespace FiscalDocAPI.Infrastructure.Persistence;

public class FiscalDocumentRepository : IFiscalDocumentRepository
{
    private readonly FiscalDocContext _context;

    public FiscalDocumentRepository(FiscalDocContext context)
    {
        _context = context;
    }

    public async Task<FiscalDocument?> GetByIdAsync(Guid id)
    {
        return await _context.FiscalDocuments.FindAsync(id);
    }

    public async Task<FiscalDocument?> GetByHashAsync(string xmlHash)
    {
        return await _context.FiscalDocuments
            .FirstOrDefaultAsync(d => d.XmlHash == xmlHash);
    }

    public async Task<FiscalDocument?> GetByDocumentKeyAsync(string documentKey)
    {
        return await _context.FiscalDocuments
            .FirstOrDefaultAsync(d => d.DocumentKey == documentKey);
    }

    public async Task<(List<FiscalDocument> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? cnpj = null,
        string? uf = null,
        string? documentType = null)
    {
        var query = _context.FiscalDocuments.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(d => d.IssueDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(d => d.IssueDate <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(cnpj))
            query = query.Where(d => d.EmitterCnpj == cnpj || d.RecipientCnpj == cnpj);

        if (!string.IsNullOrWhiteSpace(uf))
            query = query.Where(d => d.EmitterUF == uf);

        if (!string.IsNullOrWhiteSpace(documentType))
            query = query.Where(d => d.DocumentType == documentType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(FiscalDocument document)
    {
        await _context.FiscalDocuments.AddAsync(document);
    }

    public Task UpdateAsync(FiscalDocument document)
    {
        _context.FiscalDocuments.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FiscalDocument document)
    {
        _context.FiscalDocuments.Remove(document);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
