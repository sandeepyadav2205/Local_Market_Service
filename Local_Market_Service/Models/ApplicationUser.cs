using Microsoft.AspNetCore.Identity;

namespace Local_Market_Service.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? profileImage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public Provider? Provider { get; set; }
        public Customer? Customer { get; set; }
    }
}
