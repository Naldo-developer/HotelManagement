using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Rooms;

public class UpdateRoomOperationalStatusDto
{
    [Required(ErrorMessage = "El estado operativo es obligatorio.")]
    [RegularExpression("^(ACTIVE|OUT_OF_SERVICE|INACTIVE)$",
        ErrorMessage = "El estado debe ser ACTIVE, OUT_OF_SERVICE o INACTIVE.")]
    public string OperationalStatus { get; set; } = string.Empty;
}