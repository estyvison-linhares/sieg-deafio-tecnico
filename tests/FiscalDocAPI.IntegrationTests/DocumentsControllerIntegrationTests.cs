using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using NUnit.Framework;

namespace FiscalDocAPI.IntegrationTests;

[TestFixture]
public class DocumentsControllerIntegrationTests
{
  private WebApplicationFactoryFixture _factory = null!;
  private HttpClient _client = null!;

  [SetUp]
  public void Setup()
  {
    _factory = new WebApplicationFactoryFixture();
    _client = _factory.CreateClient();
  }

  [TearDown]
  public void TearDown()
  {
    _client?.Dispose();
    _factory?.Dispose();
  }

  [Test]
  public async Task UploadXml_ValidNFe_ReturnsOkWithDocumentId()
  {
    // Arrange
    var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "nfe_test.xml");
    var xmlContent = await File.ReadAllBytesAsync(xmlPath);

    using var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(xmlContent);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
    content.Add(fileContent, "xmlFile", "nfe_test.xml");

    // Act
    var response = await _client.PostAsync("/api/documents/upload", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var responseContent = await response.Content.ReadAsStringAsync();
    responseContent.Should().Contain("documentId");
    responseContent.Should().Contain("isNewDocument");
  }

  [Test]
  public async Task UploadXml_NoFile_ReturnsBadRequest()
  {
    // Arrange
    using var content = new MultipartFormDataContent();

    // Act
    var response = await _client.PostAsync("/api/documents/upload", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ListDocuments_ReturnsPagedResult()
  {
    // Arrange
    var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "nfe_test.xml");
    var xmlContent = await File.ReadAllBytesAsync(xmlPath);

    using var uploadContent = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(xmlContent);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
    uploadContent.Add(fileContent, "xmlFile", "nfe_test.xml");

    await _client.PostAsync("/api/documents/upload", uploadContent);

    // Act
    var response = await _client.GetAsync("/api/documents?page=1&pageSize=10");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var responseContent = await response.Content.ReadAsStringAsync();
    responseContent.Should().Contain("items");
    responseContent.Should().Contain("page");
    responseContent.Should().Contain("totalCount");
  }

  [Test]
  public async Task GetDocumentById_ExistingDocument_ReturnsDocument()
  {
    // Arrange
    var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "nfe_test.xml");
    var xmlContent = await File.ReadAllBytesAsync(xmlPath);

    using var uploadContent = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(xmlContent);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
    uploadContent.Add(fileContent, "xmlFile", "nfe_test.xml");

    var uploadResponse = await _client.PostAsync("/api/documents/upload", uploadContent);
    var uploadJson = await uploadResponse.Content.ReadAsStringAsync();

    var documentIdStart = uploadJson.IndexOf("\"documentId\":\"") + 14;
    var documentIdEnd = uploadJson.IndexOf("\"", documentIdStart);
    var documentId = uploadJson.Substring(documentIdStart, documentIdEnd - documentIdStart);

    // Act
    var response = await _client.GetAsync($"/api/documents/{documentId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var responseContent = await response.Content.ReadAsStringAsync();
    responseContent.Should().Contain("documentKey");
    responseContent.Should().Contain("emitterCnpj");
  }

  [Test]
  public async Task GetDocumentById_NonExistingDocument_ReturnsNotFound()
  {
    // Arrange
    var nonExistingId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/api/documents/{nonExistingId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Test]
  public async Task DeleteDocument_ExistingDocument_ReturnsOk()
  {
    // Arrange
    var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "nfe_test.xml");
    var xmlContent = await File.ReadAllBytesAsync(xmlPath);

    using var uploadContent = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(xmlContent);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
    uploadContent.Add(fileContent, "xmlFile", "nfe_test.xml");

    var uploadResponse = await _client.PostAsync("/api/documents/upload", uploadContent);
    var uploadJson = await uploadResponse.Content.ReadAsStringAsync();

    var documentIdStart = uploadJson.IndexOf("\"documentId\":\"") + 14;
    var documentIdEnd = uploadJson.IndexOf("\"", documentIdStart);
    var documentId = uploadJson.Substring(documentIdStart, documentIdEnd - documentIdStart);

    // Act
    var response = await _client.DeleteAsync($"/api/documents/{documentId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Test]
  public async Task HealthCheck_ReturnsOk()
  {
    // Act
    var response = await _client.GetAsync("/health");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var responseContent = await response.Content.ReadAsStringAsync();
    responseContent.Should().Contain("Healthy");
  }
}
