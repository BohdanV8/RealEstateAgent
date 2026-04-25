using System.ComponentModel.DataAnnotations;

namespace RealEstateAgent.Models
{
    public class Tenant : ITenantEntity
    {
        [Key]
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ClientBotToken { get; set; }
        public string? RealtorBotToken { get; set; }
    }
}
