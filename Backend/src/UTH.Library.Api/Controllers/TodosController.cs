using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Todos;
using UTH.Library.Application.Features.Todos;

namespace UTH.Library.Api.Controllers;

[ApiController]
[NonController]
[Route("api/todos")]
public sealed class TodosController(TodoService todoService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoResponse>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError(nameof(request.Title), "Title is required.");
            return ValidationProblem(ModelState);
        }

        var todo = await todoService.CreateAsync(new CreateTodoCommand(request.Title), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, ToResponse(todo));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var todo = await todoService.GetByIdAsync(id, cancellationToken);
        return todo is null ? NotFound() : Ok(ToResponse(todo));
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var todo = await todoService.MarkCompletedAsync(id, cancellationToken);
        return todo is null ? NotFound() : Ok(ToResponse(todo));
    }

    private static TodoResponse ToResponse(TodoModel todo) =>
        new(todo.Id, todo.Title, todo.IsCompleted, todo.CreatedAtUtc);
}
