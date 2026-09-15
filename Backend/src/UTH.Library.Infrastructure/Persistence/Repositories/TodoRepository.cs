using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class TodoRepository(LibraryDbContext dbContext) : ITodoRepository
{
    public Task AddAsync(TodoItem todo, CancellationToken cancellationToken) => dbContext.Todos.AddAsync(todo, cancellationToken).AsTask();

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => dbContext.Todos.SingleOrDefaultAsync(todo => todo.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}