using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Models
{
    public class AppUser : IdentityUser //<int> was causing TypeCast error
    {
        // Additional properties if needed
    }
}
