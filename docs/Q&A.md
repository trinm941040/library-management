# Library Management Project — Q&A

Tài liệu ghi lại các câu hỏi và câu trả lời kỹ thuật về codebase. Các câu trả lời lấy source code hiện tại làm nguồn chính.

---

## Q001 — Private method `Issue` trong controller dùng để làm gì?

### Câu hỏi

> Tôi thấy trong mỗi controller có private method là `Issue`. Method này dùng để làm gì, vì sao phải có nó, nếu không phải nó thì thế nào, điểm mạnh và điểm yếu khi có nó?

### Trả lời ngắn

Trong code hiện tại, private method `Issue(...)` chỉ xuất hiện trong `AuthController`, không phải trong mỗi controller. Nó là helper dùng chung cho hai endpoint `login` và `refresh` để biến kết quả từ `AuthService` thành HTTP response:

```text
AuthService result
  |
  +--> thất bại/null
  |      -> xóa refresh cookie cũ
  |      -> trả 401 ProblemDetails
  |
  +--> thành công
         -> ghi raw refresh token vào HttpOnly cookie
         -> chỉ trả access token + thời hạn + currentUser trong JSON
         -> trả 200 OK
```

Method nằm tại:

```text
Backend/src/UTH.Library.Api/Controllers/AuthController.cs
```

Hai nơi gọi nó:

```csharp
return Issue(await authService.LoginAsync(...));
return Issue(await authService.RefreshAsync(...));
```

### Logic cụ thể

Chữ ký hiện tại:

```csharp
private IActionResult Issue(
    (bool Succeeded, string? Error, AuthResult? Result) result)
```

Nếu authentication thất bại hoặc `Result` là `null`:

```text
Issue
  -> AuthenticationFailed(...)
  -> DeleteRefreshCookie()
  -> 401 Unauthorized
  -> ProblemDetails:
       code = authentication.failed
       correlationId = request trace id
```

Nếu thành công:

```text
Issue
  -> Response.Cookies.Append("uth_refresh", rawRefreshToken, options)
       HttpOnly = true
       Secure = true ngoài Development
       SameSite = Strict
       Path = /api/v1/auth
       Expires = refresh-token expiry
  -> 200 OK SessionResponse
       accessToken
       accessTokenExpiresAtUtc
       currentUser
```

Raw refresh token không được đưa vào JSON. JavaScript không thể đọc cookie vì cookie có `HttpOnly`. Access token được frontend nhận trong JSON và chỉ giữ trong RAM.

### Vì sao cần có `Issue(...)`?

`login` và `refresh` có đầu vào khác nhau nhưng đầu ra thành công giống nhau:

```text
LOGIN
  email/password
       |
       v
  AuthResult
       |
       v
     Issue

REFRESH
  refresh cookie
       |
       v
  AuthResult
       |
       v
     Issue
```

Nếu không gom vào helper, cả hai action phải lặp lại cùng logic:

```csharp
if (!result.Succeeded || result.Result is null)
    return AuthenticationFailed(...);

Response.Cookies.Append(...);
return Ok(new SessionResponse(...));
```

Phần bị lặp không chỉ là mapping response mà còn là chính sách bảo mật của refresh cookie. Nếu một endpoint cấu hình cookie khác endpoint còn lại, session có thể hoạt động không nhất quán.

### Nếu xóa `Issue(...)` thì điều gì xảy ra?

Nếu chỉ xóa method mà không thay code gọi:

```text
Backend không compile vì Login() và Refresh() vẫn gọi Issue(...).
```

Nếu thay bằng code viết trực tiếp trong từng action:

```text
Hệ thống vẫn có thể hoạt động giống hiện tại
NHƯNG
logic cookie, lỗi 401 và SessionResponse bị nhân đôi.
```

Ví dụ rủi ro sau khi viết lặp:

```text
Login đặt SameSite=Strict
Refresh vô tình đặt SameSite=Lax

hoặc

Login không trả refresh token trong JSON
Refresh vô tình trả cả refresh token

hoặc

Login xóa cookie khi thất bại
Refresh quên xóa cookie hỏng
```

Có thể thay `Issue(...)` bằng một abstraction khác, không bắt buộc phải giữ đúng method này:

```text
1. Viết inline trong Login và Refresh
   -> đơn giản nhưng lặp code.

2. Đổi thành helper có tên rõ hơn
   -> WriteSessionResponse(...)
   -> ToSessionActionResult(...)

3. Tách cookie handling thành IRefreshCookieService
   -> controller gọi cookieService.Append/Delete
   -> dễ unit test và thay chính sách cookie.

4. Dùng typed result thay tuple
   -> AuthenticationResult.Success(...)
   -> AuthenticationResult.Failure(...)
   -> rõ contract hơn bool + nullable values.
```

