using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GK.GlobalKineticAssessment.Infrastructure.Encryption;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration config)
    {
        var key = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");
        var iv  = config["Encryption:IV"]
            ?? throw new InvalidOperationException("Encryption:IV is not configured.");

        _key = Convert.FromBase64String(key);
        _iv  = Convert.FromBase64String(iv);

        if (_key.Length != 32)
            throw new InvalidOperationException($"Encryption:Key must be 32 bytes (256-bit). Got {_key.Length}.");
        if (_iv.Length != 16)
            throw new InvalidOperationException($"Encryption:IV must be 16 bytes (128-bit). Got {_iv.Length}.");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        using var aes = CreateAes();
        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(enc.TransformFinalBlock(bytes, 0, bytes.Length));
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        using var aes = CreateAes();
        using var dec = aes.CreateDecryptor();
        var bytes = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(dec.TransformFinalBlock(bytes, 0, bytes.Length));
    }

    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key     = _key;
        aes.IV      = _iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }
}
