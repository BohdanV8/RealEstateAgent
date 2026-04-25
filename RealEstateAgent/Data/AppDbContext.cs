using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealEstateAgent.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RealEstateAgent.Services;
namespace RealEstateAgent.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        private readonly Guid _currentTenantId;
        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _currentTenantId = tenantProvider.GetTenantId();
        }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<BotClient> BotClients { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().
              HasMany(u => u.RefreshTokens).
              WithOne(token => token.User).
              HasForeignKey(token => token.UserId);

            // НАЙГОЛОВНІШИЙ РЯДОК У SAAS: Глобальний фільтр запитів
            // EF Core автоматично додаватиме "WHERE TenantId = _currentTenantId" до ВСІХ SQL запитів
            modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => u.TenantId == _currentTenantId);
            modelBuilder.Entity<BotClient>().HasQueryFilter(ws => ws.TenantId == _currentTenantId);
        }
    }
}