Không nên chuyển trực tiếp việc ghi HTTP cookie vào `AuthService`, vì `AuthService` thuộc Infrastructure/identity logic và hiện không phụ thuộc vào `HttpContext`. Để controller hoặc một HTTP-layer cookie service xử lý cookie sẽ giữ ranh giới kiến trúc sạch hơn.

### Điểm mạnh

1. Giảm lặp code giữa `login` và `refresh`.
2. Đảm bảo hai endpoint dùng cùng chính sách refresh cookie.
3. Đảm bảo raw refresh token chỉ đi vào HttpOnly cookie, không xuất hiện trong response body.
4. Chuẩn hóa `SessionResponse` trả về frontend.
5. Chuẩn hóa failure flow: xóa cookie không hợp lệ và trả cùng dạng `401 ProblemDetails`.
6. Giữ `Login()` và `Refresh()` ngắn, dễ đọc: action chỉ xử lý phần đầu vào riêng rồi chuyển kết quả cho helper.
7. Đặt HTTP concern đúng tầng controller: service tạo token, controller quyết định cookie và HTTP response.

### Điểm yếu

1. Tên `Issue` hơi mơ hồ. Người đọc có thể hiểu là “phát hành token”, “phát sinh vấn đề”, hoặc nhầm với `MembershipCard.Issue`. `WriteSessionResponse` sẽ rõ hơn.
2. Method nhận một tuple `(bool, string?, AuthResult?)`. Contract này cho phép trạng thái vô lý, ví dụ `Succeeded=true` nhưng `Result=null`.
3. Method vừa map response vừa tạo side effect trên `Response.Cookies`, nên không phải hàm chuyển đổi thuần túy.
4. Nó phụ thuộc trực tiếp vào `HttpContext`, `IWebHostEnvironment`, cookie name/path và DTO API; unit test riêng sẽ cần dựng controller context.
5. Chính sách cookie bị hard-code trong controller. Nếu sau này có nhiều controller hoặc nhiều loại client cần phát session, logic khó tái sử dụng ngoài `AuthController`.
6. `Secure` phụ thuộc vào `IsDevelopment()`, không dựa trực tiếp vào request scheme hay một typed cookie option; việc triển khai qua proxy cần cấu hình môi trường/forwarded headers đúng.
7. Helper đang gộp cả success và failure mapping. Khi auth result cần thêm trạng thái như MFA required, password expired hoặc account onboarding, tuple và method này sẽ phình ra.

### Đánh giá cho project hiện tại

Với chỉ hai caller là `Login()` và `Refresh()`, việc có helper là hợp lý. Nó ngắn, giữ cookie policy nhất quán và không cần tạo thêm service chỉ để tránh vài dòng lặp.

Cải tiến phù hợp nhất nếu refactor sau này:

```text
Issue(...)                         -> WriteSessionResponse(...)
(bool, string?, AuthResult?)       -> typed AuthenticationResult
hard-coded CookieOptions           -> typed RefreshCookieOptions
```

Không cần loại bỏ helper. Nên đổi tên và cải thiện kiểu kết quả trước khi hệ thống có thêm MFA hoặc nhiều kiểu phiên đăng nhập.

### Không nhầm với `MembershipCard.Issue(...)`

Project còn có:

```text
Backend/src/UTH.Library.Domain/Entities/MembershipCard.cs
public static MembershipCard Issue(...)
```

Đây là factory method của domain dùng để tạo thẻ độc giả mới. Nó không liên quan đến JWT, cookie hay HTTP response.

---

## Q002 — Project có đang sử dụng CQRS không? Nếu có thì hoàn toàn hay một phần?

### Trả lời ngắn

**Có, nhưng chỉ sử dụng một phần rất nhỏ.** Cách gọi chính xác nhất là **partial CQRS** hoặc **CQRS-inspired**, không phải CQRS toàn phần.

Phần CQRS thực sự hiện chỉ xuất hiện rõ ở một số use case của module Books:

- `BookListQuery` triển khai `IQuery<BookPageModel>`.
- `CreateBookCommand` triển khai `ICommand<BookResult>`.
- `BookService` triển khai cả query handler và command handler tương ứng.
- `BooksController` gọi hai handler cho `GET /books` và `POST /books`.
- `KioskController` tái sử dụng query handler để tìm sách công khai.

Phần lớn endpoint còn lại vẫn đi theo kiến trúc truyền thống:

