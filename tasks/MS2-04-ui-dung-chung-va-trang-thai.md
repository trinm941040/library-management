# MS2-04 — Hoàn thiện UI dùng chung và trạng thái màn hình

- Phân loại: Frontend / UI / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: MS2-03

## Mô tả chi tiết

Hoàn thiện bộ UI dùng chung theo Atomic Design để các feature Phase 2 tái sử dụng: DataTable, EntityForm, BarcodeInput, filter panel, pagination, modal xác nhận, toast, status badge và app shell. Mọi màn hình nghiệp vụ phải có trạng thái loading, empty, error/retry, forbidden, conflict và success.

## Acceptance criteria

### Frontend

- [ ] DataTable hỗ trợ server pagination, sort, filter, stable row id, selection và bulk action có nhãn truy cập.
- [ ] Form hiển thị lỗi field và lỗi server, chặn submit lặp và giữ dữ liệu khi lỗi có thể sửa.
- [ ] Modal, menu, tab và toast dùng bàn phím được, focus được quản lý đúng và có accessible name.
- [ ] BarcodeInput hỗ trợ máy quét như bàn phím và nhập tay.
- [ ] Component dùng semantic token, responsive và có focus-visible rõ ràng.

## Checklist hoàn thành

### Frontend

- [ ] Chuẩn hóa atoms, molecules, organisms và public export.
- [ ] Loại bỏ UI trùng lặp giữa các feature.
- [ ] Bổ sung formatter ngày giờ, tiền tệ, mã và trạng thái.
- [ ] Kiểm tra accessibility thủ công cho luồng bàn phím chính.

## Happy-case test

1. Mở một trang danh sách, lọc, sắp xếp, chuyển trang và chọn nhiều dòng.
2. Mở form, nhập dữ liệu hợp lệ, submit và nhận toast thành công.
3. Thực hiện toàn bộ thao tác bằng bàn phím.

## Build test local

- [ ] `pnpm build` thành công.
- [ ] Component showcase hoặc Storybook hiện có build thành công nếu repository có cấu hình.
