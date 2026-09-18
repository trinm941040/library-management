# MS2-07 — Quản lý tài khoản truy cập

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P0
- Phụ thuộc: MS2-05, MS2-08, MS2-09

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Xây dựng quản lý Access Account tách biệt hồ sơ nhân viên: tạo tài khoản cho đúng một Employee, khóa/mở khóa, đặt lại quyền truy cập, xem và thu hồi phiên, gán vai trò. Không xóa Employee hoặc lịch sử khi tài khoản bị khóa.

## Acceptance criteria

### Backend

- [ ] API account thực thi ràng buộc một Employee–một account, transaction khóa/revoke session và không trả dữ liệu Identity nhạy cảm.

### Frontend

- [ ] Màn hình account có list/detail/create/status/session actions, permission boundary, confirmation và phản hồi conflict/partial failure.

### Tích hợp

- [ ] Một account liên kết đúng một Employee; một Employee có tối đa một account.
- [ ] Tạo account bắt buộc chọn ít nhất một role và không làm thay đổi dữ liệu nhân sự ngoài liên kết.
- [ ] Khóa account thu hồi toàn bộ phiên đang hoạt động ngay trong cùng workflow.
- [ ] Mở khóa không tự khôi phục refresh token đã bị thu hồi.
- [ ] Mọi thao tác tạo, khóa, mở, reset và revoke session được ghi AuditLog.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity configuration, command/query, validator, permission, transaction và AuditLog cho account/session.

### Frontend

- [ ] Hoàn thiện API hooks, account form/table/detail, role picker và session revoke UI.

### Tích hợp

- [ ] API danh sách, chi tiết, tạo, đổi trạng thái, reset và quản lý session.
- [ ] Màn hình `/access-accounts` có tìm kiếm, lọc trạng thái và hành động theo quyền.
- [ ] Các hành động nhạy cảm có xác nhận và phản hồi thành công/thất bại rõ ràng.
- [ ] Không trả password hash, token hash hoặc security stamp ra frontend.

## Happy-case test

1. Chọn Employee chưa có account, tạo account và gán role.
2. Đăng nhập bằng account mới.
3. Khóa account từ màn hình quản trị và xác nhận phiên hiện tại bị vô hiệu hóa.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
