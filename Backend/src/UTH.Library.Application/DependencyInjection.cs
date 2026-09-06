using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Features.Books;
using UTH.Library.Application.Features.Borrowings;
using UTH.Library.Application.Features.Reservations;
using UTH.Library.Application.Features.Employees;
using UTH.Library.Application.Features.Todos;
using UTH.Library.Application.Features.Violations;

namespace UTH.Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<EmployeeService>();
        services.AddScoped<TodoService>();
        services.AddScoped<BookService>();
        services.AddScoped<BorrowingService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<ViolationService>();
        return services;
    }
}
