# MS2-35 — Kiểm thử tích hợp local và hoàn thiện phát hành

- Phân loại: Full-stack / QA / Release
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01 đến MS2-34

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Chạy kiểm thử tích hợp local cho các chuỗi nghiệp vụ quan trọng, sửa lỗi chặn phát hành và hoàn thiện tài liệu vận hành/build. Không bắt buộc bổ sung unit test hoặc chạy Docker; bắt buộc build hai phía và kiểm tra happy case xuyên frontend–backend với database local.

## Acceptance criteria

### Backend

- [ ] Solution backend build sạch bằng `dotnet build`; không chạy `dotnet test`.

### Frontend

- [ ] Frontend build/type-check/lint sạch; route, form, table, keyboard flow, responsive state và error handling chính đạt.
- [ ] Ma trận role/permission được kiểm tra trên sidebar, mobile menu, route, tab, button, form field/section, bulk action, export và API tương ứng.

### Tích hợp

- [ ] Backend và frontend build sạch bằng lệnh chuẩn của repository.
- [ ] Migration tạo được database local mới và nâng cấp được database có dữ liệu mẫu.
- [ ] Các hành trình login, catalog, nhập kho, thành viên, checkout, return, renewal, reservation, payment, inventory, report và permission đều qua happy case.
- [ ] Có kiểm tra ít nhất các lỗi authorization, validation và concurrency quan trọng; không còn lỗi P0/P1 mở.
- [ ] README mô tả setup local, biến môi trường, migration, tài khoản mẫu, lệnh build và lệnh chạy kiểm tra.
- [ ] Backlog Phase 2 được cập nhật trạng thái và liên kết bằng chứng nghiệm thu.

## Checklist hoàn thành

### Backend

- [ ] Chạy build, migration, seed, API happy cases và kiểm tra log/AuditLog trên local.

### Frontend

- [ ] Chạy install/build, route smoke, critical workflows, accessibility keyboard và responsive checks trên local.

### Tích hợp

- [ ] Chuẩn bị dữ liệu mẫu có nhiều role, Branch, BookCopy, Member và policy.
- [ ] Kiểm tra trực tiếp các happy case theo thứ tự nghiệp vụ trên môi trường local và lưu kết quả.
- [ ] Kiểm tra quyền route/action/API bằng tối thiểu admin, manager và staff giới hạn quyền.
- [ ] Với mỗi role mẫu, ghi nhận menu/UI được phép hiển thị; xác nhận item ngoài quyền bị ẩn hoặc read-only/disabled đúng quy ước và URL/API trực tiếp trả forbidden.
- [ ] Kiểm tra responsive desktop/tablet, keyboard flow và trạng thái lỗi chính.
- [ ] Ghi nhận remaining risk không chặn phát hành với owner và mức ưu tiên.

## Happy-case test

1. Đăng nhập, tạo hoặc chọn Book, nhập kho để tạo BookCopy.
2. Tạo Member/thẻ, checkout, renewal, return và thanh toán khoản phạt nếu phát sinh.
3. Kiểm kê bản sao, xem dashboard/report/audit và xác nhận dữ liệu xuyên module nhất quán.

## Build test local

- [ ] `dotnet clean` và `dotnet build` thành công.
- [ ] `pnpm install --frozen-lockfile` và `pnpm build` thành công.
- [ ] Không chạy `dotnet test`; frontend integration/E2E script chỉ chạy khi task yêu cầu hoặc cần xác minh giao diện.
