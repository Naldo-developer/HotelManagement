namespace HotelManagement.Application.Dtos.Hotels;

public class HotelDto
{
    public long HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? TaxId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string Timezone { get; set; } = string.Empty;

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }
}