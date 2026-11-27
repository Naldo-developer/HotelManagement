using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Employee
{
    public long EmployeeId { get; set; }

    public long HotelId { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentNumber { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Position { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? AppUser { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;

    public virtual ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
}
