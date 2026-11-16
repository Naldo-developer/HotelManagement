using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Rooms;

public class RoomAvailabilityFilterDto
{
    [Required(ErrorMessage = "El hotel es obligatorio.")]
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El hotel debe tener un identificador positivo.")]
    public long? HotelId { get; set; }

    [Required(ErrorMessage = "La fecha de entrada es obligatoria.")]
    public DateOnly? CheckInDate { get; set; }

    [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
    public DateOnly? CheckOutDate { get; set; }

    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El tipo de habitación debe tener un identificador positivo.")]
    public long? RoomTypeId { get; set; }
}