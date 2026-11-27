using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Infrastructure.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required] public string Issuer { get; set; } = "HotelManagement.Api";
    [Required] public string Audience { get; set; } = "HotelManagement.Web";
    [Required] public string SigningKey { get; set; } = string.Empty;
    [Range(1, 60)] public int AccessTokenMinutes { get; set; } = 15;
}
