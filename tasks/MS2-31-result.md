# MS2-31 — Kết quả mẫu thông báo và gửi thông báo vận hành

Ngày hoàn thiện và kiểm tra: 17/09/2026. Đã hoàn thành toàn diện phân hệ Mẫu thông báo (`NotificationTemplate`) và Thông báo vận hành (`Notification`) cho cả tầng Backend (.NET 9 Clean Architecture) và Frontend (React + TypeScript + Vite + Tailwind/shadcn).

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **Quản lý mẫu thông báo (NotificationTemplate Management):**
  - Quản trị viên/thủ thư có quyền tạo, cập nhật, xóa và kích hoạt/hủy kích hoạt mẫu thông báo.
  - Template lưu mã duy nhất (`Code`), tên (`Name`), kênh gửi (`Channel`: `InApp`, `Email`, `Sms`), tiêu đề mẫu (`SubjectTemplate`), nội dung mẫu (`BodyTemplate`), danh sách biến cho phép (`AllowedVariables`) và trạng thái (`IsActive`).
  - Hỗ trợ kiểm soát tương tranh lạc quan (Optimistic Concurrency) qua `ConcurrencyToken` (UUID).
  - Tự động seed các mẫu thông báo nghiệp vụ chuẩn trong `IdentitySeeder.cs` (`RESERVATION_READY`, `BORROWING_DUE_REMINDER`, `VIOLATION_NOTICE`).

- **Bộ render mẫu an toàn & chống tấn công injection:**
  - `Template renderer` phân tích cú pháp biến dạng `{{variable_name}}`.
  - Kiểm tra đối chiếu với allow-list biến cấu hình trong mẫu; từ chối và báo lỗi rõ ràng nếu sử dụng biến không được phép.
  - Bắt buộc cung cấp đầy đủ giá trị cho các biến xuất hiện trong template trước khi gửi/preview.
  - Nội dung biến được HTML-encode tự động qua `WebUtility.HtmlEncode()` để triệt tiêu nguy cơ injection/XSS.

- **Kênh truyền gửi và Notification Sender Adapters:**
  - Thiết kế kiến trúc phân tách độc lập qua interface `INotificationSenderAdapter`.
  - Cung cấp 3 adapter riêng biệt:
    - `InAppNotificationSenderAdapter`: Ghi log và tạo bản ghi thông báo nội bộ cho tài khoản người dùng.
    - `EmailNotificationSenderAdapter`: Mô phỏng gửi email an toàn cho môi trường test/local (giả lập lỗi nếu email chứa `fail@`).
    - `SmsNotificationSenderAdapter`: Mô phỏng gửi SMS an toàn cho môi trường test/local (giả lập lỗi nếu số điện thoại `0000000000`).
  - Lưu trữ kết quả gửi: `SentAtUtc`, trạng thái `Sent` hoặc `Failed`, cùng `FailureReason` chi tiết khi xảy ra lỗi.
  - Nội dung render được lưu cố định (snapshot) trong bản ghi `Notification` độc lập với các thay đổi của template trong tương lai.

- **Thông báo nội bộ & Quản lý trạng thái đọc:**
  - Theo dõi `ReadAtUtc` và cờ `IsRead` riêng biệt với trạng thái gửi `Status`. Gửi thành công không tự động đồng nghĩa đã đọc.
  - API `MarkRead` (`PUT /api/v1/notifications/{id}/read`) và `MarkAllRead` (`PUT /api/v1/notifications/read-all`) enforce phân quyền chặt chẽ: chỉ chính người nhận (`RecipientId == UserId`) hoặc tài khoản `Administrator` mới được phép cập nhật trạng thái đọc.
  - API đếm thông báo chưa đọc `GET /api/v1/notifications/unread-count` và danh sách thông báo cá nhân `GET /api/v1/notifications/my`.

- **Phân quyền và bảo mật danh sách người nhận:**
  - API tìm kiếm người nhận `GET /api/v1/notifications/recipients` kiểm soát quyền:
    - Tìm kiếm `Staff` đòi hỏi quyền `employees.read` hoặc vai trò `Administrator`.
    - Tìm kiếm `Member` đòi hỏi quyền `members.read` hoặc vai trò `Administrator`.
    - Không làm lộ danh sách người nhận vượt ngoài phạm vi quyền hạn được cấp.
  - Thao tác tạo/sửa/xóa template yêu cầu quyền `notification-templates.manage` hoặc `Administrator`.
  - Thao tác gửi và gửi lại thông báo yêu cầu quyền `notifications.manage` hoặc `Administrator`.

- **Nhật ký kiểm toán (Audit Logging):**
  - Mọi thao tác quản lý mẫu (`notification_template.create`, `notification_template.update`, `notification_template.delete`) và gửi thông báo (`notification.send`, `notification.retry`) được tự động ghi nhận vào `AuditLogs` kèm snapshot before/after, Actor UserId, IpAddress, CorrelationId và Timestamp UTC.

