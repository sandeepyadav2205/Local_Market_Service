using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        public int ProviderId { get; set; }
        [ForeignKey("ProviderId")]
        public Provider? Provider { get; set; }

        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Cart>? Carts { get; set; }
    }
}
