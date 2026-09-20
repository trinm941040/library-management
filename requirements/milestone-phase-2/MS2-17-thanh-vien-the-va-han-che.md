# MS2-17 — Quản lý thành viên thẻ và hạn chế

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-03, MS2-04

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện Member, MembershipCard và MemberRestriction. Hỗ trợ tạo/cập nhật hồ sơ, cấp/gia hạn/khóa thẻ, thay đổi trạng thái thành viên, thêm/gỡ hạn chế và xem lịch sử mượn, trả, gia hạn, reservation, vi phạm và thanh toán.

## Acceptance criteria

### Backend

- [ ] API Member/Card/Restriction enforce unique codes, lifecycle rules, permission, concurrency và giới hạn PII theo người gọi.

### Frontend

- [ ] Route Member có list/detail/form, card/restriction actions, history tabs, URL filter và đầy đủ state chuẩn.

### Tích hợp

- [ ] Mã thành viên và số thẻ là duy nhất; Member không có credential, role hoặc session.
- [ ] Mỗi Member có tối đa một thẻ hiện hành; cấp/gia hạn lưu lịch sử cần thiết.
- [ ] Hạn chế có loại, lý do, thời gian hiệu lực, người tạo/gỡ và ảnh hưởng đúng eligibility.
- [ ] Trang chi tiết hiển thị lịch sử liên quan theo permission, phân trang và không lộ PII ngoài quyền.
- [ ] Thay đổi trạng thái, thẻ và hạn chế được ghi AuditLog.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, migration, command/query, validator, permission, redaction và AuditLog Member.

### Frontend

- [ ] Hoàn thiện API schema/hooks, form/table/detail, card/restriction workflow và history pagination.

### Tích hợp

- [ ] API member, card, restriction và history.
- [ ] Route `/members` và `/members/:id` đầy đủ trạng thái chuẩn.
- [ ] Form validate thông tin định danh/liên hệ và xử lý mã trùng.
- [ ] Tích hợp member lookup dùng mã thẻ cho circulation.

## Happy-case test

1. Tạo Member và cấp thẻ có ngày hết hạn hợp lệ.
2. Gia hạn thẻ, thêm rồi gỡ một hạn chế tạm thời.
3. Mở lịch sử và xác nhận các giao dịch được tổng hợp đúng.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
