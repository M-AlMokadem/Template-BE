using API.Exceptions;
using API.Models.Users;
using Domain.Context;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Util.Core;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;
    private readonly ApplicationContext applicationContext;

    public UsersController(ApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
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
        var query = applicationContext.ApplicationUsers
            .AsNoTracking()
            .Where(user => !user.IsDeleted);

        var normalizedSearch = searchedValue?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var normalizedColumns = (columnsKey ?? [])
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Select(column => column.Trim().ToLowerInvariant())
                .ToHashSet();

            var filterByName = normalizedColumns.Count == 0 || normalizedColumns.Contains("fullname");
            var filterByEmail = normalizedColumns.Count == 0 || normalizedColumns.Contains("email");

            query = query.Where(user =>
                (filterByName && EF.Functions.ILike(user.FullName, $"%{normalizedSearch}%")) ||
                (filterByEmail && EF.Functions.ILike(user.Email ?? string.Empty, $"%{normalizedSearch}%")));
        }

        var totalCount = await query.CountAsync();

        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;

        if (isPaginationRequest)
        {
            query = query
                .OrderBy(user => user.FullName)
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize);
        }
        else
        {
            query = query.OrderBy(user => user.FullName);
            safePageNumber = 1;
            safePageSize = Math.Max(totalCount, 1);
        }

        var items = await query
            .Select(ToSummaryExpression())
            .ToListAsync();

        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)Math.Max(safePageSize, 1)), 1);
        var payload = new PagedResult<UserSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalPages = totalPages,
            HasNextPage = safePageNumber < totalPages,
            HasPreviousPage = safePageNumber > 1
        };

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, payload, "Users retrieved successfully."));
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

    private static System.Linq.Expressions.Expression<Func<ApplicationUser, UserSummaryResponse>> ToSummaryExpression()
    {
        return user => new UserSummaryResponse
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive
        };
    }
}
