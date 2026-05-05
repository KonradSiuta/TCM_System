using Microsoft.EntityFrameworkCore;
using TCM.Shared.Models;

namespace TCM.Api.Data
{
    public class TcmDbContext : DbContext
    {
        public TcmDbContext(DbContextOptions<TcmDbContext> options) : base(options) { }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Requirement> Requirements { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<TestStep> TestSteps { get; set; }
        public DbSet<TestRun> TestRuns { get; set; }
        public DbSet<TestExecution> TestExecutions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<RequirementCoverage> RequirementCoverages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            modelBuilder.Entity<RequirementCoverage>()
                .HasKey(rc => new { rc.TestCaseId, rc.RequirementId });
        }
    }
}