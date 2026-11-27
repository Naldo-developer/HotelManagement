namespace HotelManagement.Application.Interfaces.Services;

public interface ICredentialHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}