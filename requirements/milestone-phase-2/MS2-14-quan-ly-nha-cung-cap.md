# MS2-14 — Quản lý nhà cung cấp

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P1
- Phụ thuộc: MS2-01, MS2-03, MS2-04

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Xây dựng quản lý Supplier phục vụ nhập kho, gồm mã, tên, người liên hệ, email, số điện thoại, địa chỉ và trạng thái. Nhà cung cấp có phiếu nhập lịch sử không được xóa cứng.

## Acceptance criteria

### Backend

- [ ] API Supplier validate mã/liên hệ, trạng thái, uniqueness, permission và bảo toàn StockReceipt lịch sử.

### Frontend

- [ ] UI Supplier có list/detail/form, search/filter, active-state handling và picker dùng chung cho receipt.

### Tích hợp

- [ ] Tạo, cập nhật, xem và tìm kiếm Supplier theo mã, tên, liên hệ và trạng thái.
- [ ] Mã Supplier là duy nhất; email và số điện thoại được validate.
- [ ] Supplier ngừng hoạt động không thể được chọn cho phiếu nhập mới.
- [ ] Lịch sử StockReceipt vẫn truy cập được khi Supplier ngừng hoạt động.
- [ ] API và UI được bảo vệ bằng permission phù hợp.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, migration, command/query, validator, permission và AuditLog Supplier.

### Frontend

- [ ] Hoàn thiện API hooks/schema, table, form, detail và Supplier picker.

### Tích hợp

- [ ] Entity, configuration, API và query supplier.
- [ ] UI danh sách, form và picker dùng trong stock receipt.
- [ ] Chuẩn hóa lỗi duplicate và conflict.
- [ ] AuditLog cho thay đổi trạng thái.

## Happy-case test

1. Tạo Supplier hợp lệ.
2. Tìm Supplier theo mã và chọn trong form phiếu nhập.
3. Cập nhật thông tin liên hệ và xác nhận dữ liệu mới được hiển thị.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
