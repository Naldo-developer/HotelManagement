namespace HotelManagement.Application.DTOs.Auth;

public class AuthenticatedUserDto
{
    public long UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];
}