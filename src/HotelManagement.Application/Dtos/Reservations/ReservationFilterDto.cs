using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Reservations;

public class ReservationFilterDto
{
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El hotel debe tener un identificador positivo.")]
    public long? HotelId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "La habitación debe tener un identificador positivo.")]
    public long? RoomId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El huésped debe tener un identificador positivo.")]
    public long? PrimaryGuestId { get; set; }

    [RegularExpression(
        "^(PENDING|CONFIRMED|CHECKED_IN|COMPLETED|CANCELLED|NO_SHOW)$",
        ErrorMessage = "El estado de la reserva no es válido.")]
    public string? Status { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "La página debe ser mayor o igual a uno.")]
    public int Page { get; set; } = 1;

    [Range(1, 100,
        ErrorMessage = "El tamaño de página debe estar entre uno y cien.")]
    public int PageSize { get; set; } = 10;
}