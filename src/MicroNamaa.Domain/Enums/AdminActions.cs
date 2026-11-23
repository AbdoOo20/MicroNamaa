namespace MicroNamaa.Domain.Enums;

/// <summary>What a staff member may do in an <see cref="AdminArea"/>; combined as flags.</summary>
[Flags]
public enum AdminActions
{
    None = 0,

    View = 1,

    Create = 2,

    Update = 4,

    Delete = 8
}
