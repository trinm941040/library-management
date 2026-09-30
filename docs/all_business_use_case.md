# Toàn bộ Business Use Case — Library Management

> Tài liệu được đối chiếu với source code runtime hiện tại của project `Library_Management`, không dựa vào README cũ.

## 1. Phạm vi

Tài liệu liệt kê các use case nghiệp vụ đang chạy trên toàn ứng dụng, gồm:

- authentication, hồ sơ cá nhân và session;
- tài khoản truy cập, user, role, permission và nhân viên;
- sách, bản sao, vị trí, nhà cung cấp, nhập kho và kiểm kê;
- độc giả, thẻ, hạn chế, mượn–trả–gia hạn, đặt chỗ, vi phạm và tiền phạt;
- audit log, dashboard;
- gửi/nhận thông báo và realtime;
- báo cáo, bộ lọc cá nhân và kiosk công khai.

Theo yêu cầu “trừ các nghiệp vụ cấu hình”, tài liệu **không liệt kê**:

- quản lý Circulation Policy;
- System Settings;
- export/import Configuration Package;
- đọc/kiểm thử SMTP settings;
- CRUD Notification Template và danh sách event-template;
- `TodosController`, vì class mang `[NonController]` và không được expose lúc runtime.

Các rule của circulation policy vẫn được nhắc trong mượn, gia hạn, đặt chỗ và tính phạt vì đó là rule được nghiệp vụ tiêu thụ, không phải use case cấu hình policy.

## 2. Quy ước

Mỗi use case có ba loại flow:

- **Normal (N):** luồng thành công chính.
- **Alternative (A):** biến thể hợp lệ, hệ thống vẫn trả kết quả nghiệp vụ có kiểm soát; có thể là danh sách rỗng, no-op idempotent, partial success hoặc kết quả “không đủ điều kiện” dùng để preview.
- **Exception (E):** use case không hoàn tất do xác thực, phân quyền, dữ liệu, trạng thái, tài nguyên ngoài hoặc lỗi hệ thống.

### 2.1 Các exception dùng chung

| Mã | Exception flow dùng chung |
|---|---|
| `E-C01` | Không có, sai, hết hạn JWT; `sub`/`sid` sai; account, employee hoặc session family không hoạt động → `401`. |
| `E-C02` | Đã xác thực nhưng thiếu permission, sai branch hoặc vi phạm ownership → `403`. |
| `E-C03` | JSON/form/query sai định dạng hoặc dữ liệu vi phạm validation → `400` hoặc `422`. |
| `E-C04` | Entity không tồn tại hoặc bị ẩn bởi ownership filter → `404`. |
| `E-C05` | Trùng unique value, trạng thái không cho phép, stale concurrency token hoặc dependency conflict → `409`. |
| `E-C06` | Vượt rate limit → `429`. |
| `E-C07` | Embedding/external provider không khả dụng → `503`. |
| `E-C08` | Lỗi không được xử lý riêng → `500` với Problem Details và correlation ID. |

### 2.2 Luồng chung của use case được bảo vệ

```text
Actor gửi request
  -> middleware correlation/rate limit
  -> xác thực JWT + kiểm tra session hiện hành
  -> authorization policy đọc permission hiện hành từ database
  -> model binding/validation
  -> Controller map request sang command/query
  -> Application/Identity Service
  -> Repository/UserManager/EF Core
  -> Domain rule
  -> Unit of Work/SaveChanges/transaction
  -> audit interceptor hoặc explicit audit
  -> response hoặc Problem Details
```

Các use case protected dưới đây mặc định có `E-C01`, `E-C02` khi endpoint yêu cầu permission, `E-C03` và `E-C08`; bảng tập trung ghi thêm exception đặc thù.

## 3. Actors

| Actor | Trách nhiệm chính |
|---|---|
| Người dùng ẩn danh | Đăng nhập, refresh/logout bằng cookie, tra cứu kiosk. |
| Nhân viên đã đăng nhập | Xem hồ sơ, inbox, dashboard và chức năng được cấp quyền. |
| Thủ thư/nhân viên lưu thông | Mượn, trả, gia hạn, đặt chỗ, độc giả, vi phạm và thanh toán. |
| Nhân viên kho | Bản sao, vị trí, nhà cung cấp, phiếu nhập và kiểm kê. |
| Quản trị viên | Tài khoản, role/permission, nhân viên, thông báo, báo cáo và phạm vi toàn chi nhánh. |
| Background worker | Lấy email pending, gửi SMTP, retry và cập nhật trạng thái. |
| SignalR client | Kết nối theo user group và nhận notification realtime. |

## 4. Authentication, session và hồ sơ cá nhân

### UC-AUTH-01 — Đăng nhập

**Actor:** Người dùng ẩn danh.  
**Endpoint:** `POST /api/v1/auth/login`.

- **N:** Client gửi email/password và browser headers hợp lệ → Identity kiểm tra Argon2id, account/employee/lockout → tải role/permission → khóa transaction theo user → tạo refresh-token family và audit → ký RS256 access token → ghi refresh token vào HttpOnly cookie → `200 SessionResponse`.
- **A1:** Password đang ở legacy Identity format nhưng hợp lệ → hasher trả `SuccessRehashNeeded`, password có thể được nâng cấp sang Argon2id và đăng nhập tiếp tục.
- **A2:** User có nhiều role → effective permissions được hợp nhất rồi đưa vào response/token.
- **E:** Sai credential, email chưa xác nhận khi bắt buộc, account/employee inactive hoặc bị lockout → generic `401`; browser guard/cross-site request sai → `403`; quá 10 request/IP/phút → `429`; lỗi transaction → `E-C08`.

### UC-AUTH-02 — Refresh access token

**Actor:** Browser có refresh cookie.  
**Endpoint:** `POST /api/v1/auth/refresh`.

- **N:** Hash raw cookie → tìm session → advisory lock theo user → kiểm tra account/employee/family → đánh dấu token cũ đã dùng/revoked → tạo token con cùng family và hạn tuyệt đối → ký access token mới → thay cookie → `200 SessionResponse`.
- **A1:** Các tab browser dùng `navigator.locks` để tuần tự hóa rotation cookie.
- **A2:** Rotation giữ nguyên absolute expiry 30 ngày, không kéo dài session family.
- **E:** Thiếu/không biết/hết hạn cookie → `401`; token cũ bị dùng lại → revoke toàn family, ghi audit reuse và trả `401`; account/employee/session inactive → `401`; rate limit → `429`; concurrency/database error → `409/500`.

### UC-AUTH-03 — Đăng xuất session hiện tại

**Actor:** Người dùng có refresh cookie.  
**Endpoint:** `POST /api/v1/auth/logout`; alias `POST /api/v1/auth/revoke`.

- **N:** Tìm token → revoke toàn family với reason `logout` → ghi audit → xóa cookie → `204`.
- **A:** Cookie thiếu hoặc không còn khớp session → xử lý idempotent, vẫn xóa cookie và trả `204`.
- **E:** Browser guard không hợp lệ → `403`; lỗi database → `E-C08`.

### UC-AUTH-04 — Đăng xuất tất cả thiết bị

**Actor:** Người dùng đã đăng nhập.  
**Endpoint:** `POST /api/v1/auth/logout-all`.

- **N:** Xác định UserId → revoke mọi refresh session đang active → audit → xóa cookie hiện tại → `204`.
- **A:** Không còn session active khác → thao tác vẫn thành công/idempotent.
- **E:** `E-C01`; browser guard sai → `403`; lỗi transaction → `E-C08`.

### UC-AUTH-05 — Xem session/hồ sơ hiện tại

**Actor:** Người dùng có session.  
**Endpoint:** `GET /api/v1/auth/me`, `GET /api/v1/auth/current-session`, `GET /api/v1/me`.

- **N:** Đọc user, employee, branch, role và effective permission → `200 CurrentProfileResponse`.
- **A:** User không có branch hoặc là administrator → profile biểu diễn phạm vi global.
- **E:** Token/principal sai hoặc account/employee không khả dụng → `401`; profile không tồn tại → `401/404` tùy route.

### UC-AUTH-06 — Cập nhật hồ sơ cá nhân

**Actor:** Người dùng đã đăng nhập.  
**Endpoint:** `PATCH /api/v1/me/profile`.

