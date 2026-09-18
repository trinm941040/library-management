# MS2-08 — Quản lý hồ sơ nhân viên

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-03, MS2-12

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện CRUD hồ sơ Employee gồm mã nhân viên, liên hệ, chức danh, phòng ban, chi nhánh, ngày làm việc và trạng thái việc làm. Hồ sơ nhân viên tồn tại độc lập với tài khoản truy cập và được giữ lại để bảo toàn lịch sử nghiệp vụ.

## Acceptance criteria

### Backend

- [ ] API Employee validate mã duy nhất, chi nhánh/trạng thái hợp lệ, concurrency token và giữ nguyên khóa tham chiếu lịch sử.

### Frontend

- [ ] Trang Staff có list/detail/form, filter trong URL, branch picker, trạng thái loading/error/conflict và permission-aware actions.

### Tích hợp

- [ ] Tạo, xem, cập nhật và tra cứu Employee theo mã, tên, chi nhánh và trạng thái.
- [ ] Mã nhân viên là duy nhất; email, ngày và trường bắt buộc được validate.
- [ ] Cập nhật việc làm hoặc chi nhánh không làm mất liên kết lịch sử giao dịch.
- [ ] Khi chuyển trạng thái nghỉ việc, hệ thống cảnh báo và cho phép workflow vô hiệu hóa account liên kết.
- [ ] Thao tác thay đổi hồ sơ quan trọng được ghi AuditLog.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, migration, command/query, validation, permission và AuditLog Employee.

### Frontend

- [ ] Hoàn thiện API schema/hooks, form, table, detail và luồng chuyển trạng thái việc làm.

### Tích hợp

- [ ] API list/detail/create/update/status có phân trang và permission.
- [ ] Trang `/staff` có bộ lọc, form và chi tiết liên kết account nếu có.
- [ ] Phân biệt rõ nhãn Staff, Access Account và Member.
- [ ] Xử lý conflict khi hồ sơ bị cập nhật đồng thời.

## Happy-case test

1. Tạo một Employee tại chi nhánh đang hoạt động.
2. Cập nhật chức danh và chuyển chi nhánh.
3. Tra cứu lại và xác nhận account liên kết, nếu có, vẫn nguyên vẹn.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
