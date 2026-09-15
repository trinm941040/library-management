# MS2-00 — Đối chiếu hiện trạng codebase và backlog

Ngày đối chiếu: 2026-09-09  
Nguồn chuẩn: `MS2-00-doi-chieu-hien-trang-codebase.md`, 9 sơ đồ trong `system/usecase-diagrams`, kiến trúc trong `system/architectures`, sơ đồ lớp trong `system/class-diagrams`, toàn bộ source và hướng dẫn repository.

## 1. Quy ước trạng thái

| Trạng thái | Ý nghĩa |
|---|---|
| `Đã hoàn thành` | Có luồng frontend–backend–database phù hợp use case chính và không thấy sai lệch trọng yếu. |
| `Hoàn thành một phần` | Đã có luồng chạy được nhưng thiếu nhánh nghiệp vụ, dữ liệu, UI state hoặc tích hợp. |
| `Chưa có` | Không tìm thấy implementation tương đương; mock UI không được tính là implementation. |
| `Cần refactor` | Có implementation nhưng contract, bảo mật, kiến trúc hoặc mô hình dữ liệu lệch mục tiêu. |

## 2. Inventory hiện tại

### 2.1 Backend

| Module | Endpoint / handler | Permission | Entity / bảng / migration | Trạng thái và bằng chứng |
|---|---|---|---|---|
| Authentication | `POST /api/v1/auth/register`, `login`, `refresh`, `logout`, `logout-all`; `GET /api/v1/auth/me` → `AuthService` | Public cho register/login/refresh/logout; `[Authorize]` cho logout-all/me | `ApplicationUser`, `RefreshTokenSession`; `users`, `refresh_token_sessions`; `AddIdentityAndAuthorization`, `SeedAdministrator` | `Hoàn thành một phần`: login, RS256 JWT, Argon2id, refresh rotation/reuse revocation có; thiếu confirm-email, forgot/reset-password, rate limit và cleanup token. Public registration trái phạm vi staff-only. Bằng chứng: `Backend/src/UTH.Library.Api/Controllers/AuthController.cs`, `Backend/src/UTH.Library.Infrastructure/Identity/AuthService.cs`, `Argon2PasswordHasher.cs`, `JwtTokenService.cs`. |
| Current profile | `GET /api/v1/me`, `PATCH /api/v1/me/profile`, `POST /api/v1/me/change-password` → `CurrentProfileService` | Authenticated | `ApplicationUser → Employee → Branch`, `AuditLog`; `employees`, `branches`, `audit_logs`; `EnhanceEmployeeProfile`, `AddAuditCorrelationId` | `Đã hoàn thành`: whitelist profile, session roles/permissions, concurrency, password policy, revoke sessions và redacted audit. Bằng chứng: `Backend/src/UTH.Library.Api/Controllers/MeController.cs`, `Contracts/Profile/ProfileContracts.cs`, `Backend/src/UTH.Library.Infrastructure/Identity/CurrentProfileService.cs`. |
| User accounts | `GET/POST/PUT/DELETE /api/v1/users`, `GET /{id}` → `UserManagementService` | `users.read/create/update/deactivate` | Identity `users`, `employees`, `audit_logs`, `refresh_token_sessions` | `Hoàn thành một phần`: create/link/update/deactivate và revoke session có; không có activate/unlock endpoint. Bằng chứng: `Backend/src/UTH.Library.Api/Controllers/UsersController.cs`, `Backend/src/UTH.Library.Infrastructure/Identity/UserManagementService.cs`. |
| Roles/permissions | CRUD `/api/v1/roles`, CRUD `/api/v1/permissions`, replace role permissions, replace user roles → `RolePermissionManagementService` | `roles.*`, `permissions.*`; Administrator bypass | `ApplicationRole`, `Permission`, `RolePermission`, Identity user-role; `roles`, `permissions`, `role_permissions`, `AspNetUserRoles` | `Đã hoàn thành` ở API: assignment dùng transaction, bảo vệ system role/permission và last administrator, revoke session khi đổi role. Bằng chứng: `RolesController.cs`, `PermissionsController.cs`, `RolePermissionManagementService.cs`. |
| Employees | list/filter/get/branches/create/update/status/delete → `EmployeeService` | `employees.read/create/update/delete` | `Employee`, `Branch`, `AuditLog`; `employees`, `branches`, `audit_logs`; `AddEmployeeManagement`, `EnhanceEmployeeProfile` | `Đã hoàn thành` cho hồ sơ hiện tại: validation, audit, concurrency token và soft termination. Bằng chứng: `EmployeesController.cs`, `Features/Employees/EmployeeService.cs`, `Entities/Employee.cs`. |
| Catalog books | list/filter/get/create/update/delete → `BookService` | `books.read/create/update/delete` | `Book`; `books`; `AddBooks` | `Cần refactor`: CRUD chạy được nhưng author/category là chuỗi, không có publisher/copy và delete vật lý thay vì ngừng sử dụng. Không audit/concurrency. Bằng chứng: `BooksController.cs`, `BookService.cs`, `Entities/Book.cs`. |
| Borrowings | list/create/return → `BorrowingService` | `borrowings.read/create/return` | `Borrowing`, `Book`, `Member`; `borrowings`, `books`, `members`; `AddBorrowings`, `AddMemberFinanceForeignKeys` | `Hoàn thành một phần`: eligibility member/card/restriction/limit và trả sách có; quản lý theo đầu sách/quantity, không theo bản sao/barcode, không gia hạn, mất/hỏng hay audit. Bằng chứng: `BorrowingsController.cs`, `BorrowingService.cs`, `Entities/Borrowing.cs`. |
| Reservations | list/create/cancel/fulfill → `ReservationService` | `reservations.read/create/cancel/fulfill` | `Reservation`, `Borrowing`, `Book`, `Member`; `reservations`; `AddReservations` | `Hoàn thành một phần`: giữ chỗ/cancel/fulfill có; không hàng đợi, assigned copy, pickup workflow chi tiết, audit hay concurrency. Bằng chứng: `ReservationsController.cs`, `ReservationService.cs`, `Entities/Reservation.cs`. |
| Members/fines | member CRUD/detail; card issue/renew/status; restriction add/remove; payment/adjustment → `MemberService` | `members.read/create/update/manage-cards/manage-restrictions/manage-finances` | `Member`, `MembershipCard`, `MemberRestriction`, `FinePayment`, `FineAdjustment`, `Violation`; các bảng cùng tên snake_case; 3 migration member | `Hoàn thành một phần`: hồ sơ, thẻ, hạn chế, history, payment/adjustment và audit có; chưa có fine policy/calculation, concurrency exception mapping trong repository và transaction explicit. Bằng chứng: `MembersController.cs`, `MemberService.cs`, `MemberRepository.cs`, `docs/task/reader-management.md`. |
| Violations | list/create/pay/waive → `ViolationService` | `violations.read/create/resolve` | `Violation`; `violations`; `AddViolations` | `Hoàn thành một phần`: ghi nhận/resolve có; payment/adjustment tồn tại ở member API nhưng chưa có policy tính phạt, audit ở service vi phạm hoặc liên kết borrowing/copy. Bằng chứng: `ViolationsController.cs`, `ViolationService.cs`, `Entities/Violation.cs`. |
| Todos | `POST /api/todos`, `GET /api/todos/{id}`, `POST /complete` → `TodoService` | Không có authorize/policy | `TodoItem`; `todos`; `InitialCreate` | `Cần refactor`: endpoint không version `/api/v1`, không authorization dù permission đã seed. Bằng chứng: `TodosController.cs`, `TodoService.cs`. |
| Reports/audit/config/notifications/inventory/receipts | Không có controller/service/repository | Không có permission catalog | Chỉ `AuditLog` tồn tại; các entity mục tiêu khác chưa có | `Chưa có`. `ActivityLogPage` không có endpoint tương ứng. Bằng chứng phủ định: `Backend/src/UTH.Library.Api/Controllers`, `Backend/src/UTH.Library.Domain/Entities`, `LibraryDbContext.cs`. |

