using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Exceptions;

public class BadRequestException(string message = "Bad request.") : AppException(ResultStatus.BadRequest, message);
