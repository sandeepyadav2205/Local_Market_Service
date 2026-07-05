using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? ApplicationUser { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State {  get; set; }
        public string? Pincode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Cart>? Carts { get; set; }
    }
}
