using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Users;

public sealed class CreateUserDto
{
    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Required, StringLength(60, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<string> RoleCodes { get; set; } = [];
}