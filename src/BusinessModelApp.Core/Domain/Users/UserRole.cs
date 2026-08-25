using Microsoft.AspNetCore.Identity;

namespace BusinessModelApp.Core.Domain.Users
{
    public class UserRole : IdentityUserRole<Guid>
    {
        // Additional properties can be added here if needed.
        // For example:
        // public DateTime AssignedAt { get; set; }
        // public string AssignedBy { get; set; }
    }
}
