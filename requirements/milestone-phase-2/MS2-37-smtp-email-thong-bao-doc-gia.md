# MS2-37 — Triển khai SMTP và email thông báo độc giả

- Phân loại: Full-stack / Feature / Integration / Security
- Mức ưu tiên: P0
- Phụ thuộc: MS2-09, MS2-17, MS2-18, MS2-23, MS2-30, MS2-31, MS2-32

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; tái sử dụng `NotificationTemplate`, `Notification`, `SystemSetting`, AuditLog và các adapter hiện có, không tạo mô hình trùng lặp.

Triển khai kênh SMTP để gửi email tự động cho độc giả theo các sự kiện nghiệp vụ: sắp đến hạn trả, trễ hạn, vi phạm, phát sinh tiền phạt, thay đổi tiền phạt/thanh toán, reservation sẵn sàng hoặc sắp hết hạn, thẻ thành viên sắp hết hạn và các trạng thái được đăng ký thêm sau này.

Cấu hình SMTP phải lấy từ cấu hình hệ thống. Quản trị viên có trang quản lý cấu hình SMTP, kiểm tra kết nối và quản lý template email theo từng loại sự kiện. Việc phát sinh thông báo và việc gửi SMTP phải tách rời bằng outbox/queue hoặc cơ chế nền tương đương để giao dịch nghiệp vụ không phụ thuộc trực tiếp vào SMTP.

## Phạm vi sự kiện ban đầu

- `Borrowing.DueSoon`
- `Borrowing.Overdue`
- `MemberViolation.Created`
- `Fine.Created`
- `Fine.Adjusted`
- `Fine.PaymentRecorded`
- `Reservation.ReadyForPickup`
- `Reservation.Expiring`
- `MembershipCard.Expiring`

Danh mục sự kiện phải mở rộng được qua registry/configuration, không hard-code logic render và gửi rải rác trong controller hoặc frontend.

## Cấu hình SMTP

- `Smtp.Host`
- `Smtp.Port`
- `Smtp.SecurityMode`: `None`, `StartTls` hoặc `SslTls` theo thư viện SMTP đang dùng.
- `Smtp.Username`
- `Smtp.Password`: secret, không trả ngược về frontend và không ghi log/export dạng rõ.
- `Smtp.FromAddress`
- `Smtp.FromName`
- `Smtp.ReplyToAddress`: tùy chọn.
- `Smtp.TimeoutSeconds`
- `Smtp.MaxRetryCount`
- `Smtp.Enabled`

Ngưỡng gửi như số ngày sắp đến hạn, tần suất scheduler và batch size phải là typed system setting, validate giới hạn hợp lệ và dùng timezone hệ thống đã cấu hình.

## Acceptance criteria

### Backend

- [ ] Có SMTP adapter triển khai interface kênh email của notification service; domain/application không phụ thuộc trực tiếp thư viện SMTP cụ thể.
- [ ] SMTP adapter đọc typed setting mới nhất từ `SystemSetting` qua abstraction cấu hình; không hard-code credential hoặc server trong source code.
- [ ] Password SMTP được lưu bằng secret mechanism phù hợp, bị che trong API/UI/log/audit và không nằm trong configuration package dạng rõ.
- [ ] API đọc/cập nhật SMTP setting yêu cầu permission quản trị, validate host, port, security mode, email và timeout.
- [ ] Có thao tác kiểm tra kết nối/gửi email thử; kết quả không làm lộ credential, SMTP response nhạy cảm hoặc stack trace.
- [ ] Event registry ánh xạ event code với template, allow-list biến và quy tắc người nhận.
- [ ] Template email có code duy nhất, event code, subject, HTML body, plain-text body, allow-list biến, trạng thái active và concurrency token.
- [ ] Renderer từ chối biến không được phép/biến bắt buộc bị thiếu, encode dữ liệu động và sanitize HTML theo chính sách đã chốt.
- [ ] Khi sự kiện nghiệp vụ phát sinh, hệ thống tạo notification/outbox record trong cùng transaction; gửi SMTP được xử lý ngoài transaction nghiệp vụ.
- [ ] Worker/scheduler xử lý batch có idempotency key, retry có backoff, trạng thái `Pending/Processing/Sent/Failed/Cancelled` và không gửi trùng cùng event–recipient.
- [ ] Job nhắc hạn chỉ tạo email một lần cho mỗi mốc cấu hình; chạy lại scheduler không tạo bản ghi trùng.
- [ ] Chỉ gửi cho Member có email hợp lệ và đủ điều kiện nhận thông báo; trường hợp thiếu email được ghi trạng thái bỏ qua/lý do rõ ràng.
- [ ] Snapshot subject/body/recipient/template version được lưu để truy vết dù template thay đổi sau đó.
- [ ] Ghi AuditLog khi thay đổi SMTP setting, template, gửi thử, retry hoặc hủy email.

### Frontend