- **N:** Đọc profile → kiểm tra concurrency token và ngày sinh → cập nhật employee personal fields cùng display name → audit → `200 CurrentProfileResponse`.
- **A:** Field optional không được cung cấp → giữ giá trị cũ; giá trị trắng hợp lệ theo contract có thể được normalize về null.
- **E:** Ngày sinh tương lai/input sai → `422`; stale token → `409`; profile không tồn tại → `404`; `E-C01`.

### UC-AUTH-07 — Đổi mật khẩu

**Actor:** Người dùng đã đăng nhập.  
**Endpoint:** `POST /api/v1/me/change-password`.

- **N:** Kiểm tra mật khẩu hiện tại → validate mật khẩu mới → lưu Argon2id hash → revoke toàn bộ session với reason `password-changed` → audit/xóa cookie → `204`; người dùng phải đăng nhập lại.
- **A:** Password policy trả nhiều lỗi → response tổng hợp lỗi validation.
- **E:** Mật khẩu hiện tại sai hoặc mật khẩu mới không đạt policy → `422`; `E-C01`; conflict/database error → `409/500`.

## 5. Quản lý tài khoản truy cập

### UC-ACC-01 — Tra cứu danh sách tài khoản

- **N:** Quản trị viên lọc/phân trang user, join employee và role → `200 AccessAccountPageResponse`.
- **A:** Không có kết quả → page rỗng với `TotalCount=0`; filter/search/sort hợp lệ thay đổi tập kết quả.
- **E:** Paging/filter sai → `400/422`; `E-C01/E-C02(users.read)`.

### UC-ACC-02 — Tra cứu nhân viên đủ điều kiện tạo tài khoản

- **N:** Tìm employee active chưa liên kết user → `200` collection.
- **A:** Tất cả nhân viên đã có tài khoản → collection rỗng.
- **E:** `E-C01/E-C02(users.create)`.

### UC-ACC-03 — Xem chi tiết tài khoản

- **N:** Đọc user, employee liên kết và roles → `200`.
- **A:** Tài khoản hợp lệ nhưng chưa có employee/role optional → trả summary tương ứng.
- **E:** Không tồn tại → `404`; `E-C01/E-C02(users.read)`.

### UC-ACC-04 — Tạo tài khoản cho nhân viên

- **N:** Kiểm tra employee active/chưa liên kết, email unique, password và active roles → tạo Identity user → gán roles → link employee → audit → `201`.
- **A:** Role list optional/rỗng nếu contract và rule cho phép → account được tạo chưa có quyền nghiệp vụ.
- **E:** Employee/role không tồn tại → `404`; employee đã link hoặc email trùng → `409`; password/role set/input sai → `422`; `E-C01/E-C02(users.create)`.

### UC-ACC-05 — Kích hoạt hoặc vô hiệu hóa tài khoản

- **N:** Đổi `users.IsActive` → nếu disable thì revoke tất cả session → audit → `200`.
- **A:** Yêu cầu đặt lại đúng trạng thái hiện tại có thể trở thành no-op theo service nhưng vẫn trả trạng thái hiện hành.
- **E:** Không tồn tại → `404`; tự khóa, protected account hoặc transition không hợp lệ → `409`; input sai → `422`; thiếu `users.deactivate` → `403`.

### UC-ACC-06 — Reset mật khẩu tài khoản

- **N:** Tạo Identity reset token → đặt temporary password → revoke mọi session → audit → `204`.
- **A:** User chưa có session active → reset vẫn thành công.
- **E:** Account không tồn tại → `404`; protected/conflict → `409`; password không đạt policy → `422`; thiếu `users.update` → `403`.

### UC-ACC-07 — Xem các session của tài khoản

- **N:** Đọc refresh-token rows, tính `IsActive` từ expiry/use/revocation → `200` collection.
- **A:** Chưa từng đăng nhập → collection rỗng; session cũ vẫn được trả để audit nhưng `IsActive=false`.
- **E:** Account không tồn tại → `404`; `E-C01/E-C02(users.read)`.

### UC-ACC-08 — Thu hồi một session

- **N:** Xác minh session thuộc account → revoke toàn family → audit → `204`.
- **A:** Một family có nhiều token rotation → tất cả row trong family cùng bị vô hiệu hóa.
- **E:** Account/session không tồn tại hoặc không thuộc nhau → `404`; protected/self rule hoặc state conflict → `409`; thiếu `users.update` → `403`.

### UC-ACC-09 — Thay role của tài khoản

- **N:** Kiểm tra user và active roles → thay toàn bộ `AspNetUserRoles` bằng tập mới → audit → `204`.
- **A:** Danh sách mới giống hiện tại → diff rỗng/no-op; danh sách rỗng loại toàn bộ role nếu rule cho phép.
- **E:** User/role không tồn tại → `404`; protected account/role conflict → `409`; duplicate/inactive role hoặc set sai → `422`; thiếu `roles.assign` → `403`.

## 6. Legacy/general user API

### UC-USR-01 — Tra cứu user

- **N:** Lọc/phân trang users và roles → `200 UserPageResponse`; xem theo ID → `200 UserResponse`.
- **A:** Search không khớp → page rỗng; user không có role → role collection rỗng.
- **E:** ID không tồn tại → `404`; query sai → `400/422`; `E-C01/E-C02(users.read)`.

### UC-USR-02 — Tạo user tổng quát

- **N:** Validate email và employee optional → tạo Identity user → audit → `201`.
- **A:** Không gắn employee → tạo user độc lập nếu rule cho phép.
- **E:** Input/password sai → `400`; email hoặc employee link trùng → `409`; `E-C01/E-C02(users.create)`.

### UC-USR-03 — Cập nhật user

- **N:** Đọc user → update email/display name cùng normalized identity fields → audit → `200`.
- **A:** Chỉ một số field thay đổi → giữ field còn lại.
- **E:** `404`; tự sửa/protected/duplicate email → `409`; validation → `400`; thiếu `users.update` → `403`.

### UC-USR-04 — Vô hiệu hóa user

- **N:** Soft deactivate user → revoke session → audit → `204`.
- **A:** Không xóa vật lý nên lịch sử và quan hệ vẫn được giữ.
- **E:** `404`; tự vô hiệu hóa/protected/conflict → `409`; thiếu `users.deactivate` → `403`.

## 7. Role và permission

### UC-RBAC-01 — Tra cứu role

- **N:** Search/page roles và permission assignments; hoặc đọc một role theo ID → `200`.
- **A:** Không khớp → page rỗng; role chưa có permission → collection rỗng.
- **E:** Role ID không tồn tại → `404`; query sai → `400/422`; thiếu `roles.read` → `403`.

### UC-RBAC-02 — Tạo role

- **N:** Normalize tên → kiểm tra unique → insert role → `201`.
- **A:** Description optional có thể rỗng.
- **E:** Tên trùng → `409`; tên/input sai → `400`; thiếu `roles.create` → `403`.

### UC-RBAC-03 — Cập nhật role

- **N:** Cập nhật name/description/status → `200`.
- **A:** Chỉ status hoặc description đổi → giữ phần còn lại.
- **E:** `404`; system/protected role hoặc tên trùng → `409`; validation → `400`; thiếu `roles.update` → `403`.

### UC-RBAC-04 — Xóa role

- **N:** Xác nhận role không phải system và không còn assignment bị cấm → xóa role/relations → `204`.
- **A:** Assignment có thể được loại trong cùng transaction nếu rule cho phép.
- **E:** `404`; role system/protected/đang được sử dụng → `409`; thiếu `roles.delete` → `403`.

### UC-RBAC-05 — Thay permission của role

- **N:** Validate role và toàn bộ permission IDs → replace `role_permissions` trong transaction → `200 RoleResponse`.
- **A:** Set giống cũ → no-op; set rỗng loại quyền nếu role không protected.
- **E:** Role/permission không tồn tại → `404`; protected role/conflict → `409`; set sai/duplicate → `400`; thiếu `roles.assign` → `403`.

### UC-RBAC-06 — Thay role của user

- **N:** Validate user và active roles → tính diff add/remove → lưu assignments → audit → `204`.
- **A:** Không có diff → no-op thành công.
- **E:** User/role `404`; protected/self/conflict → `409`; validation → `400`; thiếu `roles.assign` → `403`.

### UC-RBAC-07 — Tra cứu permission catalog

- **N:** Search/page permissions, lấy module list hoặc permission theo ID → `200`.
- **A:** Search/module không khớp → list rỗng.
- **E:** Permission ID `404`; query sai → `400/422`; thiếu `permissions.read` → `403`.

