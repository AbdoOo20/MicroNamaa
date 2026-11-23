using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>What one staff member may do in one admin area.</summary>
public class StaffPermission : BaseEntity<int>
{
    public int UserId { get; set; }

    public AdminArea Area { get; set; }

    public AdminActions Actions { get; set; }

    public User User { get; set; } = null!;
}