```text
Controller
   -> concrete Application Service
      -> Repository / Unit of Work
         -> cùng một DbContext / database
```

### CQRS là gì trong trường hợp này?

CQRS — Command Query Responsibility Segregation — tách hai loại thao tác về mặt trách nhiệm:

```text
Query   = đọc dữ liệu, không làm thay đổi trạng thái
Command = yêu cầu thay đổi trạng thái hệ thống
```

CQRS không bắt buộc phải dùng hai database. Điểm cốt lõi là tách rõ mô hình và luồng xử lý đọc/ghi. Hai kho dữ liệu hoặc read projection riêng chỉ là mức triển khai cao hơn khi hệ thống thực sự cần.

### Bằng chứng CQRS trong code

Các abstraction CQRS được định nghĩa tại:

```text
Backend/src/UTH.Library.Application/Common/Messaging.cs
```

Trong đó có:

```csharp
ICommand<TResult>
IQuery<TResult>
ICommandHandler<TCommand, TResult>
IQueryHandler<TQuery, TResult>
```

Module Books sử dụng các contract này:

```text
Backend/src/UTH.Library.Application/Features/Books/BookModels.cs
Backend/src/UTH.Library.Application/Features/Books/BookService.cs
```

Luồng query danh sách sách:

```text
GET /api/books
   -> BooksController
   -> IQueryHandler<BookListQuery, BookPageModel>.HandleAsync(query)
   -> BookService.GetAsync(query)
   -> repository đọc dữ liệu
   -> BookPageModel
   -> HTTP 200
```

Luồng tạo sách:

```text
POST /api/books
   -> BooksController
   -> ICommandHandler<CreateBookCommand, BookResult>.HandleAsync(command)
   -> BookService.CreateAsync(command)
   -> repository / Unit of Work ghi dữ liệu
   -> BookResult
   -> HTTP response
```

Các handler được đăng ký DI tại:

```text
Backend/src/UTH.Library.Application/DependencyInjection.cs
```

Controller sử dụng chúng tại:

```text
Backend/src/UTH.Library.Api/Controllers/BooksController.cs
Backend/src/UTH.Library.Api/Controllers/KioskController.cs
```

### Vì sao đây không phải CQRS toàn phần?

#### 1. Chỉ Books dùng interface CQRS thật sự

Project có nhiều record mang tên `...Command` và `...Query`, chẳng hạn các model của borrowing, reservation, fine payment, employee và inventory audit. Tuy nhiên, chỉ đặt tên DTO là `Command` hoặc `Query` **không làm cho luồng đó trở thành CQRS**.

Các model này phần lớn:

- không triển khai `ICommand<TResult>` hoặc `IQuery<TResult>`;
- không có class handler tương ứng;
- được controller truyền trực tiếp vào method của service cụ thể.

Ví dụ luồng phổ biến vẫn là:

```text
BorrowingsController
   -> BorrowingService.GetAsync(BorrowingListQuery)
   -> repository
```

Tên `BorrowingListQuery` thể hiện ý nghĩa request, nhưng luồng này chưa dùng CQRS abstraction của project.

#### 2. Không có handler riêng cho từng use case

Trong cách CQRS rõ ràng hơn, mỗi use case thường có handler độc lập, ví dụ:

```text
CreateBookCommandHandler
UpdateBookCommandHandler
DeleteBookCommandHandler
GetBookByIdQueryHandler
ListBooksQueryHandler
```

Project hiện dùng một `BookService` lớn để vừa xử lý query, vừa xử lý command. Hai interface handler cuối cùng vẫn trỏ về cùng một instance `BookService`.

#### 3. Ngay trong Books cũng chỉ một số action đi qua handler

Hai luồng nổi bật là list và create. Các thao tác Books khác như lấy chi tiết, cập nhật, xóa, import hoặc semantic search vẫn chủ yếu gọi method service trực tiếp. Vì vậy Books cũng chưa được chuyển hoàn toàn sang CQRS.

#### 4. Không có mediator/dispatcher trung tâm

Project không dùng MediatR hoặc một command/query bus tương đương. Controller inject trực tiếp generic handler hoặc concrete service. Mediator không phải điều kiện bắt buộc của CQRS, nhưng việc không có dispatcher cùng với phạm vi handler rất nhỏ cho thấy CQRS chưa phải kiến trúc bao phủ toàn application.

#### 5. Read side và write side vẫn dùng chung hạ tầng

Hai phía hiện vẫn chia sẻ:

