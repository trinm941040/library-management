namespace UTH.Library.Api.Contracts.Todos;

public sealed record CreateTodoRequest(string? Title);

public sealed record TodoResponse(Guid Id, string Title, bool IsCompleted, DateTime CreatedAtUtc);