# MS2-21 — Gia hạn khoản mượn

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P0
- Phụ thuộc: MS2-18, MS2-19, MS2-22

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện gia hạn Borrowing, kiểm tra Member, trạng thái khoản mượn, giới hạn số lần gia hạn, overdue, restriction và reservation đang chờ. Lưu Renewal riêng để không ghi đè lịch sử hạn trả cũ.

## Acceptance criteria

### Backend

- [ ] Renewal handler kiểm tra policy/restriction/reservation, lưu Renewal và cập nhật Borrowing/AuditLog nguyên tử với version check.

### Frontend

- [ ] Loan detail hiển thị eligibility, lý do từ chối, hạn cũ/mới, lịch sử renewal và xử lý `403/409` rõ ràng.

### Tích hợp

- [ ] Chỉ Borrowing active và đủ điều kiện theo policy mới được gia hạn.
- [ ] Từ chối khi đạt max renewals, đã quá hạn không được phép, Member bị hạn chế hoặc có reservation ưu tiên.
- [ ] Renewal lưu hạn cũ, hạn mới, thời điểm và nhân viên thực hiện.
- [ ] Borrowing cập nhật due date và version trong cùng transaction với Renewal/AuditLog.
- [ ] UI hiển thị lý do không đủ điều kiện và hạn mới trước khi xác nhận.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện eligibility query, renew command, validator, transaction, permission, concurrency và AuditLog.

### Frontend

- [ ] Hoàn thiện API hooks/schema, renewal preview, confirm action, history timeline và cache invalidation.

### Tích hợp

- [ ] API eligibility/preview và confirm renewal.
- [ ] Route `/loans/:id` có lịch sử Renewal.
- [ ] Permission riêng cho xem và thực hiện gia hạn.
- [ ] Xử lý `409` khi Borrowing thay đổi đồng thời.

## Happy-case test

1. Mở Borrowing active chưa đạt giới hạn gia hạn và không có reservation chờ.
2. Xem hạn mới, xác nhận gia hạn.
3. Kiểm tra due date mới và một Renewal record đầy đủ.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
