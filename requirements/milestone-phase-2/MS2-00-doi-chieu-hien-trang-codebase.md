# MS2-00 — Đối chiếu hiện trạng codebase và chốt phạm vi

- Phân loại: Full-stack / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: Không

## Mô tả chi tiết

Đọc toàn bộ source frontend và backend, đối chiếu từng route, API, entity, migration và màn hình với 9 sơ đồ use case, kiến trúc hệ thống và sơ đồ lớp. Lập ma trận `Đã hoàn thành / Hoàn thành một phần / Chưa có / Cần refactor`; ghi rõ bằng chứng bằng đường dẫn file và loại bỏ khỏi backlog những hạng mục đã đạt yêu cầu.

## Acceptance criteria

### Backend

- [ ] Có inventory endpoint, handler, entity, migration, permission và integration đang tồn tại kèm đường dẫn code.

### Frontend

- [ ] Có inventory route, page, feature, API client, schema, state và component đang tồn tại kèm đường dẫn code.

### Tích hợp

- [ ] Có ma trận truy vết từ từng use case đến frontend route, API endpoint, application handler, domain entity và bảng dữ liệu liên quan.
- [ ] Mọi chức năng hiện có được đánh dấu trạng thái và có đường dẫn code làm bằng chứng.
- [ ] Các khác biệt giữa model hiện tại và sơ đồ lớp đích được liệt kê, gồm dữ liệu cần migration.
- [ ] README backlog được cập nhật thứ tự thực hiện, phụ thuộc và trạng thái chính xác.

## Checklist hoàn thành

### Backend

- [ ] Ghi nhận trạng thái build, migration, API contract, transaction, audit và permission của từng module.

### Frontend

- [ ] Ghi nhận trạng thái build, route, feature, form, table, API integration và permission guard.

### Tích hợp

- [ ] Rà soát cấu hình, dependency, script và convention của cả hai repository.
- [ ] Rà soát authentication, authorization, validation, error handling, audit và concurrency.
- [ ] Rà soát tất cả trang, feature, API client, state và route guard frontend.
- [ ] Chốt danh sách endpoint và contract dùng chung cho Phase 2.

## Happy-case test

1. Chọn một use case đã có, lần theo đầy đủ từ route frontend đến dữ liệu được lưu ở backend.
2. Chọn một use case chưa có, xác nhận không tồn tại implementation tương đương và giữ lại task tương ứng.

## Build test local

- [ ] Backend hiện tại build thành công bằng `dotnet build`.
- [ ] Frontend hiện tại cài dependency và build thành công bằng `pnpm build`.
