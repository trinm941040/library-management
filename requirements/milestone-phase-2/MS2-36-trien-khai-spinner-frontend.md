# MS2-36 — Triển khai spinner dùng chung cho frontend

- Phân loại: Frontend / Feature / UI / Improvement
- Mức ưu tiên: P1
- Phụ thuộc: MS2-04

## Mô tả chi tiết

- Chỉ triển khai phần còn thiếu; spinner hoặc loading state đã đáp ứng acceptance criteria thì tái sử dụng, không viết lại.

Xây dựng spinner dùng chung theo kiến trúc UI hiện có và áp dụng thống nhất cho trạng thái tải trang, tải vùng dữ liệu và thao tác bất đồng bộ. Spinner phải sử dụng design token, hỗ trợ accessibility, responsive, dark/light theme và không làm thay đổi bố cục khi xuất hiện.

## Acceptance criteria

### Frontend

- [ ] Có component `Spinner` dùng chung với tối thiểu các kích thước `sm`, `md`, `lg`; màu sắc lấy từ design token hoặc kế thừa màu của thành phần cha.
- [ ] Spinner trang/vùng dữ liệu có accessible name, `role="status"` hoặc cơ chế tương đương; vùng đang tải có `aria-busy="true"`.
- [ ] Spinner chỉ mang tính trang trí trong button phải dùng `aria-hidden="true"`; button cung cấp nhãn trạng thái tải cho screen reader.
- [ ] Button loading bị vô hiệu hóa, ngăn submit lặp và giữ nguyên chiều rộng khi spinner thay thế hoặc đứng cạnh nhãn.
- [ ] Có wrapper dùng chung cho loading toàn vùng, inline và overlay; overlay chỉ chặn tương tác trong phạm vi đang xử lý.
- [ ] Animation tôn trọng `prefers-reduced-motion` và không gây nhấp nháy mạnh.
- [ ] Spinner hiển thị đúng trên desktop/mobile, light/dark theme và không gây layout shift.
- [ ] Không hiển thị nhiều spinner cho cùng một request hoặc dùng full-page spinner cho background refetch không chặn thao tác.
- [ ] Các spinner/loading indicator tự phát trùng lặp được thay bằng component dùng chung khi nằm trong phạm vi task.

## Checklist hoàn thành

### Frontend

- [ ] Rà soát component, CSS, icon và loading state hiện có trước khi triển khai.
- [ ] Hoàn thiện `Spinner` và wrapper loading trong shared UI theo convention/export hiện có.
- [ ] Tích hợp button loading vào Button/Form action dùng chung nếu repository có component tương ứng.
- [ ] Áp dụng cho route-level loading, vùng dữ liệu chính và mutation quan trọng đang dùng loading indicator không thống nhất.
- [ ] Bổ sung semantic token cần thiết; không hard-code màu trái design system.
- [ ] Kiểm tra keyboard, screen reader semantics, reduced motion, responsive và theme.
- [ ] Loại bỏ import/loading markup trùng lặp sau khi chuyển đổi.

## Happy-case test

1. Mở trang có dữ liệu tải bất đồng bộ và xác nhận spinner xuất hiện trong đúng vùng rồi biến mất khi tải xong.
2. Submit form và xác nhận button hiển thị loading, giữ chiều rộng và không thể submit lặp.
3. Thực hiện background refetch và xác nhận giao diện vẫn sử dụng được, không xuất hiện full-page spinner.
4. Bật dark theme và reduced motion, xác nhận spinner dễ nhìn và animation phù hợp.
5. Kiểm tra bằng bàn phím và accessibility tree, xác nhận trạng thái tải được thông báo đúng nhưng không lặp.

## Build test local

- [ ] Frontend type-check và lint thành công theo script hiện có.
- [ ] `pnpm build` thành công.
- [ ] Không yêu cầu Docker hoặc bổ sung unit test.
