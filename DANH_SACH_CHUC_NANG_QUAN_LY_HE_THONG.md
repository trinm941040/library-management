# BÁO CÁO TOÀN DIỆN: DANH SÁCH CHỨC NĂNG PHÂN HỆ QUẢN LÝ HỆ THỐNG
# (KÈM ĐẶC TẢ CHI TIẾT FRONTEND VÀ BACKEND .NET CLEAN ARCHITECTURE)

> **Dự án:** Hệ thống Quản lý Thư viện (Library Management System)  
> **Người thực hiện:** Phụ trách Phân hệ Quản lý Hệ thống (Fullstack: Frontend + Backend)  
> **Định hướng chiến lược:**  
> 1. **Cấu hình là trọng tâm cốt lõi (Single Source of Truth):** Hoàn thiện trước tiên ở cả Frontend và Backend để các thành viên phụ trách **Quản lý Tác vụ** (Mượn/Trả, Đặt trước, Vi phạm) có API và Service đổ dữ liệu vào, không bị hardcode.  
> 2. **Nhật ký hoạt động (Audit Log):** Giám sát vết thay đổi tự động ở cả 2 tầng (Frontend UI + Backend Database).  
> 3. **Thông tin & Thiết lập khác:** Tinh gọn, vừa đủ dùng.

---

## 1. TỔNG QUAN 4 MỤC QUẢN LÝ HỆ THỐNG (THEO THỨ TỰ MENU SIDEBAR)

```
QUẢN LÝ HỆ THỐNG
├── 1. Cấu hình            [⭐ TRỌNG TÂM SỐ 1 - Backend API & Frontend kết nối Tác vụ]
├── 2. Nhật ký hoạt động   [⭐ TRỌNG TÂM SỐ 2 - Backend AuditLog Entity & Frontend UI]
├── 3. Thông tin           [🔹 VỪA ĐỦ DÙNG - Hồ sơ liên hệ & Chi nhánh]
└── 4. Thiết lập khác      [🔹 VỪA ĐỦ DÙNG - Cấu hình Email SMTP & Tự động sao lưu]
```

---

## 2. PHÂN HỆ 1: CẤU HÌNH HỆ THỐNG (`/system/config`) — [TRỌNG TÂM CỐT LÕI]

### 2.1. Nghiệp vụ quy định & Kết nối Quản lý Tác vụ
| Quy định nghiệp vụ | Mã biến | Kiểu dữ liệu | Giá trị mặc định | Kết nối sang Phân hệ Quản lý Tác vụ |
|:---|:---|:---:|:---:|:---|
| **Thời hạn mượn sách mặc định** | `maxDays` | `int` | `14` ngày | Form tạo phiếu mượn (`Borrowing`) tự động điền số ngày mượn. |
| **Số sách mượn tối đa / độc giả** | `maxBooks` | `int` | `5` cuốn | `BorrowingService` kiểm tra hạn mức mượn của độc giả. |
| **Số lần gia hạn tối đa** | `maxRenewals` | `int` | `2` lần | Nút "Gia hạn" tự khóa khi vượt quá số lần quy định. |
| **Tự động chặn khi quá hạn** | `blockOverdue` | `bool` | `true` | Từ chối tạo phiếu mượn mới nếu độc giả còn sách quá hạn. |
| **Thời hạn giữ sách đặt trước** | `holdDays` | `int` | `3` ngày | `ReservationService` tính hạn chót nhận sách trước khi hủy. |
| **Đơn giá phạt quá hạn / ngày** | `finePerDay` | `decimal` | `5.000` VNĐ | Form Vi phạm tính: `Tiền phạt = Số ngày trễ × finePerDay`. |
| **Tỷ lệ đền bù làm mất / hỏng sách** | `lostBookPenaltyRatio`| `decimal` | `150` % | Form Vi phạm tính: `Tiền bồi thường = Giá bìa × (tỷ lệ / 100)`. |

### 2.2. Đặc tả Frontend (`ConfigPage.tsx` & `config-store.ts`)
* **Giao diện:** 2 Khối Card (Quy định mượn trả & Chính sách phạt).
* **Nút bấm hành động:**
  * **Lưu cấu hình (Save):** Áp dụng toàn bộ thay đổi vào hệ thống + Tự động ghi nhận vào Nhật ký hoạt động.
  * **Khôi phục mặc định (Reset to Defaults):** Đưa toàn bộ thông số về giá trị chuẩn khuyến nghị của thư viện.
  * **Sao lưu cấu hình (Backup Config):** Xuất toàn bộ gói cấu hình ra tệp tin chuẩn (`Cau_Hinh_Thu_Vien_YYYYMMDD.json`) để lưu trữ dự phòng.
  * **Phục hồi cấu hình (Restore Config):** Nạp tệp cấu hình từ máy tính để phục hồi dữ liệu nhanh chóng.

