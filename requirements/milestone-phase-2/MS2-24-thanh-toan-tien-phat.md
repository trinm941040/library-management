# MS2-24 — Thanh toán tiền phạt

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P0
- Phụ thuộc: MS2-23

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện Payment cho phép thu một phần hoặc toàn bộ số dư Violation. Lưu số tiền, phương thức, mã tham chiếu, người thu và thời điểm; cập nhật số dư/trạng thái Violation trong transaction và chống ghi nhận thanh toán lặp.

## Acceptance criteria

### Backend

- [ ] Payment command validate balance/method, chống submit lặp và commit Payment/Violation/AuditLog nguyên tử.

### Frontend

- [ ] Payment form hiển thị balance, validate amount/method, disable double submit, trả biên nhận và refresh history/balance.

### Tích hợp

- [ ] Số tiền thanh toán lớn hơn 0 và không vượt số dư còn lại trừ khi có quy tắc hoàn tiền riêng.
- [ ] Partial payment giữ Violation còn mở; thanh toán đủ chuyển sang resolved/paid.
- [ ] Payment không bị sửa/xóa trực tiếp; điều chỉnh sai phải theo workflow có audit.
- [ ] Có idempotency hoặc cơ chế chống submit lặp cho giao dịch thanh toán.
- [ ] Biên nhận hiển thị Member, Violation, số đã thu, còn lại, phương thức và người thu.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, preview/create query-command, idempotency, permission, concurrency và AuditLog.

### Frontend

- [ ] Hoàn thiện API hooks/schema, payment form, confirmation, receipt view và transaction history.

### Tích hợp

- [ ] API payment preview/create/history.
- [ ] UI `/payments` hoặc action tại Violation detail.
- [ ] Xác nhận và disable submit trong khi xử lý.
- [ ] Payment, Violation balance và AuditLog commit nguyên tử.

## Happy-case test

1. Mở Violation còn số dư.
2. Ghi nhận partial payment hợp lệ.
3. Ghi nhận phần còn lại và xác nhận Violation chuyển paid với hai Payment records.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
