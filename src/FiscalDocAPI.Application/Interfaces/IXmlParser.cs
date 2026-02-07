using FiscalDocAPI.Domain.Entities;

namespace FiscalDocAPI.Application.Interfaces;

public interface IXmlParser
{
    Task<FiscalDocument> ParseXmlAsync(Stream xmlStream, string encryptedXml, string xmlHash);
    string ComputeHash(string content);
}
