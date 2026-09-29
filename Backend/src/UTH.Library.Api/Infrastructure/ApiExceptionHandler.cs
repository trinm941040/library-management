using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Application.Common;

namespace UTH.Library.Api.Infrastructure;

internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            RequestValidationException => (422, "Dữ liệu không hợp lệ", "validation.failed"),
            ResourceNotFoundException => (404, "Không tìm thấy dữ liệu", "resource.not_found"),
            ResourceConflictException => (409, "Xung đột dữ liệu", "resource.conflict"),
            OptimisticConcurrencyException => (409, "Xung đột cập nhật", "concurrency.conflict"),
            UnauthorizedAccessException => (403, "Không có quyền truy cập", "authorization.forbidden"),
            ExternalServiceUnavailableException => (503, "Dịch vụ tạm thời không khả dụng", "external_service.unavailable"),
            _ => (500, "Lỗi máy chủ", "server.error")
        };
        if (status == 500) logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", context.TraceIdentifier);
        else logger.LogWarning(exception, "Request failed with {ErrorCode}. CorrelationId: {CorrelationId}", code, context.TraceIdentifier);

        var details = new ProblemDetails { Status = status, Title = title, Detail = status == 500 ? "Đã xảy ra lỗi không mong muốn." : exception.Message, Type = $"https://httpstatuses.com/{status}" };
        details.Extensions["code"] = code;
        details.Extensions["correlationId"] = context.TraceIdentifier;
        if (exception is RequestValidationException validation) details.Extensions["errors"] = validation.Errors;
        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
    }
}
