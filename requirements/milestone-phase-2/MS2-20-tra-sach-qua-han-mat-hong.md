# MS2-20 — Trả sách quá hạn mất và hỏng

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P0
- Phụ thuộc: MS2-19, MS2-23

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện return workflow bằng barcode. Tải Borrowing active, ghi ngày trả, cập nhật BookCopy và xử lý nhánh đúng hạn, quá hạn, mất hoặc hỏng. Nhánh vi phạm tạo Violation và tiền phạt theo policy trong cùng transaction.

## Acceptance criteria

### Backend

- [ ] Return handler xác định Borrowing active, tính overdue/lost/damaged và commit Borrowing/BookCopy/Violation/AuditLog nguyên tử, idempotent.

### Frontend

- [ ] Return UI hỗ trợ scan liên tiếp, preview khoản mượn/phạt, chọn condition, nhập ghi chú bắt buộc và hiển thị kết quả từng lần trả.

### Tích hợp

- [ ] Barcode xác định đúng BookCopy và Borrowing active cần trả.
- [ ] Trả bình thường đóng Borrowing và đưa BookCopy về available nếu không có hold khác.
- [ ] Quá hạn tính số ngày theo quy tắc UTC/ngày nghiệp vụ và tạo Violation tương ứng.
- [ ] Mất/hỏng cập nhật condition/status, ghi chú bắt buộc và không đưa bản sao về available.
- [ ] Borrowing, BookCopy, Violation và AuditLog commit nguyên tử; request lặp không xử lý trả hai lần.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện return preview/confirm query-command, policy integration, transaction, permission và concurrency.

### Frontend

- [ ] Hoàn thiện return scanner, condition form, fine preview, confirm dialog và conflict/error recovery.

### Tích hợp

- [ ] API return preview và confirm return.
- [ ] UI `/circulation/return` hỗ trợ quét liên tiếp và xác nhận nhánh mất/hỏng.
- [ ] Hiển thị khoản phạt dự kiến trước confirm.
- [ ] Cập nhật reservation queue nếu có bản sao vừa available.

## Happy-case test

1. Quét BookCopy đang mượn và xem thông tin khoản mượn.
2. Xác nhận trả đúng hạn.
3. Kiểm tra Borrowing đã đóng, BookCopy available và lịch sử Member có giao dịch trả.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
