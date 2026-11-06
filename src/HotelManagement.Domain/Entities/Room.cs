using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Room
{
    public long RoomId { get; set; }

    public long HotelId { get; set; }

    public long RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public string? Floor { get; set; }

    public string OperationalStatus { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual RoomType RoomType { get; set; } = null!;
}
