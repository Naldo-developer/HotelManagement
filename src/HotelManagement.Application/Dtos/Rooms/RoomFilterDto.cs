using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Rooms;

public class RoomFilterDto
{
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El hotel debe tener un identificador positivo.")]
    public long? HotelId { get; set; }

    [RegularExpression("^(ACTIVE|OUT_OF_SERVICE|INACTIVE)$",
        ErrorMessage = "El estado debe ser ACTIVE, OUT_OF_SERVICE o INACTIVE.")]
    public string? OperationalStatus { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "La página debe ser mayor o igual a 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100,
        ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = 10;
}