- cùng `BookService`;
- cùng repository và Unit of Work;
- cùng Entity Framework `DbContext`;
- cùng database và phần lớn domain model.

Do đó project mới tách trách nhiệm ở contract/call site của một vài use case, chưa tách read model và write model một cách hệ thống.

### Bảng đánh giá mức độ áp dụng

| Tiêu chí | Hiện trạng |
|---|---|
| Có marker `ICommand` / `IQuery` | Có |
| Có command/query handler | Có, chủ yếu cho Books list/create |
| Controller gọi handler | Có, tại một phần Books và Kiosk |
| Mỗi use case có handler riêng | Không |
| Toàn bộ module dùng CQRS nhất quán | Không |
| Có mediator/command bus/query bus | Không |
| Tách read model và write model toàn hệ thống | Không |
| Tách database đọc và ghi | Không |
| Có thể gọi là full CQRS | Không |

### Điểm mạnh của cách áp dụng một phần hiện tại

1. Có thể thử nghiệm CQRS trên một phạm vi nhỏ mà không phải viết lại toàn hệ thống.
2. Controller phụ thuộc vào contract của use case thay vì toàn bộ `BookService` đối với hai endpoint này.
3. Query và command được thể hiện rõ về mặt ý định.
4. Dễ unit test handler contract và có đường mở rộng nếu sau này muốn chuyển từng module.

### Điểm yếu và rủi ro

1. Kiến trúc không nhất quán: có controller inject handler, có controller inject service.
2. Tên `Command`/`Query` ở nhiều module có thể khiến người đọc tưởng toàn project dùng CQRS dù thực tế không phải.
3. `BookService` xử lý cả đọc lẫn ghi nên mức phân tách còn yếu.
4. Đăng ký DI thủ công sẽ dài hơn nếu thêm nhiều handler.
5. Nếu không có quy ước chuyển đổi rõ ràng, abstraction CQRS trở thành một lớp trung gian không đem lại nhiều lợi ích.

### Kết luận

```text
Kiến trúc chính của project:
Clean/layered architecture + Application Service + Repository/Unit of Work

CQRS:
Đã có abstraction và được dùng thử cho một phần nhỏ của Books,
nhưng chưa được áp dụng nhất quán trên module Books hoặc toàn hệ thống.
```

Với quy mô một library management monolith, full CQRS không nhất thiết cần thiết. Project nên chọn một trong hai hướng rõ ràng:

1. Giữ service-oriented architecture hiện tại và xem `Command`/`Query` là request model, tránh tuyên bố toàn hệ thống dùng CQRS.
2. Nếu CQRS là mục tiêu học tập hoặc yêu cầu kiến trúc, chuyển dần theo từng module: mỗi use case có command/query contract và handler riêng; mediator có thể thêm sau nếu số lượng handler đủ lớn. Chưa cần tách database đọc/ghi nếu chưa có nhu cầu hiệu năng hoặc scale.

---

## Q003 — Project đang áp dụng những design pattern nào, ở đâu và vì sao phải áp dụng?

### Trả lời tổng quan

Project áp dụng nhiều pattern ở các mức khác nhau. Những pattern thể hiện rõ trong code gồm:

1. Repository.
2. Unit of Work.
3. Dependency Injection / Inversion of Control.
4. Static Factory Method.
5. Adapter kết hợp Strategy.
6. Outbox và Background Worker.
7. Middleware Pipeline / Chain of Responsibility.
8. Interceptor.
9. CQRS một phần.
10. Publish–Subscribe/Observer qua SignalR.
11. Null Object ở phạm vi hỗ trợ unit test.
12. Options pattern và policy-based authorization của ASP.NET Core.
13. Service Layer cùng Dependency Inversion trong Clean Architecture.

Không phải tất cả đều là GoF design pattern cổ điển. Repository, Unit of Work, Outbox và CQRS là enterprise/application patterns; Middleware, Options và policy authorization là framework patterns; Clean Architecture là architectural style.

## 1. Repository Pattern

### Áp dụng ở đâu?

Contract nằm trong Application:

```text
Backend/src/UTH.Library.Application/Abstractions/Persistence/
```

Ví dụ:

```text
IBookRepository
IBorrowingRepository
IReservationRepository
IMemberRepository
INotificationRepository
IReportRepository
```

Implementation nằm trong Infrastructure:

```text
Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/
```

Ví dụ:

```csharp
public sealed class BookRepository : IBookRepository
```

Application service chỉ biết interface:

```text
BookService -> IBookRepository
BorrowingService -> IBorrowingRepository
NotificationService -> INotificationRepository
```

### Vì sao áp dụng?

