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
    INotificationRealtimePublisher realtimePublisher,
    TimeProvider timeProvider) : INotificationService
{
    private static readonly Regex VariableRegex = new(@"\{\{([a-zA-Z0-9_\-]+)\}\}", RegexOptions.Compiled);
    private static readonly IReadOnlyList<NotificationEventDefinition> EventDefinitions =
    [
        Event("Borrowing.DueSoon", ["member_name", "book_title", "due_date", "days_remaining"]),
        Event("Borrowing.Overdue", ["member_name", "book_title", "due_date", "days_overdue"]),
        Event("MemberViolation.Created", ["member_name", "violation_type", "occurred_at"]),
        Event("Fine.Created", ["member_name", "amount", "reason"]),
        Event("Fine.Adjusted", ["member_name", "amount", "reason"]),
        Event("Fine.PaymentRecorded", ["member_name", "amount", "paid_at"]),
        Event("Reservation.ReadyForPickup", ["member_name", "book_title", "expires_at"]),
        Event("Reservation.Expiring", ["member_name", "book_title", "expires_at"]),
        Event("MembershipCard.Expiring", ["member_name", "card_number", "expires_at"])
    ];

    private static NotificationEventDefinition Event(string code, string[] variables) =>
        new(code, variables, variables, "Member.Email");

    public IReadOnlyList<NotificationEventDefinition> GetEventDefinitions() => EventDefinitions;

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = await repository.GetTemplatesAsync(cancellationToken);
        return templates
            .Where(template => template.Channel != NotificationChannel.Sms)
            .Select(MapTemplateToDto)
            .ToList();
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
        ValidateTemplate(command.Code, command.SubjectTemplate, command.BodyTemplate, command.AllowedVariables);

        if (!Enum.TryParse<NotificationChannel>(command.Channel, true, out var channel))
            throw new ArgumentException($"Kênh thông báo '{command.Channel}' không hợp lệ.");
        EnsureSupportedChannel(channel);

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

        EnsureSupportedChannel(template.Channel);

        if (command.ConcurrencyToken is null || command.ConcurrencyToken != template.ConcurrencyToken)
            throw new InvalidOperationException("Mẫu email đã thay đổi. Hãy tải lại trước khi lưu.");

        if (!Enum.TryParse<NotificationChannel>(command.Channel, true, out var channel))
            throw new ArgumentException($"Kênh thông báo '{command.Channel}' không hợp lệ.");
        EnsureSupportedChannel(channel);
        ValidateTemplate(template.Code, command.SubjectTemplate, command.BodyTemplate, command.AllowedVariables);

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
        EnsureSupportedChannel(template.Channel);

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
        EnsureSupportedChannel(template.Channel);

        var allowedVars = ParseAllowedVariables(template.AllowedVariables);

        var renderedSubject = !string.IsNullOrWhiteSpace(template.SubjectTemplate)
            ? RenderTemplate(template.SubjectTemplate, query.Variables, allowedVars, htmlEncodeValues: false)
            : null;

        var renderedBody = RenderTemplate(
            template.BodyTemplate,
            query.Variables,
            allowedVars,
            htmlEncodeValues: template.Channel == NotificationChannel.Email);

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
        EnsureSupportedChannel(template.Channel);

        if (!Enum.TryParse<RecipientType>(command.RecipientType, true, out var recipientType))
            throw new ArgumentException($"Loại đối tượng nhận '{command.RecipientType}' không hợp lệ.");
        if (template.Channel == NotificationChannel.InApp && recipientType != RecipientType.Staff)
            throw new ArgumentException("Thông báo trong ứng dụng chỉ hỗ trợ tài khoản nhân viên.");

        ValidateSeverity(command.Severity);
        ValidateDeepLink(command.DeepLink);
        ValidateMetadata(command.MetadataJson);

        // Retrieve recipient details
        var recipientDetails = await repository.GetRecipientDetailsAsync(recipientType, command.RecipientId, cancellationToken);

        // Determine destination
        var destination = command.Destination;
        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = template.Channel switch
            {
                NotificationChannel.Email => recipientDetails.Email,
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
            ? RenderTemplate(template.SubjectTemplate, vars, allowedVars, htmlEncodeValues: false)
            : null;

        var renderedBody = RenderTemplate(
            template.BodyTemplate,
            vars,
            allowedVars,
            htmlEncodeValues: template.Channel == NotificationChannel.Email);

        // Create notification
        var notification = Notification.Create(
            template.Id,
            recipientType,
            command.RecipientId,
            destination,
            renderedSubject,
            renderedBody,
            eventCode: command.EventCode ?? template.Code,
            idempotencyKey: command.IdempotencyKey,
            severity: command.Severity,
            deepLink: command.DeepLink,
            metadataJson: command.MetadataJson,
            createdAtUtc: timeProvider.GetUtcNow().UtcDateTime);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey) &&
            await repository.IdempotencyKeyExistsAsync(command.IdempotencyKey, cancellationToken))
            throw new InvalidOperationException("Thông báo cho sự kiện và người nhận này đã được tạo trước đó.");

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (template.Channel != NotificationChannel.Email)
        {
            var adapter = adapters.FirstOrDefault(a => a.Channel == template.Channel)
                ?? throw new InvalidOperationException($"Chưa cấu hình adapter gửi tin cho kênh {template.Channel}.");
            var sendResult = await adapter.SendAsync(notification, cancellationToken);
            if (sendResult.Success) notification.MarkSent(nowUtc);
            else notification.MarkFailed(sendResult.FailureReason ?? "Không thể gửi thông báo.");
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

        var result = new NotificationDto(
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
            created.EventCode,
            created.Severity,
            created.DeepLink,
            created.MetadataJson,
            created.CreatedAtUtc,
            created.ReadAtUtc,
            created.IsRead);

        if (template.Channel == NotificationChannel.InApp)
            await realtimePublisher.PublishAsync(result, cancellationToken);

        return result;
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
        if (template.Channel != NotificationChannel.Email)
            throw new InvalidOperationException("Chỉ thông báo email thất bại mới có thể gửi lại.");

        notification.Retry();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (template.Channel != NotificationChannel.Email)
        {
            var adapter = adapters.FirstOrDefault(a => a.Channel == template.Channel)
                ?? throw new InvalidOperationException($"Chưa cấu hình adapter gửi tin cho kênh {template.Channel}.");
            var sendResult = await adapter.SendAsync(notification, cancellationToken);
            if (sendResult.Success) notification.MarkSent(nowUtc);
            else notification.MarkFailed(sendResult.FailureReason ?? "Không thể gửi lại thông báo.");
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
            notification.EventCode,
            notification.Severity,
            notification.DeepLink,
            notification.MetadataJson,
            notification.CreatedAtUtc,
            notification.ReadAtUtc,
            notification.IsRead);
    }

    public async Task<BulkNotificationResult> SendBulkAsync(
        SendBulkNotificationCommand command,
        Guid actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (!command.AllStaff && command.RecipientIds.Count == 0 &&
            string.IsNullOrWhiteSpace(command.RoleName) &&
            string.IsNullOrWhiteSpace(command.PermissionName) &&
            command.BranchId is null)
            throw new ArgumentException("Phải chọn người nhận hoặc phạm vi gửi thông báo.");
        if (command.AllStaff && (command.RecipientIds.Count > 0 ||
            !string.IsNullOrWhiteSpace(command.RoleName) ||
            !string.IsNullOrWhiteSpace(command.PermissionName)))
            throw new ArgumentException("Không thể kết hợp toàn bộ nhân viên với danh sách hoặc phạm vi khác.");
        if (string.IsNullOrWhiteSpace(command.EventCode) || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ArgumentException("Mã sự kiện và khóa chống gửi trùng là bắt buộc khi gửi hàng loạt.");

        var recipients = await repository.ResolveStaffRecipientsByIdsAsync(
            command.RecipientIds.Distinct().ToArray(),
            command.RoleName,
            command.PermissionName,
            command.BranchId,
            cancellationToken);
        if (recipients.Count == 0)
            throw new InvalidOperationException("Không có tài khoản nhân viên đang hoạt động trong phạm vi đã chọn.");

        var notificationIds = new List<Guid>(recipients.Count);
        foreach (var recipient in recipients.DistinctBy(value => value.Id))
        {
            var variables = new Dictionary<string, string>(command.Variables, StringComparer.OrdinalIgnoreCase);
            variables.TryAdd("name", recipient.Name);
            variables.TryAdd("recipient_name", recipient.Name);
            var notification = await SendAsync(
                new SendNotificationCommand(
                    command.TemplateCode,
                    nameof(RecipientType.Staff),
                    recipient.Id,
                    null,
                    variables,
                    command.EventCode,
                    $"{command.IdempotencyKey}:{recipient.Id:N}",
                    command.Severity,
                    command.DeepLink,
                    command.MetadataJson),
                actorUserId,
                correlationId,
                ipAddress,
                cancellationToken);
            notificationIds.Add(notification.Id);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        await repository.AddAuditLogAsync(AuditLog.Create(
            actorUserId,
            "notification.bulk-send",
            "Notification",
            notificationIds[0],
            null,
            JsonSerializer.Serialize(new
            {
                command.TemplateCode,
                command.RecipientIds,
                command.AllStaff,
                command.RoleName,
                command.PermissionName,
                command.BranchId,
                RecipientCount = notificationIds.Count,
                command.EventCode
            }),
            nowUtc,
            correlationId,
            ipAddress), cancellationToken);

        return new BulkNotificationResult(notificationIds.Count, notificationIds);
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
        string? severity,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(severity)) ValidateSeverity(severity);
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await repository.GetUserNotificationsAsync(
            recipientId,
            unreadOnly,
            severity,
            fromDateUtc,
            toDateUtc,
            pageNumber,
            pageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new NotificationPageResult(items, pageNumber, pageSize, totalCount, totalPages);
    }

    public async Task<NotificationDto?> GetMyNotificationAsync(
        Guid notificationId,
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetNotificationByIdAsync(notificationId, cancellationToken);
        if (notification is null || notification.RecipientId != recipientId || notification.RecipientType != RecipientType.Staff)
            return null;

        var template = await repository.GetTemplateByIdAsync(notification.TemplateId, cancellationToken);
        if (template is null || template.Channel != NotificationChannel.InApp) return null;
        var recipient = await repository.GetRecipientDetailsAsync(RecipientType.Staff, recipientId, cancellationToken);
        return MapNotificationToDto(notification, template, recipient.Name);
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
        _ = isAdmin;
        var notification = await repository.GetNotificationByIdAsync(notificationId, cancellationToken);
        if (notification is null) return false;

        if (notification.RecipientId != userId || notification.RecipientType != RecipientType.Staff)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật trạng thái thông báo này.");
        }

        var template = await repository.GetTemplateByIdAsync(notification.TemplateId, cancellationToken);
        if (template?.Channel != NotificationChannel.InApp) return false;

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
        HashSet<string>? allowedVariables,
        bool htmlEncodeValues)
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

            var trimmedValue = value.Trim();
            return htmlEncodeValues ? WebUtility.HtmlEncode(trimmedValue) : trimmedValue;
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

    private static void ValidateTemplate(string code, string? subject, string body, string? allowedVariables)
    {
        if (Regex.IsMatch(body, @"<\s*(script|iframe|object|embed)|\son\w+\s*=|javascript\s*:", RegexOptions.IgnoreCase))
            throw new ArgumentException("Nội dung HTML chứa thành phần không an toàn.");
        var declared = ParseAllowedVariables(allowedVariables) ?? [];
        var used = VariableRegex.Matches($"{subject} {body}").Select(match => match.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var undeclared = used.Where(value => declared.Count > 0 && !declared.Contains(value)).ToArray();
        if (undeclared.Length > 0)
            throw new ArgumentException($"Các biến chưa được khai báo: {string.Join(", ", undeclared)}.");
        var eventDefinition = EventDefinitions.FirstOrDefault(value => value.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (eventDefinition is not null && used.Any(value => !eventDefinition.AllowedVariables.Contains(value, StringComparer.OrdinalIgnoreCase)))
            throw new ArgumentException("Mẫu sử dụng biến ngoài danh mục cho phép của sự kiện.");
    }

    private static void EnsureSupportedChannel(NotificationChannel channel)
    {
        if (channel == NotificationChannel.Sms)
            throw new InvalidOperationException("Kênh SMS đã ngừng hỗ trợ. Dữ liệu SMS cũ chỉ được phép tra cứu.");
    }

    private static void ValidateSeverity(string severity)
    {
        string[] supported = ["Info", "Success", "Warning", "Error"];
        if (!supported.Contains(severity, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Mức độ thông báo phải là Info, Success, Warning hoặc Error.");
    }

    private static void ValidateDeepLink(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink)) return;
        string[] allowedPrefixes =
        [
            "/dashboard", "/borrowings", "/loans", "/reservations", "/violations",
            "/members", "/catalog", "/copies", "/stock-receipts", "/inventory-audits",
            "/notifications", "/staff", "/access-accounts"
        ];
        var path = deepLink.Split('?', '#')[0];
        if (!deepLink.StartsWith('/') || deepLink.StartsWith("//", StringComparison.Ordinal) ||
            !allowedPrefixes.Any(prefix => path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                                           path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Liên kết thông báo không thuộc danh sách route nội bộ được phép.");
    }

    private static void ValidateMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return;
        if (metadataJson.Length > 4000)
            throw new ArgumentException("Dữ liệu bổ sung của thông báo không được vượt quá 4.000 ký tự.");
        try
        {
            using var _ = JsonDocument.Parse(metadataJson);
        }
        catch (JsonException)
        {
            throw new ArgumentException("Dữ liệu bổ sung của thông báo phải là JSON hợp lệ.");
        }
    }

    private static NotificationDto MapNotificationToDto(
        Notification notification,
        NotificationTemplate template,
        string? recipientName) =>
        new(
            notification.Id,
            notification.TemplateId,
            template.Code,
            template.Name,
            template.Channel.ToString(),
            notification.RecipientType.ToString(),
            notification.RecipientId,
            recipientName ?? "Nhân viên",
            notification.Destination,
            notification.Subject,
            notification.Body,
            notification.Status.ToString(),
            notification.ScheduledAtUtc,
            notification.SentAtUtc,
            notification.FailureReason,
            notification.EventCode,
            notification.Severity,
            notification.DeepLink,
            notification.MetadataJson,
            notification.CreatedAtUtc,
            notification.ReadAtUtc,
            notification.IsRead);

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
            t.UpdatedAtUtc,
            t.ConcurrencyToken);
}
