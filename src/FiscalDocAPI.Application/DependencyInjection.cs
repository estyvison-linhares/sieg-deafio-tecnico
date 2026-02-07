using Microsoft.Extensions.DependencyInjection;
using FiscalDocAPI.Application.Interfaces;
using FiscalDocAPI.Application.Services;

namespace FiscalDocAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        
        return services;
    }
}
