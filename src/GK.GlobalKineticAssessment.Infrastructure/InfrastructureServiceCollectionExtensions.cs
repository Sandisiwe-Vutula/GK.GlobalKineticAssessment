using GK.GlobalKineticAssessment.Domain.Interfaces;
using GK.GlobalKineticAssessment.Infrastructure.Encryption;
using GK.GlobalKineticAssessment.Infrastructure.Persistence;
using GK.GlobalKineticAssessment.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GK.GlobalKineticAssessment.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
            services.AddStackExchangeRedisCache(o => { o.Configuration = redis; o.InstanceName = "GK:"; });
        else
            services.AddDistributedMemoryCache();

        services.AddDbContext<AppDbContext>((sp, o) =>
            o.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => { sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                         sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName); }));

        services.AddScoped<CustomerRepository>();
        services.AddScoped<ICustomerRepository>(sp =>
            new CachingCustomerRepository(
                sp.GetRequiredService<CustomerRepository>(),
                sp.GetRequiredService<IDistributedCache>(),
                sp.GetRequiredService<ILogger<CachingCustomerRepository>>()));

        return services;
    }
}
