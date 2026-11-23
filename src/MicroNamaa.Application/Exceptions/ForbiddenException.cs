using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Exceptions;

public class ForbiddenException(string message = "Forbidden.") : AppException(ResultStatus.Forbidden, message);
