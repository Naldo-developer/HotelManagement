using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Stays;

public sealed class CheckOutDto
{
    [Range(1, long.MaxValue)]
    public long StayId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}