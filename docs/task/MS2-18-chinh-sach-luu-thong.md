# MS2-18 — Chính sách lưu thông giới hạn mượn và tiền phạt

- Phân loại: Full-stack / Feature / Business Rules
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-09, MS2-17

## Mô tả chi tiết

Hoàn thiện CirculationPolicy, BorrowingLimit và FinePolicy theo nhóm thành viên, loại tài liệu, phạm vi chi nhánh và thời gian hiệu lực. Chính sách quyết định số lượng mượn, số ngày mượn, số lần gia hạn, thời gian giữ reservation và cách tính phạt.

## Acceptance criteria

### Backend

- [x] Policy resolver chọn đúng version/phạm vi, validate overlap và cung cấp kết quả nhất quán cho checkout, renewal, reservation và fine.

### Frontend

- [x] UI policy hỗ trợ list/version/form, effective-date validation, preview và permission-aware activate/deactivate actions.

### Tích hợp

- [x] Chính sách có version hoặc khoảng hiệu lực; giao dịch lưu căn cứ chính sách đã áp dụng.
- [x] Không tồn tại hai chính sách active chồng lấn cho cùng phạm vi nếu không có quy tắc ưu tiên rõ ràng.
- [x] BorrowingLimit trả đúng max loan, loan days, max renewal và hold days.
- [x] FinePolicy hỗ trợ mức theo ngày, cố định và trần tiền phạt bằng decimal precision đúng.
- [x] Thay đổi chính sách được bảo vệ bằng permission và ghi AuditLog trước/sau.

## Checklist hoàn thành

### Backend

- [x] Hoàn thiện entity/configuration, resolver, command/query, validator, permission, cache strategy và AuditLog policy.

### Frontend

- [x] Hoàn thiện API schema/hooks, policy editor, preview, status actions và conflict handling.

### Tích hợp

- [x] Domain policy resolver và validation.
- [x] API xem, tạo version, kích hoạt và ngừng chính sách.
- [x] UI cấu hình có xem trước kết quả với dữ liệu mẫu.
- [x] Tích hợp policy vào checkout, renewal, reservation và fine calculation.

## Happy-case test

1. Tạo policy cho một nhóm Member tại Branch.
2. Kích hoạt policy.
3. Chạy preview và xác nhận giới hạn, hạn trả, số lần gia hạn và mức phạt được tính đúng.

## Build test local

- [x] `dotnet build` thành công (0 Warning, 0 Error).
- [x] `pnpm build` thành công.
