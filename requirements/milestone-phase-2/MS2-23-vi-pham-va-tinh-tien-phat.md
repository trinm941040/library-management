# MS2-23 — Ghi nhận vi phạm và tính tiền phạt

- Phân loại: Full-stack / Feature / Business Rules
- Mức ưu tiên: P0
- Phụ thuộc: MS2-18, MS2-20

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Chuẩn hóa Violation cho quá hạn, mất và hỏng; liên kết Member, Book/BookCopy và Borrowing khi phù hợp. Tính tiền phạt theo FinePolicy hiệu lực, lưu căn cứ tính và số dư còn lại thay vì chỉ dùng trường FineAmount/IsOpen đơn giản.

## Acceptance criteria

### Backend

- [ ] Fine calculator dùng policy version, decimal chính xác, nguồn giao dịch và idempotency; Violation balance được suy ra nhất quán.

### Frontend

- [ ] Violation UI có list/detail, filter, breakdown tiền phạt, liên kết nguồn và các trạng thái loading/error/forbidden.

### Tích hợp

- [ ] Violation có loại, nguồn phát sinh, số tiền gốc, tổng điều chỉnh, tổng thanh toán, số dư và trạng thái.
- [ ] Quá hạn, mất và hỏng dùng đúng rule tương ứng, decimal precision và mức trần.
- [ ] Cùng một sự kiện không tạo Violation trùng khi request được gửi lại.
- [ ] Kết quả tính lưu policy/version và dữ liệu đầu vào đủ để kiểm tra lại.
- [ ] Màn hình `/violations` lọc theo Member, loại, trạng thái, khoảng ngày và số dư.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện model/configuration, calculator, command/query, validator, permission, idempotency và AuditLog.

### Frontend

- [ ] Hoàn thiện API schema/hooks, violation table/detail, filter URL state và fine breakdown.

### Tích hợp

- [ ] Domain fine calculator và Violation balance.
- [ ] API preview/create/list/detail.
- [ ] UI giải thích cấu phần tiền phạt và liên kết giao dịch nguồn.
- [ ] AuditLog ghi việc tạo hoặc thay đổi trạng thái vi phạm.

## Happy-case test

1. Trả một khoản mượn quá hạn có policy theo ngày.
2. Xác nhận số ngày, mức phạt và số dư được tính đúng.
3. Mở Violation detail và truy ngược được Member, Borrowing và BookCopy.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
