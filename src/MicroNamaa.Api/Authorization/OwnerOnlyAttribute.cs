namespace MicroNamaa.Api.Authorization;

/// <summary>Only the owner platform admin may call this endpoint; staff members are refused.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OwnerOnlyAttribute : Attribute;
