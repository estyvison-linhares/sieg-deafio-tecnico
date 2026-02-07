using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using FiscalDocAPI.Application.Interfaces;
using FiscalDocAPI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FiscalDocAPI.Infrastructure.Xml;

public class XmlParser : IXmlParser
{
    private readonly ILogger<XmlParser> _logger;

    public XmlParser(ILogger<XmlParser> logger)
    {
        _logger = logger;
    }

    public async Task<FiscalDocument> ParseXmlAsync(Stream xmlStream, string encryptedXml, string xmlHash)
    {
        using var reader = new StreamReader(xmlStream);
        var xmlContent = await reader.ReadToEndAsync();
        
        try
        {
            var xdoc = XDocument.Parse(xmlContent);
            var root = xdoc.Root;

            string docType;
            if (root?.Name.LocalName == "nfeProc" || root?.Descendants().Any(e => e.Name.LocalName == "NFe") == true)
            {
                docType = "NFe";
            }
            else if (root?.Name.LocalName == "cteProc" || root?.Descendants().Any(e => e.Name.LocalName == "CTe") == true)
            {
                docType = "CTe";
            }
            else if (root?.Descendants().Any(e => e.Name.LocalName == "infNfse") == true)
            {
                docType = "NFSe";
            }
            else
            {
                throw new InvalidOperationException("Tipo de documento fiscal não reconhecido");
            }

            var documentKey = ExtractDocumentKey(xdoc, docType);
            var emitter = ExtractEmitterData(xdoc, docType);
            var recipient = ExtractRecipientData(xdoc, docType);
            var totalValue = ExtractTotalValue(xdoc, docType);
            var issueDate = ExtractIssueDate(xdoc, docType);

            return FiscalDocument.Create(
                documentType: docType,
                documentKey: documentKey,
                emitterCnpj: emitter.Cnpj,
                emitterName: emitter.Name,
                emitterUF: emitter.UF,
                recipientCnpj: recipient.Cnpj,
                recipientName: recipient.Name,
                totalValue: totalValue,
                issueDate: issueDate,
                xmlContent: encryptedXml,
                xmlHash: xmlHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar XML");
            throw new InvalidOperationException($"Erro ao processar XML: {ex.Message}", ex);
        }
    }

    public string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static string ExtractDocumentKey(XDocument xdoc, string docType)
    {
        return docType switch
        {
            "NFe" => xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe")?.Attribute("Id")?.Value?.Replace("NFe", string.Empty) ?? string.Empty,
            "CTe" => xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infCte")?.Attribute("Id")?.Value?.Replace("CTe", string.Empty) ?? string.Empty,
            "NFSe" => xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Numero")?.Value ?? Guid.NewGuid().ToString(),
            _ => Guid.NewGuid().ToString()
        };
    }

    private static (string Cnpj, string Name, string UF) ExtractEmitterData(XDocument xdoc, string docType)
    {
        var emit = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "emit") ??
                   xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "PrestadorServico");
        
        if (emit == null)
            return (string.Empty, string.Empty, string.Empty);

        var cnpj = emit.Descendants().FirstOrDefault(e => e.Name.LocalName == "CNPJ")?.Value ?? string.Empty;
        var name = emit.Descendants().FirstOrDefault(e => e.Name.LocalName == "xNome")?.Value ??
                   emit.Descendants().FirstOrDefault(e => e.Name.LocalName == "RazaoSocial")?.Value ?? string.Empty;
        var uf = emit.Descendants().FirstOrDefault(e => e.Name.LocalName == "UF")?.Value ?? string.Empty;
        
        return (cnpj, name, uf);
    }

    private static (string Cnpj, string Name) ExtractRecipientData(XDocument xdoc, string docType)
    {
        var dest = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "dest") ??
                   xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "TomadorServico");
        
        if (dest == null)
            return (string.Empty, string.Empty);

        var cnpj = dest.Descendants().FirstOrDefault(e => e.Name.LocalName == "CNPJ")?.Value ??
                   dest.Descendants().FirstOrDefault(e => e.Name.LocalName == "CPF")?.Value ?? string.Empty;
        var name = dest.Descendants().FirstOrDefault(e => e.Name.LocalName == "xNome")?.Value ??
                   dest.Descendants().FirstOrDefault(e => e.Name.LocalName == "RazaoSocial")?.Value ?? string.Empty;
        
        return (cnpj, name);
    }

    private static decimal ExtractTotalValue(XDocument xdoc, string docType)
    {
        var total = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "vNF")?.Value ??
                   xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "vPrest")?.Value ??
                   xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "ValorServicos")?.Value ??
                   "0";
        
        return decimal.TryParse(total, out var value) ? value : 0;
    }

    private static DateTime ExtractIssueDate(XDocument xdoc, string docType)
    {
        var dateStr = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "dhEmi")?.Value ??
                     xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "dEmi")?.Value ??
                     xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DataEmissao")?.Value;
        
        if (DateTime.TryParse(dateStr, out var date))
            return date;
        
        return DateTime.UtcNow;
    }
}
