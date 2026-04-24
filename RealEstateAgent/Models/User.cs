namespace RealEstateAgent.Models
{
    public enum UserRole
    {
        Admin,
        Agent,
        Client
    }
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; } = UserRole.Admin;
    }
}
