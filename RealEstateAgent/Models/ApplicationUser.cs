using Microsoft.AspNetCore.Identity;

namespace RealEstateAgent.Models
{
    public class ApplicationUser : IdentityUser<Guid>, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant? Tenant { get; set; }
        public long? TelegramId { get; set; }
        public string? RegistrationToken { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public bool isActive { get; set; } = true;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
