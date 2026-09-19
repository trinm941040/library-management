# MS2-11 — Tra cứu và quản lý biểu ghi sách

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P0
- Phụ thuộc: MS2-10, MS2-03, MS2-04

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện màn hình và API tra cứu, thêm, cập nhật, xem chi tiết và ngừng sử dụng biểu ghi sách. Hỗ trợ nhập mô tả, ISBN, tác giả, nhà xuất bản, thể loại, ngôn ngữ, năm xuất bản, số trang và trạng thái.

## Acceptance criteria

### Backend

- [ ] API catalog có contract typed, validation ISBN, quan hệ Author/Publisher/Category, server paging/filter/sort, permission và concurrency.

### Frontend

- [ ] Route catalog có table/detail/form, URL state, lookup/tạo nhanh danh mục và đủ loading/empty/error/forbidden/conflict/success.

### Tích hợp

- [ ] Tìm kiếm được theo tên, ISBN, tác giả, publisher, category và trạng thái.
- [ ] Tạo/cập nhật kiểm tra ISBN và cho phép tạo nhanh Author hoặc Publisher chưa tồn tại nếu có quyền.
- [ ] Ngừng sử dụng phải kiểm tra BookCopy đang lưu kho, đang mượn hoặc liên quan reservation.
- [ ] Danh sách dùng phân trang/sort/filter phía server và giữ filter trong URL.
- [ ] Form hiển thị lỗi validation và conflict rõ ràng.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện query/command/validator, persistence mapping, permission, transaction và AuditLog catalog.

### Frontend

- [ ] Hoàn thiện schema/model, API hooks, query keys, table, form, detail và deactivate confirmation.

### Tích hợp

- [ ] API catalog list/detail/create/update/deactivate hoàn chỉnh.
- [ ] Route `/catalog` và `/catalog/:id` hoàn chỉnh các trạng thái chuẩn.
- [ ] Action được bảo vệ bằng permission riêng.
- [ ] AuditLog lưu thay đổi quan trọng trước/sau.

## Happy-case test

1. Tạo Author và Publisher mới trong workflow tạo Book.
2. Tạo biểu ghi ISBN hợp lệ, cập nhật mô tả và tìm lại bằng ISBN.
3. Ngừng sử dụng một biểu ghi không có BookCopy đang hoạt động.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