- **Giao diện người dùng (Frontend):**
  - Màn hình `/notifications` gồm 3 tab điều hướng:
    - **Gửi thông báo (SendNotificationTab):** Chọn mẫu active, chọn loại người nhận (Độc giả/Nhân viên), tìm kiếm theo quyền, điền các biến tương ứng, tính năng Xem trước nội dung (Preview) trực quan trước khi gửi, và gửi thông báo kèm thông báo phản hồi tức thời.
    - **Lịch sử gửi & Vận hành (NotificationHistoryTab):** Lọc theo kênh (Email/SMS/InApp), trạng thái (Thành công/Thất bại/Chờ gửi), khoảng ngày gửi; bảng danh sách phân trang; modal chi tiết xem toàn bộ snapshot nội dung, lỗi thất bại và nút **Gửi lại (Retry)** an toàn.
    - **Mẫu thông báo (TemplateManagementTab):** Danh sách mẫu, trạng thái kích hoạt, modal Thêm mới/Chỉnh sửa/Xóa template với hỗ trợ gợi ý danh sách biến cho phép.
  - **Chuông thông báo Header (NotificationBellDropdown):** Hiển thị huy hiệu số lượng thông báo chưa đọc, tự động làm mới định kỳ mỗi 30s, danh sách 5 thông báo gần nhất, đánh dấu đã đọc khi nhấp chuột hoặc đọc tất cả mà không cần tải lại toàn trang.

---

## 2. Danh mục API Endpoints

| Phương thức | Đường dẫn API | Quyền yêu cầu | Mô tả |
|---|---|---|---|
| `GET` | `/api/v1/notification-templates` | Authenticated | Lấy danh sách mẫu thông báo. |
| `GET` | `/api/v1/notification-templates/{id}` | Authenticated | Lấy chi tiết một mẫu thông báo theo ID. |
| `POST` | `/api/v1/notification-templates` | `notification-templates.manage` / `notifications.manage` / Admin | Tạo mẫu thông báo mới. |
| `PUT` | `/api/v1/notification-templates/{id}` | `notification-templates.manage` / `notifications.manage` / Admin | Cập nhật mẫu thông báo. |
| `DELETE` | `/api/v1/notification-templates/{id}` | `notification-templates.manage` / `notifications.manage` / Admin | Xóa mẫu thông báo. |
| `POST` | `/api/v1/notifications/preview` | Authenticated | Xem trước nội dung render của mẫu với tập biến. |
| `POST` | `/api/v1/notifications/send` | `notifications.manage` / Admin | Gửi thông báo đến người nhận. |
| `POST` | `/api/v1/notifications/{id}/retry` | `notifications.manage` / Admin | Gửi lại thông báo thất bại. |
| `GET` | `/api/v1/notifications/history` | `notifications.read` / `notifications.manage` / Admin | Lấy lịch sử gửi thông báo với bộ lọc và phân trang. |
| `GET` | `/api/v1/notifications/my` | Authenticated | Lấy danh sách thông báo gửi đến người dùng hiện tại. |
| `GET` | `/api/v1/notifications/unread-count` | Authenticated | Lấy số lượng thông báo chưa đọc của người dùng hiện tại. |
| `PUT` | `/api/v1/notifications/{id}/read` | Recipient hoặc Admin | Đánh dấu một thông báo là đã đọc (`ReadAtUtc`). |
| `PUT` | `/api/v1/notifications/read-all` | Authenticated | Đánh dấu toàn bộ thông báo của người dùng là đã đọc. |
| `GET` | `/api/v1/notifications/recipients` | `employees.read` (Staff) / `members.read` (Member) / Admin | Tìm kiếm người nhận theo loại và quyền hạn. |

---

## 3. Các tệp tin đã triển khai và hoàn thiện

### Backend (.NET 9 Clean Architecture)
1. `UTH.Library.Domain/Entities/NotificationTemplate.cs`: Domain entity mẫu thông báo với mã code, kênh, subject/body, danh sách biến, concurrency token.
2. `UTH.Library.Domain/Entities/Notification.cs`: Domain entity thông báo với recipient, destination, snapshot nội dung đã render, trạng thái gửi, sent timestamp, failure reason, và `ReadAtUtc`.
3. `UTH.Library.Domain/Enums/`: `NotificationChannel.cs` (`Email`, `Sms`, `InApp`), `NotificationStatus.cs` (`Pending`, `Sent`, `Failed`, `Cancelled`), `RecipientType.cs` (`Staff`, `Member`).
4. `UTH.Library.Application/Features/Notifications/`:
   - `INotificationService.cs`: Hợp đồng dịch vụ đầy đủ các chức năng quản lý template, preview, send, retry, history, user notifications, unread count, mark read, search recipients.
   - `NotificationModels.cs`: Các DTO và command records theo chuẩn Clean Architecture.
   - `NotificationService.cs`: Triển khai nghiệp vụ render template với allow-list validation, HTML-encoding, chọn destination, gọi adapter, lưu audit log, kiểm soát quyền truy cập.
   - `Adapters/INotificationSenderAdapter.cs`: Interface trừu tượng hóa cho các kênh gửi tin.
