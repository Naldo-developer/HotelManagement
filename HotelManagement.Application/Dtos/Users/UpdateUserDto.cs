using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Users;

public sealed class UpdateUserDto
{
    [Required, StringLength(60, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }

    [Required, MinLength(1)]
    public List<string> RoleCodes { get; set; } = [];

    public bool Active { get; set; } = true;
}