using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Market_Service.Models
{
    public class ProviderDocument
    {
        public int Id { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentUrl { get; set; }
        public string? Status { get; set; } = "pending";
        public string? Remark { get; set; } = "Document Verification";
        public DateTime UploadedAt  { get; set; } = DateTime.Now;
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }

        public int ProviderId { get; set; }
        [ForeignKey("ProviderId")]
        public Provider? Provider { get; set; }
    }
}
