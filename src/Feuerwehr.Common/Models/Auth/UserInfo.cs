namespace Feuerwehr.Common.Models.Auth
{
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? FireDepartmentId { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
