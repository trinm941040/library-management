# MS2-29 — Báo cáo bộ lọc đã lưu và xuất tệp

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P1
- Phụ thuộc: MS2-28, MS2-33

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Xây dựng Report và SavedFilter cho các báo cáo vận hành theo loại, khoảng thời gian, chi nhánh và điều kiện nghiệp vụ. Cho phép xem trước, lưu bộ lọc cá nhân và xuất tệp ở định dạng được hỗ trợ; lưu metadata kết quả báo cáo để truy vết.

## Acceptance criteria

### Backend

- [ ] Report service validate filter/permission, tạo preview phân trang, generate file an toàn và kiểm soát ownership/lifetime khi download.

### Frontend

- [ ] Report UI có filter builder, saved filters, preview, generation progress/download và đầy đủ error/forbidden/expired-file states.

### Tích hợp

- [ ] Mỗi loại báo cáo có schema filter và quyền truy cập riêng.
- [ ] SavedFilter thuộc người dùng, có scope và criteria được validate trước khi dùng lại.
- [ ] Preview dùng phân trang hoặc aggregation, không tải toàn bộ dữ liệu vào frontend.
- [ ] Export dùng đúng filter/sort hiện tại, tên cột rõ ràng, timezone/định dạng số nhất quán.
- [ ] Report lưu người tạo, thời gian, phạm vi và thông tin file; file chỉ tải được khi có quyền.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện Report/SavedFilter model, definition registry, query/generate/download endpoints, file security và AuditLog.

### Frontend

- [ ] Hoàn thiện API schema/hooks, report selector, filter form, saved-filter CRUD, preview table và export action.

### Tích hợp

- [ ] API report definition/preview/generate/download và CRUD saved filter.
- [ ] Route `/reports` có filter builder, saved filters và export state.
- [ ] Kiểm soát đường dẫn/tên file và thời gian lưu file.
- [ ] Ghi AuditLog cho export dữ liệu nhạy cảm.

## Happy-case test

1. Chọn báo cáo khoản mượn theo tháng và một Branch.
2. Lưu filter, chạy preview và tạo export.
3. Tải file, xác nhận dữ liệu và cột khớp preview.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
