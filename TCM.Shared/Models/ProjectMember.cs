namespace TCM.Shared.Models
{
    public class ProjectMember
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public ProjectRole Role { get; set; }
    }
}