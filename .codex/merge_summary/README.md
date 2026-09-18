# Ghi nhớ đối chiếu `develop`

Đọc trước mỗi task merge. Đây là kinh nghiệm từ các nhánh trước, **không thay thế việc kiểm tra** `HEAD`, worktree và `origin/develop` hiện tại. Chỉ bổ sung mẫu lệch mới đã xác minh, tối đa vài dòng/task; không lưu log dài hoặc bí mật.

## Mẫu lệch lặp lại

- Các task trước có thể đã được squash vào `develop`: Git vẫn báo `add/add` hoặc conflict dù chức năng đã có. So sánh nội dung file và phần thay đổi riêng của task hiện tại; giữ implementation ở root đã vào `develop`, chỉ hòa nhập delta còn thiếu. Không merge lại nhánh task trước.
- Nếu `origin/develop` đã là ancestor của `HEAD`, không merge lại. Nếu worktree hoặc merge dở còn thay đổi, kiểm tra và bảo toàn trước khi thao tác; không reset để “dọn” conflict.
- Migration/snapshot có thể trùng thay đổi schema giữa các nhánh. Đối chiếu lịch sử migration và schema thực tế trước khi giữ cả hai; `Up`/`Down` phải an toàn và không tạo cột/index/API trùng.

## Trường hợp đã gặp

- **MS2-15 → MS2-16:** MS2-15 đã vào `develop` bằng commit `192b388`; lịch sử khác nhau tạo conflict `add/add` khi đồng bộ MS2-16. Giữ nền `develop` và chỉ đưa phần MS2-16 còn thiếu vào root.
- **MS2-19:** Sau merge, migration audit cũ `EnhanceAuditTrail` và `EnhanceAuditLogs` có thay đổi trùng `IpAddress`/index correlation trên database mới; đã chỉnh để không thêm schema hai lần. Khi đụng migration audit, kiểm tra lịch sử trước khi sửa.
- **MS2-20:** Đã merge `develop` trong `45f0288` và giải quyết luồng trả sách/phạt; phần reservation queue gắn với bản sao giữ chỗ và happy case API/UI vẫn chưa hoàn tất (xem `MANIFEST.md`). Đây là thiếu nghiệp vụ, không phải lý do merge task trước.
