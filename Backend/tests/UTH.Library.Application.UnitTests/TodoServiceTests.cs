using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Todos;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.UnitTests;

public sealed class TodoServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_PersistsTodo()
    {
        var repository = new FakeTodoRepository();
        var service = new TodoService(repository, TimeProvider.System);

        var result = await service.CreateAsync(new CreateTodoCommand("Read"), CancellationToken.None);

        Assert.Equal("Read", result.Title);
        Assert.Single(repository.Todos);
    }

    private sealed class FakeTodoRepository : ITodoRepository
    {
        public List<TodoItem> Todos { get; } = [];

        public Task AddAsync(TodoItem todo, CancellationToken cancellationToken)
        {
            Todos.Add(todo);
            return Task.CompletedTask;
        }

        public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Todos.SingleOrDefault(todo => todo.Id == id));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}