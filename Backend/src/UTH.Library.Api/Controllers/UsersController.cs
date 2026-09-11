using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Users;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(UserPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPageResponse>> Get(
        [FromQuery] UserFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await userManagementService.GetAsync(
            new UserListQuery(
                request.Search,
                request.IsActive,
                request.Role,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new UserPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManagementService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound(CreateProblem("User was not found.")) : Ok(ToResponse(user));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.UsersCreate)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
            return InvalidWhitespace();

        var result = await userManagementService.CreateAsync(
            new CreateManagedUserCommand(
                request.Email,
                request.Password,
                request.DisplayName,
                request.EmployeeId,
                TryGetCurrentUserId(out var actorUserId) ? actorUserId : null),
            cancellationToken);

        if (!result.Succeeded || result.User is null)
            return MapFailure(result);

        var response = ToResponse(result.User);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UsersUpdate)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
            return InvalidWhitespace();
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(CreateProblem("The authenticated user identifier is invalid."));

        var result = await userManagementService.UpdateAsync(
            id,
            currentUserId,
            new UpdateManagedUserCommand(request.Email, request.DisplayName),
            cancellationToken);

        return result.Succeeded && result.User is not null
            ? Ok(ToResponse(result.User))
            : MapFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.UsersDeactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(CreateProblem("The authenticated user identifier is invalid."));

        var result = await userManagementService.DeactivateAsync(id, currentUserId, cancellationToken);
        return result.Succeeded ? NoContent() : MapFailure(result);
    }

    private ActionResult MapFailure(UserManagementResult result) => result.Failure switch
    {
        UserManagementFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "User was not found.")),
        UserManagementFailure.Conflict or
        UserManagementFailure.SelfDeactivation or
        UserManagementFailure.ProtectedResource =>
            Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "User validation failed."))
    };

    private ActionResult InvalidWhitespace()
    {
        ModelState.AddModelError("user", "Email and display name cannot contain only whitespace.");
        return ValidationProblem(ModelState);
    }

    private bool TryGetCurrentUserId(out Guid id) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out id);

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private static UserResponse ToResponse(ManagedUser user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            user.Roles,
            user.IsProtected);
}
