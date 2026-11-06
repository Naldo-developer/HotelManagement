using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class AppUser
{
    public long UserId { get; set; }

    public long EmployeeId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<Consumption> Consumptions { get; set; } = new List<Consumption>();

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReservationStatusHistory> ReservationStatusHistories { get; set; } = new List<ReservationStatusHistory>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Stay> StayCheckInByUsers { get; set; } = new List<Stay>();

    public virtual ICollection<Stay> StayCheckOutByUsers { get; set; } = new List<Stay>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
