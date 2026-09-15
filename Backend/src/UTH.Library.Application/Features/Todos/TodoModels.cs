namespace UTH.Library.Application.Features.Todos;

public sealed record TodoModel(Guid Id, string Title, bool IsCompleted, DateTime CreatedAtUtc);

public sealed record CreateTodoCommand(string Title);