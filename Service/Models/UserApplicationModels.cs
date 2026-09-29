namespace Service.Models;

public sealed class CreateUserCommand
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class UserListQuery
{
    public string? SearchValue { get; init; }
    public IReadOnlyCollection<string> Columns { get; init; } = [];
    public bool IsPaginationRequest { get; init; } = true;
    public int PageSize { get; init; } = 10;
    public int PageNumber { get; init; } = 1;
}

public sealed class UserSummary
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class PagedUsers
{
    public IReadOnlyList<UserSummary> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public sealed class UserCreationResult
{
    public UserSummary? User { get; init; }
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
