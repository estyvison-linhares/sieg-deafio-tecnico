using NUnit.Framework;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using FiscalDocAPI.Infrastructure.Xml;

namespace FiscalDocAPI.Tests.Services;

[TestFixture]
public class XmlParserTests
{
    private XmlParser _parser = null!;
    private Mock<ILogger<XmlParser>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<XmlParser>>();
        _parser = new XmlParser(_loggerMock.Object);
    }

    [Test]
    public async Task ParseXmlAsync_WithNFeXml_ShouldParseCorrectly()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<NFe>
    <infNFe Id=""NFe35220312345678000195550010000000011234567890"">
        <emit>
            <CNPJ>12345678000195</CNPJ>
            <xNome>Empresa Teste LTDA</xNome>
            <enderEmit>
                <UF>SP</UF>
            </enderEmit>
        </emit>
        <dest>
            <CNPJ>98765432000198</CNPJ>
            <xNome>Cliente Teste</xNome>
        </dest>
        <total>
            <ICMSTot>
                <vNF>1500.00</vNF>
            </ICMSTot>
        </total>
        <ide>
            <dhEmi>2023-03-15T10:30:00</dhEmi>
        </ide>
    </infNFe>
</NFe>";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var encryptedXml = "encrypted_content";
        var xmlHash = "hash123";

        // Act
        var result = await _parser.ParseXmlAsync(stream, encryptedXml, xmlHash);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("NFe");
        result.DocumentKey.Should().Be("35220312345678000195550010000000011234567890");
        result.EmitterCnpj.Should().Be("12345678000195");
        result.EmitterName.Should().Be("Empresa Teste LTDA");
        result.EmitterUF.Should().Be("SP");
        result.RecipientCnpj.Should().Be("98765432000198");
        result.RecipientName.Should().Be("Cliente Teste");
        result.TotalValue.Should().Be(1500.00m);
        result.XmlContent.Should().Be(encryptedXml);
        result.XmlHash.Should().Be(xmlHash);
    }

    [Test]
    public async Task ParseXmlAsync_WithCTeXml_ShouldParseCorrectly()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<CTe>
    <infCte Id=""CTe35220312345678000195570010000000011234567890"">
        <emit>
            <CNPJ>12345678000195</CNPJ>
            <xNome>Transportadora Teste</xNome>
            <enderEmit>
                <UF>SP</UF>
            </enderEmit>
        </emit>
        <dest>
            <CNPJ>98765432000198</CNPJ>
            <xNome>Destinatário Teste</xNome>
        </dest>
        <vPrest>
            <vTPrest>350.00</vTPrest>
        </vPrest>
        <ide>
            <dhEmi>2023-03-20T14:30:00</dhEmi>
        </ide>
    </infCte>
</CTe>";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var encryptedXml = "encrypted_cte";
        var xmlHash = "hash_cte";

        // Act
        var result = await _parser.ParseXmlAsync(stream, encryptedXml, xmlHash);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("CTe");
        result.DocumentKey.Should().Be("35220312345678000195570010000000011234567890");
        result.EmitterCnpj.Should().Be("12345678000195");
        result.EmitterName.Should().Be("Transportadora Teste");
        result.TotalValue.Should().Be(350.00m);
    }

    [Test]
    public void ParseXmlAsync_WithInvalidXml_ShouldThrowException()
    {
        // Arrange
        var xmlContent = "This is not valid XML";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _parser.ParseXmlAsync(stream, "encrypted", "hash")
        );
    }

    [Test]
    public void ParseXmlAsync_WithUnrecognizedDocumentType_ShouldThrowException()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<UnknownDocument>
    <Data>Test</Data>
</UnknownDocument>";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _parser.ParseXmlAsync(stream, "encrypted", "hash")
        );
        ex.Message.Should().Contain("Tipo de documento fiscal não reconhecido");
    }

    [Test]
    public void ComputeHash_WithSameContent_ShouldReturnSameHash()
    {
        // Arrange
        var content = "Test content for hashing";

        // Act
        var hash1 = _parser.ComputeHash(content);
        var hash2 = _parser.ComputeHash(content);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void ComputeHash_WithDifferentContent_ShouldReturnDifferentHash()
    {
        // Arrange
        var content1 = "First content";
        var content2 = "Second content";

        // Act
        var hash1 = _parser.ComputeHash(content1);
        var hash2 = _parser.ComputeHash(content2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void ComputeHash_ShouldReturnLowercaseHex()
    {
        // Arrange
        var content = "Test content";

        // Act
        var hash = _parser.ComputeHash(content);

        // Assert
        hash.Should().MatchRegex("^[0-9a-f]+$");
        hash.Length.Should().Be(64);
    }

    [Test]
    public void ComputeHash_WithXmlContent_ShouldBeConsistent()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<NFe>
    <infNFe Id=""NFe35220312345678000195550010000000011234567890"">
        <emit><CNPJ>12345678000195</CNPJ></emit>
    </infNFe>
</NFe>";

        // Act
        var hash1 = _parser.ComputeHash(xmlContent);
        var hash2 = _parser.ComputeHash(xmlContent);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64);
    }

    [Test]
    public void ComputeHash_WithEmptyString_ShouldReturnValidHash()
    {
        // Arrange
        var content = "";

        // Act
        var hash = _parser.ComputeHash(content);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().Be(64);
    }

    [Test]
    public async Task ParseXmlAsync_WithMinimalNFe_ShouldHandleMissingFields()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<NFe>
    <infNFe Id=""NFe35220312345678000195550010000000011234567890"">
        <emit>
            <CNPJ>12345678000195</CNPJ>
        </emit>
    </infNFe>
</NFe>";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var encryptedXml = "encrypted";
        var xmlHash = "hash";

        // Act
        var result = await _parser.ParseXmlAsync(stream, encryptedXml, xmlHash);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("NFe");
        result.EmitterCnpj.Should().Be("12345678000195");
    }
}
