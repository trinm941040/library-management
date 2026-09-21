using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Notifications.Adapters;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Notifications;

public sealed class NotificationService(
    INotificationRepository repository,
    IEnumerable<INotificationSenderAdapter> adapters,
    TimeProvider timeProvider) : INotificationService
{
    private static readonly Regex VariableRegex = new(@"\{\{([a-zA-Z0-9_\-]+)\}\}", RegexOptions.Compiled);

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = await repository.GetTemplatesAsync(cancellationToken);
        return templates.Select(MapTemplateToDto).ToList();
    }

    public async Task<NotificationTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var t = await repository.GetTemplateByIdAsync(id, cancellationToken);
        return t is null ? null : MapTemplateToDto(t);
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(
        CreateNotificationTemplateCommand command,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new ArgumentException("Mã mẫu thông báo không được để trống.", nameof(command.Code));
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Tên mẫu thông báo không được để trống.", nameof(command.Name));
        if (string.IsNullOrWhiteSpace(command.BodyTemplate))
            throw new ArgumentException("Nội dung mẫu không được để trống.", nameof(command.BodyTemplate));

        if (!Enum.TryParse<NotificationChannel>(command.Channel, true, out var channel))
            throw new ArgumentException($"Kênh thông báo '{command.Channel}' không hợp lệ.");

        var exists = await repository.TemplateCodeExistsAsync(command.Code, null, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Mã mẫu thông báo '{command.Code}' đã tồn tại.");

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var template = NotificationTemplate.Create(
            command.Code,
            command.Name,
            channel,
            command.SubjectTemplate,
            command.BodyTemplate,
            command.AllowedVariables,
            command.IsActive,
            nowUtc);

        var created = await repository.CreateTemplateAsync(template, cancellationToken);

        var auditLog = AuditLog.Create(
            actorUserId,
            "notification_template.create",
            "NotificationTemplate",
            created.Id,
            null,
            JsonSerializer.Serialize(MapTemplateToDto(created)),
            nowUtc,
            correlationId,
            ipAddress);

        await repository.AddAuditLogAsync(auditLog, cancellationToken);

        return MapTemplateToDto(created);
    }

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(
        Guid id,
        UpdateNotificationTemplateCommand command,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy mẫu thông báo với ID: {id}");

        if (!Enum.TryParse<NotificationChannel>(command.Channel, true, out var channel))
            throw new ArgumentException($"Kênh thông báo '{command.Channel}' không hợp lệ.");

        var beforeJson = JsonSerializer.Serialize(MapTemplateToDto(template));
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        template.Update(
            command.Name,
            channel,
            command.SubjectTemplate,
            command.BodyTemplate,
            command.AllowedVariables,
            command.IsActive,
            nowUtc);

        await repository.UpdateTemplateAsync(template, cancellationToken);

        var auditLog = AuditLog.Create(
            actorUserId,
            "notification_template.update",
            "NotificationTemplate",
            template.Id,
            beforeJson,
            JsonSerializer.Serialize(MapTemplateToDto(template)),
            nowUtc,
            correlationId,
            ipAddress);

        await repository.AddAuditLogAsync(auditLog, cancellationToken);

        return MapTemplateToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(
        Guid id,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateByIdAsync(id, cancellationToken);
        if (template is null) return false;

        var beforeJson = JsonSerializer.Serialize(MapTemplateToDto(template));
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await repository.DeleteTemplateAsync(template, cancellationToken);

        var auditLog = AuditLog.Create(
            actorUserId,
            "notification_template.delete",
            "NotificationTemplate",
            template.Id,
            beforeJson,
            null,
            nowUtc,
            correlationId,
            ipAddress);

        await repository.AddAuditLogAsync(auditLog, cancellationToken);

        return true;
    }

    public async Task<NotificationPreviewResult> PreviewAsync(
        RenderNotificationPreviewQuery query,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateByCodeAsync(query.TemplateCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy mẫu thông báo '{query.TemplateCode}'.");

        var allowedVars = ParseAllowedVariables(template.AllowedVariables);

        var renderedSubject = !string.IsNullOrWhiteSpace(template.SubjectTemplate)
            ? RenderTemplate(template.SubjectTemplate, query.Variables, allowedVars)
            : null;

        var renderedBody = RenderTemplate(template.BodyTemplate, query.Variables, allowedVars);

        return new NotificationPreviewResult(
            template.Code,
            template.Channel.ToString(),
            renderedSubject,
            renderedBody);
    }

    public async Task<NotificationDto> SendAsync(
        SendNotificationCommand command,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateByCodeAsync(command.TemplateCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy mẫu thông báo '{command.TemplateCode}'.");

        if (!template.IsActive)
            throw new InvalidOperationException($"Mẫu thông báo '{template.Code}' đang bị vô hiệu hóa.");

        if (!Enum.TryParse<RecipientType>(command.RecipientType, true, out var recipientType))
            throw new ArgumentException($"Loại đối tượng nhận '{command.RecipientType}' không hợp lệ.");

        // Retrieve recipient details
        var recipientDetails = await repository.GetRecipientDetailsAsync(recipientType, command.RecipientId, cancellationToken);

        // Determine destination
        var destination = command.Destination;
        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = template.Channel switch
            {
                NotificationChannel.Email => recipientDetails.Email,
                NotificationChannel.Sms => recipientDetails.Phone,
                NotificationChannel.InApp => command.RecipientId.ToString(),
                _ => null
            };
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy địa chỉ đích ({template.Channel}) cho người nhận '{recipientDetails.Name}'. Vui lòng cung cấp số điện thoại hoặc email.");
        }

        // Render subject & body
        var allowedVars = ParseAllowedVariables(template.AllowedVariables);

        // Auto-inject common recipient variables if not provided
        var vars = new Dictionary<string, string>(command.Variables, StringComparer.OrdinalIgnoreCase);
        if (!vars.ContainsKey("name") && !vars.ContainsKey("recipient_name"))
        {
            vars["name"] = recipientDetails.Name;
            vars["recipient_name"] = recipientDetails.Name;
        }

        var renderedSubject = !string.IsNullOrWhiteSpace(template.SubjectTemplate)
            ? RenderTemplate(template.SubjectTemplate, vars, allowedVars)
            : null;

        var renderedBody = RenderTemplate(template.BodyTemplate, vars, allowedVars);

        // Create notification
        var notification = Notification.Create(
            template.Id,
            recipientType,
            command.RecipientId,
            destination,
            renderedSubject,
            renderedBody);

        // Resolve adapter
        var adapter = adapters.FirstOrDefault(a => a.Channel == template.Channel)
            ?? throw new InvalidOperationException($"Chưa cấu hình adapter gửi tin cho kênh {template.Channel}.");

        var sendResult = await adapter.SendAsync(notification, cancellationToken);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (sendResult.Success)
        {
            notification.MarkSent(nowUtc);
        }
        else
        {
            notification.MarkFailed(sendResult.FailureReason ?? "Lỗi không xác định khi gửi thông báo.");
        }

        var created = await repository.CreateNotificationAsync(notification, cancellationToken);

        var auditLog = AuditLog.Create(
            actorUserId,
            "notification.send",
            "Notification",
            created.Id,
            null,
            JsonSerializer.Serialize(new
            {
                created.Id,
                created.TemplateId,
                template.Code,
                created.RecipientType,
                created.RecipientId,
                created.Destination,
                created.Status,
                created.SentAtUtc
            }),
            nowUtc,
            correlationId,
            ipAddress);

        await repository.AddAuditLogAsync(auditLog, cancellationToken);

        return new NotificationDto(
            created.Id,
            created.TemplateId,
            template.Code,
            template.Name,
            template.Channel.ToString(),
            created.RecipientType.ToString(),
            created.RecipientId,
            recipientDetails.Name,
            created.Destination,
            created.Subject,
            created.Body,
            created.Status.ToString(),
            created.ScheduledAtUtc,
            created.SentAtUtc,
            created.FailureReason,
            created.ReadAtUtc,
            created.IsRead);
    }

    public async Task<NotificationDto> RetryAsync(
        Guid notificationId,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetNotificationByIdAsync(notificationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID: {notificationId}");

        if (notification.Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Thông báo này đã được gửi thành công trước đó.");

        var template = await repository.GetTemplateByIdAsync(notification.TemplateId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu thông báo liên kết.");

        var adapter = adapters.FirstOrDefault(a => a.Channel == template.Channel)
            ?? throw new InvalidOperationException($"Chưa cấu hình adapter gửi tin cho kênh {template.Channel}.");

        notification.Retry();
        var sendResult = await adapter.SendAsync(notification, cancellationToken);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (sendResult.Success)
        {
            notification.MarkSent(nowUtc);
        }
        else
        {
            notification.MarkFailed(sendResult.FailureReason ?? "Lỗi không xác định khi gửi lại.");
        }

        await repository.UpdateNotificationAsync(notification, cancellationToken);

        var auditLog = AuditLog.Create(
            actorUserId,
            "notification.retry",
            "Notification",
            notification.Id,
            null,
            JsonSerializer.Serialize(new { notification.Id, notification.Status, notification.SentAtUtc }),
            nowUtc,
            correlationId,
            ipAddress);

        await repository.AddAuditLogAsync(auditLog, cancellationToken);

        var recipientDetails = await repository.GetRecipientDetailsAsync(
            notification.RecipientType,
            notification.RecipientId,
            cancellationToken);

        return new NotificationDto(
            notification.Id,
            notification.TemplateId,
            template.Code,
            template.Name,
            template.Channel.ToString(),
            notification.RecipientType.ToString(),
            notification.RecipientId,
            recipientDetails.Name,
            notification.Destination,
            notification.Subject,
            notification.Body,
            notification.Status.ToString(),
            notification.ScheduledAtUtc,
            notification.SentAtUtc,
            notification.FailureReason,
            notification.ReadAtUtc,
            notification.IsRead);
    }

    public async Task<NotificationPageResult> GetHistoryAsync(
        string? channel,
        string? status,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetNotificationHistoryAsync(
            channel,
            status,
            fromDateUtc,
            toDateUtc,
            pageNumber,
            pageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new NotificationPageResult(items, pageNumber, pageSize, totalCount, totalPages);
    }

    public async Task<NotificationPageResult> GetMyNotificationsAsync(
        Guid recipientId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetUserNotificationsAsync(
            recipientId,
            unreadOnly,
            pageNumber,
            pageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new NotificationPageResult(items, pageNumber, pageSize, totalCount, totalPages);
    }

    public async Task<NotificationUnreadCountResult> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        var count = await repository.GetUnreadCountAsync(recipientId, cancellationToken);
        return new NotificationUnreadCountResult(count);
    }

    public async Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetNotificationByIdAsync(notificationId, cancellationToken);
        if (notification is null) return false;

        // Verify authorization: only recipient or Administrator
        if (!isAdmin && notification.RecipientId != userId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật trạng thái thông báo này.");
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        notification.MarkRead(nowUtc);
        await repository.UpdateNotificationAsync(notification, cancellationToken);
        return true;
    }

    public Task MarkAllReadAsync(Guid recipientId, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        return repository.MarkAllReadAsync(recipientId, nowUtc, cancellationToken);
    }

    public Task<IReadOnlyList<NotificationRecipientDto>> SearchRecipientsAsync(
        string recipientTypeStr,
        string? keyword,
        IReadOnlyCollection<string> permissions,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RecipientType>(recipientTypeStr, true, out var recipientType))
        {
            throw new ArgumentException($"Loại đối tượng '{recipientTypeStr}' không hợp lệ.");
        }

        // Enforce authorization:
        // Searching Staff requires employees.read or Administrator
        if (recipientType == RecipientType.Staff && !isAdmin && !permissions.Contains(Permissions.EmployeesRead))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách nhân viên.");
        }

        // Searching Member requires members.read or Administrator
        if (recipientType == RecipientType.Member && !isAdmin && !permissions.Contains(Permissions.MembersRead))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách độc giả.");
        }

        return repository.SearchRecipientsAsync(recipientType, keyword, 20, cancellationToken);
    }

    private static string RenderTemplate(
        string template,
        Dictionary<string, string> variables,
        HashSet<string>? allowedVariables)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        return VariableRegex.Replace(template, match =>
        {
            var varName = match.Groups[1].Value.Trim();

            // Validate against allow-list if configured
            if (allowedVariables is not null && allowedVariables.Count > 0)
            {
                if (!allowedVariables.Contains(varName))
                {
                    throw new InvalidOperationException($"Biến '{{{{{varName}}}}}' không nằm trong danh sách biến cho phép của mẫu.");
                }
            }

            // Must be supplied
            if (!variables.TryGetValue(varName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Biến '{{{{{varName}}}}}' là bắt buộc nhưng chưa được cung cấp giá trị.");
            }

            // HTML encode to prevent injection
            return WebUtility.HtmlEncode(value.Trim());
        });
    }

    private static HashSet<string>? ParseAllowedVariables(string? allowedVariables)
    {
        if (string.IsNullOrWhiteSpace(allowedVariables)) return null;

        return allowedVariables
            .Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim().TrimStart('{').TrimEnd('}'))
            .Where(v => !string.IsNullOrEmpty(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static NotificationTemplateDto MapTemplateToDto(NotificationTemplate t) =>
        new(
            t.Id,
            t.Code,
            t.Name,
            t.Channel.ToString(),
            t.SubjectTemplate,
            t.BodyTemplate,
            t.AllowedVariables,
            t.IsActive,
            t.UpdatedAtUtc);
}
