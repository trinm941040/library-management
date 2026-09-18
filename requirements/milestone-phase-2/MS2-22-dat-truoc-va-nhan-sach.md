# MS2-22 — Đặt trước và xác nhận nhận sách

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P0
- Phụ thuộc: MS2-13, MS2-17, MS2-18

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện vòng đời Reservation: tạo, xếp hàng, gán BookCopy khi có sẵn, xác định hạn nhận, cập nhật, hủy, hết hạn và xác nhận nhận sách để chuyển sang checkout. Quản lý thứ tự ưu tiên nhất quán theo policy.

## Acceptance criteria

### Backend

- [ ] Reservation state machine và queue service enforce uniqueness/priority/expiry, transaction assign/release/pickup và reuse checkout rules.

### Frontend

- [ ] Reservation UI có list/detail/create/cancel/pickup, queue position, hold expiry và permission/conflict/error states.

### Tích hợp

- [ ] Member đủ điều kiện có thể tạo reservation cho Book/BookCopy theo policy và không tạo yêu cầu mở trùng.
- [ ] Queue có thứ tự xác định; khi có bản sao sẵn sàng, hệ thống gán đúng người đầu tiên và tính hold expiry.
- [ ] Hủy/hết hạn giải phóng bản sao và chuyển cơ hội cho reservation kế tiếp.
- [ ] Xác nhận nhận sách đóng reservation và tạo Borrowing qua cùng rule checkout.
- [ ] Tất cả transition có actor, timestamp, permission và AuditLog phù hợp.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/state machine, queue query, commands, scheduler-safe expiry, permission, transaction và AuditLog.

### Frontend

- [ ] Hoàn thiện API hooks/schema, reservation table/detail, create/cancel/pickup dialogs và cache invalidation.

### Tích hợp

- [ ] Domain state machine Reservation.
- [ ] API list/create/update/cancel/expire/assign/pickup.
- [ ] Route `/reservations` và thao tác tại `/loans/:id` hoặc Member detail.
- [ ] Hiển thị queue position, hạn nhận và trạng thái chuẩn.

## Happy-case test

1. Tạo reservation cho Member đủ điều kiện.
2. Gán BookCopy available và xác nhận hạn nhận.
3. Xác nhận Member nhận sách; reservation fulfilled và Borrowing active được tạo.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
