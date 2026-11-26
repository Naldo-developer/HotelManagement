namespace HotelManagement.Application.Dtos.Services;

public class ServiceDto
{
    public long ServiceId { get; set; }

    public long HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal DefaultPrice { get; set; }

    public string Unit { get; set; } = string.Empty;

    public bool Active { get; set; }
}