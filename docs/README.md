# Library Management documentation

Đây là nơi lưu trữ tài liệu dùng chung cho toàn bộ repository. Không tạo thêm thư mục
documentation riêng bên trong `Backend` hoặc `Frontend`.

## Tài liệu chung

- [Quy trình Issue, branch và Pull Request](issue-and-pull-request-guideline.md)

## Backend

- [Tổng quan, kiến trúc và hướng dẫn chạy Backend](backend/README.md)
- [Yêu cầu Authentication và Authorization](backend/tasks/auth-requirements.md)
- [Employee Management](backend/tasks/employee-management.md)
- [Password hashing với Argon2id](backend/tasks/improve-password-security-argon2.md)
- [JWT RSA PEM key pair](backend/tasks/jwt-rsa-pem-key-pair.md)
- [PostgreSQL runtime](backend/tasks/postgresql-runtime.md)
- [Role và Permission Management API](backend/tasks/role-permission-management-api.md)
- [User Management API](backend/tasks/user-management-api.md)

## Frontend

- [Tổng quan và cấu trúc Frontend](frontend/README.md)
- [Development workflow](frontend/guides/development-workflow.md)
- [Naming conventions](frontend/guides/naming-conventions.md)
- [Git branching strategy](frontend/guides/git-branching-strategy.md)
- [UI kit guide](frontend/guides/ui-kit-guide.md)
- [Dialog customization](frontend/guides/dialog-customization.md)

## Quy ước vị trí

```text
Library_Management/
├── Backend/
├── Frontend/
└── docs/
    ├── README.md
    ├── backend/
    │   ├── README.md
    │   └── tasks/
    ├── frontend/
    │   ├── README.md
    │   └── guides/
    └── issue-and-pull-request-guideline.md
```

- Tài liệu áp dụng cho toàn dự án đặt trực tiếp trong `docs`.
- Tài liệu chỉ dành cho một source đặt trong `docs/backend` hoặc `docs/frontend`.
- Task và quyết định kỹ thuật Backend đặt trong `docs/backend/tasks`.
- Guide Frontend đặt trong `docs/frontend/guides`.
- Khi thêm, xóa hoặc đổi tên tài liệu, cập nhật mục lục này trong cùng thay đổi.
