# MS2-38 — Hoàn thiện thông báo trong ứng dụng và loại bỏ SMS

- Phân loại: Full-stack / Feature / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: MS2-03, MS2-04, MS2-05, MS2-09, MS2-30, MS2-31

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; hạng mục đã đáp ứng acceptance criteria thì bỏ qua, không làm lại hoặc refactor ngoài phạm vi.

Hoàn thiện kênh thông báo trong ứng dụng cho tài khoản nhân viên đã đăng nhập. Bổ sung notification center, chuông thông báo trên AppHeader, số lượng chưa đọc, danh sách/thông tin chi tiết, đánh dấu đã đọc và đánh dấu tất cả đã đọc. Thông báo phải điều hướng đến đúng màn hình nghiệp vụ khi người nhận có quyền.

Loại bỏ SMS khỏi phạm vi sản phẩm: không cho tạo template, cấu hình, gửi, retry hoặc chọn kênh SMS. Email của MS2-37 và thông báo trong ứng dụng là hai kênh còn được hỗ trợ. Dữ liệu SMS lịch sử nếu đã tồn tại phải được giữ để truy vết nhưng không được gửi lại.

## Phạm vi thông báo trong ứng dụng

- Người nhận là `ApplicationUser`/tài khoản nhân viên; Member không có tài khoản đăng nhập nên không nhận thông báo trong app.
- Hỗ trợ thông báo cá nhân và thông báo theo role/permission/chi nhánh trong phạm vi người gửi.
- Mỗi thông báo có recipient, title, body, severity, event code, thời điểm tạo, trạng thái đọc, `ReadAtUtc` và deep link tùy chọn.
- Kênh in-app không dùng trạng thái gửi SMTP; thông báo được coi là khả dụng sau khi record đã commit.
- Cập nhật unread count gần thời gian thực bằng cơ chế hiện có; nếu chưa có realtime infrastructure thì dùng polling có giới hạn và refetch khi focus.

## Acceptance criteria

### Backend

- [ ] Tái sử dụng `NotificationTemplate` và `Notification` hiện có; không tạo model thông báo song song.
- [ ] Kênh in-app lưu recipient account, title, body snapshot, severity, event code, deep link, `CreatedAtUtc`, `ReadAtUtc` và metadata cần thiết.
- [ ] Có API phân trang lấy thông báo của người dùng hiện tại, lọc theo trạng thái đọc, severity và khoảng thời gian.
- [ ] Có API unread count, xem chi tiết, `MarkRead` và `MarkAllRead`; các command đánh dấu đã đọc có tính idempotent.
- [ ] Người dùng chỉ đọc/cập nhật thông báo của chính mình; tài khoản quản trị chỉ truy cập ngoài phạm vi khi có permission riêng.
- [ ] Gửi theo role/permission/chi nhánh phải resolve danh sách recipient hiệu lực tại backend, loại bỏ account inactive, trùng lặp và recipient ngoài phạm vi.
- [ ] Deep link chỉ dùng route nội bộ trong allow-list; không chấp nhận URL tùy ý có thể gây open redirect.
- [ ] Việc tạo thông báo theo sự kiện có idempotency key, không tạo trùng cùng event–recipient khi retry.
- [ ] SMS adapter/provider, SMS setting, SMS send/retry command và SMS option trong contract/registry bị loại bỏ hoặc vô hiệu hóa hoàn toàn.
- [ ] Bản ghi SMS lịch sử được giữ read-only nếu đã có dữ liệu; retry SMS luôn bị từ chối bằng lỗi nghiệp vụ rõ ràng.
- [ ] API dùng permission policy, Problem Details, cancellation token, UTC time, server paging và AuditLog cho thao tác quản trị/gửi hàng loạt.

### Frontend

- [ ] AppHeader có nút chuông với accessible name và badge unread count; `99+` được dùng khi số lượng vượt giới hạn hiển thị.
- [ ] Notification dropdown hiển thị danh sách gần đây, phân biệt đã đọc/chưa đọc, severity, thời gian và có action xem tất cả.
- [ ] Dropdown mở/đóng và điều hướng được bằng bàn phím; focus, accessible name, screen-reader announcement và mobile layout đúng quy ước shared UI.
- [ ] Route `/notifications` hiển thị danh sách phân trang, filter unread/all, severity, thời gian, detail và action đánh dấu một/tất cả đã đọc.
- [ ] Mở thông báo đánh dấu đã đọc, cập nhật badge/cache không cần reload và chỉ điều hướng deep link khi route hợp lệ, người dùng có permission.
- [ ] Khi không có quyền truy cập deep link, UI giữ thông báo đã đọc và hiển thị forbidden/message phù hợp, không làm lộ dữ liệu đích.
- [ ] Unread count được làm mới sau login/session restore, mark-read, mark-all-read, focus lại ứng dụng và theo realtime/polling strategy đã chọn.
- [ ] Loại bỏ SMS khỏi template form, channel picker, filter, menu, route, validation schema và nội dung hướng dẫn.
- [ ] Mọi màn hình có loading, empty, error/retry, forbidden và responsive state; mutation chặn submit lặp.

