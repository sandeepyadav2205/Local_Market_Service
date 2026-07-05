using System.ComponentModel.DataAnnotations;

namespace Local_Market_Service.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "password is required")]
        public string Password { get; set; }
        public bool RememberMe { get; set; } = false;
    }
}
