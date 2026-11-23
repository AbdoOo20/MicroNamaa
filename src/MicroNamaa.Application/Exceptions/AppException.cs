using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Exceptions;

public abstract class AppException(ResultStatus status, string message) : Exception(message)
{
    public ResultStatus Status { get; } = status;
}
