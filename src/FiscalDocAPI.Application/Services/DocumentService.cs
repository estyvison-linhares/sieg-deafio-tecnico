using FiscalDocAPI.Application.DTOs;
using FiscalDocAPI.Application.Interfaces;
using FiscalDocAPI.Domain.Constants;
using FiscalDocAPI.Domain.Entities;
using FiscalDocAPI.Domain.Events;
using FiscalDocAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace FiscalDocAPI.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IFiscalDocumentRepository _repository;
    private readonly IXmlParser _xmlParser;
    private readonly IEncryptionService _encryptionService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<DocumentService> _logger;
    private readonly IMapper _mapper;

    public DocumentService(
        IFiscalDocumentRepository repository,
        IXmlParser xmlParser,
        IEncryptionService encryptionService,
        IMessagePublisher messagePublisher,
        ILogger<DocumentService> logger,
        IMapper mapper)
    {
        _repository = repository;
        _xmlParser = xmlParser;
        _encryptionService = encryptionService;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<UploadXmlResponse> ProcessXmlUploadAsync(Stream xmlStream, string fileName)
    {
        var xmlContent = await ReadXmlContentAsync(xmlStream);
        var xmlHash = _xmlParser.ComputeHash(xmlContent);
        
        var idempotencyCheck = await CheckIdempotencyByHashAsync(xmlHash);
        if (idempotencyCheck != null)
            return idempotencyCheck;

        var encryptedXml = _encryptionService.Encrypt(xmlContent);
        
        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var document = await _xmlParser.ParseXmlAsync(memoryStream, encryptedXml, xmlHash);
        
        var duplicationCheck = await CheckDuplicationByKeyAsync(document.DocumentKey);
        if (duplicationCheck != null)
            return duplicationCheck;

        await SaveDocumentAsync(document);
        await PublishDocumentProcessedEventAsync(document);

        _logger.LogInformation("New document {Id} created successfully.", document.Id);
        
        return CreateSuccessResponse(document.Id);
    }

    private static async Task<string> ReadXmlContentAsync(Stream xmlStream)
    {
        using var reader = new StreamReader(xmlStream, leaveOpen: false);
        return await reader.ReadToEndAsync();
    }

    private async Task<UploadXmlResponse?> CheckIdempotencyByHashAsync(string xmlHash)
    {
        var existingDoc = await _repository.GetByHashAsync(xmlHash);
        if (existingDoc == null)
            return null;

        _logger.LogInformation("Document with hash {Hash} already exists. Skipping.", xmlHash);
        return CreateDuplicateResponse(existingDoc.Id);
    }

    private async Task<UploadXmlResponse?> CheckDuplicationByKeyAsync(string documentKey)
    {
        var existingByKey = await _repository.GetByDocumentKeyAsync(documentKey);
        if (existingByKey == null)
            return null;

        _logger.LogInformation("Document with key {Key} already exists. Skipping.", documentKey);
        return CreateDuplicateResponse(existingByKey.Id);
    }

    private async Task SaveDocumentAsync(FiscalDocument document)
    {
        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();
    }

    private async Task PublishDocumentProcessedEventAsync(FiscalDocument document)
    {
        var evt = new DocumentProcessedEvent
        {
            DocumentId = document.Id,
            DocumentType = document.DocumentType,
            DocumentKey = document.DocumentKey,
            EmitterCnpj = document.EmitterCnpj,
            TotalValue = document.TotalValue
        };
        await _messagePublisher.PublishAsync(evt, AppConstants.RoutingKeys.DocumentProcessed);
    }

    private static UploadXmlResponse CreateDuplicateResponse(Guid documentId)
    {
        return new UploadXmlResponse
        {
            DocumentId = documentId,
            Message = "Document already exists (idempotency)",
            IsNewDocument = false
        };
    }

    private static UploadXmlResponse CreateSuccessResponse(Guid documentId)
    {
        return new UploadXmlResponse
        {
            DocumentId = documentId,
            Message = "Document processed successfully",
            IsNewDocument = true
        };
    }

    public async Task<PagedResult<DocumentSummaryDto>> GetDocumentsAsync(DocumentListRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.StartDate,
            request.EndDate,
            request.Cnpj,
            request.UF,
            request.DocumentType);

        var dtos = _mapper.Map<List<DocumentSummaryDto>>(items);

        return new PagedResult<DocumentSummaryDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DocumentDetailDto?> GetDocumentByIdAsync(Guid id)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document == null)
            return null;

        return _mapper.Map<DocumentDetailDto>(document);
    }

    public async Task<bool> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document == null)
            return false;

        document.Update(
            request.EmitterName,
            request.RecipientName,
            request.ProcessingStatus,
            request.AdditionalData);

        await _repository.UpdateAsync(document);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteDocumentAsync(Guid id)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document == null)
            return false;

        await _repository.DeleteAsync(document);
        await _repository.SaveChangesAsync();

        return true;
    }
}
