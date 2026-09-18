# MS2-33 — Chuẩn hóa tìm kiếm phân trang import và export

- Phân loại: Full-stack / Improvement
- Mức ưu tiên: P1
- Phụ thuộc: MS2-02, MS2-03, MS2-04

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Chuẩn hóa contract dùng chung cho search, filter, sort, paging, saved URL state, bulk result, import preview và export. Áp dụng lần lượt cho catalog, copies, members, staff, accounts, receipts, inventory, violations, reports và audit logs.

## Acceptance criteria

### Backend

- [ ] Query/import/export contracts enforce allow-list filter/sort, page-size limit, cancellation, permission scope và row-level results.

### Frontend

- [ ] Shared table/URL/import/export adapters parse contract bằng schema, giữ navigation state và hiển thị partial failure theo dòng.

### Tích hợp

- [ ] Paging response có items, page/pageSize, totalCount và sort ổn định; giới hạn pageSize được enforce.
- [ ] Filter/sort field dùng allow-list, không ghép SQL hoặc expression từ input không tin cậy.
- [ ] Frontend đồng bộ search/filter/sort/page với URL và phục hồi khi back/forward.
- [ ] Import trả lỗi theo số dòng/trường, có preview và confirm; export tôn trọng filter, sort và permission.
- [ ] Bulk endpoint trả kết quả từng item, nêu rõ success/failure và correlation ID.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện paging/filter/sort specification, bulk result, import preview/confirm, export stream và query index review.

### Frontend

- [ ] Hoàn thiện URL-state schema, DataTable adapter, upload/preview/result components và export handling.

### Tích hợp

- [ ] Định nghĩa contract và helper backend dùng chung có giới hạn rõ ràng.
- [ ] Định nghĩa parser/schema URL state frontend.
- [ ] Chuẩn hóa DataTable adapter cho các feature.
- [ ] Bổ sung index cho truy vấn phổ biến sau khi đo trên dữ liệu mẫu đủ lớn.

## Happy-case test

1. Lọc và sắp xếp một danh sách, chuyển sang trang 2.
2. Mở chi tiết rồi quay lại.
3. Xác nhận URL và kết quả danh sách được phục hồi chính xác; export chứa đúng tập dữ liệu đã lọc.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
