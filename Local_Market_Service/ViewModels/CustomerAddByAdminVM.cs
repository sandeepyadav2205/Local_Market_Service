using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Local_Market_Service.ViewModels
{
    public class CustomerAddByAdminVM
    {
        [DisplayName("Full Name")]
        [Required(ErrorMessage = "full name required")]
        public string FullName { get; set; }
        [EmailAddress(ErrorMessage = "invalid email formate")]
        public string Email { get; set; }
        [Phone(ErrorMessage = "invalid phone number formate")]
        [Required(ErrorMessage = "phone number is required")]
        [DisplayName("Phone")]
        public string MobileNumber { get; set; }
        [Required(ErrorMessage = "address is required")]
        public string Address { get; set; }
        [Required(ErrorMessage = "city is required")]
        public string City { get; set; }
        [Required(ErrorMessage = "state is required")]
        public string State { get; set; }
        [Required(ErrorMessage = "pincode is required")]
        [DisplayName("Pin Code")]
        public string Pincode { get; set; }

        public IFormFile? Image { get; set; }
    }
}
