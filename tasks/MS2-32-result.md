# MS2-32 — Kết quả thiết lập hệ thống và gói cấu hình

Ngày hoàn thiện và kiểm tra: 12/09/2026. Đã hoàn thành toàn diện phân hệ Thiết lập hệ thống (SystemSetting) và Quản lý gói cấu hình (ConfigurationPackage) cho cả tầng Backend (.NET 9 Clean Architecture) và Frontend (React + TypeScript + Vite + Tailwind/shadcn).

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **SystemSetting Single Source of Truth:**
  - Định nghĩa danh mục tham số hệ thống chuẩn: quy định mượn trả (`circulation.max_days`, `circulation.max_books`, `circulation.max_renewals`, `circulation.hold_days`, `circulation.fine_per_day`, `circulation.block_overdue`, `circulation.lost_penalty_ratio`), thông tin thư viện (`library.name`, `library.email`, `library.address`, `library.phone`, `library.hours`), thông báo email (`notification.smtp_host`, `notification.smtp_port`, `notification.smtp_user`, `notification.smtp_password`), và sao lưu (`system.auto_backup`, `system.backup_retention_days`).
  - Kiểm soát kiểu dữ liệu nghiêm ngặt (`SettingType`: String, Number, Boolean, Json, Secret). Từ chối giá trị không đúng schema qua validation trước khi lưu.
  - Hỗ trợ kiểm soát tương tranh lạc quan (Optimistic Concurrency) thông qua `ConcurrencyToken` (UUID).