Danh sách bảng nghiệp vụ hiện có: `todos`, `books`, `borrowings`, `reservations`, `violations`, `employees`, `members`, `membership_cards`, `member_restrictions`, `fine_payments`, `fine_adjustments`, `branches`, `audit_logs`, `permissions`, `role_permissions`, `refresh_token_sessions`, cộng các bảng ASP.NET Core Identity. Bằng chứng: `Backend/src/UTH.Library.Infrastructure/Persistence/LibraryDbContext.cs` và `Persistence/Configurations`.

### 2.2 Frontend

| Route/page | Feature/API/state/component | Guard | Trạng thái và bằng chứng |
|---|---|---|---|
| `/login` | `LoginPage`, `AuthProvider`, `auth-api`; access token memory + HttpOnly refresh cookie | Redirect authenticated user | `Đã hoàn thành` cho login/restore/logout; Zod chỉ validate current-user/token, không phải mọi API. `Frontend/src/pages/login/LoginPage.tsx`, `src/auth/*`. |
| `/profile`, `/profile/change-password` | RHF + Zod forms, `profile-api`, `UserMenu`; current user dùng chung `AuthProvider` | Authenticated layout | `Đã hoàn thành`. Có loading/error/conflict/retry, unsaved browser warning, password không vào storage/global state. `src/pages/profile/*`, `src/layouts/components/UserMenu.tsx`. |
| `/users` | Table/filter/pagination/dialog + `user-api` | Chỉ authenticated; UI không guard `users.*` | `Hoàn thành một phần`: CRUD/deactivate nối API; chưa activate và chưa UI gán role. `src/pages/users/UserPage.tsx`, `user-api.ts`. `user-store.ts` là code localStorage cũ không còn được page import → cần xóa/refactor. |
| `/roles` | Role/permission CRUD, assignment dialog + `role-permission-api` | Hard-code role `Administrator` | `Hoàn thành một phần`: quản lý role-permission có; không UI gán role cho user; guard theo role thay vì effective permission. `src/pages/roles/*`, `src/routes/ProtectedRoute.tsx`. |
| `/employee` | Table/filter/summary/detail/account linking/dialog + `employee-api` | Chỉ authenticated | `Đã hoàn thành` về luồng nghiệp vụ; thiếu permission-aware navigation/actions và schema form dùng chung. `src/pages/employee/*`. |
| `/books` | CRUD/filter/pagination/dialog + `book-api` | Chỉ authenticated | `Cần refactor` cùng model catalog backend; form state thủ công, không schema response. `src/pages/books/*`. |
| `/borrowings` | list/create/return + member/book API | Chỉ authenticated | `Hoàn thành một phần`; không renew, barcode/copy, lost/damaged. `src/pages/borrowings/*`. |
| `/reservations` | list/create/cancel/fulfill | Chỉ authenticated | `Hoàn thành một phần`; không queue/copy/pickup states đầy đủ. `src/pages/reservations/*`. |
| `/violations` | list/create/pay/waive | Chỉ authenticated | `Hoàn thành một phần`; UI resolve cơ bản, tách rời payment/adjustment trong member detail. `src/pages/violations/*`. |
| `/members` | Một page chứa list, forms, detail, card, restriction, finance + `member-api` | Chỉ authenticated | `Cần refactor`: chức năng rộng đã nối API nhưng file page nén/monolithic, form HTML validation, dùng `sessionStorage` tạm giữ fine ID, thiếu permission-aware actions. `src/pages/members/MemberPage.tsx`, `member-api.ts`. |
| `/dashboard` | Metrics/queue/activity hard-code | Chỉ authenticated | `Chưa có` tích hợp nghiệp vụ; UI demo và ngày cố định. `src/pages/dashboard/DashboardPage.tsx`. |
| `/system/activity-log` | Mock array, local filter, CSV browser | Chỉ authenticated | `Chưa có` API/database integration; không phải audit viewer thật. `src/pages/activity-logs/ActivityLogPage.tsx`. |
| `/system/config`, `/system/info`, `/system/other-settings` | Forms lưu `localStorage` | Chỉ authenticated | `Chưa có` backend; UI prototype phải thay bằng contract thật. `src/pages/config/ConfigPage.tsx`, `info/InfoPage.tsx`, `other-settings/OtherSettingsPage.tsx`. |
| `/settings` | Theme/sidebar preference + `SettingsProvider` | Chỉ authenticated | `Đã hoàn thành` cho preference cục bộ, không tương đương SystemSetting nghiệp vụ. `src/pages/settings/SettingsPage.tsx`, `src/settings/SettingsProvider.tsx`. |
| inventory/stock/report/notification/forbidden/not-found | Không có route/page/feature | Không có | `Chưa có`. Wildcard hiện redirect về dashboard thay vì trang 404. `src/routes/AppRoutes.tsx`. |