### UC-RBAC-08 — Tạo permission

- **N:** Normalize name/module → kiểm tra unique → insert → `201`.
- **A:** Description optional.
- **E:** Duplicate → `409`; validation → `400`; thiếu `permissions.create` → `403`.

### UC-RBAC-09 — Cập nhật permission

- **N:** Cập nhật permission không phải system → `200`.
- **A:** Description/module optional được normalize.
- **E:** `404`; system/protected hoặc duplicate → `409`; validation → `400`; thiếu `permissions.update` → `403`.

### UC-RBAC-10 — Xóa permission

- **N:** Xác nhận permission không system/protected/in-use → xóa → `204`.
- **A:** Permission không có assignment được xóa trực tiếp.
- **E:** `404`; protected/in-use → `409`; thiếu `permissions.delete` → `403`.

## 8. Nhân viên

### UC-EMP-01 — Tra cứu nhân viên và chi nhánh chọn

- **N:** Lọc/phân trang employee cùng branch, xem chi tiết kèm linked account; lấy active branches cho picker → `200`.
- **A:** Không khớp → page rỗng; employee chưa có account → account summary null.
- **E:** Employee ID `404`; query sai → `400/422`; thiếu `employees.read` → `403`.

### UC-EMP-02 — Tạo nhân viên

- **N:** Validate code/email/branch/date và uniqueness → `Employee.Create` → lưu/audit → `201`.
- **A:** Các contact/profile field optional có thể null.
- **E:** Branch sai/inactive → `400/422`; code/email trùng → `409`; thiếu `employees.create` → `403`.

### UC-EMP-03 — Cập nhật nhân viên

- **N:** Kiểm tra concurrency → validate → update/audit → `200`.
- **A:** Cập nhật một phần dữ liệu nhưng request vẫn map sang state đầy đủ theo contract.
- **E:** `404`; stale token hoặc duplicate → `409`; input/branch sai → `400/422`; thiếu `employees.update` → `403`.

### UC-EMP-04 — Đổi trạng thái nhân viên

- **N:** Thay employment status → nếu deactivate thì disable linked account và revoke sessions → audit → `200`.
- **A:** Employee không có account → chỉ trạng thái employee thay đổi.
- **E:** `404`; tự khóa/protected/transition sai → `409`; thiếu `employees.update` → `403`.

### UC-EMP-05 — Xóa nhân viên

- **N:** Kiểm tra lifecycle/dependency → xóa → audit → `204`.
- **A:** Không có linked account/dependency thì delete trực tiếp.
- **E:** `404`; còn linked account hoặc dữ liệu nghiệp vụ/protected → `409`; thiếu `employees.delete` → `403`.

## 9. Danh mục sách

### UC-BOOK-01 — Tra cứu danh sách/chi tiết sách

- **N:** Validate search/category/page/sort → query books cùng author/category/publisher và copy counts → `200 BookPageResponse`; xem theo ID → `200 BookResponse`.
- **A:** Không khớp → page rỗng; metadata/reference optional → trả null/list rỗng; sách hết bản sẵn sàng vẫn xuất hiện với availability bằng 0.
- **E:** ID không tồn tại → `404`; filter/sort/page sai → `422`; thiếu `books.read` → `403`.

### UC-BOOK-02 — Tra cứu catalog reference

- **N:** Chọn loại `authors`, `publishers` hoặc `categories`, search active records → `200` collection.
- **A:** Search không khớp → list rỗng; không có keyword → lấy tập giới hạn mặc định.
- **E:** Reference type sai → `422`; thiếu `books.read` → `403`.

### UC-BOOK-03 — Tạo sách

- **N:** Validate ISBN/metadata → xác minh hoặc tạo normalized references → kiểm tra ISBN unique → tạo Book/junction rows/embedding state/audit trong transaction → `201`.
- **A:** Publisher optional; author/category mới có thể được normalize và tạo nếu flow cho phép.
- **E:** ISBN trùng/stale DB unique → `409`; metadata/ISBN sai → `422`; referenced ID không tồn tại → `404`; thiếu `books.create` → `403`.

### UC-BOOK-04 — Cập nhật sách

- **N:** Đọc Book → kiểm tra concurrency → validate ISBN/metadata → thay relationships → cập nhật/invalidate semantic embedding → audit → `200`.
- **A:** Reference set thay đổi một phần; metadata optional có thể được xóa.
- **E:** `404`; stale token hoặc ISBN trùng → `409`; validation → `422`; thiếu `books.update` → `403`.

### UC-BOOK-05 — Xóa mềm một sách

- **N:** Kiểm tra dependency → chuyển catalog record inactive → audit → `204`.
- **A:** Sách không có dependency được deactivate ngay; dữ liệu lịch sử vẫn được giữ.
- **E:** `404`; còn active copy/loan/reservation/dependency → `409`; thiếu `books.delete` → `403`.

### UC-BOOK-06 — Xóa hàng loạt sách

- **N:** Duyệt từng ID qua rule delete → trả `200 BulkResponse` gồm kết quả từng item.
- **A:** Partial success: sách hợp lệ bị deactivate, sách có dependency trả lỗi riêng mà không rollback toàn batch.
- **E:** Set rỗng/sai → `422`; thiếu `books.delete` → `403`; lỗi toàn hệ thống → `500`.

### UC-BOOK-07 — Export sách

- **N:** Áp dụng filter/sort → lấy tập export → encode CSV an toàn → trả file.
- **A:** Không có dữ liệu → file chỉ có header.
- **E:** Filter sai → `422`; thiếu `books.read` → `403`; lỗi stream → `500`.

### UC-BOOK-08 — Preview import sách

- **N:** Nhận CSV → kiểm tra kích thước/schema → parse/normalize ISBN và references → validate từng row không lưu DB → `200 BookImportPreviewResponse`.
- **A:** File có cả row hợp lệ và lỗi → preview đánh dấu từng row để người dùng sửa/chọn.
- **E:** File thiếu/rỗng/quá lớn/sai format → `400/422`; thiếu `books.create` → `403`.

### UC-BOOK-09 — Confirm import sách

- **N:** Revalidate accepted rows với trạng thái DB mới nhất → transaction create/update sách và relations → audit → trả import result.
- **A:** Theo contract import có thể bỏ qua row không được chọn hoặc báo kết quả theo row.
- **E:** Dữ liệu thay đổi/ISBN duplicate từ sau preview → `409`; row không hợp lệ → `422`; lỗi transaction rollback toàn phần → `500`.

### UC-BOOK-10 — Semantic search sách nội bộ

- **N:** Validate query/topK/filter → gọi embedding provider → pgvector cosine search → map catalog/availability → `200 SemanticBookSearchResponse`.
- **A:** Không có match → list rỗng; category/available-only giới hạn kết quả.
- **E:** Query/topK sai → `422`; provider/API key lỗi → `503`; rate limit → `429`; thiếu `books.read` → `403`.

### UC-BOOK-11 — Backfill semantic embeddings

- **N:** Chọn batch sách chưa có embedding → build searchable text → gọi provider → upsert vectors trong transaction → `200` số đã xử lý/còn lại.
- **A:** Không còn sách thiếu vector → `ProcessedCount=0`; batch nhỏ hơn giới hạn khi gần hoàn tất.
- **E:** Batch ngoài `1..100` → `422`; provider → `503`; thiếu `books.update` → `403`; transaction → `500`.

## 10. Bản sao sách

### UC-COPY-01 — Tra cứu bản sao

- **N:** Filter/page copies với book/location; xem theo ID hoặc normalized barcode → `200`.
- **A:** Search không khớp → page rỗng; barcode hợp lệ nhưng không có record → không có kết quả.
- **E:** ID/barcode không tồn tại → `404`; query/barcode sai → `422`; thiếu `copies.read` → `403`.

### UC-COPY-02 — Tạo bản sao

- **N:** Validate book, active shelf, normalized unique barcode, condition → tạo copy → transaction/audit → `201`.
- **A:** `stockReceiptItemId` optional nếu copy được nhập thủ công.
- **E:** Book/shelf `404`; shelf inactive hoặc barcode duplicate/state conflict → `409`; input → `422`; thiếu `copies.create` → `403`.

### UC-COPY-03 — Đổi trạng thái bản sao

