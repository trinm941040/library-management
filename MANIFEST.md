# Manifest

## Nguồn dữ liệu

- Requirements: `../requirements/milestone-phase-2`
- Quy tắc dùng chung: `./AGENTS.md`

## Trạng thái

- Đã hoàn thành: MS2-00, MS2-01, MS2-02, MS2-03, MS2-04, MS2-05, MS2-06, MS2-07, MS2-08, MS2-18, MS2-19, MS2-20, MS2-21, MS2-22, MS2-23, MS2-24, MS2-30, MS2-32, MS2-33, MS2-36.
- Hoàn thành một phần:
  - MS2-37 (đã có typed SMTP settings, bảo vệ secret, adapter SMTP, test connection/send, outbox retry/idempotency, migration và UI cấu hình/template/history; chưa tích hợp tự động đủ 9 sự kiện nghiệp vụ, scheduler nhắc hạn, plain-text template riêng, bộ lọc history theo event/recipient và chưa chạy happy case với SMTP sandbox).
  - MS2-38 (đã hoàn thiện schema/migration, API cá nhân và gửi theo role/permission/chi nhánh, ownership, idempotency, deep-link allow-list, loại SMS khỏi luồng mới, notification center, chuông/badge, query cache và happy case create/list/detail/mark-read; chưa chốt chính sách retention tự động, chưa chạy kiểm tra chéo bằng tài khoản thứ hai và không chạy mark-all trên dữ liệu dùng chung để tránh đổi trạng thái thông báo thật).
- Code và acceptance criteria là bằng chứng quyết định; trạng thái trên chỉ là dữ liệu khởi tạo.

## Quy ước

- Mỗi prompt tối ưu có cùng tên cơ sở với requirement và prompt gốc.
- Chỉ cập nhật trạng thái khi có đường dẫn code hoặc kết quả build/happy case làm bằng chứng.
