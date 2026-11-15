namespace HotelManagement.Application.Security;

public interface ICredentialHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}