- **N:** Kiểm tra concurrency và domain transition matrix → update status → audit → `200`.
- **A:** Các transition hợp lệ phụ thuộc trạng thái hiện tại, ví dụ Available ↔ Maintenance theo domain rule.
- **E:** `404`; đang được mượn/transition sai/stale token → `409`; thiếu `copies.update` → `403`.

### UC-COPY-04 — Chuyển vị trí bản sao

- **N:** Kiểm tra copy movable và destination shelf active → thay ShelfId → audit → `200`.
- **A:** Chuyển sang shelf khác trong cùng area hoặc sang hierarchy khác nếu đều active và có capacity.
- **E:** Copy/shelf `404`; borrowed/withdrawn, shelf inactive/full hoặc stale → `409`; thiếu `copies.update` → `403`.

### UC-COPY-05 — Cập nhật tình trạng vật lý

- **N:** Validate condition và concurrency → update → audit → `200`.
- **A:** Condition có thể làm thay đổi khả năng lưu thông tùy domain rule.
- **E:** `404`; invalid/stale → `409/422`; thiếu `copies.update` → `403`.

### UC-COPY-06 — Thanh lý bản sao

- **N:** Xác nhận không có active borrowing → chuyển trạng thái Withdrawn → audit → `200`.
- **A:** Copy damaged/lost nhưng không còn open loan vẫn có thể được withdraw theo rule.
- **E:** `404`; đang mượn/already withdrawn/stale → `409`; thiếu `copies.withdraw` → `403`.

### UC-COPY-07 — Thao tác hàng loạt bản sao

- **N:** Resolve operation → kiểm tra permission tương ứng → thực hiện theo từng ID → trả `200` danh sách kết quả item.
- **A:** Partial success; item không hợp lệ không hủy kết quả của item khác.
- **E:** Operation không biết → `400`; permission của operation → `403`; request sai → `422`.

### UC-COPY-08 — Preview import bản sao

- **N:** Parse rows → resolve barcode/book/shelf/condition không ghi DB → `200` preview.
- **A:** Row lỗi reference/duplicate được đánh dấu cạnh row hợp lệ.
- **E:** File/request sai → `422`; thiếu `copies.create` → `403`.

### UC-COPY-09 — Confirm import bản sao

- **N:** Re-resolve accepted rows → transactional insert copies → audit → `200` created models.
- **A:** Rows không được chọn bị bỏ qua nếu request hỗ trợ selection.
- **E:** Barcode trùng/reference thay đổi/current-state conflict → `409/422`; transaction rollback → `500`.

### UC-COPY-10 — Export bản sao

- **N:** Filter/sort → query copies → trả CSV.
- **A:** Không có match → file chỉ header.
- **E:** Filter sai → `422`; thiếu `copies.read` → `403`; lỗi stream → `500`.

## 11. Vị trí thư viện

### UC-LOC-01 — Xem cây vị trí

- **N:** Đọc Branch → Area → Shelf, có thể bao gồm node inactive → `200` tree.
- **A:** `includeInactive=false` chỉ trả hierarchy active; không có node → tree rỗng.
- **E:** Query sai → `422`; thiếu `locations.read` → `403`.

### UC-LOC-02 — Lấy danh sách kệ active

- **N:** Query tổ hợp active branch/area/shelf cho picker → `200`.
- **A:** Không có kệ đủ điều kiện → list rỗng.
- **E:** thiếu `locations.read` → `403`.

### UC-LOC-03 — Xem ảnh hưởng trước khi vô hiệu hóa/xóa vị trí

- **N:** Validate type → đếm employee/copy/receipt/audit dependencies → `200 LocationImpactResponse`.
- **A:** Không có dependency → tất cả counts bằng 0.
- **E:** Type sai → `400`; node `404`; thiếu `locations.read` → `403`.

### UC-LOC-04 — Tạo chi nhánh

- **N:** Validate unique code/name → tạo Branch → audit → `201`.
- **A:** Address optional.
- **E:** Duplicate → `409`; input → `422`; thiếu `locations.create` → `403`.

### UC-LOC-05 — Cập nhật chi nhánh

- **N:** Kiểm tra concurrency → update → audit → `200`.
- **A:** Chỉ đổi tên/address và giữ code nếu hợp lệ.
- **E:** `404`; duplicate/stale → `409`; validation → `422`; thiếu `locations.update` → `403`.

### UC-LOC-06 — Tạo/cập nhật khu vực

- **N:** Yêu cầu parent branch active, code/name unique trong scope → create `201` hoặc update `200` → audit.
- **A:** Area có thể được chuyển metadata trong cùng parent theo contract; fields optional được normalize.
- **E:** Area/branch `404`; branch inactive, duplicate hoặc stale → `409`; validation → `422`; thiếu create/update permission → `403`.

### UC-LOC-07 — Tạo/cập nhật kệ

- **N:** Yêu cầu active area hierarchy, code unique, capacity hợp lệ → create `201` hoặc update `200`.
- **A:** Label/capacity được thay đổi khi không phá usage hiện tại.
- **E:** Shelf/area `404`; parent inactive, duplicate, stale hoặc capacity thấp hơn current usage → `409`; validation → `422`; thiếu permission → `403`.

### UC-LOC-08 — Kích hoạt vị trí

- **N:** Với branch/area/shelf, kiểm tra token và parent hierarchy → activate → audit → `200`.
- **A:** Kích hoạt branch không tự động kích hoạt mọi child; từng node giữ trạng thái riêng theo service.
- **E:** Node `404`; parent inactive/stale/current-state conflict → `409`; thiếu `locations.update` → `403`.

### UC-LOC-09 — Vô hiệu hóa vị trí

- **N:** Impact check → soft deactivate branch/area/shelf theo hierarchy rule → audit → `200`.
- **A:** Node không dependency được deactivate trực tiếp; child/inventory history vẫn được giữ.
- **E:** `404`; active dependencies, stale token hoặc protected main branch → `409`; thiếu `locations.deactivate` → `403`.

### UC-LOC-10 — Xóa vĩnh viễn vị trí

- **N:** Yêu cầu node đã ở trạng thái an toàn và không dependency → physical delete → audit → `204`.
- **A:** Chỉ leaf/node độc lập đủ điều kiện được xóa.
- **E:** `404`; bất kỳ dependency/child/protected node → `409`; thiếu `locations.deactivate` → `403`.

## 12. Nhà cung cấp

### UC-SUP-01 — Tra cứu nhà cung cấp

- **N:** Filter/page suppliers, lấy active picker hoặc xem theo ID → `200`.
- **A:** Không khớp → list/page rỗng.
- **E:** ID `404`; query sai → `422`; thiếu `suppliers.read` → `403`.

### UC-SUP-02 — Tạo nhà cung cấp

- **N:** Validate unique code/name/contact → create → audit → `201`.
- **A:** Email/phone/address optional.
- **E:** Duplicate → `409`; validation → `422`; thiếu `suppliers.create` → `403`.

### UC-SUP-03 — Cập nhật nhà cung cấp

- **N:** Check concurrency → update → audit → `200`.
- **A:** Contact fields có thể được thêm/xóa.
- **E:** `404`; duplicate/stale → `409`; validation → `422`; thiếu `suppliers.update` → `403`.

### UC-SUP-04 — Kích hoạt nhà cung cấp

- **N:** Chuyển supplier active → audit → `200`.
- **A:** Existing receipts không bị thay đổi.
- **E:** `404`; stale/current-state conflict → `409`; thiếu `suppliers.update` → `403`.

### UC-SUP-05 — Vô hiệu hóa nhà cung cấp

- **N:** Soft deactivate → audit → `200`; receipt lịch sử được giữ.
- **A:** Supplier có receipt cũ vẫn có thể được deactivate vì không xóa relation.
- **E:** `404`; stale/current-state conflict → `409`; thiếu `suppliers.deactivate` → `403`.

## 13. Phiếu nhập kho

### UC-REC-01 — Tra cứu phiếu nhập

- **N:** Filter/page receipts với supplier/branch/totals; xem detail và items theo ID → `200`.
- **A:** Không khớp → page rỗng; draft chưa có confirmation data vẫn trả trạng thái hiện hành.
- **E:** ID `404`; filter sai → `422`; thiếu `stock-receipts.read` → `403`.

### UC-REC-02 — Tạo phiếu nhập nháp

