using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Maintenance
{
    public long MaintenanceId { get; set; }

    public long RoomId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Category { get; set; }

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public long? ReportedByUserId { get; set; }

    public long? AssignedEmployeeId { get; set; }

    public decimal Cost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Employee? AssignedEmployee { get; set; }

    public virtual AppUser? ReportedByUser { get; set; }

    public virtual Room Room { get; set; } = null!;
}
