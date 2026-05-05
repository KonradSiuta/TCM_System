using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public List<ProjectMember> ProjectMemberships { get; set; } = new();
    }
}