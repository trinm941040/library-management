# MS2-32 — Thiết lập hệ thống và gói cấu hình

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P1
- Phụ thuộc: MS2-18, MS2-30

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện SystemSetting và ConfigurationPackage cho tham số hệ thống, cấu hình thông báo và chính sách vận hành. Hỗ trợ export package có version/checksum và import theo quy trình upload, validate, preview diff, xác nhận và audit.

## Acceptance criteria

### Backend

- [ ] Setting/configuration service enforce typed schema, secret handling, checksum/version và import transaction có preview diff.

### Frontend

- [ ] Settings UI render control theo value type, che secret, hiển thị import diff và yêu cầu confirmation/permission trước apply.

### Tích hợp

- [ ] Setting có key duy nhất, value type, schema validation, scope và metadata người cập nhật.
- [ ] Secret không được lưu hoặc export như setting thường; giá trị nhạy cảm được che trên UI/log.
- [ ] Export tạo package có version, checksum và chỉ chứa nhóm cấu hình được phép.
- [ ] Import từ chối file sai schema/checksum/version, không thay đổi dữ liệu trước confirm.
- [ ] Import confirm áp dụng transaction, ghi before/after và AuditLog đầy đủ.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện model/configuration, schema registry, export/validate/import handlers, file security, permission và AuditLog.

### Frontend

- [ ] Hoàn thiện API schema/hooks, typed setting form, package upload/export, diff viewer và import result.

### Tích hợp

- [ ] API setting list/update và configuration export/validate/import.
- [ ] Route `/settings` và `/configuration` theo permission.
- [ ] UI preview diff nêu rõ add/change/remove và cảnh báo tác động.
- [ ] Có cơ chế khôi phục cấu hình local từ package đã export.

## Happy-case test

1. Cập nhật một setting hợp lệ.
2. Export configuration package.
3. Validate và import package vào database local khác, xác nhận giá trị và audit đúng.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
