# MS2-13 — Quản lý bản sao mã vạch và vị trí

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P0
- Phụ thuộc: MS2-10, MS2-12

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện BookCopy là đối tượng vật lý trực tiếp tham gia mượn, trả, đặt trước, kiểm kê và nhập kho. Mỗi bản sao có barcode duy nhất, condition, circulation status, ngày nhập, Shelf và nguồn StockReceiptItem nếu có.

## Acceptance criteria

### Backend

- [ ] Domain/API BookCopy enforce barcode duy nhất, state transition, optimistic concurrency và toàn vẹn liên kết Borrowing/Shelf/Receipt/Audit.

### Frontend

- [ ] Trang Copies hỗ trợ barcode scanner, filter, detail, relocate/status actions và hiển thị validation/conflict theo từng thao tác.

### Tích hợp

- [ ] Tạo và tra cứu BookCopy theo barcode, Book, Branch, Shelf, condition và status.
- [ ] State transition chỉ cho phép luồng hợp lệ giữa available, borrowed, reserved/held, lost, damaged, transferred và withdrawn.
- [ ] Barcode là duy nhất, chuẩn hóa trước khi so sánh và dùng được với máy quét.
- [ ] Relocate cập nhật Shelf/Branch nhất quán và ghi lịch sử/audit.
- [ ] Không cho sửa trạng thái làm phá vỡ Borrowing hoặc InventoryAudit đang hoạt động.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, state machine, command/query, index barcode, permission và AuditLog.

### Frontend

- [ ] Hoàn thiện API schema/hooks, BarcodeInput, table/detail, location picker và action confirmation.

### Tích hợp

- [ ] Domain method và validator cho state transition.
- [ ] API list/detail/update status/relocate.
- [ ] Trang `/copies` có filter, barcode lookup và lịch sử liên quan.
- [ ] Mọi feature bỏ sử dụng Book.Quantity làm nguồn tồn kho chính.

## Happy-case test

1. Tạo BookCopy với barcode và Shelf hợp lệ.
2. Tra cứu bằng máy quét, chuyển sang Shelf khác.
3. Xác nhận trạng thái, vị trí và AuditLog được cập nhật.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
