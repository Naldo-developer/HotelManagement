using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Reservations;

public class CreateReservationDto
{
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "La habitación debe tener un identificador positivo.")]
    public long RoomId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El huésped debe tener un identificador positivo.")]
    public long PrimaryGuestId { get; set; }

    [Required(ErrorMessage = "La fecha de entrada es obligatoria.")]
    public DateOnly? CheckInDate { get; set; }

    [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
    public DateOnly? CheckOutDate { get; set; }

    [Range(1, short.MaxValue,
        ErrorMessage = "Debe haber al menos un adulto.")]
    public short Adults { get; set; } = 1;

    [Range(0, short.MaxValue,
        ErrorMessage = "La cantidad de niños debe ser cero o mayor.")]
    public short Children { get; set; }

    public string? SpecialRequests { get; set; }
}