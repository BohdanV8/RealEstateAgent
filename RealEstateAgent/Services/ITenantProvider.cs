namespace RealEstateAgent.Services
{
    public interface ITenantProvider
    {
        Guid GetTenantId();
    }
}
