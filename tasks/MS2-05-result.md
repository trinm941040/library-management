# MS2-05 — Kết quả xác thực và phiên đăng nhập

Ngày kiểm tra: 11/09/2026. Đã triển khai và kiểm tra API local + session client. **Chưa xác nhận happy case giao diện bằng trình duyệt** vì công cụ không có browser kết nối. Không chạy `dotnet test`, không dùng Docker, không ghi vào Supabase.

## Hành vi sau sửa

- Access token RS256 chỉ giữ trong biến bộ nhớ frontend; mặc định 10 phút theo `Jwt:AccessTokenMinutes`. Không ghi vào localStorage/sessionStorage/TanStack Query.
- Refresh token ngẫu nhiên 32 byte; database chỉ lưu SHA-256. Cookie `uth_refresh`: HttpOnly, SameSite=Strict, Path=/api/v1/auth, Secure ngoài Development.
- Refresh giữ nguyên hạn tuyệt đối của family (mặc định 30 ngày), đánh dấu token cũ đã dùng/thu hồi và tạo token con trong một transaction. PostgreSQL advisory transaction lock theo user và cập nhật có điều kiện ngăn hai refresh cùng thành công.
- Reuse thu hồi toàn family, ghi audit không chứa bí mật. Access token có `sid`; mỗi request xác minh family còn hoạt động. Logout, logout-all và đổi mật khẩu làm access token tương ứng mất hiệu lực ngay.
- Login yêu cầu credential hợp lệ, account hoạt động/không lockout và Employee liên kết có trạng thái Active. Không tự tạo Employee cho tài khoản cũ. Lỗi credential/account/Employee dùng thông báo chung.
- Quyền hiệu lực lấy từ database: hợp quyền các role active, loại assignment đã gỡ; không bypass theo tên Administrator và không tin permission claim cũ để authorize. Account chưa có quyền vẫn xác thực được nhưng bị chặn ở endpoint yêu cầu quyền.
- Bootstrap và các request 401 dùng chung một refresh promise; mỗi request retry tối đa một lần. Web Locks tuần tự hóa thao tác cookie giữa các tab có hỗ trợ. Generation guard chặn response cũ khôi phục phiên đã xóa.
- AuthProvider là nguồn authorization chung cho route, sidebar/mobile và PermissionBoundary. Không render app shell trước bootstrap. Reset query cache khi đổi account/quyền; cập nhật profile sau mutation quyền, khi focus/visible, định kỳ 30 giây và khi gặp 403. BroadcastChannel chỉ gửi tên sự kiện, không gửi token.
- Safe intended route chỉ nhận đường dẫn nội bộ, loại URL ngoài, login/API, ký tự điều khiển và backslash. Lỗi khôi phục phiên do mạng có màn hình retry, không coi là đăng nhập thành công.
- Bổ sung PermissionBoundary cho action/form tài khoản, vai trò/quyền và sách để quyền cập nhật được phản ánh trên consumer hiện có; không thêm feature nghiệp vụ.

## Contract

| Endpoint | Kết quả |
|---|---|
| POST /api/v1/auth/login | `{ accessToken, accessTokenExpiresAtUtc, currentUser }` + refresh cookie |
| POST /api/v1/auth/refresh | Cùng SessionResponse; rotate cookie |
| GET /api/v1/me, /api/v1/auth/me, /api/v1/auth/current-session | CurrentProfileResponse với roles/permissions hiệu lực |
| POST /api/v1/auth/logout hoặc /revoke | 204; thu hồi family của cookie, xóa cookie |
| POST /api/v1/auth/logout-all | Bearer bắt buộc; 204, thu hồi tất cả family của account |
| PUT /api/v1/roles/{id} | Bổ sung `isActive` tùy chọn; response role có `isActive`; không vô hiệu role hệ thống |

