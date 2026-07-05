using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public double? Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public DateOnly PaymentDate { get; set; } 
        public string? Status { get; set; }
        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }
    }
}
