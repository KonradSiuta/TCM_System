using Microsoft.EntityFrameworkCore;
using TCM.Shared.Models; // Ważne: to odniesienie pozwala API widzieć Twoje modele

namespace TCM.Api.Data
{
    public class TcmDbContext : DbContext
    {
        public TcmDbContext(DbContextOptions<TcmDbContext> options) : base(options)
        {
        }

        // Deklarujemy DbSet dla każdej encji (to będą nasze tabele w bazie)
        public DbSet<Project> Projects { get; set; }
        public DbSet<Requirement> Requirements { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<TestStep> TestSteps { get; set; }
        public DbSet<TestRun> TestRuns { get; set; }
        public DbSet<TestExecution> TestExecutions { get; set; }

        // Ta metoda służy do konfiguracji zaawansowanych właściwości tabel (np. ograniczeń i relacji)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Wymuszenie unikalności dla ExternalRequirementId (np. REQ-102 musi być unikalne)
            modelBuilder.Entity<Requirement>()
                .HasIndex(r => r.ExternalRequirementId)
                .IsUnique();

            // Wymuszenie unikalności dla ExternalId w testach (np. TC_VOLT_004)
            modelBuilder.Entity<TestCase>()
                .HasIndex(tc => tc.ExternalId)
                .IsUnique();

            // Tutaj w przyszłości możemy dodawać kolejne konfiguracje Fluent API
        }
    }
}