### Tích hợp

- [ ] Tạo/gửi thông báo in-app bằng template active thành công và nội dung snapshot không thay đổi khi template được sửa sau đó.
- [ ] Chuông, dropdown, trang danh sách và unread badge dùng cùng API/query keys, không duy trì nhiều nguồn trạng thái thông báo.
- [ ] Thông báo chỉ xuất hiện cho đúng recipient; API trực tiếp không cho đọc hoặc mark-read thông báo của tài khoản khác.
- [ ] Thay đổi role/permission/chi nhánh được phản ánh khi resolve recipient và khi mở deep link.
- [ ] Không còn luồng tạo/gửi/retry SMS từ UI, API, worker hoặc scheduler; email và in-app tiếp tục hoạt động độc lập.
- [ ] Migration/seed nếu có không làm mất Notification/AuditLog lịch sử và không tạo lại SMS template.

## Checklist hoàn thành

### Backend

- [ ] Rà soát implementation MS2-31 và chỉ bổ sung phần in-app còn thiếu.
- [ ] Hoàn thiện entity/configuration/migration/index cho recipient, unread query, event idempotency và `ReadAtUtc` khi schema hiện có chưa đủ.
- [ ] Hoàn thiện query danh sách/detail/unread count và command mark-read/mark-all-read.
- [ ] Hoàn thiện recipient resolver theo account, role, permission và Branch.
- [ ] Bổ sung deep-link allow-list, permission validation, idempotency và retention rule.
- [ ] Xóa/vô hiệu hóa SMS adapter, provider registration, setting, command, worker path, seed và public contract.
- [ ] Giữ dữ liệu SMS cũ read-only nếu đã tồn tại; không xóa lịch sử không thể khôi phục.
- [ ] Rà soát authorization, concurrency, paging, cancellation token, AuditLog và OpenAPI.

### Frontend

- [ ] Hoàn thiện API schema, query keys, hooks và cache invalidation cho in-app notification.
- [ ] Hoàn thiện notification bell, unread badge và dropdown trong AppHeader.
- [ ] Hoàn thiện route `/notifications`, danh sách, detail, filter và mark-read/mark-all-read.
- [ ] Tích hợp realtime hoặc polling/refetch strategy phù hợp với kiến trúc hiện có.
- [ ] Bảo vệ route/action/deep link theo permission hiệu lực.
- [ ] Xóa SMS khỏi channel picker, template UI, filter, navigation, schema và text hiển thị.
- [ ] Kiểm tra keyboard, screen reader, responsive, dark/light theme và các state chuẩn.

### Tích hợp

- [ ] Kiểm tra schema/DTO/Zod model thống nhất cho `InApp` và `Email`; SMS không còn là channel có thể chọn.
- [ ] Kiểm tra notification center sau login, refresh session và logout; không dùng cache của tài khoản trước.
- [ ] Kiểm tra recipient ownership và permission bằng UI lẫn API trực tiếp.
- [ ] Kiểm tra email của MS2-37 không bị ảnh hưởng khi loại SMS.
- [ ] Cập nhật tài liệu API, channel registry và seed/configuration liên quan.

## Happy-case test

1. Đăng nhập bằng tài khoản nhân viên active và xác nhận chuông hiển thị unread count đúng.
2. Tạo một thông báo in-app cho tài khoản; xác nhận dropdown và `/notifications` hiển thị cùng nội dung.
3. Mở thông báo có deep link hợp lệ; xác nhận `ReadAtUtc` được ghi, badge giảm và điều hướng đúng route.
4. Chọn đánh dấu tất cả đã đọc; xác nhận unread count bằng 0 mà không reload trang.
5. Đăng nhập bằng tài khoản khác; xác nhận không thấy và không thể truy cập thông báo của tài khoản trước.
6. Tạo template/thông báo mới; xác nhận chỉ có kênh `InApp` và `Email`, không có SMS trong UI/API.
7. Xác nhận bản ghi SMS lịch sử nếu có vẫn tra cứu được nhưng không thể retry; email và in-app vẫn hoạt động.

## Build test local

- [ ] `dotnet build` thành công và không có lỗi.
- [ ] Frontend type-check/lint thành công theo script hiện có.
- [ ] `pnpm build` thành công.
- [ ] Không chạy `dotnet test`, không yêu cầu Docker và không bắt buộc bổ sung unit test.