### 2.3. Đặc tả Backend .NET (Clean Architecture)
* **Domain Entity:** `SystemSetting.cs` (hoặc lưu dạng key-value JSON trong bảng Settings).
* **Application Service:** `ISystemConfigService` cung cấp phương thức:
  * `Task<CirculationConfigModel> GetCirculationConfigAsync(CancellationToken ct);`
  * `Task<SystemConfigResult> UpdateCirculationConfigAsync(UpdateCirculationConfigCommand cmd, CancellationToken ct);`
* **API Endpoints (`SystemConfigController.cs`):**
  * `GET /api/v1/system/config` -> Lấy toàn bộ tham số cấu hình.
  * `PUT /api/v1/system/config` -> Cập nhật tham số quy định mượn trả, phạt.
  * `POST /api/v1/system/config/reset` -> Khôi phục về cấu hình mặc định.
* **Cơ chế đổ dữ liệu sang Tác vụ:**
  * `BorrowingService` inject `ISystemConfigService` -> khi `command.LoanDays <= 0`, tự lấy `config.MaxDays`.
  * `ViolationService` inject `ISystemConfigService` -> tự động nhân `config.FinePerDay`.

---

## 3. PHÂN HỆ 2: NHẬT KÝ HOẠT ĐỘNG (`/system/activity-logs`) — [TRỌNG TÂM CỐT LÕI]

### 3.1. Nghiệp vụ Giám sát & Ghi vết
* Ghi nhận mọi hành động: Ai làm (`ActorUserId`, Tên, Email), Thao tác gì (`Action`), Trên phân hệ nào (`EntityType`), Thời điểm (`CreatedAtUtc`), Dữ liệu trước khi sửa (`BeforeJson`), Dữ liệu sau khi sửa (`AfterJson`).

### 3.2. Đặc tả Frontend (`ActivityLogPage.tsx` & `activity-log-store.ts`)
* **Bộ lọc 4 tiêu chí:**
  1. *Tìm kiếm:* Từ khóa tên nhân viên, email, hành động, địa chỉ IP.
  2. *Phân hệ (Module):* `Tất cả`, `Cấu hình`, `Mượn trả`, `Đặt trước`, `Vi phạm`, `Độc giả`, `Kho sách`, `Nhân viên`, `Tài khoản`.
  3. *Trạng thái:* `Tất cả`, `Thành công` (Xanh), `Cảnh báo` (Vàng), `Thất bại` (Đỏ).
  4. *Khoảng thời gian:* Từ ngày -> Đến ngày.
* **Bảng dữ liệu & Phân trang:** 10 bản ghi/trang, có nút Trước / Sau.
* **Modal Xem chi tiết (Audit Detail Dialog):** Popup so sánh trực quan các trường dữ liệu thay đổi Before / After.
* **Xuất CSV Tiếng Việt:** Xuất file `nhat_ky_hoat_dong.csv` định dạng **UTF-8 with BOM**, mở Excel tiếng Việt chuẩn 100%.

### 3.3. Đặc tả Backend .NET (Clean Architecture)
* **Domain Entity:** Đã có sẵn entity `AuditLog.cs` trong `UTH.Library.Domain.Entities.AuditLog`.
* **API Endpoints (`AuditLogsController.cs`):**
  * `GET /api/v1/system/audit-logs?search=&module=&status=&from=&to=&pageNumber=1&pageSize=10` -> Trả về danh sách phân trang.
  * `GET /api/v1/system/audit-logs/{id}` -> Lấy chi tiết BeforeJson / AfterJson.
  * `GET /api/v1/system/audit-logs/export-csv` -> Tải file CSV xuất dữ liệu nhật ký.

---

## 4. PHÂN HỆ 3: THÔNG TIN THƯ VIỆN (`/system/info`) — [VỪA ĐỦ DÙNG]

### 4.1. Nghiệp vụ & Dữ liệu
* Quản lý thông tin định danh: Tên thư viện (`libName`), Email liên hệ (`libEmail`), Địa chỉ (`libAddress`), Hotline (`libPhone`), Giờ mở cửa (`libHours`).
* Tích hợp: Tự động hiển thị trên thanh Header, Footer và đầu Phiếu in mượn trả.

### 4.2. Đặc tả Frontend (`InfoPage.tsx`)
* Form nhập 5 trường thông tin cơ bản.
* Nút **"Lưu thông tin"** hiển thị toast thành công và tự động ghi log sang Phân hệ 2.

