using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class RoomType
{
    public long RoomTypeId { get; set; }

    public long HotelId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public short MaxAdults { get; set; }

    public short MaxChildren { get; set; }

    public decimal BaseRate { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
