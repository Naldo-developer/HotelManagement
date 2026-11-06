using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Reservation
{
    public long ReservationId { get; set; }

    public string ConfirmationCode { get; set; } = null!;

    public long RoomId { get; set; }

    public long PrimaryGuestId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public short Adults { get; set; }

    public short Children { get; set; }

    public decimal NightlyRate { get; set; }

    public string Status { get; set; } = null!;

    public decimal CancellationFee { get; set; }

    public string? CancellationReason { get; set; }

    public string? SpecialRequests { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AppUser? CreatedByUser { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Guest PrimaryGuest { get; set; } = null!;

    public virtual ICollection<ReservationStatusHistory> ReservationStatusHistories { get; set; } = new List<ReservationStatusHistory>();

    public virtual Room Room { get; set; } = null!;

    public virtual Stay? Stay { get; set; }

    public virtual ICollection<Guest> Guests { get; set; } = new List<Guest>();
}
