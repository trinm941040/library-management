# Manifest

## Nguồn dữ liệu

- Requirements: `../requirements/milestone-phase-2`
- Quy tắc dùng chung: `./AGENTS.md`

## Trạng thái

- Đã hoàn thành: MS2-00, MS2-01, MS2-02, MS2-03, MS2-04, MS2-05, MS2-06, MS2-07, MS2-08, MS2-17, MS2-18, MS2-30, MS2-32, MS2-33, MS2-36.
- Hoàn thành một phần: MS2-11 (đã chặn catalog/import tạo hoặc thu hồi BookCopy không có kệ; cần kiểm tra lại happy case đầy đủ và dữ liệu Quantity cũ); MS2-12 (migration Supabase, API Branch–Area–Shelf và luồng Employee đã kiểm tra; bước gán BookCopy vào Shelf và concurrency liên vị trí chưa xác nhận); MS2-13 (API/UI BookCopy, migration barcode, test tạo với Shelf và validate nguồn phiếu; chưa xác nhận happy case chuyển kệ, liên kết Borrowing–BookCopy và thay thế Book.Quantity trong lưu thông); MS2-14 (Supplier API/UI và migration trạng thái đã có ở root/Supabase; happy case ghi dữ liệu thật bị chặn); MS2-15 (API/UI phiếu nhập đã build/lint; chưa kiểm chứng happy case ghi dữ liệu thật và cảnh báo điều hướng Back của trình duyệt).
- Chưa xác nhận: MS2-09 đến MS2-35 ngoại trừ MS2-11, MS2-12, MS2-13, MS2-14, MS2-15, MS2-17, MS2-18, MS2-30, MS2-32, MS2-33.
- Code và acceptance criteria là bằng chứng quyết định; trạng thái trên chỉ là dữ liệu khởi tạo.

## Quy ước

- Mỗi prompt tối ưu có cùng tên cơ sở với requirement và prompt gốc.
- Chỉ cập nhật trạng thái khi có đường dẫn code hoặc kết quả build/happy case làm bằng chứng.
