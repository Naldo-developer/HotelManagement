using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Consumption
{
    public long ConsumptionId { get; set; }

    public long StayId { get; set; }

    public long ServiceId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Subtotal { get; set; }

    public DateTime ConsumedAt { get; set; }

    public long? RecordedByUserId { get; set; }

    public string? Notes { get; set; }

    public virtual AppUser? RecordedByUser { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual Stay Stay { get; set; } = null!;
}
