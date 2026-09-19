# MS2-10 — Chuẩn hóa dữ liệu biên mục

- Phân loại: Backend / Database / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã triển khai và đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Chuyển dữ liệu Book hiện có từ các trường chuỗi tác giả, thể loại và tổng số lượng sang mô hình chuẩn hóa Book–Author, Book–Category, Publisher–Book và Book–BookCopy. Bổ sung value object hoặc validator ISBN, trạng thái biểu ghi và dữ liệu mô tả ấn bản.

## Acceptance criteria

### Backend

- [ ] Một Book có nhiều Author và Category; Publisher được tái sử dụng cho nhiều Book.
- [ ] ISBN được chuẩn hóa, kiểm tra định dạng và uniqueness theo quy tắc ấn bản.
- [ ] Số lượng khả dụng được tính từ BookCopy, không phụ thuộc trường Quantity có thể sai lệch.
- [ ] Dữ liệu Book cũ được migration sang quan hệ mới và có báo cáo các bản ghi không ánh xạ được.
- [ ] Ngừng sử dụng biểu ghi không xóa lịch sử hoặc BookCopy liên quan.

## Checklist hoàn thành

### Backend

- [ ] Bổ sung entity configuration và migration dữ liệu.
- [ ] Chuẩn hóa command/query contract cho catalog.
- [ ] Cập nhật các nghiệp vụ đang dùng BookId hoặc Quantity cũ.
- [ ] Xác nhận không còn business rule phụ thuộc chuỗi Author/Category cũ.

## Happy-case test

1. Migration một Book cũ có tác giả và thể loại.
2. Gắn thêm tác giả, category và publisher đã tồn tại.
3. Truy vấn Book trả đủ dữ liệu chuẩn hóa và số bản sao thực tế.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] Migration áp dụng thành công trên database local có dữ liệu mẫu.
