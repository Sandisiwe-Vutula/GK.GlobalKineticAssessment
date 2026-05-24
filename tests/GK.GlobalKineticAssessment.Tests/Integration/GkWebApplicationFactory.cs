using GK.GlobalKineticAssessment.Infrastructure.Encryption;
using GK.GlobalKineticAssessment.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Text;

namespace GK.GlobalKineticAssessment.Tests.Integration;

public sealed class GkWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestUsername = "gkadmin";
    private const string TestPassword = "GK@Assessment2026!";
    private readonly string _dbName = $"GKTest_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BasicAuth:Username"] = TestUsername,
                ["BasicAuth:Password"] = TestPassword,
                ["Encryption:Key"] = "zCCSkHsZFM+Xu3zpvnNBC4k4YbLXEky3bwYQuWIqYe4=",
                ["Encryption:IV"] = "tJ2XP0ZUJF+/+GzAQctSvQ==",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=GKTest;Trusted_Connection=True;",
                ["ConnectionStrings:Redis"] = ""
            }));

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.RemoveAll<IEncryptionService>();
            services.AddSingleton<IEncryptionService, NoOpEncryptionService>();

            services.AddDbContext<AppDbContext>((_, o) => o.UseInMemoryDatabase(_dbName));

            services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            services.AddDistributedMemoryCache();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{TestUsername}:{TestPassword}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
        return client;
    }

    private sealed class NoOpEncryptionService : IEncryptionService
    {
        public string Encrypt(string p) => p;
        public string Decrypt(string c) => c;
    }
}