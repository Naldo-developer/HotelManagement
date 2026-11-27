using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Stays;

public class CheckInDto
{
    [Range(1, long.MaxValue,
        ErrorMessage = "Debe indicar una reserva válida.")]
    public long ReservationId { get; set; }

    [MaxLength(1000,
        ErrorMessage = "Las notas no deben superar los 1000 caracteres.")]
    public string? Notes { get; set; }
}