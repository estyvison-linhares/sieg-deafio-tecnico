using NUnit.Framework;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FiscalDocAPI.API.Controllers;
using FiscalDocAPI.Application.DTOs;
using FiscalDocAPI.Application.Interfaces;

namespace FiscalDocAPI.Tests.Controllers;

[TestFixture]
public class DocumentsControllerTests
{
    private Mock<IDocumentService> _documentServiceMock = null!;
    private Mock<ILogger<DocumentsController>> _loggerMock = null!;
    private DocumentsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _documentServiceMock = new Mock<IDocumentService>();
        _loggerMock = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_documentServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task UploadXml_WithValidFile_ShouldReturnOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "<?xml version=\"1.0\"?><root></root>";
        var fileName = "test.xml";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

        var expectedResponse = new UploadXmlResponse
        {
            DocumentId = Guid.NewGuid(),
            IsNewDocument = true,
            Message = "Documento processado com sucesso"
        };

        _documentServiceMock
            .Setup(x => x.ProcessXmlUploadAsync(It.IsAny<Stream>(), fileName))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UploadXml(fileMock.Object);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as UploadXmlResponse;
        response.Should().NotBeNull();
        response!.DocumentId.Should().Be(expectedResponse.DocumentId);
        response.IsNewDocument.Should().BeTrue();
    }

    [Test]
    public async Task UploadXml_WithNullFile_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.UploadXml(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetDocument_WithExistingId_ShouldReturnDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var expectedDocument = new DocumentDetailDto
        {
            Id = documentId,
            DocumentType = "NFe",
            DocumentKey = "12345",
            EmitterCnpj = "12345678000190",
            EmitterName = "Test Company",
            TotalValue = 1000
        };

        _documentServiceMock
            .Setup(x => x.GetDocumentByIdAsync(documentId))
            .ReturnsAsync(expectedDocument);

        // Act
        var result = await _controller.GetDocument(documentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as DocumentDetailDto;
        response.Should().NotBeNull();
        response!.Id.Should().Be(documentId);
        response.DocumentType.Should().Be("NFe");
    }

    [Test]
    public async Task GetDocument_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock
            .Setup(x => x.GetDocumentByIdAsync(documentId))
            .ReturnsAsync((DocumentDetailDto?)null);

        // Act
        var result = await _controller.GetDocument(documentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ListDocuments_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var expectedResult = new PagedResult<DocumentSummaryDto>
        {
            Items = new List<DocumentSummaryDto>
            {
                new DocumentSummaryDto
                {
                    DocumentId = Guid.NewGuid(),
                    DocumentType = "NFe",
                    EmitterCnpj = "12345678000190",
                    EmitterName = "Test Company",
                    TotalValue = 1000,
                    IssueDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _documentServiceMock
            .Setup(x => x.GetDocumentsAsync(It.IsAny<DocumentListRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ListDocuments(
            startDate: null,
            endDate: null,
            cnpj: "12345678000190",
            uf: null,
            documentType: null,
            page: 1,
            pageSize: 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var pagedResult = okResult!.Value as PagedResult<DocumentSummaryDto>;
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().HaveCount(1);
        pagedResult.Items[0].EmitterCnpj.Should().Be("12345678000190");
        pagedResult.TotalPages.Should().Be(1);
    }

    [Test]
    public async Task UpdateDocument_WithValidData_ShouldUpdateAndReturnOk()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var updateRequest = new UpdateDocumentRequest
        {
            ProcessingStatus = "Processed"
        };

        _documentServiceMock
            .Setup(x => x.UpdateDocumentAsync(documentId, updateRequest))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateDocument(documentId, updateRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task UpdateDocument_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var updateRequest = new UpdateDocumentRequest
        {
            ProcessingStatus = "Processed"
        };

        _documentServiceMock
            .Setup(x => x.UpdateDocumentAsync(documentId, updateRequest))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateDocument(documentId, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task DeleteDocument_WithExistingId_ShouldDeleteAndReturnOk()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock
            .Setup(x => x.DeleteDocumentAsync(documentId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task DeleteDocument_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentServiceMock
            .Setup(x => x.DeleteDocumentAsync(documentId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
