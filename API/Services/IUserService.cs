using API.Models.Users;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public interface IUserService
{
    Task<PagedResult<UserSummaryResponse>> GetUsersAsync(
        string? searchedValue,
        List<string>? columnsKey,
        bool isPaginationRequest,
        int pageSize,
        int pageNumber);

    Task<(ApplicationUser? User, IdentityResult Result)> CreateUserAsync(CreateUserRequest request);
}
