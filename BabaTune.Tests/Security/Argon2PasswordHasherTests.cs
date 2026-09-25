using BabaTune.Application.Implementations;

namespace BabaTune.Tests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _sut = new();

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrueAndWrongPasswordReturnsFalse()
    {
        var hash = _sut.HashPassword("correct horse");

        Assert.True(_sut.VerifyPassword("correct horse", hash));
        Assert.False(_sut.VerifyPassword("wrong horse", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashesOfExpectedLength()
    {
        var first = _sut.HashPassword("password");
        var second = _sut.HashPassword("password");

        Assert.NotEqual(first, second);
        Assert.Equal(48, Convert.FromBase64String(first).Length);
        Assert.Equal(48, Convert.FromBase64String(second).Length);
    }

    [Fact]
    public void Verify_TamperedHash_ReturnsFalse()
    {
        var bytes = Convert.FromBase64String(_sut.HashPassword("password"));
        bytes[^1] ^= 0xFF;

        Assert.False(_sut.VerifyPassword("password", Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Verify_MalformedHash_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _sut.VerifyPassword("password", "not-base64!"));
    }
}
