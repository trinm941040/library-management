# MS2-15 — Tiếp nhận và cập nhật phiếu nhập sách

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P1
- Phụ thuộc: MS2-11, MS2-12, MS2-14

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện workflow tiếp nhận lô sách và phiếu nhập nháp với StockReceipt, StockReceiptItem. Cho phép tra cứu ISBN, chọn Book, nhập số lượng dự kiến/thực nhận/hỏng, đơn giá, ghi chú và người tiếp nhận.

## Acceptance criteria

### Backend

- [ ] API StockReceipt/Item validate trạng thái, số lượng, đơn giá, liên kết Book/Supplier/Branch và concurrency khi sửa phiếu.

### Frontend

- [ ] UI receipt có form nhiều dòng, ISBN lookup, tính tổng, unsaved-change warning và trạng thái validation/conflict.

### Tích hợp

- [ ] Số phiếu nhập là duy nhất và phiếu gắn Supplier, Branch, người tiếp nhận, thời điểm và trạng thái.
- [ ] Dòng phiếu tham chiếu Book hợp lệ; các số lượng không âm và tuân thủ quan hệ logic.
- [ ] Chỉ phiếu ở trạng thái nháp/đang tiếp nhận mới được sửa.
- [ ] Hiển thị tổng số lượng và tổng giá trị chính xác từ các dòng.
- [ ] Có tra cứu phiếu theo số, khoảng thời gian, Supplier, Branch và trạng thái.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, command/query, validator, transaction, permission và AuditLog phiếu nháp.

### Frontend

- [ ] Hoàn thiện API hooks/schema, receipt editor, ISBN lookup, totals và route list/detail.

### Tích hợp

- [ ] API create/update/detail/search StockReceipt.
- [ ] Route `/stock-receipts` và `/stock-receipts/:id` với workflow dòng hàng.
- [ ] ISBN lookup tái sử dụng catalog contract.
- [ ] Form cảnh báo khi rời trang còn thay đổi chưa lưu.

## Happy-case test

1. Tạo phiếu tiếp nhận tại một Branch với Supplier active.
2. Thêm sách bằng ISBN, nhập số lượng và đơn giá.
3. Lưu nháp, mở lại và xác nhận tổng số liệu chính xác.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
