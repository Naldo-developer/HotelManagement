using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Consumptions;

public class CreateConsumptionDto
{
    [Range(1, long.MaxValue,
        ErrorMessage = "Debe indicar una estadía válida.")]
    public long StayId { get; set; }

    [Range(1, long.MaxValue,
        ErrorMessage = "Debe indicar un servicio válido.")]
    public long ServiceId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000",
        ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public decimal Quantity { get; set; }

    [MaxLength(1000,
        ErrorMessage = "Las notas no deben superar los 1000 caracteres.")]
    public string? Notes { get; set; }
}