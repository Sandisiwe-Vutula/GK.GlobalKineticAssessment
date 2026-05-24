using Xunit;
using FluentAssertions;
using GK.GlobalKineticAssessment.Infrastructure.Encryption;
using GK.GlobalKineticAssessment.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace GK.GlobalKineticAssessment.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class AesEncryptionServiceTests
{
    private readonly IEncryptionService _sut;

    public AesEncryptionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "zCCSkHsZFM+Xu3zpvnNBC4k4YbLXEky3bwYQuWIqYe4=",
                ["Encryption:IV"] = "tJ2XP0ZUJF+/+GzAQctSvQ=="
            })
            .Build();
        _sut = new AesEncryptionService(config);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginal()
    {
        var customer = CustomerBuilder.Default();
        var cipher = _sut.Encrypt(customer.Email);
        _sut.Decrypt(cipher).Should().Be(customer.Email);
    }

    [Fact]
    public void Encrypt_ProducesDifferentOutput()
    {
        var customer = CustomerBuilder.Unique();
        _sut.Encrypt(customer.Email).Should().NotBe(customer.Email);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        _sut.Encrypt("").Should().Be("");
        _sut.Decrypt("").Should().Be("");
    }


    [Fact]
    public void Constructor_WrongKeyLength_Throws()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = Convert.ToBase64String(new byte[16]),
                ["Encryption:IV"] = Convert.ToBase64String(new byte[16])
            }).Build();
        Assert.Throws<InvalidOperationException>(() => new AesEncryptionService(cfg));
    }
}