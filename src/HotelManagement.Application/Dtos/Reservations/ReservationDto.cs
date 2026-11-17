namespace HotelManagement.Application.Dtos.Reservations;

public class ReservationDto
{
    public long ReservationId { get; set; }

    public string ConfirmationCode { get; set; } = string.Empty;

    public long RoomId { get; set; }

    public long PrimaryGuestId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public short Adults { get; set; }

    public short Children { get; set; }

    public decimal NightlyRate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? SpecialRequests { get; set; }
    
    public string? CancellationReason { get; set; }
}