- Che giấu câu lệnh EF Core và chi tiết database khỏi business logic.
- Application không phụ thuộc trực tiếp vào `LibraryDbContext`.
- Gom query liên quan một aggregate/module về một nơi.
- Có thể thay repository thật bằng fake/mock khi unit test.
- Thực thi Dependency Inversion: lớp nghiệp vụ phụ thuộc abstraction, Infrastructure cung cấp implementation.

### Điểm cần lưu ý

EF Core `DbSet` đã mang một phần đặc điểm Repository. Custom repository vẫn hữu ích vì project cần query nghiệp vụ, nhưng repository quá lớn sẽ dễ trở thành nơi chứa lẫn business logic.

## 2. Unit of Work Pattern

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Application/Abstractions/Persistence/IUnitOfWork.cs
Backend/src/UTH.Library.Infrastructure/Persistence/UnitOfWork.cs
```

Được dùng trong các service như:

```text
BookService
EmployeeService
CopyService
LocationService
StockReceiptService
InventoryAuditService
```

Luồng điển hình:

```text
Application Service
   -> unitOfWork.ExecuteAsync(...)
      -> bắt đầu transaction
      -> nhiều repository/entity operation
      -> SaveChangesAsync
      -> commit

Nếu có exception
      -> rollback
```

`UnitOfWork` còn chuyển `DbUpdateConcurrencyException` và PostgreSQL unique violation thành application exception dễ hiểu hơn.

### Vì sao áp dụng?

- Bảo đảm một use case gồm nhiều thao tác ghi là atomic.
- Commit một lần khi toàn bộ nghiệp vụ thành công.
- Rollback khi bất kỳ bước nào thất bại.
- Chuẩn hóa xử lý concurrency và conflict database.
- Không để service phải tự biết API transaction của EF Core.

### Điểm cần lưu ý

`DbContext` tự thân đã là một Unit of Work. Wrapper của project có giá trị vì bổ sung transaction callback và exception mapping, nhưng cần tránh tạo thêm abstraction nếu chỉ gọi lại `SaveChangesAsync` mà không thêm hành vi.

## 3. Dependency Injection và Inversion of Control

### Áp dụng ở đâu?

Các registration tập trung tại:

```text
Backend/src/UTH.Library.Api/DependencyInjection.cs
Backend/src/UTH.Library.Application/DependencyInjection.cs
Backend/src/UTH.Library.Infrastructure/DependencyInjection.cs
Backend/src/UTH.Library.Api/Program.cs
```

Ví dụ:

```csharp
services.AddScoped<IBookRepository, BookRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<INotificationService, NotificationService>();
services.AddSingleton<IJwtTokenService, JwtTokenService>();
```

Dependencies được đưa vào bằng constructor thay vì class tự `new` chúng.

### Vì sao áp dụng?

- Giảm coupling giữa Application và Infrastructure.
- Cho phép thay implementation theo môi trường hoặc khi test.
- Container quản lý lifecycle `Scoped`, `Singleton` và hosted service.
- Dependency graph được cấu hình tại composition root thay vì rải khắp code.

`AddSingleton` ở đây là **DI lifetime**, không đồng nghĩa class tự triển khai classic Singleton bằng private constructor và `GetInstance()`.

## 4. Static Factory Method

### Áp dụng ở đâu?

Nhiều domain entity dùng private constructor và public static factory:

```text
Book.Create(...)
Borrowing.Create(...)
Reservation.Create(...)
Notification.Create(...)
AuditLog.Create(...)
MembershipCard.Issue(...)
IsbnValue.Create(...)
```

Các file nằm chủ yếu tại:

```text
Backend/src/UTH.Library.Domain/Entities/
Backend/src/UTH.Library.Domain/ValueObjects/IsbnValue.cs
```

Infrastructure còn có factory kỹ thuật:

```text
Backend/src/UTH.Library.Infrastructure/Notifications/SmtpServices.cs
SmtpMessageFactory.CreateClient(...)
SmtpMessageFactory.Create(...)
```

### Vì sao áp dụng?

- Không cho tạo entity ở trạng thái thiếu hoặc không hợp lệ.
- Validation, normalize dữ liệu, sinh ID và trạng thái ban đầu được tập trung.
- Tên `Create`, `Issue`, `CreateWithCopy` diễn đạt intent tốt hơn constructor dài.
- Có thể thay đổi quy trình khởi tạo mà không buộc caller biết chi tiết.

Ví dụ `Book.Create` kiểm tra đầu vào, chuẩn hóa ISBN, sinh `Guid` và concurrency token trước khi trả entity.

## 5. Adapter Pattern kết hợp Strategy Pattern

### Áp dụng ở đâu?

Contract:

```text
Backend/src/UTH.Library.Application/Features/Notifications/Adapters/INotificationSenderAdapter.cs
```

Implementations:

```text
Backend/src/UTH.Library.Infrastructure/Notifications/NotificationSenderAdapters.cs

