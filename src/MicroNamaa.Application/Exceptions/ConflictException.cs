using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Exceptions;

public class ConflictException(string message = "Conflict.") : AppException(ResultStatus.Conflict, message);