Kiến trúc frontend hiện là page-centric: API client nằm cạnh page và dùng `authenticatedFetch`, state chủ yếu `useState`; chưa có QueryClient/cache/query hooks, toast provider, MSW/test helpers, feature public exports hay permission utility như kiến trúc đích mô tả tại `system/architectures/frontend_architecture_vi.drawio`.

## 3. Ma trận truy vết 9 nhóm use case

### UC-01 — Xác thực và phiên đăng nhập

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Đăng nhập; xác thực; tạo token | `/login` → `LoginPage` → `auth-api.login` | `POST /api/v1/auth/login` → `AuthService.LoginAsync/IssueAsync`, `JwtTokenService`, `Argon2PasswordHasher` | `users`, `roles`, `role_permissions`, `refresh_token_sessions` | `Đã hoàn thành` |
| Xem hồ sơ cá nhân | `/profile` → `ProfilePage` | `GET /api/v1/me` → `CurrentProfileService.GetAsync` | `users`, `employees`, `branches`, role/permission tables | `Đã hoàn thành` |
| Đổi mật khẩu | `/profile/change-password` → `ChangePasswordPage` | `POST /api/v1/me/change-password` → `CurrentProfileService.ChangePasswordAsync` | `users`, `refresh_token_sessions`, `audit_logs` | `Đã hoàn thành` |
| Đăng xuất; thu hồi phiên | `UserMenu` → `AuthProvider.logout` | `POST /api/v1/auth/logout` → `AuthService.LogoutAsync` | `refresh_token_sessions`, `audit_logs` | `Đã hoàn thành` |
| Đặt lại quyền truy cập | Không có | Không có forgot/reset password flow | Chưa có token/reset delivery table/adapter | `Chưa có` |
| Khóa/mở khóa tài khoản | `/users` chỉ deactivate | `DELETE /api/v1/users/{id}` → `DeactivateAsync`; không activate/unlock | `users`, refresh sessions, audit | `Hoàn thành một phần` |

