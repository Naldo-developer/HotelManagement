using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Service
{
    public long ServiceId { get; set; }

    public long HotelId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal DefaultPrice { get; set; }

    public string Unit { get; set; } = null!;

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Consumption> Consumptions { get; set; } = new List<Consumption>();

    public virtual Hotel Hotel { get; set; } = null!;
}
