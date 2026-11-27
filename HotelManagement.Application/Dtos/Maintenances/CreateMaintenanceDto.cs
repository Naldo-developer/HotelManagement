using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Maintenances;

public class CreateMaintenanceDto
{
    [Range(1, long.MaxValue)]
    public long RoomId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    [Required]
    public string Description { get; set; } = null!;
}