### UC-02 — Quản lý biên mục

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Tra cứu/thêm/cập nhật biểu ghi | `/books` → `BooksPage`, `book-api` | Books GET/POST/PUT → `BookService`, `Book` | `books` | `Hoàn thành một phần` |
| Ngừng sử dụng biểu ghi; kiểm tra bản sao | UI delete | `DELETE /books/{id}` hard delete, không kiểm tra copy | `books`; không có `book_copies` | `Cần refactor` |
| Kiểm tra ISBN | Form/API gửi ISBN | Normalize đơn giản + unique index/check | `books.Isbn` | `Hoàn thành một phần` |
| Nhập mô tả; gán tác giả/NXB/thể loại; thêm danh mục liên quan | Chỉ input title/author/category | `Book` lưu author/category dạng chuỗi; không Publisher/Author/Category | `books` | `Chưa có` theo model đích |

### UC-03 — Mượn, trả, gia hạn và đặt trước

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Lập phiếu mượn; tính hạn trả | `/borrowings`, dialog | `POST /borrowings` → `BorrowingService.CreateAsync`, `Borrowing.Create` | `borrowings`, `books`, `members`, card/restriction | `Hoàn thành một phần` — theo Book, không BookCopy |
| Ghi nhận trả | `/borrowings` action | `POST /borrowings/{id}/return` → `MarkReturned`, `Book.CheckIn` | `borrowings`, `books` | `Hoàn thành một phần` — không condition/copy |
| Gia hạn khoản mượn | Không có | Không endpoint/entity `Renewal` | Không bảng | `Chưa có` |
| Quản lý đặt trước; xác nhận nhận sách | `/reservations` | reservation list/create/cancel/fulfill; fulfill tạo Borrowing | `reservations`, `borrowings`, `books`, `members` | `Hoàn thành một phần` |
| Quét thẻ/mã vạch; kiểm tra bản sao | Không có | Member eligibility có, BookCopy/barcode không có | Không `book_copies` | `Chưa có` |
| Kiểm tra điều kiện mượn/gia hạn | UI hiển thị lỗi API | Member/card/restriction/limit/duplicate checks trong `BorrowingService` | member/card/restriction/borrowing | `Hoàn thành một phần` — không renewal policy/copy |
| Xử lý quá hạn | Filter/status UI | `IsOverdue` chỉ suy ra trạng thái | `borrowings` | `Hoàn thành một phần` |
| Ghi nhận mất/hỏng | Không có | Không CopyCondition/BookCopy action | Không bảng | `Chưa có` |

