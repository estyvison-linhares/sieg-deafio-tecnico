using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FiscalDocAPI.Application.Interfaces;
using FiscalDocAPI.Domain.Interfaces;
using FiscalDocAPI.Infrastructure.Messaging;
using FiscalDocAPI.Infrastructure.Persistence;
using FiscalDocAPI.Infrastructure.Security;
using FiscalDocAPI.Infrastructure.Xml;

namespace FiscalDocAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FiscalDocContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IFiscalDocumentRepository, FiscalDocumentRepository>();

        services.AddScoped<IXmlParser, XmlParser>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

        return services;
    }
}
