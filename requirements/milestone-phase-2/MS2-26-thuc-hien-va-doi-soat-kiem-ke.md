# MS2-26 — Thực hiện và đối soát kiểm kê

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P1
- Phụ thuộc: MS2-12, MS2-13

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện InventoryAudit và InventoryAuditItem cho một Branch, Area hoặc Shelf. Nhân viên tạo đợt, quét barcode, ghi vị trí thực tế, đối chiếu với dữ liệu kỳ vọng, phân loại found/missing/unexpected/misplaced và hoàn tất đợt.

## Acceptance criteria

### Backend

- [ ] Inventory service tạo snapshot, chống scan trùng, tính reconciliation và khóa đợt khi complete với transaction/concurrency phù hợp.

### Frontend

- [ ] Inventory UI hỗ trợ create/resume/scan/progress/reconcile/complete, hoạt động tốt với máy quét và không mất trạng thái sau reload.

### Tích hợp

- [ ] Đợt kiểm kê có phạm vi, người bắt đầu, thời gian, trạng thái và snapshot dữ liệu kỳ vọng.
- [ ] Mỗi BookCopy chỉ có một dòng trong cùng đợt; quét lặp không tạo trùng.
- [ ] Đối soát xác định đúng thiếu, thừa/sai phạm vi, sai vị trí và sai trạng thái.
- [ ] Hoàn tất khóa dữ liệu quét, tạo summary và yêu cầu xử lý chênh lệch có chủ đích.
- [ ] Không tự ghi đè trạng thái/vị trí thực tế nếu người có quyền chưa xác nhận.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện model/configuration, snapshot query, scan/reconcile/complete commands, permission, transaction và AuditLog.

### Frontend

- [ ] Hoàn thiện API hooks/schema, audit wizard, scanner, progress, discrepancy table và complete confirmation.

### Tích hợp

- [ ] API create/start/scan/reconcile/complete/detail/export.
- [ ] Route `/inventory-audits` có barcode workflow và tiến độ.
- [ ] Hỗ trợ tiếp tục đợt đang làm sau khi reload.
- [ ] AuditLog cho bắt đầu, hoàn tất và áp dụng chênh lệch.

## Happy-case test

1. Tạo đợt kiểm kê cho một Shelf có dữ liệu kỳ vọng.
2. Quét đủ các BookCopy đúng vị trí.
3. Hoàn tất và xác nhận summary không có chênh lệch.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
