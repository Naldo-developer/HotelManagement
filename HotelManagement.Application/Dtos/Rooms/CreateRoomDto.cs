using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Rooms;

public class CreateRoomDto
{
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El hotel debe tener un identificador positivo.")]
    public long HotelId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El tipo de habitación debe tener un identificador positivo.")]
    public long RoomTypeId { get; set; }

    [Required(ErrorMessage = "El número de habitación es obligatorio.")]
    [StringLength(20,
        ErrorMessage = "El número de habitación admite hasta 20 caracteres.")]
    public string RoomNumber { get; set; } = string.Empty;

    [StringLength(20,
        ErrorMessage = "El piso admite hasta 20 caracteres.")]
    public string? Floor { get; set; }
    
    [Required(ErrorMessage = "El estado operativo es obligatorio.")]
    [RegularExpression("^(ACTIVE|OUT_OF_SERVICE|INACTIVE)$",
        ErrorMessage = "El estado debe ser ACTIVE, OUT_OF_SERVICE o INACTIVE.")]
    public string OperationalStatus { get; set; } = "ACTIVE";
}