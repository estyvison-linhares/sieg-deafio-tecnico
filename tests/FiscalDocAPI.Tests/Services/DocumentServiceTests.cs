using NUnit.Framework;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using FiscalDocAPI.Application.Services;
using FiscalDocAPI.Application.DTOs;
using FiscalDocAPI.Application.Interfaces;
using FiscalDocAPI.Domain.Entities;
using FiscalDocAPI.Domain.Events;
using FiscalDocAPI.Domain.Interfaces;

namespace FiscalDocAPI.Tests.Services;

[TestFixture]
public class DocumentServiceTests
{
  private Mock<IFiscalDocumentRepository> _repositoryMock = null!;
  private Mock<IXmlParser> _xmlParserMock = null!;
  private Mock<IEncryptionService> _encryptionServiceMock = null!;
  private Mock<IMessagePublisher> _messagePublisherMock = null!;
  private Mock<ILogger<DocumentService>> _loggerMock = null!;
  private DocumentService _service = null!;

  [SetUp]
  public void Setup()
  {
    _repositoryMock = new Mock<IFiscalDocumentRepository>();
    _xmlParserMock = new Mock<IXmlParser>();
    _encryptionServiceMock = new Mock<IEncryptionService>();
    _messagePublisherMock = new Mock<IMessagePublisher>();
    _loggerMock = new Mock<ILogger<DocumentService>>();

    _service = new DocumentService(
        _repositoryMock.Object,
        _xmlParserMock.Object,
        _encryptionServiceMock.Object,
        _messagePublisherMock.Object,
        _loggerMock.Object
    );
  }

