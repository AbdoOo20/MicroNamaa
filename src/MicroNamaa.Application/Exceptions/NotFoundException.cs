using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Exceptions;

public class NotFoundException(string message = "Resource was not found.") : AppException(ResultStatus.NotFound, message);
