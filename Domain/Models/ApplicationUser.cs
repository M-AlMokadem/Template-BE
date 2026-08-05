using Microsoft.AspNetCore.Identity;

namespace Domain.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
}
