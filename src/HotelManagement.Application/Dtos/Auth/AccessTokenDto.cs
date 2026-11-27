namespace HotelManagement.Application.Dtos.Auth;

public sealed record AccessTokenDto(string AccessToken, DateTimeOffset ExpiresAtUtc);
