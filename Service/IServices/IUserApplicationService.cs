using Service.Models;

namespace Service.IServices;

public interface IUserApplicationService
{
    Task<PagedUsers> GetUsersAsync(UserListQuery query);
    Task<UserCreationResult> CreateUserAsync(CreateUserCommand command);
    Task<UserSummary?> GetByIdAsync(Guid id);
    Task<UserSummary?> UpdateStatusAsync(Guid id, bool isActive);
    Task<bool> DeleteAsync(Guid id);
}
