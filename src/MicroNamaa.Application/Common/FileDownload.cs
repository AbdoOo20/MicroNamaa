namespace MicroNamaa.Application.Common;

public record FileDownload(Stream Content, string ContentType, string FileName);
