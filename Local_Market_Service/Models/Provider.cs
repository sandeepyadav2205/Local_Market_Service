using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class Provider
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? ApplicationUser { get; set; }
        public string? BusinessName { get; set; }
        public string? Experience { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public bool? IsVerified { get; set; } = false;
        public string? AverageRating { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Service>? Services { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ProviderDocument? ProviderDocument { get; set; }
    }
}
