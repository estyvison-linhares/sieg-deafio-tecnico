using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FiscalDocAPI.Infrastructure.Persistence;

namespace FiscalDocAPI.IntegrationTests;

public class WebApplicationFactoryFixture : WebApplicationFactory<Program>
{
  private readonly string _databaseName = $"TestDatabase_{Guid.NewGuid()}";

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
    {
      services.RemoveAll(typeof(DbContextOptions<FiscalDocContext>));

      services.AddDbContext<FiscalDocContext>(options =>
          {
          options.UseInMemoryDatabase(_databaseName);
        });

      var sp = services.BuildServiceProvider();
      using var scope = sp.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<FiscalDocContext>();

      db.Database.EnsureCreated();
    });

    builder.UseEnvironment("Testing");
  }
}
