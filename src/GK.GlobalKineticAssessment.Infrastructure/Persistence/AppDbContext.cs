using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Infrastructure.Encryption;
using GK.GlobalKineticAssessment.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace GK.GlobalKineticAssessment.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IEncryptionService _encryption;

    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService encryption)
        : base(options)
    {
        _encryption = encryption;
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CustomerConfiguration(_encryption));
    }
}
