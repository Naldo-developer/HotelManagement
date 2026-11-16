using HotelManagement.Application.Security;
using Microsoft.AspNetCore.Identity;

namespace HotelManagement.Infrastructure.Security;

public sealed class CredentialHasher : ICredentialHasher
{
    private static readonly object HashOwner = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashOwner, password);

    public bool Verify(string hash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(HashOwner, hash, password)
                   != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}