### UC-04 — Thành viên và tiền phạt

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Quản lý hồ sơ/trạng thái/giới hạn | `/members` list/form/detail | Members GET/POST/PUT → `MemberService`, `Member` | `members` | `Đã hoàn thành` về chức năng; frontend `Cần refactor` |
| Cấp/gia hạn thẻ | Member details actions | card issue/renew/status endpoints → `MembershipCard` | `membership_cards` | `Đã hoàn thành` |
| Quản lý hạn chế | Member details | restriction add/remove → `MemberRestriction` | `member_restrictions` | `Đã hoàn thành` |
| Xem lịch sử mượn/đặt | Member details | `GetHistoryAsync` tổng hợp Borrowing/Reservation | `borrowings`, `reservations` | `Đã hoàn thành` |
| Quản lý/tính tiền phạt | Violation/member detail | Violation CRUD state có; không `FinePolicy.Calculate` | `violations` | `Hoàn thành một phần` |
| Ghi nhận thanh toán | Member fine action | payments endpoint → `FinePayment` | `fine_payments` | `Đã hoàn thành` |
| Miễn/điều chỉnh tiền phạt | Waive + adjustment UI | waive endpoint và fine adjustment endpoint | `violations`, `fine_adjustments` | `Đã hoàn thành` |

### UC-05 — Kiểm kê và bản sao

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Đợt kiểm kê; quét mã; đối soát | Không có | Không InventoryAudit/Item | Không bảng | `Chưa có` |
| Trạng thái/điều chuyển/mất-hỏng/thanh lý bản sao | Không có | Không BookCopy/Area/Shelf | Không bảng | `Chưa có` |
| Cập nhật hàng loạt; nhập/xuất dữ liệu | Không có | Không endpoint/job/import validation | Không bảng | `Chưa có` |

### UC-06 — Nhân sự, tài khoản và phân quyền

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Quản lý hồ sơ; việc làm; chi nhánh | `/employee` | employees CRUD/status + branches read → `EmployeeService` | `employees`, `branches`, `audit_logs` | `Đã hoàn thành` |
| Cấp/liên kết tài khoản | Employee details account form | users POST có `EmployeeId` → `UserManagementService`, `Employee.LinkUser` | `users`, `employees`, audit | `Đã hoàn thành` |
| Khóa/mở khóa tài khoản | `/users` deactivate | Backend chỉ deactivate | `users`, refresh sessions | `Hoàn thành một phần` |
| Quản lý vai trò/quyền | `/roles` | full role/permission APIs | roles/permissions/assignments | `Đã hoàn thành` |
| Gán vai trò cho tài khoản | Không có UI | `PUT /api/v1/users/{userId}/roles` có | `AspNetUserRoles`, refresh sessions, audit | `Hoàn thành một phần` |
| Nhật ký thay đổi quyền | Không có viewer thật | Role/user services ghi AuditLog | `audit_logs` | `Hoàn thành một phần` |

### UC-07 — Báo cáo, nhật ký và thông báo

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Dashboard; thống kê/cảnh báo | `/dashboard` static | Không API tổng hợp | Không report/read model | `Chưa có` |
| Lập/lọc/lưu/xuất báo cáo | Chỉ CSV mock ở activity page | Không Report/SavedFilter | Không bảng | `Chưa có` |
| Tra cứu audit; lịch sử hoạt động | `/system/activity-log` mock | Audit được ghi rải rác nhưng không GET endpoint | `audit_logs` | `Hoàn thành một phần` về dữ liệu, UI `Chưa có` tích hợp |
| Mẫu/gửi thông báo | Không có | Không NotificationTemplate/Notification | Không bảng | `Chưa có` |

### UC-08 — Cấu hình chi nhánh và hệ thống

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Quản lý chi nhánh | Không có page CRUD | Chỉ branch seed/read qua employees | `branches` | `Hoàn thành một phần` về model, use case `Chưa có` |
| Khu vực/kệ | Không có | Không Area/Shelf | Không bảng | `Chưa có` |
| Quy tắc lưu thông/giới hạn/phạt/thông báo | `/system/config` localStorage | Không policy entities/API | Không bảng | `Chưa có` |
| Thiết lập hệ thống | `/system/info`, `/system/other-settings` localStorage | Không SystemSetting | Không bảng | `Chưa có` |
| Nhập/xuất/audit cấu hình | Không có thực | Không ConfigurationPackage | Không bảng | `Chưa có` |

### UC-09 — Nhập sách vào kho

