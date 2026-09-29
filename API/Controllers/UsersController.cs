using API.Constants;
using API.Exceptions;
using API.Models.Users;
using API.Services;
using Domain.Context;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Util.Core;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoles.Admin)]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationContext applicationContext;
    private readonly IUserService userService;

    public UsersController(ApplicationContext applicationContext, IUserService userService)
    {
        this.applicationContext = applicationContext;
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
        var payload = await userService.GetUsersAsync(
            searchedValue,
            columnsKey,
            isPaginationRequest,
            pageSize,
            pageNumber);

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "Users retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var (user, result) = await userService.CreateUserAsync(request);
        if (!result.Succeeded || user is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        var response = ToSummary(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new DefaultResponse(
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
        var user = await FindActiveUserAsync(id);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var payload = ToSummary(user);
        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "User retrieved successfully."));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await FindActiveUserAsync(id);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        user.IsActive = request.IsActive;
        user.ModifiedOn = DateTime.UtcNow;

        await applicationContext.SaveChangesAsync();

        var payload = ToSummary(user);
        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "User status updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DefaultResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await FindActiveUserAsync(id);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.ModifiedOn = DateTime.UtcNow;

        await applicationContext.SaveChangesAsync();

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, true, "User deleted successfully."));
    }

    private async Task<ApplicationUser?> FindActiveUserAsync(Guid id)
    {
        return await applicationContext.ApplicationUsers
            .FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted);
    }

    private static UserSummaryResponse ToSummary(ApplicationUser user)
    {
        return new UserSummaryResponse
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive
        };
    }

}
