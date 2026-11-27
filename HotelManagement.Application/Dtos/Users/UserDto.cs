namespace HotelManagement.Application.Dtos.Users;

public sealed class UserDto
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}