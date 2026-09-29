using API.Constants;
using API.Exceptions;
using API.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.IServices;
using Service.Models;
using Util.Core;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoles.Admin)]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private const int DefaultPageSize = 10;
    private readonly IUserApplicationService userService;

    public UsersController(IUserApplicationService userService)
    {
        this.userService = userService;
    }

    [HttpGet]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? searchedValue,
        [FromQuery] List<string>? columnsKey,
        [FromQuery] bool isPaginationRequest = true,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] int pageNumber = 1)
    {
        var result = await userService.GetUsersAsync(new UserListQuery
        {
            SearchValue = searchedValue,
            Columns = columnsKey ?? [],
            IsPaginationRequest = isPaginationRequest,
            PageSize = pageSize,
            PageNumber = pageNumber
        });

        var payload = new PagedResult<UserSummaryResponse>
        {
            Items = result.Items.Select(ToResponse).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        };

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "Users retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await userService.CreateUserAsync(new CreateUserCommand
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password
        });

        if (!result.Succeeded || result.User is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return ValidationProblem(ModelState);
        }

        var response = ToResponse(result.User);
        return CreatedAtAction(nameof(GetById), new { id = result.User.Id }, new DefaultResponse(
            true,
            StatusCodes.Status201Created,
            response,
            "User created successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var payload = ToResponse(user);
        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "User retrieved successfully."));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await userService.UpdateStatusAsync(id, request.IsActive);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var payload = ToResponse(user);
        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "User status updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await userService.DeleteAsync(id);

        if (!deleted)
        {
            throw new NotFoundException("User was not found.");
        }

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, true, "User deleted successfully."));
    }

    private static UserSummaryResponse ToResponse(UserSummary user)
    {
        return new UserSummaryResponse
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive
        };
    }
}
