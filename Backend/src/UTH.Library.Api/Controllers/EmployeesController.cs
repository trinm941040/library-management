using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UTH.Library.Api.Contracts.Employees;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Employees;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/employees")]
public sealed class EmployeesController(EmployeeService employeeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.EmployeesRead)]
    [ProducesResponseType(typeof(EmployeePageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeePageResponse>> Get(
        [FromQuery] EmployeeFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await employeeService.GetAsync(
            new EmployeeListQuery(
                request.Search,
                request.Department,
                request.Position,
                request.Status,
                request.BranchId,
                request.PageNumber,
                request.PageSize),
            cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new EmployeePageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpGet("branches")]
    [Authorize(Policy = Permissions.EmployeesRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<EmployeeBranchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EmployeeBranchResponse>>> GetBranches(
        CancellationToken cancellationToken) =>
        Ok((await employeeService.GetBranchesAsync(cancellationToken))
            .Select(branch => new EmployeeBranchResponse(branch.Id, branch.Code, branch.Name))
            .ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesRead)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var employee = await employeeService.GetByIdAsync(id, cancellationToken);
        return employee is null
            ? NotFound(Problem("Employee was not found."))
            : Ok(ToResponse(employee));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeesCreate)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.CreateAsync(ToCommand(request), cancellationToken, GetCurrentUserId());
        if (!result.Succeeded || result.Employee is null)
            return MapFailure(result);

        var response = ToResponse(result.Employee);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.UpdateAsync(id, ToCommand(request), cancellationToken, GetCurrentUserId());
        return result.Succeeded && result.Employee is not null
            ? Ok(ToResponse(result.Employee))
            : MapFailure(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateEmployeeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.UpdateStatusAsync(
            id,
            request.Status,
            request.ConcurrencyToken,
            request.DeactivateLinkedAccount,
            cancellationToken,
            GetCurrentUserId());
        return result.Succeeded && result.Employee is not null
            ? Ok(ToResponse(result.Employee))
            : MapFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await employeeService.DeleteAsync(id, cancellationToken, GetCurrentUserId());
        return result.Succeeded ? NoContent() : MapFailure(result);
    }

    private ActionResult MapFailure(EmployeeManagementResult result)
    {
        var detail = result.Errors.FirstOrDefault() ?? "Employee management operation failed.";
        return result.Failure switch
        {
            EmployeeManagementFailure.NotFound => NotFound(Problem(detail)),
            EmployeeManagementFailure.Conflict => Conflict(Problem(detail)),
            _ => BadRequest(Problem(detail))
        };
    }

    private static SaveEmployeeCommand ToCommand(CreateEmployeeRequest request) =>
        new(
            request.EmployeeCode,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Address,
            request.Position,
            request.Department,
            request.HireDate,
            request.Status,
            request.BranchId);

    private static SaveEmployeeCommand ToCommand(UpdateEmployeeRequest request) =>
        new(
            request.EmployeeCode,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Address,
            request.Position,
            request.Department,
            request.HireDate,
            request.Status,
            request.BranchId,
            request.ConcurrencyToken,
            request.DeactivateLinkedAccount);

    private static ProblemDetails Problem(string detail) => new() { Detail = detail };

    private static EmployeeResponse ToResponse(EmployeeModel employee) =>
        new(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.Address,
            employee.Position,
            employee.Department,
            employee.BranchId,
            employee.BranchCode,
            employee.BranchName,
            employee.UserId,
            employee.HireDate,
            employee.Status,
            employee.ConcurrencyToken,
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : null;
}