InAppNotificationSenderAdapter
EmailNotificationSenderAdapter
```

`NotificationService` nhận toàn bộ adapter:

```csharp
IEnumerable<INotificationSenderAdapter> adapters
```

Sau đó chọn implementation theo:

```csharp
adapter.Channel == template.Channel
```

### Tại sao vừa là Adapter vừa là Strategy?

- **Adapter:** bọc cách gửi cụ thể như SMTP hoặc in-app sau một interface thống nhất `SendAsync`.
- **Strategy:** lúc runtime, service chọn thuật toán/kênh gửi phù hợp dựa trên `NotificationChannel`.

Luồng:

```text
NotificationService
   -> chọn adapter theo Channel
      -> InApp adapter
      hoặc Email adapter
```

### Vì sao áp dụng?

- Business logic không phụ thuộc trực tiếp `SmtpClient` hay SignalR.
- Thêm SMS/Push có thể bằng implementation mới thay vì sửa toàn bộ service.
- Mỗi kênh tự xử lý lỗi và kết quả gửi.
- Dễ fake sender khi test.

## 6. Transactional Outbox và Background Worker

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Infrastructure/Notifications/EmailOutboxWorker.cs
Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/NotificationRepository.cs
Backend/src/UTH.Library.Domain/Entities/Notification.cs
```

`NotificationService` tạo notification trong database. Với email, việc gửi không được thực hiện ngay trong HTTP request. `EmailOutboxWorker : BackgroundService` định kỳ:

```text
đọc notification Pending
   -> MarkProcessing
   -> chọn EmailNotificationSenderAdapter
   -> gửi SMTP
   -> MarkSent
      hoặc ScheduleRetry theo exponential backoff
      hoặc MarkFailed
```

### Vì sao áp dụng?

- HTTP request không phải chờ SMTP.
- Email không mất ngay khi SMTP tạm thời lỗi.
- Có trạng thái, số lần thử và lịch retry để theo dõi.
- Worker có thể xử lý theo batch.

### Mức độ triển khai

Đây là outbox dựa trên chính bảng notification, không phải generic integration-event outbox. Độ bảo đảm “transactional” còn phụ thuộc việc tạo notification và thay đổi nghiệp vụ gốc có cùng transaction hay không; không nên mặc định coi nó là exactly-once delivery.

## 7. Middleware Pipeline / Chain of Responsibility

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Api/Program.cs
Backend/src/UTH.Library.Api/Infrastructure/CorrelationIdMiddleware.cs
```

Pipeline hiện tại gồm các bước như:

```text
Forwarded Headers
   -> Exception Handler
   -> Correlation ID Middleware
   -> Rate Limiter
   -> Authentication
   -> Authorization
   -> Controller/Endpoint
```

Mỗi middleware xử lý một trách nhiệm rồi gọi `next(context)` để chuyển request cho mắt xích kế tiếp.

### Vì sao áp dụng?

- Xử lý cross-cutting concern một lần cho toàn bộ API.
- Không lặp authentication, correlation ID, rate limit hoặc exception handling trong controller.
- Thứ tự pipeline thể hiện rõ thứ tự xử lý request.
- Có thể chặn request sớm khi một bước thất bại.

Đây là biến thể pipeline/Chain of Responsibility do ASP.NET Core cung cấp.

## 8. Interceptor Pattern

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Infrastructure/Persistence/AuditSaveChangesInterceptor.cs
```

Interceptor được gắn vào EF Core tại `Infrastructure/DependencyInjection.cs`:

```csharp
options.AddInterceptors(...AuditSaveChangesInterceptor...);
```

Trước `SaveChanges`, interceptor:

- cập nhật row version của refresh-token session;
- đọc EF ChangeTracker;
- tạo audit log tự động;
- bổ sung correlation ID và IP;
- che dữ liệu nhạy cảm trong before/after JSON.

### Vì sao áp dụng?

- Audit là cross-cutting concern, không nên copy vào mọi service.
- Bắt thay đổi ở điểm tập trung ngay trước khi lưu database.
- Giảm nguy cơ developer quên ghi audit cho CRUD thông thường.
- Chính sách redaction được áp dụng nhất quán.

