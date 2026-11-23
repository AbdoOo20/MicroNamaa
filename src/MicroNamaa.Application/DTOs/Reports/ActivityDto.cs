namespace MicroNamaa.Application.DTOs.Reports;

/// <summary>A line of "Recent Activity". Badge is a short tag for the chip (New, Low, Out, Sent, Return, Cancelled).</summary>
public record ActivityDto(string Type, string Title, string? Reference, string Badge, string? RelatedType, long? RelatedId, DateTime At);
