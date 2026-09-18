# MS2-02 — Chuẩn hóa kiến trúc backend và xử lý dùng chung

- Phân loại: Backend / Architecture / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: MS2-00, MS2-01

## Mô tả chi tiết

Chuẩn hóa dependency direction `Api → Application → Domain`, để Infrastructure triển khai các port của Application. Mỗi use case có command/query, validator, handler và transaction boundary; controller chỉ xử lý HTTP. Bổ sung lỗi chuẩn, correlation ID, permission policy, audit hook, clock và unit of work dùng chung. Identity Framework được cô lập trong Infrastructure; `TodoItem` không được tham gia dependency graph của các module thư viện cốt lõi.

## Acceptance criteria

### Backend

- [ ] Domain không phụ thuộc ASP.NET Core, EF Core hoặc HTTP contract.
- [ ] Các lớp `IdentityUserRole`, `IdentityUserClaim`, `IdentityUserLogin`, `IdentityUserToken`, `IdentityUserPasskey` và `IdentityRoleClaim` chỉ được truy cập qua ASP.NET Core Identity/Infrastructure, không bị sao chép thành domain entity nghiệp vụ.
- [ ] `TodoItem` nằm trong module thử nghiệm/kỹ thuật riêng hoặc được loại bỏ an toàn khỏi production composition; module nghiệp vụ không tham chiếu `TodoItem`.
- [ ] Controller không chứa business rule và trả mã lỗi thống nhất cho validation, unauthorized, forbidden, not found, conflict và server error.
- [ ] Các giao dịch quan trọng commit dữ liệu nghiệp vụ và AuditLog nguyên tử.
- [ ] Có optimistic concurrency và trả `409 Conflict` khi dữ liệu đã bị thay đổi.
- [ ] OpenAPI mô tả đầy đủ contract, mã phản hồi và yêu cầu quyền.

## Checklist hoàn thành

### Backend

- [ ] Tách hoặc bổ sung module, handler, validator và repository còn thiếu.
- [ ] Chuẩn hóa Problem Details và correlation ID.
- [ ] Chuẩn hóa UTC time, paging contract và cancellation token.
- [ ] Loại bỏ tham chiếu ngược hoặc import xuyên module không qua public contract.
- [ ] Kiểm tra dependency graph để xác nhận Domain/Application không phụ thuộc entity Identity framework hoặc module Todo thử nghiệm.

## Happy-case test

1. Gọi một command tạo dữ liệu qua API.
2. Xác nhận handler thực thi, transaction commit, response đúng contract và AuditLog được tạo.

## Build test local

- [ ] `dotnet build` thành công và không phát sinh warning kiến trúc mới.
