namespace RealEstateAgent.DTOs
{
    public class RegisterEntity
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
    }
}
