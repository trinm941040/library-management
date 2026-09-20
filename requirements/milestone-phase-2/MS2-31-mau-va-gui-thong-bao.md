# MS2-31 — Mẫu thông báo và gửi thông báo vận hành

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P1
- Phụ thuộc: MS2-09, MS2-17, MS2-30

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện NotificationTemplate và Notification cho thông báo vận hành. Quản trị viên tạo/cập nhật mẫu theo sự kiện và kênh; người có quyền chọn mẫu, đối tượng nhận, xem preview và gửi. Nội dung render được lưu riêng để không thay đổi theo template về sau. Thông báo nội bộ phải theo dõi trạng thái chưa đọc/đã đọc và thời điểm đọc.

## Acceptance criteria

### Backend

- [ ] Template renderer validate allow-list biến, escape nội dung và Notification service lưu snapshot/status/failure với adapter kênh tách biệt.
- [ ] Notification nội bộ có `ReadAtUtc`, truy vấn chưa đọc và command `MarkRead`; chỉ đúng người nhận hoặc tài khoản có quyền quản trị mới được cập nhật trạng thái đọc.

### Frontend

- [ ] Notification UI có template CRUD, recipient picker theo quyền, preview, send/retry, history status/error states và chỉ báo số thông báo chưa đọc.
- [ ] Người nhận nội bộ có thể mở thông báo và đánh dấu đã đọc; UI cập nhật số chưa đọc không cần tải lại toàn trang.

### Tích hợp

- [ ] Template có code duy nhất, subject/body, channel, biến được phép và trạng thái active.
- [ ] Dữ liệu render được escape/validate; biến thiếu tạo lỗi rõ ràng trước khi gửi.
- [ ] Notification lưu recipient, destination, nội dung đã render, trạng thái gửi, thời điểm gửi, failure reason và `ReadAtUtc` đối với thông báo nội bộ.
- [ ] Trạng thái gửi và trạng thái đọc được quản lý độc lập; gửi thành công không tự động đồng nghĩa đã đọc.
- [ ] Chỉ gửi đến Staff hoặc Member phù hợp quyền/phạm vi và không lộ danh sách người nhận ngoài quyền.
- [ ] Mọi thay đổi template và thao tác gửi được audit.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện model/configuration, renderer, adapter interface, command/query, permission, retry/idempotency, `MarkRead`, unread count và AuditLog.

### Frontend

- [ ] Hoàn thiện API schema/hooks, template form, preview, recipient selection, send confirmation, history, unread badge và mark-read mutation.

### Tích hợp

- [ ] API CRUD template, preview, create/send và history.
- [ ] API lấy thông báo của người nhận hiện tại, unread count và mark read được bảo vệ đúng recipient/permission.
- [ ] Route `/notifications` có template management và send workflow.
- [ ] Adapter kênh gửi có interface rõ ràng; local dùng implementation kiểm thử an toàn.
- [ ] UI hiển thị sent/failed và cho phép retry có kiểm soát.

## Happy-case test

1. Tạo template thông báo reservation sẵn sàng.
2. Chọn một Member, preview và gửi bằng adapter local.
3. Xác nhận Notification ở trạng thái sent và nội dung snapshot đúng.
4. Đăng nhập bằng người nhận nội bộ, mở thông báo và xác nhận `ReadAtUtc` được ghi, unread count giảm đúng.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
