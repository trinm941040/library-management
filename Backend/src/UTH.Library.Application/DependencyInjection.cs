using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Features.Books;
using UTH.Library.Application.Features.Employees;
using UTH.Library.Application.Features.Todos;

namespace UTH.Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<EmployeeService>();
        services.AddScoped<TodoService>();
        services.AddScoped<BookService>();
        return services;
    }
}
