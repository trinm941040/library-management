using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Features.Todos;

namespace UTH.Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<TodoService>();
        return services;
    }
}