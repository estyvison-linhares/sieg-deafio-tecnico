using Microsoft.AspNetCore.Mvc;
using FiscalDocAPI.Application.DTOs;
using FiscalDocAPI.Application.Interfaces;
using FiscalDocAPI.Domain.Constants;

namespace FiscalDocAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(
    IDocumentService documentService,
    ILogger<DocumentsController> logger) : ControllerBase
{
  private readonly IDocumentService _documentService = documentService;
  private readonly ILogger<DocumentsController> _logger = logger;

  [HttpPost("upload")]
  [Consumes("multipart/form-data")]
  [ProducesResponseType(typeof(UploadXmlResponse), 200)]
  [ProducesResponseType(400)]
  public async Task<ActionResult<UploadXmlResponse>> UploadXml(IFormFile xmlFile)
  {
    if (xmlFile == null || xmlFile.Length == 0)
    {
      return BadRequest(new { error = AppConstants.ValidationMessages.XmlFileRequired });
    }

    if (!xmlFile.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
    {
      return BadRequest(new { error = AppConstants.ValidationMessages.InvalidXmlExtension });
    }

    try
    {
      using var stream = xmlFile.OpenReadStream();
      var result = await _documentService.ProcessXmlUploadAsync(stream, xmlFile.FileName);
      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing XML");
      return BadRequest(new { error = $"Error processing XML: {ex.Message}" });
    }
  }

  [HttpGet]
  [ProducesResponseType(typeof(PagedResult<DocumentSummaryDto>), 200)]
  public async Task<ActionResult<PagedResult<DocumentSummaryDto>>> ListDocuments(
      [FromQuery] DateTime? startDate,
      [FromQuery] DateTime? endDate,
      [FromQuery] string? cnpj,
      [FromQuery] string? uf,
      [FromQuery] string? documentType,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10)
  {
    var request = new DocumentListRequest
    {
      StartDate = startDate,
      EndDate = endDate,
      Cnpj = cnpj,
      UF = uf,
      DocumentType = documentType,
      Page = page < AppConstants.Pagination.MinPageSize ? AppConstants.Pagination.DefaultPage : page,
      PageSize = pageSize < AppConstants.Pagination.MinPageSize || pageSize > AppConstants.Pagination.MaxPageSize 
        ? AppConstants.Pagination.DefaultPageSize 
        : pageSize
    };

    var result = await _documentService.GetDocumentsAsync(request);
    return Ok(result);
  }

  [HttpGet("{id}")]
  [ProducesResponseType(typeof(DocumentDetailDto), 200)]
  [ProducesResponseType(404)]
  public async Task<ActionResult<DocumentDetailDto>> GetDocument(Guid id)
  {
    var document = await _documentService.GetDocumentByIdAsync(id);

    if (document == null)
    {
      return NotFound(new { error = AppConstants.ValidationMessages.DocumentNotFound });
    }

    return Ok(document);
  }

  [HttpPut("{id}")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentRequest request)
  {
    var success = await _documentService.UpdateDocumentAsync(id, request);

    if (!success)
    {
      return NotFound(new { error = AppConstants.ValidationMessages.DocumentNotFound });
    }

    return Ok(new { message = AppConstants.ValidationMessages.DocumentUpdatedSuccessfully });
  }

  [HttpDelete("{id}")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  public async Task<IActionResult> DeleteDocument(Guid id)
  {
    var success = await _documentService.DeleteDocumentAsync(id);

    if (!success)
    {
      return NotFound(new { error = AppConstants.ValidationMessages.DocumentNotFound });
    }

    return Ok(new { message = AppConstants.ValidationMessages.DocumentDeletedSuccessfully });
  }
}