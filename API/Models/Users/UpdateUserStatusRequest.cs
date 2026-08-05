using System.ComponentModel.DataAnnotations;

namespace API.Models.Users;

public sealed class UpdateUserStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}
