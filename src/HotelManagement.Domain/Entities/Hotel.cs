using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Hotel
{
    public long HotelId { get; set; }

    public string Name { get; set; } = null!;

    public string? TaxId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public string Timezone { get; set; } = null!;

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
