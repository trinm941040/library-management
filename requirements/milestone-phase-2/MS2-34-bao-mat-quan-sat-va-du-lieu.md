# MS2-34 — Bảo mật quan sát hệ thống và bảo vệ dữ liệu

- Phân loại: Backend + Frontend / Improvement / Security
- Mức ưu tiên: P0
- Phụ thuộc: MS2-02, MS2-05, MS2-09, MS2-30

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện hardening, observability và khả năng khôi phục cho bản phát hành: HTTPS assumptions, security headers, validation biên, authorization toàn bộ endpoint, log có cấu trúc, correlation ID, health check, che PII/secret, giới hạn upload/export, backup/restore dữ liệu và dependency audit. Không đưa credential, token hoặc dữ liệu cá nhân nhạy cảm vào log/client storage.

## Acceptance criteria

### Backend

- [ ] Middleware/endpoint enforce authentication, permission, validation, upload/download safety, structured logging, health checks và secret/PII redaction.
- [ ] Có cơ chế backup nhất quán cho database và vùng file nội bộ, kèm metadata thời điểm/phiên bản/checksum; backup không chứa secret ở dạng rõ.
- [ ] Có quy trình restore có kiểm soát, kiểm tra checksum/schema và xác minh quan hệ giữa database với file sau khôi phục.

### Frontend

- [ ] Frontend enforce safe storage/redirect/rendering, permission-aware UI, PII masking và không ghi dữ liệu nhạy cảm ra console/client log.

### Tích hợp

- [ ] Mọi endpoint ngoài login/health công khai đều có authentication và permission phù hợp.
- [ ] Response/log không chứa password hash, refresh token, token hash, security stamp hoặc secret.
- [ ] Có global exception handling, correlation ID xuyên request và structured log mức phù hợp.
- [ ] Upload kiểm tra size, extension, content/schema và tên file; download chống path traversal.
- [ ] Health check phản ánh API/database/file storage cần thiết mà không lộ cấu hình nhạy cảm.
- [ ] Backup và restore chỉ dành cho tài khoản/quy trình vận hành được ủy quyền; mọi lần thực hiện được ghi log/audit phù hợp.
- [ ] Frontend không dùng unsafe HTML, open redirect hoặc lưu token nhạy cảm trái thiết kế.

## Checklist hoàn thành

### Backend

- [ ] Rà soát endpoint inventory, security headers, rate limit, exception middleware, correlation ID, health checks và dependency findings.
- [ ] Bổ sung script hoặc hướng dẫn chạy backup/restore local cho database và vùng file; không yêu cầu Docker và không chạy `dotnet test`.
- [ ] Thực hiện một lần backup/restore trên dữ liệu local mẫu và kiểm tra checksum, migration/schema, số lượng bản ghi cùng file tham chiếu.

### Frontend

- [ ] Rà soát token/storage, redirect, unsafe HTML, error boundary, PII display/export và dependency findings.

### Tích hợp

- [ ] Rà soát authorization matrix bằng endpoint inventory.
- [ ] Ghi rõ retention, vị trí lưu, quyền truy cập và quy trình xử lý backup lỗi/restore lỗi.
- [ ] Bổ sung rate/abuse protection hợp lý cho login, refresh, import và export.
- [ ] Rà soát dependency và loại bỏ package không dùng.
- [ ] Rà soát accessibility và privacy cho PII trên danh sách/chi tiết/export.

## Happy-case test

1. Gọi các endpoint được phép bằng tài khoản đúng quyền và nhận correlation ID.
2. Thực hiện login, checkout và export bình thường.
3. Kiểm tra log local có đủ trace nhưng không chứa secret hoặc PII dư thừa.
4. Tạo backup database và vùng file local, restore sang vị trí local tách biệt và xác nhận dữ liệu/file tham chiếu nhất quán.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
- [ ] Dependency audit chạy được bằng script hiện có và các finding nghiêm trọng đã được xử lý hoặc ghi nhận.
