# Quy tắc thực hiện chung

- Requirement được liên kết trong từng prompt là nguồn yêu cầu duy nhất; không lặp lại nội dung requirement trong prompt.
- Đọc `IMPLEMENTATION-STATUS.md`, requirement và đúng phạm vi ghi trong prompt trước khi mở code.
- Chỉ đọc module/file thuộc phạm vi và dependency trực tiếp; chỉ mở rộng bằng `rg` khi contract hoặc quan hệ chưa rõ.
- Đối chiếu code với acceptance criteria; phần đã đáp ứng thì bỏ qua, không viết lại hoặc refactor ngoài phạm vi.
- Backend phải enforce authentication/permission; frontend áp dụng cùng quyền cho menu, route và action khi requirement liên quan.
- Hoàn thành phần acceptance criteria, checklist và happy case còn thiếu.
- Không chạy `dotnet test`; backend chỉ cần `dotnet build` không lỗi.
- Không yêu cầu Docker; frontend chạy build/type-check/lint theo script hiện có.
- Phản hồi chỉ gồm file thay đổi, kiểm tra đã chạy, kết quả và blocker.
