using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        public int Quantity { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        
    }
}
