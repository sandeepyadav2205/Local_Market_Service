using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        public DateTime BookingDate { get; set; }
        public DateOnly ServiceDate { get; set; }
        public string? Address { get; set; }
        public decimal? Amount { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public int ProviderId { get; set; }
        [ForeignKey("ProviderId")]
        public Provider? Provider { get; set; }

        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }
        public ICollection<Review>? Reviews { get; set; }

        public Payment? Payment { get; set; }
    }
}
