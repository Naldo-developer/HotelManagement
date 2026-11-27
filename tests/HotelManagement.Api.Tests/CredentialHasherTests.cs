using HotelManagement.Infrastructure.Services;

namespace HotelManagement.Api.Tests;

public class CredentialHasherTests
{
    private readonly CredentialHasher _hasher = new();

    [Fact]
    public void Hash_is_not_the_plain_password_and_verifies()
    {
        var hash = _hasher.Hash("Clave-de-prueba-1");

        Assert.NotEqual("Clave-de-prueba-1", hash);
        Assert.True(_hasher.Verify(hash, "Clave-de-prueba-1"));
        Assert.False(_hasher.Verify(hash, "otra-clave"));
    }

    [Fact]
    public void Invalid_hash_does_not_verify()
    {
        Assert.False(_hasher.Verify("esto-no-es-un-hash", "Clave-de-prueba-1"));
    }
}