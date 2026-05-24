using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GK.GlobalKineticAssessment.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    private readonly IEncryptionService _encryption;
    public CustomerConfiguration(IEncryptionService encryption) => _encryption = encryption;

    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        var conv = new EncryptedStringConverter(_encryption);

        b.Property(c => c.FirstName).IsRequired().HasMaxLength(512).HasConversion(conv);
        b.Property(c => c.LastName).IsRequired().HasMaxLength(512).HasConversion(conv);
        b.Property(c => c.Email).IsRequired().HasMaxLength(512).HasConversion(conv);
        b.Property(c => c.Age).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        b.Property(c => c.UpdatedAt);
    }
}