- **N:** Validate active supplier/branch, receipt number và book items/quantities → create draft/items → audit → `201`.
- **A:** Notes optional; nhiều item sách được tạo trong một transaction.
- **E:** Reference `404`; number trùng → `409`; quantities/items sai → `422`; thiếu `stock-receipts.create` → `403`.

### UC-REC-03 — Cập nhật phiếu nhập nháp

- **N:** Yêu cầu Draft và đúng concurrency → replace/update items → audit → `200`.
- **A:** Thêm, xóa hoặc đổi item khi phiếu chưa confirm.
- **E:** `404`; confirmed/cancelled/stale → `409`; validation → `422`; thiếu `stock-receipts.update` → `403`.

### UC-REC-04 — Xem trước xác nhận nhập kho

- **N:** Load expected items, copies đã tạo và discrepancy hiện có → `200 ConfirmStockReceiptResult`.
- **A:** Chưa có copy/discrepancy → các collection tương ứng rỗng.
- **E:** `404`; thiếu `stock-receipts.read` → `403`.

### UC-REC-05 — Xác nhận nhập kho

- **N:** Lock/check draft/token → validate actual quantities/barcodes/shelves → create BookCopy rows → tạo discrepancy reports nếu lệch → mark confirmed/actor → commit → `200`.
- **A:** Actual khác expected nhưng được rule chấp nhận → confirm và tạo discrepancy report; không lệch → không tạo report.
- **E:** Barcode duplicate/reference sai → `409`; state/token/quantity sai → `409/422`; receipt `404`; thiếu `stock-receipts.confirm` → `403`; transaction rollback → `500`.

## 14. Kiểm kê kho

### UC-INV-01 — Tra cứu scope kiểm kê

- **N:** Đọc branch/area/shelf hợp lệ cho audit picker → `200`.
- **A:** Không có location đủ điều kiện → list rỗng.
- **E:** thiếu `inventory-audits.read` → `403`.

### UC-INV-02 — Tra cứu phiên kiểm kê

- **N:** Filter/page audit sessions; xem detail cùng expected/scanned items → `200`.
- **A:** Không khớp → page rỗng; phiên mới chưa scan có scanned list rỗng.
- **E:** ID `404`; query sai → `422`; thiếu `inventory-audits.read` → `403`.

### UC-INV-03 — Tạo và bắt đầu kiểm kê

- **N:** Validate active scope và actor → tạo `InProgress` audit → snapshot copies kỳ vọng vào audit items → audit event → `201`.
- **A:** Scope không có copy → phiên vẫn có thể tạo với expected set rỗng nếu rule cho phép.
- **E:** Scope inactive/sai → `422`; actor thiếu → `403`; thiếu `inventory-audits.create` → `403`; transaction → `500`.

### UC-INV-04 — Start compatibility endpoint

- **N:** Đọc audit đang `InProgress` → trả `200`; không mutate vì create đã start.
- **A:** Đây là idempotent compatibility call cho client cũ.
- **E:** `404`; status khác `InProgress` → `409`; thiếu create permission → `403`.

### UC-INV-05 — Scan bản sao

- **N:** Normalize barcode → tìm copy → kiểm tra audit scope/state → upsert scanned result → `200`.
- **A:** Scan copy đúng vị trí, sai vị trí hoặc ngoài expected set tạo result khác nhau để reconcile.
- **E:** Audit/copy `404`; completed/out-of-scope/duplicate hoặc stale → `409`; barcode/input → `422`; thiếu `inventory-audits.scan` → `403`.

### UC-INV-06 — Đối soát kiểm kê

- **N:** So expected với scanned → phân loại matched/missing/misplaced/unexpected → `200`.
- **A:** Không discrepancy → tất cả items matched; phiên đang chạy được preview theo state được service cho phép.
- **E:** `404`; lifecycle state không cho reconcile → `409`; thiếu read permission → `403`.

### UC-INV-07 — Hoàn tất kiểm kê

- **N:** Reconcile → nếu hợp lệ/đã acknowledge discrepancy thì mark Completed → audit → `200`.
- **A:** Có discrepancy nhưng request xác nhận acknowledge → vẫn hoàn tất và giữ discrepancy để xử lý.
- **E:** `404`; discrepancy chưa acknowledge hoặc state/stale sai → `409`; thiếu complete permission → `403`.

### UC-INV-08 — Áp dụng điều chỉnh sau kiểm kê

- **N:** Validate audit Completed và 1..100 correction unique → check từng copy concurrency → relocate/change status/condition → transaction/audit → `200 {appliedCount}`.
- **A:** Chỉ các discrepancy được chọn mới được apply; các item còn lại giữ nguyên.
- **E:** Audit/copy `404`; stale/invalid correction → `409/422`; thiếu apply permission → `403`; transaction rollback → `500`.

### UC-INV-09 — Export kết quả kiểm kê

- **N:** Load reconciliation result → tạo CSV → file response.
- **A:** Không có discrepancy → file vẫn chứa các item/summary theo implementation.
- **E:** `404`; thiếu `inventory-audits.export` → `403`; file generation → `500`.

## 15. Độc giả, thẻ và hạn chế

### UC-MEM-01 — Tra cứu độc giả

- **N:** Filter/page members; xem detail gồm thẻ hiện hành, restrictions, loan/reservation/fine summary → `200`.
- **A1:** Search không khớp → page rỗng.
- **A2:** Actor có `members.update` được xem PII đầy đủ; actor chỉ có read nhận dữ liệu PII đã masking.
- **E:** Member ID `404`; query sai → `400/422`; thiếu `members.read` → `403`.

### UC-MEM-02 — Xem lịch sử độc giả

- **N:** Validate category → query paged activity của member → `200 MemberHistoryPageResponse`.
- **A:** Category filter hợp lệ chỉ trả borrowing/reservation/violation/payment tương ứng; không có activity → page rỗng.
- **E:** Member `404`; category sai → `400`; paging sai → `422`; thiếu `members.read` → `403`.

### UC-MEM-03 — Tạo độc giả

- **N:** Validate unique member code/email, member group và limits → `Member.Create` → save/audit → `201`.
- **A:** Phone/address/profile optional.
- **E:** Code/email duplicate → `409`; input/group/limit sai → `400`; thiếu `members.create` → `403`.

### UC-MEM-04 — Cập nhật độc giả

- **N:** Check concurrency → update profile/group/status/limits → audit → `200`.
- **A:** Một số profile field được clear hoặc giữ theo request contract.
- **E:** `404`; duplicate/stale → `409`; validation → `400`; thiếu `members.update` → `403`.

### UC-MEM-05 — Cấp thẻ thư viện

- **N:** Parse wrapped hoặc unwrapped payload → validate card number/date/token → `MembershipCard.Issue` → attach member → audit → `200 MemberResponse`.
- **A:** Controller chấp nhận hai shape JSON để tương thích client.
- **E:** Payload malformed/date sai → `400`; member `404`; card number trùng, đã có active card hoặc stale member → `409`; thiếu `members.manage-cards` → `403`.

### UC-MEM-06 — Gia hạn thẻ

- **N:** Tìm current card → validate expiry/concurrency → renew → audit → `200`.
- **A:** Ngày hết hạn mới dài hơn hiện tại theo rule; thông tin member khác giữ nguyên.
- **E:** Member/card `404`; revoked/expiry sai/stale → `409/400`; thiếu manage-cards → `403`.

### UC-MEM-07 — Đổi trạng thái thẻ

- **N:** Validate transition → update card status → audit → `200`.
- **A:** Có thể suspend/reactivate/revoke tùy transition matrix.
- **E:** Member/card `404`; transition sai/stale → `409`; input → `400`; thiếu manage-cards → `403`.

### UC-MEM-08 — Áp hạn chế độc giả

- **N:** Validate type/reason/period/concurrency → tạo MemberRestriction → audit → `200`.
- **A:** Restriction có thể có thời hạn hoặc không có `EndAtUtc`.
- **E:** Member `404`; overlap/stale → `409`; period/type/reason sai → `400`; thiếu manage-restrictions → `403`.

### UC-MEM-09 — Gỡ hạn chế độc giả

- **N:** Tìm restriction active thuộc member → mark removed với reason/actor/time → audit → `200`.
- **A:** Chỉ một restriction cụ thể bị gỡ, restrictions khác vẫn hiệu lực.
- **E:** Member/restriction không tồn tại hoặc không thuộc nhau → `404`; đã removed/stale → `409`; thiếu manage-restrictions → `403`.

### UC-MEM-10 — Ghi thanh toán từ màn hình độc giả

