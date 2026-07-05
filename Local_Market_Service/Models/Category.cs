using System.ComponentModel.DataAnnotations;

namespace Local_Market_Service.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool isActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Service>? Services { get; set; }
    }
}
