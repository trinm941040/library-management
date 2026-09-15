# MS2-16 — Xác nhận nhập kho và tạo bản sao

- Phân loại: Full-stack / Feature / Transaction
- Mức ưu tiên: P0
- Phụ thuộc: MS2-13, MS2-15

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Xây dựng bước xác nhận phiếu nhập: lập DiscrepancyReport khi số lượng/tình trạng sai lệch, gán barcode và vị trí cho từng bản sao, tạo BookCopy, cập nhật tồn kho và khóa phiếu. Toàn bộ bước xác nhận là một transaction có kiểm soát đồng thời.

## Acceptance criteria

### Backend

- [ ] Confirm handler tạo DiscrepancyReport/BookCopy idempotent trong một transaction, enforce barcode/Shelf và optimistic concurrency.
- [ ] `DiscrepancyReport` lưu tối thiểu `Type`, `ExpectedQuantity`, `ActualQuantity`, `Description`, `CreatedAtUtc`, người lập và liên kết `StockReceipt`; số lượng được tính từ dữ liệu phiếu thay vì tin giá trị tổng do client gửi.

### Frontend

- [ ] UI hỗ trợ gán barcode/vị trí hàng loạt, preview discrepancy theo loại/số dự kiến/số thực tế/mô tả, chặn confirm khi dữ liệu thiếu và hiển thị kết quả theo dòng.

### Tích hợp

- [ ] Mỗi bản sao thực nhận đủ điều kiện có barcode duy nhất và Shelf hợp lệ trước khi xác nhận.
- [ ] Sai lệch số lượng hoặc hư hỏng tạo DiscrepancyReport liên kết đúng phiếu, đúng loại, số lượng dự kiến, số lượng thực tế, mô tả và thời điểm lập.
- [ ] Xác nhận tạo đúng số BookCopy, gắn StockReceiptItem và đặt status/condition phù hợp.
- [ ] Phiếu chỉ được xác nhận một lần; request lặp không tạo bản sao trùng.
- [ ] StockReceipt, BookCopy, discrepancy và AuditLog commit nguyên tử; conflict trả `409`.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện confirm command, validator, idempotency, transaction, permission, DiscrepancyReport mapping/persistence và AuditLog.

### Frontend

- [ ] Hoàn thiện scan/grid editor, discrepancy review đầy đủ trường, confirm dialog, API mutation và conflict handling.

### Tích hợp

- [ ] Domain rule và transaction handler confirm receipt.
- [ ] UI gán barcode/vị trí có hỗ trợ quét và kiểm tra trùng.
- [ ] Màn hình review chênh lệch trước xác nhận.
- [ ] Hiển thị kết quả số bản sao tạo thành công và trạng thái phiếu.

## Happy-case test

1. Mở phiếu có hai dòng và gán barcode/Shelf cho từng bản sao.
2. Xác nhận phiếu.
3. Tra cứu BookCopy mới và xác nhận số lượng, nguồn phiếu, vị trí và trạng thái available.
4. Với phiếu có sai lệch hợp lệ, xác nhận DiscrepancyReport hiển thị đúng type, expected quantity, actual quantity, description và created time.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
