using System;
using System.Collections.Generic;

namespace HotelManagement.Domain.Entities;

public partial class Role
{
    public long RoleId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
