using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Guest
{
    public long GuestId { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentNumber { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Nationality { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Reservation> ReservationsNavigation { get; set; } = new List<Reservation>();
}
