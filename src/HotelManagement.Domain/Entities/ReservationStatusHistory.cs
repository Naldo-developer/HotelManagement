using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class ReservationStatusHistory
{
    public long HistoryId { get; set; }

    public long ReservationId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public long? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Notes { get; set; }

    public virtual AppUser? ChangedByUser { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
}