  [Test]
  public async Task ProcessXmlUploadAsync_WithNewDocument_ShouldCreateSuccessfully()
  {
    // Arrange
    var xmlContent = "<?xml version=\"1.0\"?><NFe><infNFe><emit><CNPJ>12345678000195</CNPJ></emit></infNFe></NFe>";
    var xmlHash = "hash123";
    var encryptedXml = "encrypted_content";
    var documentKey = "35220312345678000195550010000000011234567890";

    var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

    var document = FiscalDocument.Create(
        "NFe",
        documentKey,
        "12345678000195",
        "Empresa Teste",
        "SP",
        null,
        null,
        1500.00m,
        DateTime.Parse("2023-03-15"),
        encryptedXml,
        xmlHash
    );

    _xmlParserMock.Setup(x => x.ComputeHash(xmlContent)).Returns(xmlHash);
    _repositoryMock.Setup(x => x.GetByHashAsync(xmlHash)).ReturnsAsync((FiscalDocument?)null);
    _encryptionServiceMock.Setup(x => x.Encrypt(xmlContent)).Returns(encryptedXml);
    _xmlParserMock.Setup(x => x.ParseXmlAsync(It.IsAny<Stream>(), encryptedXml, xmlHash))
        .ReturnsAsync(document);
    _repositoryMock.Setup(x => x.GetByDocumentKeyAsync(documentKey)).ReturnsAsync((FiscalDocument?)null);
    _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FiscalDocument>())).Returns(Task.CompletedTask);
    _repositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
    _messagePublisherMock.Setup(x => x.PublishAsync(It.IsAny<DocumentProcessedEvent>(), It.IsAny<string>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _service.ProcessXmlUploadAsync(stream, "test.xml");

    // Assert
    result.Should().NotBeNull();
    result.DocumentId.Should().Be(document.Id);
    result.IsNewDocument.Should().BeTrue();
    result.Message.Should().Be("Documento processado com sucesso");

    _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FiscalDocument>()), Times.Once);
    _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    _messagePublisherMock.Verify(x => x.PublishAsync(It.IsAny<DocumentProcessedEvent>(), "fiscal.document.processed"), Times.Once);
  }

  [Test]
  public async Task ProcessXmlUploadAsync_WithDuplicateHash_ShouldReturnIdempotencyResponse()
  {
    // Arrange
    var xmlContent = "<?xml version=\"1.0\"?><NFe></NFe>";
    var xmlHash = "hash123";
    var existingDocId = Guid.NewGuid();

    var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

    var existingDoc = FiscalDocument.Create(
        "NFe",
        "35220312345678000195550010000000011234567890",
        "12345678000195",
        "Empresa Teste",
        "SP",
        null,
        null,
        1500.00m,
        DateTime.Parse("2023-03-15"),
        "encrypted",
        xmlHash
    );

    _xmlParserMock.Setup(x => x.ComputeHash(xmlContent)).Returns(xmlHash);
    _repositoryMock.Setup(x => x.GetByHashAsync(xmlHash)).ReturnsAsync(existingDoc);

    // Act
    var result = await _service.ProcessXmlUploadAsync(stream, "test.xml");

    // Assert
    result.Should().NotBeNull();
    result.DocumentId.Should().Be(existingDoc.Id);
    result.IsNewDocument.Should().BeFalse();
    result.Message.Should().Be("Documento já existente (idempotência)");

    _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FiscalDocument>()), Times.Never);
    _messagePublisherMock.Verify(x => x.PublishAsync(It.IsAny<DocumentProcessedEvent>(), It.IsAny<string>()), Times.Never);
  }

  [Test]
  public async Task ProcessXmlUploadAsync_WithDuplicateKey_ShouldReturnDuplicationResponse()
  {
    // Arrange
    var xmlContent = "<?xml version=\"1.0\"?><NFe></NFe>";
    var xmlHash = "new_hash";
    var encryptedXml = "encrypted_content";
    var documentKey = "35220312345678000195550010000000011234567890";

    var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

    var document = FiscalDocument.Create(
        "NFe",
        documentKey,
        "12345678000195",
        "Empresa Teste",
        "SP",
        null,
        null,
        1500.00m,
        DateTime.Parse("2023-03-15"),
        encryptedXml,
        xmlHash
    );

    var existingDoc = FiscalDocument.Create(
        "NFe",
        documentKey,
        "12345678000195",
        "Empresa Teste",
        "SP",
        null,
        null,
        1500.00m,
        DateTime.Parse("2023-03-15"),
        "old_encrypted",
        "old_hash"
    );

    _xmlParserMock.Setup(x => x.ComputeHash(xmlContent)).Returns(xmlHash);
    _repositoryMock.Setup(x => x.GetByHashAsync(xmlHash)).ReturnsAsync((FiscalDocument?)null);
    _encryptionServiceMock.Setup(x => x.Encrypt(xmlContent)).Returns(encryptedXml);
    _xmlParserMock.Setup(x => x.ParseXmlAsync(It.IsAny<Stream>(), encryptedXml, xmlHash))
        .ReturnsAsync(document);
    _repositoryMock.Setup(x => x.GetByDocumentKeyAsync(documentKey)).ReturnsAsync(existingDoc);

    // Act
    var result = await _service.ProcessXmlUploadAsync(stream, "test.xml");

    // Assert
    result.Should().NotBeNull();
    result.DocumentId.Should().Be(existingDoc.Id);
    result.IsNewDocument.Should().BeFalse();
    result.Message.Should().Be("Documento já existente (idempotência)");

    _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FiscalDocument>()), Times.Never);
    _messagePublisherMock.Verify(x => x.PublishAsync(It.IsAny<DocumentProcessedEvent>(), It.IsAny<string>()), Times.Never);
  }

  [Test]
  public async Task GetDocumentsAsync_ShouldReturnPagedResult()
  {
    // Arrange
    var documents = new List<FiscalDocument>
        {
            FiscalDocument.Create("NFe", "key1", "12345678000195", "Empresa 1", "SP", null, null, 1000m, DateTime.Now, "enc1", "hash1"),
            FiscalDocument.Create("CTe", "key2", "98765432000198", "Empresa 2", "RJ", null, null, 2000m, DateTime.Now, "enc2", "hash2")
        };

    var request = new DocumentListRequest
    {
      Page = 1,
      PageSize = 10
    };

    _repositoryMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, null))
        .ReturnsAsync((documents, 2));

    // Act
    var result = await _service.GetDocumentsAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCount(2);
    result.TotalCount.Should().Be(2);
    result.Page.Should().Be(1);
    result.PageSize.Should().Be(10);
    result.Items[0].DocumentType.Should().Be("NFe");
    result.Items[1].DocumentType.Should().Be("CTe");
  }

  [Test]
  public async Task GetDocumentsAsync_WithFilters_ShouldApplyFilters()
  {
    // Arrange
    var document = FiscalDocument.Create("NFe", "key1", "12345678000195", "Empresa 1", "SP", null, null, 1000m, DateTime.Now, "enc1", "hash1");

    var request = new DocumentListRequest
    {
      Page = 1,
      PageSize = 10,
      Cnpj = "12345678000195",
      UF = "SP",
      DocumentType = "NFe"
    };

    _repositoryMock.Setup(x => x.GetPagedAsync(1, 10, null, null, "12345678000195", "SP", "NFe"))
        .ReturnsAsync((new List<FiscalDocument> { document }, 1));

    // Act
    var result = await _service.GetDocumentsAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCount(1);
    result.TotalCount.Should().Be(1);
    result.Items[0].EmitterCnpj.Should().Be("12345678000195");
    result.Items[0].DocumentType.Should().Be("NFe");
  }

  [Test]
  public async Task GetDocumentByIdAsync_WithExistingId_ShouldReturnDocument()
  {
    // Arrange
    var documentId = Guid.NewGuid();
    var document = FiscalDocument.Create(
        "NFe",
        "key1",
        "12345678000195",
        "Empresa Teste",
        "SP",
        "98765432000198",
        "Cliente Teste",
        1500m,
        DateTime.Parse("2023-03-15"),
        "encrypted",
        "hash1"
    );

    _repositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);

    // Act
    var result = await _service.GetDocumentByIdAsync(documentId);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(document.Id);
    result.DocumentType.Should().Be("NFe");
    result.DocumentKey.Should().Be("key1");
    result.EmitterCnpj.Should().Be("12345678000195");
    result.EmitterName.Should().Be("Empresa Teste");
    result.TotalValue.Should().Be(1500m);
  }

  [Test]
  public async Task GetDocumentByIdAsync_WithNonExistingId_ShouldReturnNull()
  {
    // Arrange
    var documentId = Guid.NewGuid();
    _repositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((FiscalDocument?)null);

    // Act
    var result = await _service.GetDocumentByIdAsync(documentId);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public async Task UpdateDocumentAsync_WithExistingId_ShouldUpdateAndReturnTrue()
  {
    // Arrange
    var documentId = Guid.NewGuid();
    var document = FiscalDocument.Create(
        "NFe",
        "key1",
        "12345678000195",
        "Empresa Teste",
        "SP",
        null,
        null,
        1500m,
        DateTime.Now,
        "encrypted",
        "hash1"
    );

    var updateRequest = new UpdateDocumentRequest
    {
      EmitterName = "Empresa Atualizada",
      RecipientName = "Cliente Atualizado",
      ProcessingStatus = "Processed",
      AdditionalData = "{\"info\":\"updated\"}"
    };

    _repositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
    _repositoryMock.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
    _repositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

    // Act
    var result = await _service.UpdateDocumentAsync(documentId, updateRequest);

    // Assert
    result.Should().BeTrue();
    _repositoryMock.Verify(x => x.UpdateAsync(document), Times.Once);
    _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
  }

  [Test]
  public async Task UpdateDocumentAsync_WithNonExistingId_ShouldReturnFalse()
  {
    // Arrange
    var documentId = Guid.NewGuid();
    var updateRequest = new UpdateDocumentRequest
    {
      EmitterName = "Empresa Atualizada"
    };

    _repositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((FiscalDocument?)null);

    // Act
    var result = await _service.UpdateDocumentAsync(documentId, updateRequest);

    // Assert
    result.Should().BeFalse();
    _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FiscalDocument>()), Times.Never);
    _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
  }

  [Test]
  public async Task DeleteDocumentAsync_WithExistingId_ShouldDeleteAndReturnTrue()
  {
    // Arrange
    var documentId = Guid.NewGuid();
    var document = FiscalDocument.Create(
        "NFe",
        "key1",
        "12345678000195",
        "Empresa Teste",
        "SP",
        null,
        null,
        1500m,
        DateTime.Now,
        "encrypted",
        "hash1"
    );

    _repositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
    _repositoryMock.Setup(x => x.DeleteAsync(document)).Returns(Task.CompletedTask);
    _repositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

    // Act
    var result = await _service.DeleteDocumentAsync(documentId);

    // Assert
    result.Should().BeTrue();
    _repositoryMock.Verify(x => x.DeleteAsync(document), Times.Once);
    _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
  }

  [Test]
  public async Task DeleteDocumentAsync_WithNonExistingId_ShouldReturnFalse()
  {
    // Arrange
    var documentId = Guid.NewGuid();
    _repositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((FiscalDocument?)null);

    // Act
    var result = await _service.DeleteDocumentAsync(documentId);

    // Assert
    result.Should().BeFalse();
    _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<FiscalDocument>()), Times.Never);
    _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
  }
}
