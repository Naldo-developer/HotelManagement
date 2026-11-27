using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Stay
{
    public long StayId { get; set; }

    public long ReservationId { get; set; }

    public DateTime CheckInAt { get; set; }

    public DateTime? CheckOutAt { get; set; }

    public string Status { get; set; } = null!;

    public long? CheckInByUserId { get; set; }

    public long? CheckOutByUserId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? CheckInByUser { get; set; }

    public virtual AppUser? CheckOutByUser { get; set; }

    public virtual ICollection<Consumption> Consumptions { get; set; } = new List<Consumption>();

    public virtual Reservation Reservation { get; set; } = null!;
}
