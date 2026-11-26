namespace HotelManagement.Application.Dtos.RoomTypes;

public class RoomTypeDto
{
    public long RoomTypeId { get; set; }

    public long HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public short MaxAdults { get; set; }

    public short MaxChildren { get; set; }

    public decimal BaseRate { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }
}