using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Employees;

public sealed class CreateEmployeeDto
{
    [Range(1, long.MaxValue)]
    public long HotelId { get; set; }

    [MaxLength(20)]
    public string? DocumentType { get; set; }

    [MaxLength(30)]
    public string? DocumentNumber { get; set; }

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Position { get; set; }

    [EmailAddress, MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }
}