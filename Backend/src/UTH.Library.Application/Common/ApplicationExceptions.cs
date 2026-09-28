namespace UTH.Library.Application.Common;

public abstract class ApplicationExceptionBase(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}
public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : ApplicationExceptionBase("Một hoặc nhiều trường dữ liệu không hợp lệ.", "validation.failed")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
public sealed class ResourceNotFoundException(string message) : ApplicationExceptionBase(message, "resource.not_found");
public sealed class ResourceConflictException(string message) : ApplicationExceptionBase(message, "resource.conflict");
public sealed class OptimisticConcurrencyException(string message, Exception? innerException = null)
    : ApplicationExceptionBase(message, "concurrency.conflict")
{
    public Exception? Cause { get; } = innerException;
}
