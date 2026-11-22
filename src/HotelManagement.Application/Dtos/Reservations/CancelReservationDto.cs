using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Reservations;

public class CancelReservationDto
{
    [Required(ErrorMessage = "El motivo de cancelación es obligatorio.")]
    [StringLength(500,
        ErrorMessage = "El motivo no debe superar los 500 caracteres.")]
    public string CancellationReason { get; set; } = string.Empty;
}