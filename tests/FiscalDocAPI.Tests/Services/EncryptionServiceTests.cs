using NUnit.Framework;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using FiscalDocAPI.Infrastructure.Security;

namespace FiscalDocAPI.Tests.Services;

[TestFixture]
public class EncryptionServiceTests
{
    private EncryptionService _service = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void Setup()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption:Key"] = "12345678901234567890123456789012",
            ["Encryption:IV"] = "1234567890123456"
        });
        _configuration = configBuilder.Build();
        _service = new EncryptionService(_configuration);
    }

    [Test]
    public void Encrypt_WithValidText_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "This is a test message";

        // Act
        var result = _service.Encrypt(plainText);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe(plainText);
    }

    [Test]
    public void Decrypt_WithEncryptedText_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "This is a test message";
        var encrypted = _service.Encrypt(plainText);

        // Act
        var result = _service.Decrypt(encrypted);

        // Assert
        result.Should().Be(plainText);
    }

    [Test]
    public void Encrypt_SameTextTwice_ShouldReturnSameEncryptedValue()
    {
        // Arrange
        var plainText = "Consistent encryption test";

        // Act
        var encrypted1 = _service.Encrypt(plainText);
        var encrypted2 = _service.Encrypt(plainText);

        // Assert
        encrypted1.Should().Be(encrypted2);
    }

    [Test]
    public void EncryptDecrypt_WithXmlContent_ShouldWorkCorrectly()
    {
        // Arrange
        var xmlContent = @"<?xml version=""1.0""?>
<NFe>
    <infNFe>
        <emit>
            <CNPJ>12345678000195</CNPJ>
            <xNome>Empresa Teste LTDA</xNome>
        </emit>
        <total>
            <ICMSTot>
                <vNF>1500.00</vNF>
            </ICMSTot>
        </total>
    </infNFe>
</NFe>";

        // Act
        var encrypted = _service.Encrypt(xmlContent);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(xmlContent);
    }

    [Test]
    public void Constructor_WithMissingKey_ShouldThrowException()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption:IV"] = "1234567890123456"
        });
        var config = configBuilder.Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new EncryptionService(config));
    }

    [Test]
    public void Constructor_WithMissingIV_ShouldThrowException()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption:Key"] = "12345678901234567890123456789012"
        });
        var config = configBuilder.Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new EncryptionService(config));
    }

    [Test]
    public void Encrypt_WithEmptyString_ShouldReturnEncryptedEmpty()
    {
        // Arrange
        var plainText = "";

        // Act
        var result = _service.Encrypt(plainText);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var decrypted = _service.Decrypt(result);
        decrypted.Should().Be(plainText);
    }

    [Test]
    public void Encrypt_WithSpecialCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        var plainText = "Test with special chars: àáâãäåèéêë ñ ç @#$%¨&*()";

        // Act
        var encrypted = _service.Encrypt(plainText);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }
}
