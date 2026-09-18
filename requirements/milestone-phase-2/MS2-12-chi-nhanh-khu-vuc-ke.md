# MS2-12 — Quản lý chi nhánh khu vực và kệ

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-03, MS2-04

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Xây dựng quản lý cấu trúc vị trí `Branch → Area → Shelf`, gồm mã, tên, địa chỉ, trạng thái và quan hệ sở hữu. Các vị trí đang được Employee hoặc BookCopy sử dụng không được xóa cứng.

## Acceptance criteria

### Backend

- [ ] API đảm bảo cấu trúc Branch–Area–Shelf, uniqueness theo phạm vi, trạng thái hợp lệ và không xóa cứng vị trí có tham chiếu.
- [ ] Branch chỉ được kích hoạt khi có ít nhất một Area active và Area đó có ít nhất một Shelf active; backend trả lỗi nghiệp vụ rõ ràng nếu chưa đủ cấu trúc.

### Frontend

- [ ] UI quản lý cây vị trí, form/picker loại trừ vị trí inactive và hiển thị đối tượng bị ảnh hưởng trước khi ngừng hoạt động.
- [ ] UI hiển thị trạng thái chưa đủ điều kiện kích hoạt Branch, chỉ rõ Area/Shelf còn thiếu và dẫn người dùng đến thao tác bổ sung.

### Tích hợp

- [ ] Tạo, xem, cập nhật, kích hoạt/ngừng hoạt động Branch, Area và Shelf theo quyền.
- [ ] Mã duy nhất trong phạm vi phù hợp; Shelf luôn thuộc Area và Area luôn thuộc Branch.
- [ ] Không thể kích hoạt Branch khi chưa có cấu trúc Area–Shelf active tối thiểu; khi kích hoạt thành công, cấu trúc hợp lệ được kiểm tra lại trong cùng command.
- [ ] Không thể chọn vị trí ngừng hoạt động cho Employee, BookCopy, kiểm kê hoặc phiếu nhập mới.
- [ ] Ngừng hoạt động vị trí có kiểm tra và hiển thị các đối tượng đang liên kết.
- [ ] Thay đổi cấu trúc và trạng thái được ghi AuditLog.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity configuration, migration, hierarchy query, command, validation, permission và AuditLog location.
- [ ] Bổ sung domain rule/validator cho điều kiện kích hoạt Branch và kiểm tra ảnh hưởng khi ngừng Area/Shelf cuối cùng của Branch active.

### Frontend

- [ ] Hoàn thiện route, tree/table, form, dependent picker, API hooks và trạng thái conflict/forbidden.
- [ ] Bổ sung readiness indicator và thông báo nguyên nhân không thể kích hoạt Branch.

### Tích hợp

- [ ] API phân cấp và query dùng cho combobox được hoàn thiện.
- [ ] Trang `/branches` hỗ trợ điều hướng Branch–Area–Shelf.
- [ ] Form vị trí xử lý loading, validation, conflict và permission.
- [ ] Cập nhật các feature dùng location sang cùng một contract.

## Happy-case test

1. Tạo Branch, Area và Shelf theo đúng thứ tự.
2. Kích hoạt Branch sau khi Area và Shelf đã active.
3. Gán Employee và BookCopy vào vị trí mới.
4. Tra cứu cây vị trí và xác nhận dữ liệu liên kết chính xác.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
