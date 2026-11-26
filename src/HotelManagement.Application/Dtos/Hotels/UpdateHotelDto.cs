using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Hotels;

public class UpdateHotelDto
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? TaxId { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(120)]
    public string? Email { get; set; }

    [Required, RegularExpression("^[A-Z]{3}$")]
    public string CurrencyCode { get; set; } = "PEN";

    [Required, StringLength(50)]
    public string Timezone { get; set; } = "America/Lima";
}