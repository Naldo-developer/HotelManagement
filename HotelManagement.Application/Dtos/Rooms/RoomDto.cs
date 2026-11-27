namespace HotelManagement.Application.Dtos.Rooms;

public class RoomDto
{
    public long RoomId { get; set; }

    public long HotelId { get; set; }

    public long RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string? Floor { get; set; }

    public string OperationalStatus { get; set; } = string.Empty;
}