- **N:** Validate violation thuộc member và current balance → tạo FinePayment → recompute outstanding/resolution → audit → `200 MemberResponse`.
- **A:** Thanh toán một phần giữ violation open; thanh toán đủ chuyển resolved.
- **E:** Member/violation `404`; overpayment/duplicate/stale balance → `409`; amount/method sai → `400`; thiếu manage-finances → `403`.

### UC-MEM-11 — Điều chỉnh tiền phạt từ màn hình độc giả

- **N:** Validate ownership/delta/reason → tạo FineAdjustment → recompute balance → audit → `200`.
- **A:** Delta dương tăng, delta âm giảm balance nếu không vi phạm rule.
- **E:** Member/violation `404`; balance âm/state sai → `409`; delta/reason sai → `400`; thiếu manage-finances → `403`.

## 16. Mượn sách, trả sách và gia hạn

### UC-BOR-01 — Tra cứu lượt mượn

- **N:** Filter/page loans với book/member summary; xem detail gồm copy, renewals và return state → `200`.
- **A:** Search/filter không khớp → page rỗng; open và returned loans được chọn bằng status filter.
- **E:** Borrowing ID `404`; filter sai → `422`; thiếu `borrowings.read` → `403`.

### UC-BOR-02 — Tạo lượt mượn theo legacy endpoint

- **N:** Resolve active circulation policy → validate borrower/book → tạo Borrowing với policy snapshot → `201`.
- **A:** Nếu không có policy cụ thể, resolver dùng system default policy.
- **E:** Member/book `404`; duplicate active loan hoặc policy restriction → `409`; validation → `422`; thiếu `borrowings.create` → `403`.

### UC-BOR-03 — Trả sách theo legacy endpoint

- **N:** Load open borrowing → mark returned → đặt copy Available phù hợp → transaction/audit → `200`.
- **A:** Legacy path không có đầy đủ condition/fine workflow của confirm-return mới.
- **E:** `404`; already returned hoặc stale state → `409`; thiếu `borrowings.return` → `403`.

### UC-BOR-04 — Lookup độc giả trước checkout

- **N:** Normalize card/member code → load member/card/restrictions/open loans/fines/policy → trả eligibility, loan count và limit → `200`.
- **A:** Member tồn tại nhưng không đủ điều kiện → vẫn `200` với reason như card inactive, restriction, overdue hoặc đạt limit; đây là preview chứ chưa tạo loan.
- **E:** Member/card không tồn tại → `404`; search key sai → `400/422`; thiếu `borrowings.create` → `403`.

### UC-BOR-05 — Lookup bản sao trước checkout

- **N:** Normalize barcode → load copy/book/location/status → `200`.
- **A:** Copy tồn tại nhưng unavailable → `200` với status/eligibility để UI hiển thị; conflict chỉ xảy ra khi confirm checkout.
- **E:** Copy `404`; barcode sai → `422`; thiếu `borrowings.create` → `403`.

### UC-BOR-06 — Checkout bản sao

- **N:** Resolve member/card/copy/employee/policy → kiểm tra active card, restriction, overdue block, loan limit, copy availability → transaction đổi copy Borrowed + tạo Borrowing/policy snapshot + audit → `201`.
- **A1:** Policy cụ thể theo branch/member group/document type được chọn; nếu không có thì default policy.
- **A2:** Member có khoản phạt nhưng dưới ngưỡng/block rule vẫn có thể checkout.
- **E:** Entity `404`; command sai → `400/422`; blocked member, limit, duplicate/open loan, copy unavailable hoặc concurrency → `409`; thiếu `borrowings.create` → `403`.

### UC-BOR-07 — Lookup bản sao để trả

- **N:** Barcode → copy → active borrowing → policy/fine preview → `200 BookCopyReturnLookupResponse`.
- **A:** Trễ hạn/damaged/lost được trả như preview outcome và estimated fine, chưa ghi mutation.
- **E:** Copy hoặc open loan không tồn tại → `404`; barcode sai → `422`; thiếu `borrowings.return` → `403`.

### UC-BOR-08 — Xác nhận trả sách

- **N:** Load active loan/copy/member/policy → tính overdue/lost/damaged outcome → mark returned → update copy condition/status → tùy kết quả tạo violation/fine → commit/audit → `200 ReturnExecutionResponse`.
- **A1:** Trả đúng hạn và bình thường → copy Available, không violation.
- **A2:** Trả trễ → tạo overdue violation/fine theo policy.
- **A3:** Damaged/lost → copy chuyển condition/status tương ứng và có thể tạo penalty.
- **E:** Entity `404`; condition/input sai → `400/422`; already returned, copy mismatch hoặc stale token → `409`; thiếu return permission → `403`; rollback khi một bước lỗi.

### UC-BOR-09 — Preview gia hạn

- **N:** Load open loan/member/reservations/policy → tính eligibility và due date mới → `200 RenewalPreviewResponse`.
- **A:** Không đủ điều kiện do quá hạn, max renewals, restriction hoặc reservation chặn → vẫn `200` với `Eligible=false` và reason.
- **E:** Borrowing `404`; input sai → `422`; thiếu read permission → `403`.

### UC-BOR-10 — Gia hạn lượt mượn

- **N:** Re-evaluate eligibility tại thời điểm commit → update due date/count → insert Renewal cùng immutable policy snapshot → audit → `200`.
- **A:** Policy resolver có thể chọn rule hiện hành nhưng renewal record giữ snapshot được áp dụng.
- **E:** Eligibility bị từ chối → `403`; returned/stale/max count/reservation race → `409`; borrowing `404`; thiếu `borrowings.renew` → `403`.

## 17. Đặt chỗ

### UC-RES-01 — Tra cứu đặt chỗ

- **N:** Filter/page reservations với computed lifecycle status; xem detail gồm book/member/policy → `200`.
- **A:** Không khớp → page rỗng; status được tính từ fulfilled/cancelled/expiry fields.
- **E:** Reservation ID `404`; query sai → `422`; thiếu `reservations.read` → `403`.

### UC-RES-02 — Tạo đặt chỗ

- **N:** Resolve member/book/policy → validate card, restrictions, duplicate open reservation → tính hold expiry → lưu reservation/policy snapshot → `201`.
- **A:** Không có policy cụ thể → default hold rule; sách chưa available vẫn có thể được reserve nếu rule cho phép.
- **E:** Member/book `404`; duplicate/ineligible → `409`; validation → `422`; thiếu `reservations.create` → `403`.

### UC-RES-03 — Hủy đặt chỗ

- **N:** Yêu cầu reservation open, reason/token hợp lệ → mark cancelled → audit → `200`.
- **A:** Người có quyền hủy ghi lý do optional/required theo contract; inventory không thay đổi nếu chưa fulfill.
- **E:** `404`; fulfilled/already cancelled/stale → `409`; input → `422`; thiếu `reservations.cancel` → `403`.

### UC-RES-04 — Hoàn tất đặt chỗ

- **N:** Yêu cầu open/unexpired reservation và matching available copy → mark fulfilled; phối hợp checkout nếu command yêu cầu → audit → `200`.
- **A:** Fulfill có thể chỉ đóng reservation hoặc gắn với copy/checkout tùy command hiện hành.
- **E:** Reservation/copy `404`; expired/no copy/already closed/stale → `409`; thiếu `reservations.fulfill` → `403`.

## 18. Vi phạm

### UC-VIO-01 — Tra cứu vi phạm

- **N:** Filter/page violations với outstanding balance; xem detail gồm policy snapshot, payments và adjustments → `200`.
- **A:** Không khớp → page rỗng; resolved violation vẫn được xem trong lịch sử.
- **E:** ID `404`; filter sai → `422`; thiếu `violations.read` → `403`.

### UC-VIO-02 — Preview mức phạt

- **N:** Resolve policy → tính overdue/fixed/lost/damaged amount và cap, không lưu → `200 FinePreviewResponse`.
- **A:** Không có policy cụ thể → dùng default; số ngày bằng 0 có thể cho fine bằng 0.
- **E:** Reference `404`; violation type/input sai → `422`; thiếu read permission → `403`.

### UC-VIO-03 — Tạo vi phạm