## 9. CQRS Pattern — áp dụng một phần

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Application/Common/Messaging.cs
Backend/src/UTH.Library.Application/Features/Books/BookModels.cs
Backend/src/UTH.Library.Application/Features/Books/BookService.cs
Backend/src/UTH.Library.Api/Controllers/BooksController.cs
Backend/src/UTH.Library.Api/Controllers/KioskController.cs
```

Hiện có:

```text
BookListQuery -> IQueryHandler
CreateBookCommand -> ICommandHandler
```

### Vì sao áp dụng?

- Phân biệt intent đọc và ghi.
- Controller phụ thuộc use-case contract nhỏ hơn.
- Tạo nền tảng để tách handler về sau.

Tuy nhiên đây chỉ là partial CQRS; phân tích đầy đủ nằm tại Q002.

## 10. Publish–Subscribe / Observer qua SignalR

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Application/Features/Notifications/INotificationRealtimePublisher.cs
Backend/src/UTH.Library.Api/Notifications/SignalRNotificationRealtimePublisher.cs
Backend/src/UTH.Library.Api/Notifications/NotificationHub.cs
```

Sau khi tạo in-app notification:

```text
NotificationService
   -> INotificationRealtimePublisher.PublishAsync
   -> SignalR Hub
   -> group của user
   -> các client đang kết nối nhận NotificationReceived
```

### Vì sao áp dụng?

- Service không cần biết connection hoặc client cụ thể.
- Một sự kiện có thể được phát tới các subscriber đang kết nối.
- UI nhận thông báo realtime mà không phải polling liên tục.
- Interface giữ Application độc lập với SignalR của tầng API.

Đây là publish–subscribe ở mức transport. Nó không phải domain-event bus hoàn chỉnh.

## 11. Null Object Pattern — phạm vi hỗ trợ test

### Áp dụng ở đâu?

Trong `BookService.cs` có:

```text
EmptyRequestContext
NullEmbeddingService
NullSemanticRepository
```

Các object này được constructor tương thích cũ của `BookService` dùng khi unit test chỉ truyền repository và `TimeProvider`.

### Vì sao áp dụng?

- Tránh rải `null` check trong service.
- Cho phép test use case truyền thống mà không cần dựng AI/semantic dependencies.
- Cung cấp hành vi vô hại, dự đoán được.

### Điểm cần lưu ý

Các Null Object này hiện chủ yếu phục vụ backward-compatible test constructor. Nếu đi vào production path, chúng có thể che giấu việc thiếu cấu hình thật.

## 12. Options Pattern

### Áp dụng ở đâu?

```text
JwtOptions
Argon2PasswordHasherOptions
AiEmbeddingOptions
AuditRetentionOptions
```

Chúng được bind và validate trong `Infrastructure/DependencyInjection.cs`, sau đó inject qua `IOptions<T>`.

### Vì sao áp dụng?

- Chuyển configuration chuỗi thành typed object.
- Validate cấu hình ngay khi ứng dụng khởi động bằng `ValidateOnStart()`.
- Tránh đọc `IConfiguration` rải rác trong business code.
- Dễ test bằng options riêng.

Đây là ASP.NET Core Options pattern, không phải GoF pattern.

## 13. Policy-based Authorization

### Áp dụng ở đâu?

```text
Backend/src/UTH.Library.Api/Authorization/PermissionRequirement.cs
Backend/src/UTH.Library.Api/DependencyInjection.cs
```

Mỗi permission được đăng ký thành authorization policy. `PermissionAuthorizationHandler` tải authorization state mới nhất rồi quyết định requirement có thành công hay không.

### Vì sao áp dụng?

- Controller chỉ khai báo permission cần có.
- Logic kiểm tra quyền tập trung trong handler.
- Dễ thêm requirement khác mà không viết authorization thủ công trong từng action.
- Trạng thái tài khoản/quyền có thể được kiểm tra lại phía server thay vì chỉ tin claim cũ trong JWT.

Đây là policy/handler pattern của ASP.NET Core, không phải Strategy Pattern theo nghĩa GoF nghiêm ngặt.

## 14. Service Layer và Clean Architecture

### Áp dụng ở đâu?

Solution được chia thành:

```text
Domain
   <- Application
      <- Infrastructure
      <- Api/composition root
```

Application service như `BookService`, `BorrowingService`, `ReservationService` điều phối use case giữa domain entity, repository, unit of work và external abstractions.

Architecture tests tại:

```text
Backend/tests/UTH.Library.ArchitectureTests/DependencyTests.cs
```

kiểm tra Domain không tham chiếu Application/Infrastructure/API và Application không tham chiếu Infrastructure/API.

