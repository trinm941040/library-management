# MS2-30 — Nhật ký kiểm toán và lịch sử hoạt động

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P0
- Phụ thuộc: MS2-02, MS2-09

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện AuditLog bất biến cho tài khoản, quyền, cấu hình, biên mục, kho, thành viên, lưu thông và tiền phạt. Cho phép kiểm toán viên tra cứu theo actor, action, entity, khoảng thời gian, correlation ID, địa chỉ IP và xem before/after đã che dữ liệu nhạy cảm.

## Acceptance criteria

### Backend

- [ ] Audit writer tạo record bất biến trong transaction, chuẩn hóa action/entity, redaction dữ liệu nhạy cảm, lưu `IpAddress` đã chuẩn hóa và query theo scope quyền.
- [ ] Địa chỉ IP chỉ được lấy từ connection hoặc forwarded header đã cấu hình trusted proxy; không tin trực tiếp header do client tùy ý gửi.

### Frontend

- [ ] Audit UI có server filter/paging, filter IP, detail diff dễ đọc, correlation link và che trường nhạy cảm theo contract.

### Tích hợp

- [ ] Các use case quan trọng trong kiến trúc đều tạo AuditLog với actor, action, entity, timestamp UTC, correlation ID và `IpAddress` khi request có địa chỉ nguồn hợp lệ.
- [ ] AuditLog không chứa password, token, secret hoặc PII không cần thiết.
- [ ] Bản ghi audit không được sửa/xóa qua API nghiệp vụ thông thường.
- [ ] Tra cứu có phân trang server, sort ổn định và filter theo thời gian/action/actor/entity/IP.
- [ ] Trang chi tiết hiển thị before/after dễ đọc và tuân thủ permission.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện audit abstraction/interceptor, persistence/index, IP resolver, trusted proxy configuration, redaction, query API, permission và retention rule.

### Frontend

- [ ] Hoàn thiện API hooks/schema, filter URL state gồm IP, table/detail diff và entity activity link.

### Tích hợp

- [ ] Chuẩn hóa danh mục action/entity và redaction rule.
- [ ] API `/audit-log` list/detail và entity activity history.
- [ ] Route `/audit-log` cho auditor/admin.
- [ ] Liên kết từ màn hình nghiệp vụ đến lịch sử đối tượng khi có quyền.

## Happy-case test

1. Cập nhật một Book hoặc Role bằng tài khoản có quyền.
2. Tra cứu theo correlation ID.
3. Xác nhận actor, before/after, địa chỉ IP, correlation ID và thời điểm đúng, không có dữ liệu nhạy cảm.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