- **N:** Kiểm tra idempotency theo borrowing/copy/type → validate member/book → tính hoặc nhận fine → tạo violation cùng policy/calculation snapshot → audit → `201`.
- **A:** Đã tồn tại match idempotent → trả entity hiện có như success thay vì tạo duplicate.
- **E:** Member/book `404`; domain data sai → `400`; persistence race/unique conflict → `409`; thiếu `violations.create` → `403`.

### UC-VIO-04 — Thanh toán vi phạm qua legacy shortcut

- **N:** Load open balance → record/resolve payment state theo legacy behavior → `200 ViolationResponse`.
- **A:** Nếu thanh toán chưa đủ theo khả năng endpoint → violation còn open; đủ → resolved.
- **E:** `404`; already resolved/amount-state sai → `409/422`; thiếu `violations.resolve` → `403`.

### UC-VIO-05 — Miễn vi phạm

- **N:** Load open violation → waive/resolve → audit → `200`.
- **A:** Outstanding balance được xử lý theo trạng thái waived thay vì payment.
- **E:** `404`; already resolved → `409`; thiếu `violations.resolve` → `403`.

## 19. Thanh toán tiền phạt

### UC-PAY-01 — Tra cứu thanh toán

- **N:** Filter/page FinePayments joined violation/member; xem receipt theo ID → `200`.
- **A:** Không khớp → page rỗng; reference optional có thể null.
- **E:** Payment ID `404`; filter sai → `422`; thiếu `violations.read` → `403`.

### UC-PAY-02 — Preview thanh toán

- **N:** Load violation → cộng payments/adjustments → tính remaining payable → `200 FinePaymentPreviewResponse`.
- **A:** Balance bằng 0 → preview chỉ ra không còn khoản phải trả.
- **E:** Violation `404`; thiếu read permission → `403`.

### UC-PAY-03 — Ghi nhận thanh toán

- **N:** Validate member/violation/amount/method/idempotency/current balance → insert payment → nếu trả đủ thì resolve violation → audit → `201 receipt`.
- **A:** Partial payment giữ violation open; exact payment đóng violation; reference có thể optional theo method.
- **E:** References `404`; overpayment, duplicate idempotency hoặc stale balance → `409`; amount/method/input → `400/422`; thiếu `violations.resolve` → `403`.

## 20. Audit log và dashboard

### UC-AUD-01 — Tra cứu audit log

- **N:** Validate time/IP/filter → page immutable audit rows; xem detail theo ID → `200`.
- **A:** Không khớp → page rỗng; actor có thể là System nên ActorUserId null.
- **E:** Time range/IP sai → `400`; detail `404`; thiếu `audit-logs.read` → `403`.

### UC-AUD-02 — Xem lịch sử một entity

- **N:** Filter theo entity type/id và page → `200 AuditLogPageResponse`.
- **A:** Entity có thể không còn tồn tại nhưng audit history vẫn được trả; không có log → page rỗng.
- **E:** Paging/type sai → `422`; thiếu read permission → `403`.

### UC-AUD-03 — Export audit log

- **N:** Validate filters → query export set → escape spreadsheet formula/CSV → file UTF-8 BOM.
- **A:** Không có row → file header only.
- **E:** Time/IP sai → `400`; thiếu `audit-logs.export` → `403`; stream/database → `500`.

### UC-DASH-01 — Xem dashboard tổng hợp

- **N:** Load profile/branch scope → normalize range/timezone → aggregate KPI, alerts, recent activity và branches → `200 DashboardSummaryResponse`.
- **A1:** Administrator hoặc user không có branch → xem global/branch được chọn.
- **A2:** Non-admin → branch filter bị constrain về branch của user.
- **A3:** Không có dữ liệu trong period → metric bằng 0 và list rỗng.
- **E:** Range/branch sai → `422/404`; principal sai → `401`; query aggregation → `500`.

### UC-DASH-02 — Lấy chi nhánh cho dashboard

- **N:** Query active branches → `200` picker list.
- **A:** Không có branch → list rỗng.
- **E:** `E-C01`; database → `500`.

## 21. Gửi thông báo và inbox

> CRUD Notification Template đã bị loại theo phạm vi cấu hình. Các use case dưới đây sử dụng template đã tồn tại.

### UC-NOT-01 — Preview thông báo

- **N:** Load template theo code → validate variables → render subject/body không persistence → `200 NotificationPreviewResult`.
- **A:** Subject optional; email body HTML-encode variable, in-app render theo channel rule.
- **E:** Template `404`; thiếu/không được phép variable hoặc channel sai → `400`; controller hiện có thể map unexpected error → `500`; `E-C01`.

### UC-NOT-02 — Gửi một thông báo

- **N:** Kiểm tra admin hoặc `notifications.manage` → resolve recipient/destination → validate event/severity/deep-link/idempotency → render → tạo notification/audit → in-app gửi adapter + SignalR hoặc email để Pending cho outbox → `200 NotificationDto`.
- **A1:** Destination không truyền → lấy email/member/staff destination từ recipient record.
- **A2:** In-app được gửi tức thời và publish realtime; email được worker gửi bất đồng bộ.
- **A3:** Client chưa online → notification vẫn lưu DB và xuất hiện khi tải inbox.
- **E:** Permission/cross-branch → `403`; template/recipient → `404`; invalid event/channel/destination/deep-link → `400`; duplicate idempotency → `409`; unexpected → `500`.

### UC-NOT-03 — Gửi thông báo hàng loạt

- **N:** Validate manage permission và branch scope → resolve explicit/all staff/role/permission recipients → deduplicate → tạo từng notification với derived idempotency → audit bulk → `200 BulkNotificationResult`.
- **A:** Recipient xuất hiện ở nhiều selector chỉ nhận một notification; tập recipient rỗng trả result zero hoặc validation tùy selection.
- **E:** Cross-branch/permission → `403`; selection sai → `400`; duplicate idempotency → `409`; template → `404`; failure giữa batch có thể tạo partial records nếu không nằm chung transaction.

### UC-NOT-04 — Retry email thất bại

- **N:** Require manage → load failed email → reset retry state/gửi qua email adapter → update attempt/status → audit → `200 NotificationDto`.
- **A:** SMTP tiếp tục lỗi → entity giữ Failed/Pending retry metadata và response phản ánh trạng thái thay vì giả thành công.
- **E:** `403`; notification `404`; non-email/already sent/state sai → `400`; SMTP/unexpected controller error → `500`.

### UC-NOT-05 — Background worker gửi email pending

**Actor:** `EmailOutboxWorker`.

- **N:** Mỗi chu kỳ tạo DI scope → đọc SMTP settings và pending batch → MarkProcessing → Email adapter gửi → MarkSent → save.
- **A1:** SMTP disabled → bỏ qua chu kỳ, item vẫn pending.
- **A2:** Gửi lỗi nhưng chưa vượt max retry → ScheduleRetry với exponential backoff tối đa 60 phút.
- **A3:** Vượt max retry → MarkFailed.
- **E:** Batch-level exception được log, worker không chết và thử lại chu kỳ sau; shutdown cancellation kết thúc vòng lặp sạch.

### UC-NOT-06 — Tra cứu lịch sử vận hành thông báo

- **N:** Require read/manage → filter/page mọi notification nghiệp vụ → `200 NotificationPageResult`.
- **A:** Không khớp → page rỗng; filter theo status/channel/date thu hẹp kết quả.
- **E:** Permission → `403`; filter → `422`; database → `500`.

### UC-NOT-07 — Xem inbox của tôi

- **N:** Query in-app notifications có RecipientId là current user; hỗ trợ unread/severity/date filters → `200` page.
- **A:** Inbox rỗng → page empty; email notification không xuất hiện trong in-app inbox.
- **E:** `E-C01`; filter sai → `422`.

### UC-NOT-08 — Xem một notification của tôi

- **N:** Load in-app notification thuộc current user → `200 NotificationDto`.
- **A:** Không có alternative mutation; chỉ projection owned item.
- **E:** Missing, wrong channel hoặc not owned đều bị ẩn thành `404`; `E-C01`.

### UC-NOT-09 — Xem số chưa đọc

- **N:** Count unread in-app notifications của current user → `200 {count}`.
- **A:** Không có unread → `{count: 0}`.
- **E:** `E-C01`; database → `500`.

### UC-NOT-10 — Đánh dấu đã đọc

- **N:** Load notification → enforce in-app/ownership → set `ReadAtUtc` → `204`.
- **A:** Đã read trước đó → idempotent/no-op nếu service cho phép.
- **E:** `404`; ownership violation → `403`; `E-C01`; database → `500`.

