using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Maintenances;

public class CloseMaintenanceDto
{
    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Cost { get; set; }
}