namespace RealEstateAgent.Services
{
    public class JwtTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public Guid GetTenantId()
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return Guid.Empty;
            }
            // Беремо поточного юзера з HTTP-запиту
            var user = _httpContextAccessor.HttpContext?.User;

            // Шукаємо Claim з назвою "tenantId", який ми зашили при логіні
            var tenantClaim = user?.FindFirst("tenantId")?.Value;

            if (Guid.TryParse(tenantClaim, out Guid tenantId))
            {
                return tenantId;
            }

            return Guid.Empty;
        }
    }
}