### UC-NOT-11 — Đánh dấu tất cả đã đọc

- **N:** Bulk update unread in-app notifications của current user → `204`.
- **A:** Không có unread → no-op `204`.
- **E:** `E-C01`; database → `500`.

### UC-NOT-12 — Tìm người nhận thông báo

- **N:** Dựa trên role/permission hiện hành để search staff/member recipient type được phép → `200`.
- **A:** Không khớp → list rỗng; branch scope tự giới hạn candidate.
- **E:** Recipient scope không được phép → `403`; type/query sai → `400`; `E-C01`.

### UC-NOT-13 — Nhận notification realtime

**Actor:** SignalR client.

- **N:** Client lấy JWT → connect `/api/v1/notifications/hub` → JwtBearer validate qua query token chỉ trên hub path → Hub đọc `sub`, join group `user:<id>` → publisher gửi `NotificationReceived` → UI cập nhật unread/list.
- **A1:** Mất kết nối → client reconnect theo lịch `0, 2, 5, 10, 30` giây.
- **A2:** Client offline → không nhận event realtime nhưng notification đã lưu và được query khi online.
- **E:** Token/session/account sai → handshake `401`; user ID sai → không join group; network lỗi → reconnect/fallback polling.

## 22. Báo cáo và bộ lọc cá nhân

### UC-REP-01 — Xem danh mục báo cáo được phép

- **N:** Load profile → filter in-memory report definitions theo admin/effective permissions → `200`.
- **A:** Không có permission tương ứng → definition bị ẩn; profile unavailable có thể trả tập rỗng/giới hạn.
- **E:** Principal sai → `401`; unexpected profile error → `500`.

### UC-REP-02 — Preview báo cáo

- **N:** Resolve definition/required permission → enforce branch scope → validate filter/sort/timezone → repository query/project/page → `200 ReportPreviewResult`.
- **A:** Không có dữ liệu → page rỗng; global admin được chọn branch, staff bị constrain về branch của mình.
- **E:** Definition `404`; permission/branch → `403`; input sai có thể bị controller hiện tại map `500`; query error → `500`.

### UC-REP-03 — Export báo cáo

- **N:** Chạy cùng authorization/query không page limit → generate CSV/file bytes → persist report metadata, owner và expiry → `200 ReportExportResult`.
- **A:** Không có dữ liệu → artifact hợp lệ với header/empty body theo generator.
- **E:** Definition `404`; permission/branch → `403`; generation/storage → `500`.

### UC-REP-04 — Tải artifact báo cáo

- **N:** Load report metadata → enforce owner hoặc admin → kiểm tra expiry/artifact → trả file.
- **A:** Administrator tải artifact của user khác; owner tải artifact của chính mình.
- **E:** Metadata/file missing → `404`; not owner → `403`; expired → `410`; storage → `500`.

### UC-FLT-01 — Xem saved filters của tôi

- **N:** Query filters theo current UserId và scope → `200`.
- **A:** Scope không có filter → list rỗng.
- **E:** Principal → `401`; scope sai → `400/422`.

### UC-FLT-02 — Tạo saved filter

- **N:** Validate name/scope/criteria/sort JSON → insert với owner → `201`.
- **A:** Sort optional; criteria có thể rỗng nếu schema cho phép.
- **E:** Validation/JSON → `400`; duplicate owner+scope+name → `409`; `E-C01`.

### UC-FLT-03 — Cập nhật saved filter

- **N:** Load theo ID + owner → validate và update → `204`.
- **A:** Không đổi nội dung → no-op `204`.
- **E:** Missing hoặc not owned → `404`; validation → `400`; duplicate → `409`; `E-C01`.

### UC-FLT-04 — Xóa saved filter

- **N:** Load owned filter → delete → `204`.
- **A:** Không có alternative thành công đặc biệt.
- **E:** Missing/not owned → `404`; `E-C01`; database → `500`.

## 23. Kiosk công khai

### UC-KIOSK-01 — Tìm sách công khai

**Actor:** Người dùng ẩn danh.  
**Endpoint:** `GET /api/v1/kiosk/books`.

- **N:** Validate search/filter/page → query active books và public availability → `200 KioskBookPageResponse`.
- **A:** Không khớp → page rỗng; book không có copy available vẫn có thể hiển thị availability 0.
- **E:** Query sai → `400/422`; quá 120 search/IP/phút → `429`; database → `500`.

### UC-KIOSK-02 — Semantic search công khai

**Actor:** Người dùng ẩn danh.  
**Endpoint:** `POST /api/v1/kiosk/books/semantic-search`.

- **N:** Validate query/topK → embedding provider → pgvector search → map chỉ public fields → `200 KioskSemanticSearchResponse`.
- **A:** Không match → list rỗng; available-only/category filter giới hạn tập.
- **E:** Validation → `422`; provider → `503`; quá 20 request/IP/phút → `429`; unexpected → `500`.

### UC-KIOSK-03 — Xem chi tiết sách công khai

**Actor:** Người dùng ẩn danh.  
**Endpoint:** `GET /api/v1/kiosk/books/{id}`.

- **N:** Load active book, public references, copies và public location → `200 KioskBookDetailResponse`.
- **A:** Không có available copy → detail vẫn trả với availability phù hợp.
- **E:** Book missing/inactive → `404`; database → `500`.

## 24. Ma trận bao phủ

| Module runtime | Use case trong tài liệu | Trạng thái |
|---|---:|---|
| Auth + Me | UC-AUTH-01..07 | Đã bao phủ |
| AccessAccounts | UC-ACC-01..09 | Đã bao phủ |
| Users | UC-USR-01..04 | Đã bao phủ; list/detail gộp trong một use case tra cứu |
| Roles + Permissions | UC-RBAC-01..10 | Đã bao phủ; các route tra cứu được gộp theo mục tiêu nghiệp vụ |
| Employees | UC-EMP-01..05 | Đã bao phủ |
| Books | UC-BOOK-01..11 | Đã bao phủ |
| Copies | UC-COPY-01..10 | Đã bao phủ |
| Locations | UC-LOC-01..10 | Đã bao phủ; create/update/status cho ba cấp được gộp theo intent |
| Suppliers | UC-SUP-01..05 | Đã bao phủ |
| StockReceipts | UC-REC-01..05 | Đã bao phủ |
| InventoryAudits | UC-INV-01..09 | Đã bao phủ |
| Members | UC-MEM-01..11 | Đã bao phủ |
| Borrowings | UC-BOR-01..10 | Đã bao phủ |
| Reservations | UC-RES-01..04 | Đã bao phủ |
| Violations | UC-VIO-01..05 | Đã bao phủ |
| Payments | UC-PAY-01..03 | Đã bao phủ |
| AuditLogs | UC-AUD-01..03 | Đã bao phủ |
| Dashboard | UC-DASH-01..02 | Đã bao phủ |
| Notifications + SignalR | UC-NOT-01..13 | Đã bao phủ |
| Reports | UC-REP-01..04 | Đã bao phủ |
| SavedFilters | UC-FLT-01..04 | Đã bao phủ |
| Kiosk | UC-KIOSK-01..03 | Đã bao phủ |
| CirculationPolicies | — | Loại trừ: cấu hình |
| SystemSettings | — | Loại trừ: cấu hình |
| Configuration import/export | — | Loại trừ: cấu hình |
| SMTP administration | — | Loại trừ: cấu hình |
| NotificationTemplates | — | Loại trừ: cấu hình |
| Todos | — | Loại trừ: `[NonController]`, không chạy runtime |

## 25. Tổng kết

Luồng nghiệp vụ chủ đạo của ứng dụng là:

```text
Quản trị danh mục/kho
  -> tạo sách và bản sao
  -> quản lý vị trí/nhập kho/kiểm kê

Quản lý độc giả
  -> cấp thẻ và restrictions
  -> checkout
  -> return/renew/reserve
  -> tạo violation/fine
  -> payment/adjustment

Hỗ trợ vận hành
  -> audit/dashboard/report
  -> notification outbox + realtime inbox
```

Normal flow mô tả happy path có commit và response. Alternative flow bao gồm các biến thể hợp lệ như preview không đủ điều kiện, partial bulk success, idempotent replay, PII masking, branch scoping, default policy và offline notification. Exception flow bao gồm authentication/authorization, validation, not-found, business conflict, optimistic concurrency, rate limit, external provider và lỗi hệ thống.
