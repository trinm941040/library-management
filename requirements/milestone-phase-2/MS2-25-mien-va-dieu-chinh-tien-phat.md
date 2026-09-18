# MS2-25 — Miễn và điều chỉnh tiền phạt

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P1
- Phụ thuộc: MS2-23, MS2-24

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện FineAdjustment cho miễn, giảm, tăng hoặc sửa số tiền phạt theo permission quản lý. Mỗi điều chỉnh cần loại, số tiền, lý do, người phê duyệt và thời điểm; không sửa ngược Violation hoặc Payment đã ghi.

## Acceptance criteria

### Backend

- [ ] FineAdjustment command enforce permission/lý do/số dư, lưu record bất biến và commit balance/AuditLog với concurrency check.

### Frontend

- [ ] Adjustment UI chỉ hiện theo permission, hiển thị balance trước/sau, yêu cầu lý do và xử lý conflict với payment đồng thời.

### Tích hợp

- [ ] Chỉ tài khoản có permission điều chỉnh/miễn phạt mới thực hiện được.
- [ ] Lý do là bắt buộc; số tiền và chiều điều chỉnh không làm số dư âm.
- [ ] Điều chỉnh được lưu bất biến và cập nhật balance Violation trong cùng transaction.
- [ ] Miễn toàn bộ chuyển Violation sang resolved/waived theo trạng thái rõ ràng.
- [ ] AuditLog lưu dữ liệu trước/sau và người phê duyệt.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện model/configuration, preview/create command-query, validator, permission, transaction và AuditLog.

### Frontend

- [ ] Hoàn thiện API hooks/schema, adjustment form, confirm dialog, balance preview và history.

### Tích hợp

- [ ] API preview/create adjustment và history.
- [ ] UI confirmation hiển thị số dư trước/sau.
- [ ] Tách permission xem, điều chỉnh và miễn toàn bộ.
- [ ] Xử lý concurrency khi có thanh toán hoặc điều chỉnh đồng thời.

## Happy-case test

1. Mở Violation còn số dư bằng tài khoản quản lý.
2. Tạo điều chỉnh giảm với lý do hợp lệ.
3. Xác nhận số dư, lịch sử adjustment và AuditLog được cập nhật.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
