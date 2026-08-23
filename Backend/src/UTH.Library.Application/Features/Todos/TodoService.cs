using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Todos;

public sealed class TodoService(ITodoRepository repository, TimeProvider timeProvider)
{
    public async Task<TodoModel> CreateAsync(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = TodoItem.Create(command.Title, timeProvider.GetUtcNow().UtcDateTime);
        await repository.AddAsync(todo, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(todo);
    }

    public async Task<TodoModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(id, cancellationToken);
        return todo is null ? null : Map(todo);
    }

    public async Task<TodoModel?> MarkCompletedAsync(Guid id, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return null;
        }

        todo.MarkCompleted();
        await repository.SaveChangesAsync(cancellationToken);
        return Map(todo);
    }

    private static TodoModel Map(TodoItem todo) => new(todo.Id, todo.Title, todo.IsCompleted, todo.CreatedAtUtc);
}