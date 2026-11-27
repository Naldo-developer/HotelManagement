using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long ReservationId { get; set; }

    public string TransactionType { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Reference { get; set; }

    public DateTime PaidAt { get; set; }

    public long? RecordedByUserId { get; set; }

    public string? Notes { get; set; }

    public virtual AppUser? RecordedByUser { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
}
