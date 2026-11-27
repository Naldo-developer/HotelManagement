using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Guests;

namespace HotelManagement.Application.Dtos.Reservations;

/// <summary>Registra un huésped nuevo y su reserva como una sola operación.</summary>
public sealed class CreateGuestReservationDto
{
    [Required] public CreateGuestDto Guest { get; set; } = null!;
    [Range(typeof(long), "1", "9223372036854775807")] public long RoomId { get; set; }
    [Required] public DateOnly? CheckInDate { get; set; }
    [Required] public DateOnly? CheckOutDate { get; set; }
    [Range(1, short.MaxValue)] public short Adults { get; set; } = 1;
    [Range(0, short.MaxValue)] public short Children { get; set; }
    [StringLength(1000)] public string? SpecialRequests { get; set; }
}
