using System.ComponentModel.DataAnnotations;

namespace RealEstateAgent.Models
{
    public class BotClient : ITenantEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Tenant? Tenant { get; set; }
        public long TelegramId { get; set; }
    }
}