5. `UTH.Library.Infrastructure/Notifications/NotificationSenderAdapters.cs`: Triển khai các adapter `InAppNotificationSenderAdapter`, `EmailNotificationSenderAdapter`, `SmsNotificationSenderAdapter` với cơ chế kiểm thử cục bộ an toàn.
6. `UTH.Library.Infrastructure/Persistence/Repositories/NotificationRepository.cs`: Truy vấn tối ưu EF Core, xử lý phân trang, join template, lookup tên đối tượng nhận từ Members/Employees.
7. `UTH.Library.Infrastructure/Persistence/Configurations/CatalogInventoryConfiguration.cs`: Cấu hình Fluent API bảng `NotificationTemplates` và `Notifications` trong EF Core.
8. `UTH.Library.Infrastructure/Identity/IdentitySeeder.cs`: Tự động khởi tạo quyền thông báo và seed các mẫu thông báo nghiệp vụ chuẩn (`RESERVATION_READY`, `BORROWING_DUE_REMINDER`, `VIOLATION_NOTICE`).
9. `UTH.Library.Api/Controllers/NotificationTemplatesController.cs`: Controller quản lý CRUD mẫu thông báo.
10. `UTH.Library.Api/Controllers/NotificationsController.cs`: Controller xử lý preview, send, retry, history, my notifications, unread-count, mark-read, recipients.
11. `UTH.Library.Api/Contracts/Notifications/NotificationContracts.cs`: Contracts API request/response DTOs.

### Frontend (React + TypeScript + Vite + Tailwind/shadcn)
1. `src/pages/notifications/notifications-api.ts`: API client đầy đủ các phương thức gọi REST API tương ứng với backend.
2. `src/pages/notifications/NotificationsPage.tsx`: Màn hình điều phối chính với các tab Gửi thông báo, Lịch sử gửi & Vận hành, Mẫu thông báo.
3. `src/pages/notifications/components/SendNotificationTab.tsx`: Form chọn mẫu, chọn đối tượng nhận với tìm kiếm theo quyền, nhập biến động theo template, xem trước (preview) và gửi tin.
4. `src/pages/notifications/components/NotificationHistoryTab.tsx`: Bộ lọc lịch sử gửi đa tiêu chí, bảng dữ liệu phân trang, xem chi tiết snapshot nội dung và chức năng thử lại (retry).
5. `src/pages/notifications/components/TemplateManagementTab.tsx`: Bảng quản trị mẫu thông báo, modal tạo mới/sửa/xóa template với hướng dẫn cấu hình biến.
6. `src/layouts/components/NotificationBellDropdown.tsx`: Dropdown chuông thông báo trên topbar với badge số lượng chưa đọc, tự động đồng bộ, đánh dấu đã đọc tức thời.
7. `src/layouts/components/Header.tsx`: Tích hợp chuông thông báo vào thanh header ứng dụng.
8. `src/routes/AppRoutes.tsx`: Khai báo route `/notifications` được bảo vệ đăng nhập.
9. `src/app/navigation.ts`: Đăng ký mục "Thông báo" trong menu điều hướng hệ thống.

---

## 4. Kết quả nghiệm thu Acceptance Criteria (MS2-31)

- [x] **Backend:**
  - Template renderer validate allow-list biến, escape nội dung bằng `WebUtility.HtmlEncode` và Notification service lưu snapshot/status/failure với adapter kênh tách biệt (`InApp`, `Email`, `Sms`).
  - Notification nội bộ có `ReadAtUtc`, truy vấn chưa đọc (`unread-count`, `my`) và command `MarkRead`/`MarkAllRead`; chỉ đúng người nhận hoặc tài khoản có quyền quản trị mới được cập nhật trạng thái đọc.
- [x] **Frontend:**
  - Notification UI có template CRUD (`TemplateManagementTab`), recipient picker theo quyền (`SendNotificationTab`), preview (`previewNotification`), send/retry, history status/error states (`NotificationHistoryTab`) và chỉ báo số thông báo chưa đọc (`NotificationBellDropdown`).
  - Người nhận nội bộ có thể mở thông báo và đánh dấu đã đọc; UI cập nhật số chưa đọc không cần tải lại toàn trang.
- [x] **Tích hợp:**
  - Template có code duy nhất, subject/body, channel, biến được phép và trạng thái active.
  - Dữ liệu render được escape/validate; biến thiếu hoặc biến ngoài allow-list tạo lỗi rõ ràng trước khi gửi.
  - Notification lưu recipient, destination, nội dung đã render, trạng thái gửi, thời điểm gửi, failure reason và `ReadAtUtc` đối với thông báo nội bộ.
  - Trạng thái gửi và trạng thái đọc được quản lý độc lập; gửi thành công không tự động đồng nghĩa đã đọc.
  - Chỉ gửi đến Staff hoặc Member phù hợp quyền/phạm vi và không lộ danh sách người nhận ngoài quyền.
  - Mọi thay đổi template và thao tác gửi được ghi AuditLog.
- [x] **Checklist hoàn thành:** Toàn bộ hạng mục Backend, Frontend, Tích hợp và Happy-case test đã được hoàn thiện và xác nhận.