| Use case | Frontend | Backend / domain | Dữ liệu | Trạng thái |
|---|---|---|---|---|
| Tiếp nhận/xác nhận/tra cứu lô sách | Không có | Không StockReceipt/Supplier | Không bảng | `Chưa có` |
| ISBN/số lượng/tạo copy/barcode/vị trí/tồn kho | Chỉ CRUD `Book.Quantity` | Không receipt/copy/shelf orchestration | Chỉ `books.Quantity` | `Chưa có` theo use case đích |
| Biên bản sai lệch | Không có | Không DiscrepancyReport | Không bảng | `Chưa có` |

## 4. Khác biệt xuyên suốt

### Model và migration

- Thiếu hoàn toàn: `Author`, `Publisher`, `Category`, `BookCopy`, `Area`, `Shelf`, `InventoryAudit`, `InventoryAuditItem`, `Renewal`, `Supplier`, `StockReceipt`, `StockReceiptItem`, `DiscrepancyReport`, `Report`, `SavedFilter`, `NotificationTemplate`, `Notification`, `CirculationPolicy`, `BorrowingLimit`, `FinePolicy`, `SystemSetting`, `ConfigurationPackage` và các bảng tương ứng. Bằng chứng đích: `system/class-diagrams/*.drawio`; hiện trạng: `Backend/src/UTH.Library.Domain/Entities`, `LibraryDbContext.cs`.
- `Book.Author` và `Book.Category` là string; `Quantity` đại diện tồn kho tổng, trái mô hình bản sao vật lý. Borrowing/Reservation tham chiếu `BookId`, không có `BookCopyId`/assigned copy.
- `Branch` hiện dùng `IsActive: bool`; sơ đồ dùng `Status: BranchStatus`, có behavior activate/deactivate. Chưa có API quản trị branch.
- `AuditLog` hiện có `EntityId` không nullable, `BeforeJson/AfterJson`, `CreatedAtUtc`, `CorrelationId`; còn thiếu `IpAddress`, và tên/nullable khác `BeforeData/AfterData/OccurredAtUtc` trong sơ đồ.
- `Payment` đích hiện được hiện thực tên `FinePayment`; `FineAdjustment` dùng `AmountDelta/AdjustedAtUtc/AdjustedByUserId` thay cho `Type/Amount/CreatedAtUtc/ApprovedByUserId`. Đây là khác biệt có chủ đích cần chốt contract trước Phase 2.
- Migration hiện phủ Identity, books, borrowings, reservations, violations, employees/profile, members/card/restriction/finance và audit correlation. Chưa có migration cho các entity thiếu nêu trên.

### API contract và error handling

- Controller dùng versioning không thống nhất: đa số `/api/v1/*`, riêng Todos là `/api/todos`.
- Contract tách ở API và model application tách riêng là đúng hướng; nhưng frontend chỉ Zod-validate token/current profile. Các `*-api.ts` khác cast JSON trực tiếp, dễ lệch contract runtime.
- `ProblemDetails` có cho auth challenge/forbidden và phần lớn controller, nhưng status validation/failure mapping không đồng nhất; `MembersController.cs` bị nén một dòng, khó review.
- Chưa có endpoint dùng chung cho dashboard, audit-log, branch management, config, reports, notifications, copy/inventory và stock receipt.

### Permission và session

- Backend có catalog tập trung và policy cho user/role/permission/employee/book/borrowing/reservation/violation/member. Administrator bypass được cấu hình tại `Backend/src/UTH.Library.Api/DependencyInjection.cs`.
- Todos không áp authorization. Auth register hiện public và tạo role `User`, trái phạm vi tổng thể “chỉ nhân viên có StaffAccount”.
- Frontend chỉ kiểm tra authenticated cho hầu hết route và chỉ hard-code Administrator cho `/roles`; sidebar/action không kiểm effective permission. Người thiếu quyền vẫn thấy thao tác rồi nhận 403.
- JWT chứa role/permission tại thời điểm phát hành; đổi role revoke refresh session nhưng access token đã phát hành vẫn sống đến expiry. Logout/change-password cũng không vô hiệu access token ngay lập tức.

### Transaction, audit và concurrency