- [ ] Trang cấu hình SMTP hiển thị typed form, che password, không điền lại secret hiện có và chỉ gửi password khi người dùng nhập giá trị mới.
- [ ] UI hỗ trợ lưu cấu hình, kiểm tra kết nối/gửi thử, loading state, confirmation và lỗi an toàn theo permission.
- [ ] Trang `/email-templates` có list, search/filter theo event/status, create/edit/detail, activate/deactivate và optimistic concurrency handling.
- [ ] Template editor hiển thị biến hợp lệ của event, preview HTML/plain text bằng dữ liệu mẫu và báo biến thiếu trước khi lưu.
- [ ] Preview HTML được cô lập/sanitize, không thực thi script hoặc nội dung nguy hiểm.
- [ ] Trang lịch sử email có filter theo event, recipient, trạng thái, thời gian; xem snapshot/error đã redaction và retry khi có quyền.
- [ ] Menu, route, button và action cấu hình/template/history hiển thị theo permission hiệu lực.
- [ ] Mọi trang có loading, empty, error/retry, forbidden và conflict state phù hợp; dùng spinner/shared UI hiện có.

### Tích hợp

- [ ] Thay đổi trạng thái mượn, vi phạm, tiền phạt, reservation và thẻ thành viên tạo đúng email event, template và recipient.
- [ ] Backend là nguồn quyết định thời điểm/người nhận; frontend không tự tính lịch nhắc hoặc tạo email tự động.
- [ ] Cập nhật SMTP setting có hiệu lực cho lần gửi tiếp theo mà không cần sửa source code.
- [ ] SMTP tạm thời lỗi không rollback giao dịch nghiệp vụ; email được retry theo cấu hình và hiển thị đúng trạng thái.
- [ ] Email gửi thành công không bị gửi lại khi worker/job chạy lại.
- [ ] Template inactive hoặc thiếu không làm hỏng nghiệp vụ nguồn; notification được ghi trạng thái không gửi với lý do truy vết được.
- [ ] Không lộ SMTP password, token, PII không cần thiết hoặc nội dung email của độc giả ngoài permission.

## Checklist hoàn thành

### Backend

- [ ] Rà soát MS2-31/MS2-32 và tái sử dụng model/service hiện có.
- [ ] Bổ sung typed SMTP settings, secret handling, validator và permission.
- [ ] Hoàn thiện SMTP adapter, test-connection/test-send command và dependency registration.
- [ ] Hoàn thiện event registry, email renderer, template CRUD/preview API và seed template mặc định cần thiết.
- [ ] Hoàn thiện outbox/queue, worker/scheduler, retry/backoff, idempotency và retention.
- [ ] Tích hợp event tại các use case mượn, vi phạm, tiền phạt, reservation và membership card.
- [ ] Bổ sung migration/index cần thiết và AuditLog.

### Frontend

- [ ] Hoàn thiện API schema/hooks cho SMTP setting, test send, template và email history.
- [ ] Hoàn thiện form cấu hình SMTP với secret-safe behavior.
- [ ] Hoàn thiện `/email-templates`, template editor, variable picker và preview.
- [ ] Hoàn thiện `/email-history`, detail snapshot, filter và retry action.
- [ ] Cập nhật navigation registry và permission metadata.
- [ ] Kiểm tra responsive, accessibility, dark/light theme và spinner/loading state.

### Tích hợp

- [ ] Dùng SMTP sandbox/local capture hoặc tài khoản kiểm thử; không gửi email thật ngoài ý muốn.
- [ ] Kiểm tra không gửi trùng khi scheduler/worker chạy lại.
- [ ] Kiểm tra retry khi SMTP lỗi tạm thời và failed khi vượt số lần retry.
- [ ] Kiểm tra redaction của setting, API response, log và AuditLog.
- [ ] Cập nhật tài liệu biến môi trường/secret bootstrap nếu hệ thống cần khóa mã hóa ban đầu.

## Happy-case test

1. Quản trị viên cấu hình SMTP từ trang hệ thống và gửi email thử thành công.
2. Tạo/kích hoạt template `Borrowing.DueSoon`, preview bằng dữ liệu mẫu và xác nhận HTML/plain text đúng.
3. Tạo khoản mượn sắp đến hạn, chạy scheduler và xác nhận một email được tạo, gửi thành công và lưu snapshot.
4. Chạy lại scheduler cùng mốc và xác nhận không gửi email trùng.
5. Tạo vi phạm và tiền phạt, xác nhận đúng template, biến và người nhận.
6. Mô phỏng SMTP lỗi tạm thời, xác nhận nghiệp vụ vẫn commit và email được retry thành công.
7. Đăng nhập bằng tài khoản không có quyền và xác nhận không thấy menu, không truy cập được route/API quản trị email.

## Build test local

- [ ] `dotnet build` thành công và không có lỗi.
- [ ] Frontend type-check/lint thành công theo script hiện có.
- [ ] `pnpm build` thành công.
- [ ] Không chạy `dotnet test`, không yêu cầu Docker và không bắt buộc bổ sung unit test.
