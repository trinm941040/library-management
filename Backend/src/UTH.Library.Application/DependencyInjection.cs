using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Features.Books;
using UTH.Library.Application.Features.Borrowings;
using UTH.Library.Application.Features.Reservations;
using UTH.Library.Application.Features.Employees;
using UTH.Library.Application.Features.Violations;
using UTH.Library.Application.Features.Members;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.CirculationPolicies;
using UTH.Library.Application.Features.AuditLogs;
using UTH.Library.Application.Features.Locations;
using UTH.Library.Application.Features.StockReceipts;
using UTH.Library.Application.Features.Copies;
using UTH.Library.Application.Features.Suppliers;

namespace UTH.Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<EmployeeService>();
        services.AddScoped<BookService>();
        services.AddScoped<BookTransferService>();
        services.AddScoped<BookCommandValidator>();
        services.AddScoped<IQueryHandler<BookListQuery, BookPageModel>>(provider => provider.GetRequiredService<BookService>());
        services.AddScoped<ICommandHandler<CreateBookCommand, BookResult>>(provider => provider.GetRequiredService<BookService>());
        services.AddScoped<BorrowingService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<ViolationService>();
        services.AddScoped<MemberService>();
        services.AddScoped<CirculationPolicyService>();
        services.AddScoped<ICirculationPolicyResolver, CirculationPolicyResolver>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<LocationService>();
        services.AddScoped<StockReceiptService>();
        services.AddScoped<CopyService>();
        services.AddScoped<SupplierService>();
        return services;
    }
}