### 4.3. Đặc tả Backend
* **API Endpoints:**
  * `GET /api/v1/system/info` -> Lấy thông tin thư viện.
  * `PUT /api/v1/system/info` -> Cập nhật thông tin thư viện (tự động ghi bản ghi vào bảng `AuditLogs`).

---

## 5. PHÂN HỆ 4: THIẾT LẬP KHÁC (`/system/other-settings`) — [VỪA ĐỦ DÙNG]

### 5.1. Nghiệp vụ & Dữ liệu
* **Cấu hình Email (SMTP):** Máy chủ SMTP (`smtpHost`), Cổng (`smtpPort`), Email hệ thống (`smtpUser`) để gửi email thông báo mượn/trả/quá hạn cho độc giả.
* **Chính sách Sao lưu (Backup):** Tự động sao lưu (`autoBackup`: true/false), Thời gian lưu trữ bản sao (`backupRetention`: số ngày).

### 5.2. Đặc tả Frontend (`OtherSettingsPage.tsx`)
* Card Email Server + Card Sao lưu định kỳ.
* Nút **"Lưu cài đặt"** hiển thị toast thành công và tự động ghi log sang Phân hệ 2.

### 5.3. Đặc tả Backend
* **API Endpoints:**
  * `GET /api/v1/system/other-settings` -> Lấy cấu hình SMTP & Backup.
  * `PUT /api/v1/system/other-settings` -> Cập nhật cài đặt (tự động ghi bản ghi vào bảng `AuditLogs`).

---

## 6. SƠ ĐỒ ĐỔ DỮ LIỆU CẤU HÌNH VÀO PHÂN HỆ "QUẢN LÝ TÁC VỤ"

```
                       ┌──────────────────────────────────────────────┐
                       │       BACKEND: SystemConfigService           │
                       │       FRONTEND: config-store.ts              │
                       │         (Single Source of Truth)             │
                       └───────────────────────┬──────────────────────┘
                                               │
             ┌─────────────────────────────────┼─────────────────────────────────┐
             ▼                                 ▼                                 ▼
   ┌───────────────────┐             ┌───────────────────┐             ┌───────────────────┐
   │ QUẢN LÝ MƯỢN/TRẢ  │             │ QUẢN LÝ ĐẶT TRƯỚC │             │  QUẢN LÝ VI PHẠM  │
   │ (Borrowings)      │             │ (Reservations)    │             │  (Violations)     │
   ├───────────────────┤             ├───────────────────┤             ├───────────────────┤
   │• maxDays (ngày)   │             │• holdDays (hạn    │             │• finePerDay       │
   │• maxBooks (sách)  │             │  giữ sách đặt)    │             │  (tiền phạt/ngày) │
   │• blockOverdue     │             │                   │             │• lostBookPenalty  │
   └───────────────────┘             └───────────────────┘             └───────────────────┘
             │                                 │                                 │
             └─────────────────────────────────┼─────────────────────────────────┘
                                               ▼
                       ┌──────────────────────────────────────────────┐
                       │         NHẬT KÝ HOẠT ĐỘNG (Audit Log)        │
                       │       Tự động ghi nhận mọi biến động         │
                       └──────────────────────────────────────────────┘
```

---

## 7. BẢNG TIẾN ĐỘ THỰC HIỆN TOÀN DIỆN (FRONTEND + BACKEND)

| Thứ tự | Hạng mục | Tệp tin Frontend | Tệp tin Backend | Trạng thái |
|:---:|:---|:---|:---|:---:|
| **1** | **Cấu hình hệ thống** | `src/pages/config/ConfigPage.tsx`<br>`src/pages/config/config-store.ts` | `Controllers/SystemConfigController.cs`<br>`Features/SystemConfig/` | **Frontend xong UI**<br>Sẵn sàng làm Backend |
| **2** | **Nhật ký hoạt động** | `src/pages/activity-logs/ActivityLogPage.tsx`<br>`src/pages/activity-logs/activity-log-store.ts` | `Controllers/AuditLogsController.cs`<br>`Domain/Entities/AuditLog.cs` | **Frontend có store**<br>Entity Backend đã có |
| **3** | **Thông tin thư viện** | `src/pages/info/InfoPage.tsx` | Đính kèm `SystemConfigController` | Sẵn sàng hoàn thiện |
| **4** | **Thiết lập khác** | `src/pages/other-settings/OtherSettingsPage.tsx` | Đính kèm `SystemConfigController` | Sẵn sàng hoàn thiện |
| **-** | **Đổ dữ liệu Tác vụ** | `BorrowingFormDialog.tsx`<br>`ViolationFormDialog.tsx` | `BorrowingService.cs`<br>`ViolationService.cs` | Sẵn sàng tích hợp |
