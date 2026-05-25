namespace TCM.Shared.Models
{
    public class ProjectMemberDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public ProjectRole Role { get; set; }
    }

    public class UpsertMemberDto
    {
        public int ProjectId { get; set; }
        public string Username { get; set; }
        public ProjectRole Role { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }
}