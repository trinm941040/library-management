# AGENTS.md

## Phạm vi

Áp dụng cho toàn bộ prompt.

## Quy tắc thực hiện

- Requirement được liên kết trong prompt là nguồn yêu cầu duy nhất.
- Đọc `MANIFEST.md`, requirement và phạm vi ghi trong prompt trước khi mở code.
- Chỉ đọc module/file thuộc phạm vi và dependency trực tiếp; chỉ mở rộng bằng `rg` khi contract hoặc quan hệ chưa rõ.
- Đối chiếu code với acceptance criteria; bỏ qua phần đã đáp ứng, không làm lại hoặc refactor ngoài phạm vi.
- Backend enforce authentication/permission; frontend dùng cùng quyền cho menu, route và action khi có liên quan.
- Hoàn thành acceptance criteria, checklist và happy case còn thiếu.
- Không chạy `dotnet test`; backend chỉ cần `dotnet build` không lỗi.
- Không yêu cầu Docker; frontend chạy build/type-check/lint theo script hiện có.
- Phản hồi chỉ gồm file thay đổi, kiểm tra đã chạy, kết quả và blocker.

## Task sao khi hoàn thành
- Cập nhật lại trạng thái trong file `MANIFEST.md` về mã task