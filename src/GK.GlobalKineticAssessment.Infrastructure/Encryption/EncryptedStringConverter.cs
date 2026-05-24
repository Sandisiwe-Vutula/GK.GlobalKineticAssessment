using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GK.GlobalKineticAssessment.Infrastructure.Encryption;

public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService svc)
        : base(plain => svc.Encrypt(plain), cipher => svc.Decrypt(cipher)) { }
}