- Explicit transaction: refresh rotation, password change, user create/update/deactivate và role/user-role replacement (`AuthService`, `CurrentProfileService`, `UserManagementService`, `RolePermissionManagementService`).
- Book/borrowing/reservation/violation/employee/member dùng một scoped `LibraryDbContext.SaveChangesAsync`; một lần save là transaction ngầm, nhưng application chưa có `IUnitOfWork`/transaction boundary rõ như kiến trúc đích.
- Audit có ở employee, member/card/restriction/finance, account/role assignment, profile/password/logout. Thiếu ở book, borrowing, return, reservation, violation và mọi module chưa có. Audit payload employee/member chứa PII đầy đủ; cần policy redaction thống nhất. Correlation ID mới chỉ được gắn trong profile/password/logout.
- Optimistic concurrency có ở `Employee.ConcurrencyToken`, `Member.ConcurrencyToken`, `RefreshTokenSession.RowVersion` và Identity stamp. Book, Borrowing, Reservation, Violation cùng các action quantity/status chưa có concurrency token; có nguy cơ lost update/race khi checkout/return/fulfill.

### UI state và kiến trúc frontend

- Các page API có loading/error/empty/success cơ bản và chống submit lặp bằng local state; profile dùng RHF/Zod. Phần lớn form khác dùng state/HTML validation, chưa có schema thống nhất.
- Không có query cache/invalidation; mỗi page tự fetch/reload. Auth profile là nguồn dùng chung duy nhất qua `AuthProvider`.
- Dashboard/activity/config/info/other-settings là mock hoặc localStorage nhưng được đặt trong authenticated navigation như chức năng thật; cần gắn nhãn prototype hoặc ẩn đến khi có backend.
- Thiếu forbidden/not-found page, permission guard/action gate, toast/error boundary và test layer theo kiến trúc frontend đích.

## 5. Contract dùng chung đề xuất chốt cho Phase 2

Giữ nguyên các contract đã chạy và chỉ version bằng `/api/v1`: auth/current-user, users, roles, permissions, employees, members, books, borrowings, reservations, violations. Trước khi mở rộng cần chốt các nhóm mới:

1. Catalog chuẩn hóa: `/authors`, `/publishers`, `/categories`, `/books/{id}/copies`.
2. Copy/location: `/book-copies`, `/branches`, `/areas`, `/shelves`.
3. Circulation: `/borrowings/{id}/renew`, return payload condition/copy, reservation queue/assignment.
4. Inventory: `/inventory-audits` và scan/reconcile/complete/import/export actions.
5. Stock: `/suppliers`, `/stock-receipts` và confirm/discrepancy actions.
6. Operations: `/dashboard`, `/reports`, `/saved-filters`, `/audit-logs`, `/notification-templates`, `/notifications`.
7. Configuration: `/circulation-policies`, `/system-settings`, `/configuration-packages`.

Mỗi contract mới phải có permission `<resource>.<action>`, Problem Details/error code, pagination/filter URL state, cancellation token, audit/redaction, transaction boundary và concurrency token cho mutation.

## 6. Backlog đã cập nhật

Các hạng mục `Đã hoàn thành` ở ma trận không lặp lại trong backlog. Thứ tự sau dựa trên khoảng trống dữ liệu và rủi ro tích hợp; cột phụ thuộc là phụ thuộc kỹ thuật, không dùng để chặn công việc MS2-00 này.

| Thứ tự | Ưu tiên | Hạng mục còn lại | Trạng thái | Phụ thuộc kỹ thuật / bằng chứng |
|---:|---|---|---|---|
| 1 | P0 | Chuẩn hóa authorization/security: bỏ public staff registration hoặc giới hạn Development/admin; bảo vệ Todos; permission-aware frontend; activate/unlock; rate limit; reset password | `Cần refactor` | `AuthController.cs`, `TodosController.cs`, `ProtectedRoute.tsx`, `Sidebar.tsx` |
| 2 | P0 | Chuẩn hóa API/error/audit/concurrency: `/api/v1`, Problem Details/error code, audit redaction/correlation toàn cục, token invalidation strategy, concurrency cho circulation | `Cần refactor` | Các khác biệt mục 4 |
| 3 | P0 | Tách Book catalog và physical copy/location; migration Author/Publisher/Category/BookCopy/Area/Shelf | `Chưa có` | Nền tảng bắt buộc cho UC-02/03/05/09 |
| 4 | P0 | Refactor checkout/return sang BookCopy/barcode; thêm renewal, condition, lost/damaged và race-safe transaction | `Hoàn thành một phần` | Sau physical copy; `BorrowingService.cs` |
| 5 | P1 | Hoàn thiện reservation queue, assigned copy, pickup/expiry và audit | `Hoàn thành một phần` | Sau physical copy/circulation |
| 6 | P1 | Inventory audit, scan/reconcile, bulk update, transfer, liquidation, import/export | `Chưa có` | BookCopy + Branch/Area/Shelf |
| 7 | P1 | Supplier/stock receipt/items/confirm/discrepancy và tạo copy khi nhập | `Chưa có` | Catalog + BookCopy/location |
| 8 | P1 | FinePolicy/CirculationPolicy/BorrowingLimit và tự động quá hạn/tính phạt | `Hoàn thành một phần` | Member + circulation hiện có |
| 9 | P1 | Audit-log query API/UI thật; dashboard read model; report/export/saved filters | `Chưa có` | Audit schema/redaction chuẩn hóa |
| 10 | P2 | Notification template/delivery/operation notifications | `Chưa có` | Event/audit hoặc job infrastructure |
| 11 | P2 | Branch CRUD, Area/Shelf UI; SystemSetting và configuration import/export/audit | `Chưa có` | Location model + permission catalog |
| 12 | P2 | Frontend refactor theo feature: shared API parser, Zod contracts, query cache/hooks, permission utility, forbidden/404, tách `MemberPage` | `Cần refactor` | Contract backend ổn định |
| 13 | P2 | Xóa dead code/mock gây hiểu nhầm (`user-store.ts`, dashboard/activity/config local prototypes) hoặc gắn nhãn prototype | `Cần refactor` | Có thể làm độc lập |

