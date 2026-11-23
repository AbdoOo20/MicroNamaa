using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sync;

public record SyncCategoryDto(int Id, string Name, string? Description, string? ImageUrl, RecordStatus Status, int SortOrder, DateTime UpdatedAt);