- **Xử lý bí mật an toàn (Secret Handling & Redaction):**
  - Tham số nhạy cảm (`notification.smtp_password`) được che trên UI (`********`).
  - Khi xuất gói cấu hình (`export-package`), giá trị bí mật được che hoàn toàn thành `[REDACTED]`.
  - Trong tầng `AuditSaveChangesInterceptor`, giá trị của setting có `ValueType == SettingType.Secret` cùng các thông tin xác thực (`PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `RowVersion`) được tự động che thành `[REDACTED]` trước khi ghi vào `AuditLogs`.

- **Quản lý gói cấu hình (ConfigurationPackage Import/Export):**
  - **Export:** Tạo file JSON chuẩn `Cau_Hinh_He_Thong_v{version}_{timestamp}.json` chứa danh sách thiết lập, version, metadata người xuất và mã kiểm tra toàn vẹn **Checksum SHA-256**.
  - **Validate:** Kiểm tra cấu trúc JSON, xác minh tính toàn vẹn thông qua Checksum (ngăn chặn chỉnh sửa bất hợp pháp hoặc hư hại tệp tin). Tạo preview diff trực quan so sánh dữ liệu với database hiện tại (`CHANGE`, `ADD`, `SAME`) cùng cảnh báo tác động vận hành (`hasImpactWarning`).
  - **Import:** Áp dụng toàn bộ gói trong một Database Transaction an toàn. Lưu vết gói đã nạp vào bảng `configuration_packages` và tự động ghi `AuditLog` chi tiết cho từng thay đổi.
  - **Reset Defaults:** Cho phép khôi phục tức thời các thiết lập về giá trị chuẩn của hệ thống, giữ nguyên bí mật SMTP.

- **Phân quyền và bảo mật điều hướng:**
  - Backend bảo vệ các API bằng policy `Permissions.SettingsRead` (`settings.read`) và `Permissions.SettingsManage` (`settings.manage`).
  - Frontend bảo vệ các route `/system/config` và `/configuration` qua `<ProtectedRoute requiredRole="Administrator">`, đồng thời ẩn các mục này khỏi Sidebar đối với người dùng không phải Quản trị viên.

---

## 2. Danh mục API Endpoints

| Phương thức | Đường dẫn API | Quyền yêu cầu | Mô tả |
|---|---|---|---|
| `GET` | `/api/v1/settings` (alias `/api/v1/configuration`, `/api/v1/system/config`) | `settings.read` | Lấy danh sách toàn bộ thiết lập (hỗ trợ lọc theo `scope`). |
| `GET` | `/api/v1/settings/{key}` | `settings.read` | Lấy chi tiết một thiết lập theo khóa. |
| `PUT` | `/api/v1/settings/{key}` | `settings.manage` | Cập nhật giá trị một thiết lập (kèm kiểm tra concurrency token). |
| `PUT` | `/api/v1/settings/batch` | `settings.manage` | Cập nhật hàng loạt thiết lập. |
| `GET` | `/api/v1/settings/export-package` | `settings.manage` | Tải về tệp tin JSON gói cấu hình có checksum SHA-256. |
| `POST` | `/api/v1/settings/validate-package` | `settings.manage` | Xác thực tính hợp lệ, kiểm tra checksum và tạo preview diff Before/After. |
| `POST` | `/api/v1/settings/import-package` | `settings.manage` | Áp dụng gói cấu hình vào database trong Transaction. |
| `GET` | `/api/v1/settings/packages` | `settings.read` | Lấy lịch sử 20 gói cấu hình gần nhất đã nạp. |
| `POST` | `/api/v1/settings/reset-defaults` | `settings.manage` | Khôi phục toàn bộ tham số về mặc định hệ thống. |

---

## 3. Các tệp tin đã triển khai và hoàn thiện

### Backend (.NET 9)
1. `UTH.Library.Domain/Entities/SystemSetting.cs`: Domain entity cho thiết lập hệ thống.
2. `UTH.Library.Domain/Entities/ConfigurationPackage.cs`: Domain entity cho gói cấu hình.
3. `UTH.Library.Domain/Enums/SettingType.cs`: Enum phân loại kiểu dữ liệu (`String`, `Number`, `Boolean`, `Json`, `Secret`).
4. `UTH.Library.Application/Features/SystemSettings/`:
   - `ISystemSettingService.cs`: Interface service cho thiết lập và gói cấu hình.
   - `SystemSettingModels.cs`: Các DTO (GetAll, Update, Export, Validate/Diff, Import, History).
5. `UTH.Library.Infrastructure/Services/SystemSettingService.cs`:
   - Registry quản lý schema và giá trị mặc định.
   - Đảm bảo tự động seed tham số thiếu khi khởi động.
   - Tính toán và xác thực Checksum SHA-256.
   - Sinh preview diff so sánh với DB.
   - Transactional import và lưu lịch sử package.
6. `UTH.Library.Infrastructure/Persistence/Configurations/CatalogInventoryConfiguration.cs`:
   - Đồng bộ cột `Value` (`SystemSetting`) và `Data` (`ConfigurationPackage`) sang kiểu `text`.
7. `UTH.Library.Infrastructure/Persistence/AuditSaveChangesInterceptor.cs`:
   - Bổ sung cơ chế che dữ liệu nhạy cảm (`[REDACTED]`) cho Secret và Auth properties khi lưu AuditLog.
8. `UTH.Library.Api/Controllers/SystemSettingsController.cs`: Controller cung cấp đầy đủ 9 endpoints.
9. `UTH.Library.IntegrationTests/SystemSettingsApiTests.cs`: Bộ kiểm thử tích hợp tự động cho toàn bộ vòng đời SystemSettings & Packages.

### Frontend (React + TypeScript)
1. `src/pages/config/system-settings-api.ts`: API client client-side tương thích hoàn toàn với backend endpoints.
2. `src/pages/config/ConfigPage.tsx`: Màn hình cấu hình chính với 3 tab (Tham số hệ thống, Chính sách lưu thông, Lịch sử gói cấu hình), quản lý form theo kiểu dữ liệu, nút sao lưu, khôi phục mặc định và nạp gói.
3. `src/pages/config/components/ConfigPackageDialog.tsx`: Dialog tải file, hiển thị trạng thái giải mã checksum SHA-256, bảng Diff Before/After chi tiết theo từng loại (Cập nhật, Thêm mới, Giữ nguyên), cảnh báo rủi ro vận hành và nút áp dụng an toàn.
4. `src/routes/AppRoutes.tsx`: Thiết lập `ProtectedRoute requiredRole="Administrator"` cho các đường dẫn `/configuration` và `/system/config`.
5. `src/layouts/components/Sidebar.tsx`: Khóa các mục quản lý hệ thống theo vai trò Administrator.

---

## 4. Kết quả nghiệm thu Acceptance Criteria (MS2-32)

- [x] **Acceptance Criteria Backend:** Setting/configuration service enforce typed schema, secret handling, checksum/version và import transaction có preview diff.
- [x] **Acceptance Criteria Frontend:** Settings UI render control theo value type, che secret, hiển thị import diff và yêu cầu confirmation/permission trước apply.
- [x] **Acceptance Criteria Tích hợp:**
  - Setting có key duy nhất, value type, schema validation, scope và metadata người cập nhật.
  - Secret không được lưu hoặc export như setting thường; giá trị nhạy cảm được che trên UI/log.
  - Export tạo package có version, checksum và chỉ chứa nhóm cấu hình được phép.
  - Import từ chối file sai schema/checksum/version, không thay đổi dữ liệu trước confirm.
  - Import confirm áp dụng transaction, ghi before/after và AuditLog đầy đủ.
- [x] **Checklist hoàn thành:** Toàn bộ hạng mục Backend, Frontend, Routing, và Happy-case tests đã được tích hợp và kiểm tra.
