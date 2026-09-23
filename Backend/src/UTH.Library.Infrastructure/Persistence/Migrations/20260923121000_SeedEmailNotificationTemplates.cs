using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260923121000_SeedEmailNotificationTemplates")]
public sealed class SeedEmailNotificationTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var rows = new (string Code, string Name, string Subject, string Body, string Variables)[]
        {
            ("Borrowing.DueSoon", "Nhắc sắp đến hạn trả", "Sách {{book_title}} sắp đến hạn", "Xin chào {{member_name}}, sách {{book_title}} sẽ đến hạn vào {{due_date}} (còn {{days_remaining}} ngày).", "member_name,book_title,due_date,days_remaining"),
            ("Borrowing.Overdue", "Nhắc khoản mượn quá hạn", "Sách {{book_title}} đã quá hạn", "Xin chào {{member_name}}, sách {{book_title}} đã quá hạn từ {{due_date}} ({{days_overdue}} ngày).", "member_name,book_title,due_date,days_overdue"),
            ("MemberViolation.Created", "Thông báo vi phạm", "Thông báo vi phạm thư viện", "Xin chào {{member_name}}, hệ thống ghi nhận vi phạm {{violation_type}} lúc {{occurred_at}}.", "member_name,violation_type,occurred_at"),
            ("Fine.Created", "Thông báo phát sinh tiền phạt", "Phát sinh tiền phạt", "Xin chào {{member_name}}, khoản phạt {{amount}} đã phát sinh. Lý do: {{reason}}.", "member_name,amount,reason"),
            ("Fine.Adjusted", "Thông báo điều chỉnh tiền phạt", "Tiền phạt đã được điều chỉnh", "Xin chào {{member_name}}, tiền phạt được điều chỉnh {{amount}}. Lý do: {{reason}}.", "member_name,amount,reason"),
            ("Fine.PaymentRecorded", "Xác nhận thanh toán tiền phạt", "Đã ghi nhận thanh toán", "Xin chào {{member_name}}, hệ thống đã ghi nhận thanh toán {{amount}} lúc {{paid_at}}.", "member_name,amount,paid_at"),
            ("Reservation.ReadyForPickup", "Đặt trước sẵn sàng nhận", "Tài liệu đặt trước đã sẵn sàng", "Xin chào {{member_name}}, tài liệu {{book_title}} đã sẵn sàng và được giữ đến {{expires_at}}.", "member_name,book_title,expires_at"),
            ("Reservation.Expiring", "Đặt trước sắp hết hạn", "Đặt trước sắp hết hạn", "Xin chào {{member_name}}, thời hạn nhận {{book_title}} sẽ kết thúc vào {{expires_at}}.", "member_name,book_title,expires_at"),
            ("MembershipCard.Expiring", "Thẻ thành viên sắp hết hạn", "Thẻ thư viện sắp hết hạn", "Xin chào {{member_name}}, thẻ {{card_number}} sẽ hết hạn vào {{expires_at}}.", "member_name,card_number,expires_at")
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var id = Guid.Parse($"37000000-0000-0000-0000-{i + 1:D12}");
            migrationBuilder.Sql($"""
                INSERT INTO notification_templates ("Id", "Code", "Name", "Channel", "SubjectTemplate", "BodyTemplate", "AllowedVariables", "IsActive", "UpdatedAtUtc", "ConcurrencyToken")
                VALUES ('{id}', '{rows[i].Code}', '{rows[i].Name.Replace("'", "''")}', 'Email', '{rows[i].Subject.Replace("'", "''")}', '{rows[i].Body.Replace("'", "''")}', '{rows[i].Variables}', TRUE, TIMESTAMPTZ '2026-09-23 00:00:00Z', gen_random_uuid())
                ON CONFLICT ("Code", "Channel") DO NOTHING;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DELETE FROM notification_templates WHERE \"Id\"::text LIKE '37000000-0000-0000-0000-%';");
}
