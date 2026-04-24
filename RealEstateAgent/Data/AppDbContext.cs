using Microsoft.EntityFrameworkCore;

namespace RealEstateAgent.Data
{
    public class AppDbContext : DbContext
    {
        private readonly Guid _currentTenantId;
        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _currentTenantId = tenantProvider.GetTenantId();
        }
        //public DbSet<Property> Properties { get; set; }
    }
}
