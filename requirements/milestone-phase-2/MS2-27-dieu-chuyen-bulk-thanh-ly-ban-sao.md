# MS2-27 — Điều chuyển cập nhật hàng loạt và thanh lý bản sao

- Phân loại: Full-stack / Feature / Improvement
- Mức ưu tiên: P1
- Phụ thuộc: MS2-13, MS2-26

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Xây dựng các thao tác vận hành BookCopy: điều chuyển Branch/Area/Shelf, cập nhật hàng loạt condition/status/location, đánh dấu mất/hỏng, rút khỏi lưu hành và nhập/xuất danh sách bản sao. Mỗi dòng phải có kết quả độc lập khi xử lý bulk.

## Acceptance criteria

### Backend

- [ ] Bulk/relocate/withdraw API validate từng BookCopy, permission/state/version, trả kết quả từng dòng và audit đủ truy vết.

### Frontend

- [ ] Copies UI có bulk selection/action, import preview, partial-failure result, export theo filter và confirmation cho hành động phá hủy.

### Tích hợp

- [ ] Chỉ BookCopy đủ điều kiện mới được relocate, mark lost/damaged hoặc withdraw.
- [ ] Thanh lý/rút khỏi lưu hành yêu cầu lý do, permission quản lý và không phá Borrowing active.
- [ ] Bulk action kiểm tra từng dòng, trả thành công/thất bại theo dòng và không che giấu partial failure.
- [ ] Import có template, preview, validate toàn bộ và không lưu trước bước xác nhận.
- [ ] Export tôn trọng filter và permission dữ liệu hiện tại.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện commands/query, row-level validation, transaction strategy, import/export service, permission và AuditLog.

### Frontend

- [ ] Hoàn thiện bulk toolbar, upload/preview, result table, export action và trạng thái retry/partial failure.

### Tích hợp

- [ ] API relocate/status/withdraw/bulk/import-preview/import-confirm/export.
- [ ] UI bulk selection, confirmation và result summary.
- [ ] Ghi lịch sử vị trí/trạng thái hoặc AuditLog đủ truy vết.
- [ ] Chống update đồng thời bằng version của BookCopy.

## Happy-case test

1. Chọn nhiều BookCopy available tại cùng Shelf.
2. Điều chuyển sang Shelf hợp lệ khác.
3. Xác nhận mọi dòng thành công, vị trí mới và lịch sử thay đổi chính xác.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
