# MS2-28 — Dashboard thống kê và cảnh báo vận hành

- Phân loại: Full-stack / Feature
- Mức ưu tiên: P1
- Phụ thuộc: MS2-19, MS2-20, MS2-23, MS2-26

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện dashboard tổng hợp số liệu tài liệu, bản sao, thành viên, mượn/trả, overdue, tiền phạt, reservation, kiểm kê và hoạt động gần đây. Dữ liệu được giới hạn theo chi nhánh và permission của người xem.

## Acceptance criteria

### Backend

- [ ] Dashboard read model tính KPI/cảnh báo theo time range, timezone, Branch và permission; query có index/cache strategy phù hợp.

### Frontend

- [ ] Dashboard UI có widget KPI/cảnh báo, filter phạm vi, timestamp dữ liệu, drill-down và loading/error/partial-data states.

### Tích hợp

- [ ] Các KPI có định nghĩa, khoảng thời gian, timezone và phạm vi chi nhánh rõ ràng.
- [ ] Cảnh báo hiển thị ít nhất overdue cần xử lý, reservation sắp hết hạn, bản sao mất/hỏng và chênh lệch kiểm kê.
- [ ] API tổng hợp không tải dataset không giới hạn và đáp ứng trong ngưỡng hiệu năng đã chốt.
- [ ] Widget xử lý loading, empty, error/retry và dữ liệu một phần.
- [ ] Click KPI/cảnh báo điều hướng đến danh sách đã áp dụng filter tương ứng.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện aggregate queries, DTO, permission scoping, index/measurement và error handling dashboard.

### Frontend

- [ ] Hoàn thiện API hooks/schema, widgets, filter, responsive layout, drill-down link và refresh behavior.

### Tích hợp

- [ ] Query/read model dashboard và index cần thiết.
- [ ] Route `/dashboard` responsive cho desktop/tablet.
- [ ] Bộ chọn thời gian/chi nhánh theo quyền.
- [ ] Hiển thị thời điểm dữ liệu được cập nhật.

## Happy-case test

1. Đăng nhập bằng quản lý một chi nhánh có dữ liệu mẫu.
2. Mở dashboard và xác nhận KPI khớp dữ liệu nguồn.
3. Chọn cảnh báo overdue và được chuyển đến danh sách đúng filter.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
