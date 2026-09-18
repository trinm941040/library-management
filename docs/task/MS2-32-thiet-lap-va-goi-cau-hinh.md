# MS2-32 — Thiết lập hệ thống và gói cấu hình

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P1
- Phụ thuộc: MS2-18, MS2-30

## Mô tả chi tiết

Hoàn thiện SystemSetting và ConfigurationPackage cho tham số hệ thống, cấu hình thông báo và chính sách vận hành. Hỗ trợ export package có version/checksum và import theo quy trình upload, validate, preview diff, xác nhận và audit.

## Acceptance criteria

### Backend

- [x] Setting/configuration service enforce typed schema, secret handling, checksum/version và import transaction có preview diff.

### Frontend

- [x] Settings UI render control theo value type, che secret, hiển thị import diff và yêu cầu confirmation/permission trước apply.

### Tích hợp

- [x] Setting có key duy nhất, value type, schema validation, scope và metadata người cập nhật.
- [x] Secret không được lưu hoặc export như setting thường; giá trị nhạy cảm được che trên UI/log.
- [x] Export tạo package có version, checksum và chỉ chứa nhóm cấu hình được phép.
- [x] Import từ chối file sai schema/checksum/version, không thay đổi dữ liệu trước confirm.
- [x] Import confirm áp dụng transaction, ghi before/after và AuditLog đầy đủ.

## Checklist hoàn thành

### Backend

- [x] Hoàn thiện model/configuration, schema registry, export/validate/import handlers, file security, permission và AuditLog.

### Frontend

- [x] Hoàn thiện API schema/hooks, typed setting form, package upload/export, diff viewer và import result.

### Tích hợp

- [x] API setting list/update và configuration export/validate/import.
- [x] Route `/settings` và `/configuration` theo permission.
- [x] UI preview diff nêu rõ add/change/remove và cảnh báo tác động.
- [x] Có cơ chế khôi phục cấu hình local từ package đã export.

## Happy-case test

1. Cập nhật một setting hợp lệ.
2. Export configuration package.
3. Validate và import package vào database local khác, xác nhận giá trị và audit đúng.

## Build test local

- [x] `dotnet build` thành công.
- [x] `pnpm build` thành công.
