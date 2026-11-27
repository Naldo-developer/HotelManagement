namespace HotelManagement.Application.Dtos.Employees;

public sealed class EmployeeDto
{
    public long EmployeeId { get; set; }
    public long HotelId { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool Active { get; set; }
}