Các POST auth yêu cầu `X-Requested-With: XMLHttpRequest`, từ chối `Sec-Fetch-Site: cross-site`. Dùng same-origin Vite/reverse proxy; không mở credentialed CORS. Login giới hạn 10 request/phút/IP, refresh 60; trả 429 có Retry-After. Auth response no-store; lỗi có Problem Details/correlation ID. Endpoint đăng ký công khai được gỡ khỏi API nội bộ; tạo account tiếp tục qua API quản trị có permission.

## Migration và dữ liệu

Migrations:

- `Backend/src/UTH.Library.Infrastructure/Persistence/Migrations/20260911062922_HardenAuthenticationSessions.cs` cùng Designer và ModelSnapshot.
- `Backend/src/UTH.Library.Infrastructure/Persistence/Migrations/20260911100000_LinkSeedAdministratorEmployee.cs`: tạo Employee Active và liên kết tài khoản seed `admin@example.com`. Migration không ghi đè dữ liệu nếu email, mã hoặc ID dự kiến xung đột; nó dừng với lỗi để yêu cầu mapping rõ ràng. Down giữ Employee và lịch sử để tránh mất dữ liệu; Up có tính lặp lại theo liên kết UserId.

- Up: thêm `roles.IsActive` mặc định true, index IsActive, FamilyId, ParentTokenId. Không xóa user, Employee, session hoặc lịch sử.
- Down: bỏ cột/index mới; dữ liệu nghiệp vụ được giữ. **Rollback làm mất trạng thái inactive của role**; khi nâng cấp lại, các role mặc định active. Cần lưu trạng thái role trước rollback thật.
- Seed không tái cấp permission đã bị quản trị viên thu hồi khi restart; chỉ bootstrap role mới hoặc permission hệ thống mới xuất hiện.
- Vẫn một LibraryDbContext, một connection key `ConnectionStrings:LibraryDatabase`; không thêm DBContextFactory hay sửa appsettings. Kiểm tra dùng environment override tới cluster PostgreSQL tạm, không đổi database runtime của dự án.
- Đã cài PostgreSQL 17 local để kiểm tra. Database tạm: `library_ms205`, cổng 55435; dữ liệu fixture nằm ở `/private/tmp/library-ms205.GkBKl7/data`. API và cluster tạm đã dừng sau kiểm tra; không xóa fixture.

Đối chiếu nâng cấp lại trên dữ liệu sau rollback, trước khi khởi động API:

| Bảng | Trước Up | Sau Up |
|---|---:|---:|
| users | 2 | 2 |
| employees | 1 | 1 |
| roles | 5 | 5 |
| refresh_token_sessions | 7 | 7 |
| audit_logs | 26 | 26 |

