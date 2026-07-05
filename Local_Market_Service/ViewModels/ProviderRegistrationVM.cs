using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Local_Market_Service.ViewModels
{
    public class ProviderRegistrationVM
    {
        [Required(ErrorMessage = "full name is required")]
        [DisplayName("Full Name")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "email is required")]
        public string Email { get; set; }
        [Required(ErrorMessage = "mobile number is required")]
        [DisplayName("Mobile Number")]
        public string MobileNumber { get; set; }
        [Required(ErrorMessage = "password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "confirm password is required")]
        [DisplayName("Confirm Password")]
        [Compare("Password", ErrorMessage = "password does not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "business name is required")]
        [DisplayName("Business Name")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "experience is required")]
        public string Experience { get; set; }
        [Required(ErrorMessage = "description is required")]
        public string Description { get; set; }

        public string Address { get; set; }
        [Required(ErrorMessage = "city is required")]
        public string City { get; set; }
        [Required(ErrorMessage = "state is required")]
        public string State { get; set; }
        [Required(ErrorMessage = "pincode is required")]
        [DisplayName("Pin Code")]
        public string Pincode { get; set; }
        [Required(ErrorMessage = "category is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "please select document type")]
        [Display(Name = "Document Type")]
        public string? DocumentType { get; set; }
        [Required(ErrorMessage = "document number is required")]
        [Display(Name = "Document Number")]
        public string? DocumentNumber { get; set; }
        [Required(ErrorMessage = "adhar is required")]
        [Display(Name = "Upload Document")]
        public IFormFile DocumentUrl { get; set; }

        public IFormFile? Image { get; set; }
    }
}
