# MS2-19 — Lập phiếu mượn bằng mã thẻ và mã vạch

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P0
- Phụ thuộc: MS2-13, MS2-17, MS2-18

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện checkout workflow: quét mã thẻ, tải Member, kiểm tra eligibility, quét BookCopy, kiểm tra availability, tính hạn trả và tạo Borrowing. Mỗi BookCopy chỉ có tối đa một Borrowing active; cập nhật Borrowing, BookCopy và AuditLog trong cùng transaction.

## Acceptance criteria

### Backend

- [ ] Checkout handler enforce eligibility, policy, BookCopy uniqueness và commit Borrowing/BookCopy/AuditLog nguyên tử với concurrency control.

### Frontend

- [ ] Checkout UI tối ưu scan card/barcode, hiển thị member/copy/policy preview và xử lý validation/forbidden/conflict không mất dữ liệu đã quét.

### Tích hợp

- [ ] Từ chối thẻ hết hạn/khóa, Member bị hạn chế, vượt giới hạn, còn nghĩa vụ chặn mượn hoặc BookCopy không available.
- [ ] Due date được tính từ policy hiệu lực và lưu cùng giao dịch.
- [ ] Checkout thành công tạo Borrowing gắn Member, BookCopy và Employee xử lý; BookCopy chuyển borrowed.
- [ ] Hai request đồng thời cho cùng BookCopy chỉ một request thành công, request còn lại trả `409`.
- [ ] Frontend hiển thị từng bước quét, điều kiện mượn, hạn trả và kết quả rõ ràng.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện lookup/query, checkout command, validator, transaction, permission, concurrency và AuditLog.

### Frontend

- [ ] Hoàn thiện BarcodeInput workflow, API hooks, checkout summary, confirm action và success/reset state.

### Tích hợp

- [ ] Domain invariant và transaction handler checkout.
- [ ] API lookup card/barcode và create borrowing.
- [ ] Route `/circulation/checkout` tối ưu thao tác máy quét/bàn phím.
- [ ] AuditLog không lưu dư thừa PII.

## Happy-case test

1. Quét thẻ Member active và BookCopy available.
2. Xác nhận hạn trả, hoàn tất checkout.
3. Tra cứu khoản mượn active và xác nhận BookCopy ở trạng thái borrowed.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