## 7. Happy-case truy vết

### Use case đã có: lập phiếu mượn

`/borrowings` (`Frontend/src/pages/borrowings/BorrowingsPage.tsx`) → form tải sách và Member (`components/BorrowingFormDialog.tsx`) → `borrowing-api.createBorrowing` → `POST /api/v1/borrowings` (`BorrowingsController.cs`) → `BorrowingService.CreateAsync` kiểm Member active, card, restriction, limit, trùng khoản mượn và loan days → `Book.Checkout` + `Borrowing.Create` → `BorrowingRepository.SaveChangesAsync` → cập nhật `books.Quantity` và thêm dòng `borrowings` trong cùng DbContext save. Kết luận: luồng chạy đủ trên model hiện tại nhưng chỉ `Hoàn thành một phần` so với sơ đồ vì chưa có `BookCopy/barcode`.

### Use case chưa có: thực hiện đợt kiểm kê

Không có route inventory trong `Frontend/src/routes/AppRoutes.tsx`; không có controller trong `Backend/src/UTH.Library.Api/Controllers`; không có entity `InventoryAudit/InventoryAuditItem/BookCopy` trong `Backend/src/UTH.Library.Domain/Entities`; không có DbSet/bảng/migration tương ứng trong `LibraryDbContext.cs` và `Persistence/Migrations`. Kết luận: `Chưa có`, giữ backlog thứ tự 6.

## 8. Kết quả kiểm tra

- Backend: `dotnet build LibraryManegement.sln --no-restore --disable-build-servers -m:1` — **thành công**, 0 warning, 0 error.
- Frontend: `pnpm --config.verify-deps-before-run=false build` — **thành công**. Local không có binary `pnpm`; dùng Corepack pnpm 11.22.0 qua shim tạm `/tmp/pnpm`. Cờ trên chỉ ngăn pnpm tự xóa/cài lại `node_modules` vốn được npm quản lý, không bỏ qua TypeScript/VCheck hay Vite build.
- Cảnh báo không chặn: Vite báo main chunk khoảng 631 kB, lớn hơn ngưỡng 500 kB.
- Theo yêu cầu, **không chạy `dotnet test`**, không dùng Docker và không bổ sung test.

## 9. Đối chiếu acceptance/checklist MS2-00

- [x] Inventory backend endpoint, handler/service, entity, migration, permission và integration kèm đường dẫn.
- [x] Inventory frontend route, page, feature/API, schema/state/component kèm đường dẫn.
- [x] Ma trận truy vết đủ 9 nhóm use case tới frontend, backend/domain và bảng dữ liệu.
- [x] Mỗi chức năng được gắn một trong bốn trạng thái và bằng chứng.
- [x] Khác biệt model/migration/API/permission/transaction/audit/concurrency/UI state đã được liệt kê.
- [x] Backlog đã bỏ hạng mục hoàn tất, sắp thứ tự phần còn thiếu và ghi phụ thuộc kỹ thuật.
- [x] Contract chung cần chốt cho Phase 2 đã được liệt kê.
- [x] Happy case có implementation và happy case chưa có đã được truy vết.
- [x] Backend và frontend build thành công theo yêu cầu.