## Kiểm tra đã chạy

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build LibraryManegement.sln --no-restore -m:1 /p:UseSharedCompilation=false` trong Backend | Thành công, 0 warning / 0 error; build cả project test nhưng không chạy test |
| EF `database update` trên DB mới | Thành công, toàn bộ migration |
| EF `database update 20260910090000_AddCoreOptimisticConcurrency`, rồi `database update` | Down/Up thành công, số lượng dữ liệu như trên |
| EF `migrations has-pending-model-changes` | Không có thay đổi model chưa migration |
| `corepack pnpm --config.verify-deps-before-run=false build` trong Frontend | Thành công, bao gồm `tsc -b` và Vite build |
| `./node_modules/.bin/eslint .` | Thành công |
| Smoke test API local (script tạm đã xóa sau kiểm tra) | Thành công: login/profile, hợp quyền active, cookie/hash, refresh chain, reuse, concurrent refresh, revoke/logout-all, quyền gỡ với JWT cũ, account khóa/inactive, Employee inactive, hết hạn, lỗi chung, CSRF header và audit |
| Smoke test session frontend (script tạm đã xóa sau kiểm tra) | Thành công: 20 bootstrap/401 đồng thời, cập nhật quyền và lọc menu, response cũ, Zod, AbortSignal, safe intended route |
| Đăng nhập seed admin `admin@example.com` sau migration liên kết Employee trên DB local | HTTP 200; không ghi token/cookie ra output |
| `git diff --check` | Thành công |

Smoke test API chỉ chạy trên host loopback và database kiểm tra `library_ms205`; fixture ngẫu nhiên được giữ trong DB tạm, không in password/token/cookie. Smoke test frontend dùng fetch giả lập trên module thật, **không thay thế kiểm tra browser end-to-end**. Hai script tạm đã được xóa sau khi hoàn tất kiểm tra.

## File thay đổi

Đường dẫn bên dưới tương đối với repository:

- API: `Backend/src/UTH.Library.Api/Authorization/PermissionRequirement.cs`, `DependencyInjection.cs`, `Program.cs`; `Controllers/{AuthController,MeController,RolesController}.cs`; `Contracts/Auth/AuthContracts.cs`, `Contracts/Profile/ProfileContracts.cs`, `Contracts/Roles/RoleContracts.cs`.
- Application: `Backend/src/UTH.Library.Application/Abstractions/Identity/{AuthModels,IJwtTokenService,RolePermissionManagementModels}.cs`.
- Infrastructure: `Backend/src/UTH.Library.Infrastructure/DependencyInjection.cs`; `Identity/{AuthService,AuthorizationStateService,SessionLock,CurrentProfileService,IdentityEntities,IdentitySeeder,JwtTokenService,RolePermissionManagementService,UserManagementService}.cs`; `Persistence/{LibraryDbContext,AuditSaveChangesInterceptor}.cs`, `Persistence/Configurations/IdentitySeedConfiguration.cs`; migration/Designer/Snapshot nêu trên.
- Frontend: `Frontend/src/auth/{auth-api.ts,AuthProvider.tsx}`, `shared/auth/intended-destination.ts`, `routes/{AppRoutes,ProtectedRoute}.tsx`, `pages/login/LoginPage.tsx`, `pages/roles/{role-permission-api.ts,RolePermissionPage.tsx}`, `pages/users/UserPage.tsx`, `pages/users/components/UserRoleDialog.tsx`, `pages/books/BooksPage.tsx`.
- Báo cáo: `tasks/MS2-05-result.md`.

## Lưu ý triển khai / phần chưa xác nhận

1. **Chưa áp migration lên Supabase đang cấu hình.** Phải áp migration trước khi chạy backend mới. Migration mới liên kết Employee Active cho riêng tài khoản admin được seed; tài khoản cũ khác chưa liên kết Employee Active vẫn không đăng nhập được.
2. Frontend/backend cần phát hành đồng bộ vì login/refresh đổi contract. JWT cũ thiếu sid sẽ bị từ chối; refresh còn hợp lệ có thể khôi phục phiên theo contract mới, nếu không người dùng đăng nhập lại.
3. Chưa kiểm tra browser thực tế: mở dashboard sau login, reload trang, menu desktop/mobile, bàn phím và action khi quyền thay đổi. Công cụ trả `apps: [], browsers: []`; acceptance UI chỉ được đối chiếu source và smoke client, chưa xác nhận hoàn tất end-to-end.
4. Cookie Secure và SameSite cần HTTPS/same-origin ở production. Web Locks cần browser hỗ trợ secure context; browser không hỗ trợ chỉ bảo đảm single refresh trong mỗi tab. Reuse giữa tab trên browser đó có thể buộc đăng nhập lại.
5. Thay đổi quyền từ phiên quản trị khác có độ trễ UI tối đa khoảng 30 giây khi tab visible (hoặc cập nhật sớm khi focus/403); backend enforce theo database ngay ở request tiếp theo. Không có push thời gian thực trong task này.
6. Rate limiter hiện theo IP tiến trình API. Khi triển khai sau proxy cần cấu hình trusted forwarded headers và cân nhắc limiter dùng chung nếu nhiều instance; không tự tin cậy X-Forwarded-For do client gửi.
