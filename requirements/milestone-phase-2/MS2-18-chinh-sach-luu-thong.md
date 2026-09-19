# MS2-18 — Chính sách lưu thông giới hạn mượn và tiền phạt

- Phân loại: Full-stack / Feature / Business Rules
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-09, MS2-17

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện CirculationPolicy, BorrowingLimit và FinePolicy theo nhóm thành viên, loại tài liệu, phạm vi chi nhánh và thời gian hiệu lực. Chính sách quyết định số lượng mượn, số ngày mượn, số lần gia hạn, thời gian giữ reservation và cách tính phạt.

## Acceptance criteria

### Backend

- [ ] Policy resolver chọn đúng version/phạm vi, validate overlap và cung cấp kết quả nhất quán cho checkout, renewal, reservation và fine.

### Frontend

- [ ] UI policy hỗ trợ list/version/form, effective-date validation, preview và permission-aware activate/deactivate actions.

### Tích hợp

- [ ] Chính sách có version hoặc khoảng hiệu lực; giao dịch lưu căn cứ chính sách đã áp dụng.
- [ ] Không tồn tại hai chính sách active chồng lấn cho cùng phạm vi nếu không có quy tắc ưu tiên rõ ràng.
- [ ] BorrowingLimit trả đúng max loan, loan days, max renewal và hold days.
- [ ] FinePolicy hỗ trợ mức theo ngày, cố định và trần tiền phạt bằng decimal precision đúng.
- [ ] Thay đổi chính sách được bảo vệ bằng permission và ghi AuditLog trước/sau.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện entity/configuration, resolver, command/query, validator, permission, cache strategy và AuditLog policy.

### Frontend

- [ ] Hoàn thiện API schema/hooks, policy editor, preview, status actions và conflict handling.

### Tích hợp

- [ ] Domain policy resolver và validation.
- [ ] API xem, tạo version, kích hoạt và ngừng chính sách.
- [ ] UI cấu hình có xem trước kết quả với dữ liệu mẫu.
- [ ] Tích hợp policy vào checkout, renewal, reservation và fine calculation.

## Happy-case test

1. Tạo policy cho một nhóm Member tại Branch.
2. Kích hoạt policy.
3. Chạy preview và xác nhận giới hạn, hạn trả, số lần gia hạn và mức phạt được tính đúng.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
