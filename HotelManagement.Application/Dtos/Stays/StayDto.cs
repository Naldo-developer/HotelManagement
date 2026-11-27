namespace HotelManagement.Application.Dtos.Stays;

public class StayDto
{
    public long StayId { get; set; }

    public long ReservationId { get; set; }

    public long RoomId { get; set; }

    public DateTime CheckInAt { get; set; }

    public DateTime? CheckOutAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public long? CheckInByUserId { get; set; }

    public long? CheckOutByUserId { get; set; }

    public string? Notes { get; set; }
}