using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.Services;

public sealed class UpdateServiceDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999")]
    public decimal DefaultPrice { get; set; }

    [Required, MaxLength(30)]
    public string Unit { get; set; } = string.Empty;

    public bool Active { get; set; } = true;
}