### Vì sao áp dụng?

- Business rule không bị khóa vào HTTP, EF Core hoặc SMTP.
- Tầng ngoài phụ thuộc vào abstraction của tầng trong.
- Dễ test domain/application mà không khởi động API hay database thật.
- Mỗi tầng có trách nhiệm và phạm vi thay đổi riêng.

Clean Architecture là architectural style; Service Layer là enterprise application pattern. Chúng không phải GoF design pattern.

## Những thứ dễ bị gọi nhầm là design pattern

| Thành phần | Đánh giá đúng |
|---|---|
| `record ...Command` hoặc `...Query` | Chỉ là tên request model nếu không đi qua CQRS handler |
| `AddSingleton<T>` | DI lifetime, không tự động là classic Singleton Pattern |
| Entity có method `MarkSent`, `Return`, `Cancel` | Encapsulation/state transition; chưa phải State Pattern vì không có state object thay thế lẫn nhau |
| Controller gọi Service | Layered architecture/Service Layer, không phải Facade một cách tự động |
| `CirculationPolicyResolver` | Domain/application policy resolver; chưa phải Strategy Pattern vì không có tập strategy implementation hoán đổi |
| EF execution strategy | Cơ chế retry của EF Core; project sử dụng framework strategy chứ không tự định nghĩa GoF Strategy này |

## Các GoF pattern chưa thấy được triển khai rõ

Chưa có bằng chứng rõ về:

- Builder;
- Decorator;
- Mediator/MediatR;
- Composite;
- Prototype;
- Bridge;
- formal Specification Pattern;
- classic State Pattern;
- classic Singleton tự quản lý instance.

Không nên gán tên pattern chỉ vì một class có hành vi hơi giống; chỉ nên khẳng định khi cấu trúc và mục đích của pattern thể hiện rõ trong code.

## Bảng tóm tắt

| Pattern | Nơi áp dụng chính | Mục đích |
|---|---|---|
| Repository | Application persistence abstractions + Infrastructure repositories | Tách nghiệp vụ khỏi EF/database |
| Unit of Work | `IUnitOfWork`, `UnitOfWork` | Transaction, commit/rollback thống nhất |
| DI/IoC | Các `DependencyInjection.cs`, `Program.cs` | Giảm coupling, quản lý lifecycle |
| Static Factory Method | Domain `Create/Issue`, `SmtpMessageFactory` | Tạo object hợp lệ và che chi tiết khởi tạo |
| Adapter | Notification sender adapters | Chuẩn hóa SMTP/in-app sau một interface |
| Strategy | Chọn sender theo notification channel | Đổi hành vi gửi ở runtime |
| Outbox/Worker | Notification table + `EmailOutboxWorker` | Gửi email bất đồng bộ và retry |
| Middleware/Chain | ASP.NET Core request pipeline | Xử lý cross-cutting concern theo chuỗi |
| Interceptor | `AuditSaveChangesInterceptor` | Audit tập trung trước SaveChanges |
| CQRS | Một phần Books/Kiosk | Phân biệt intent đọc và ghi |
| Publish–Subscribe | SignalR notification publisher/hub | Đẩy notification realtime tới client |
| Null Object | Các fallback nội bộ của `BookService` | Dependency vô hại cho unit test |
| Options | JWT, Argon2, AI, audit options | Typed configuration và validation |
| Authorization Policy | Permission requirement/handler | Tập trung kiểm tra quyền |
| Service Layer/Clean Architecture | Toàn solution | Phân tầng và đảo chiều dependency |

### Kết luận

Ba pattern ảnh hưởng lớn nhất đến cấu trúc project là **Repository + Unit of Work + Dependency Injection**. Chúng tạo ra luồng chủ đạo:

```text
Controller
   -> Application Service
      -> Repository abstractions + Unit of Work
         -> EF Core implementations
            -> Database
```

Nhóm notification là nơi kết hợp pattern rõ nhất:

```text
NotificationService
   -> Strategy chọn Adapter
   -> lưu Outbox
   -> Background Worker gửi email/retry
   -> Publisher phát realtime qua SignalR
```

Các pattern được áp dụng vì chúng giải quyết nhu cầu thực tế: tách tầng, transaction nhiều bước, thay đổi kênh gửi, retry tác vụ bên ngoài, audit tập trung, typed configuration và kiểm soát quyền. Tuy nhiên project cũng có một số pattern mới được áp dụng cục bộ, đặc biệt là CQRS; không nên mô tả toàn bộ hệ thống như thể tất cả use case đều tuân theo chúng.
