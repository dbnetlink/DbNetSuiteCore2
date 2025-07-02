using DbNetSuiteCore.Timesheet.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DbNetSuiteCore.Timesheet.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Project { get; set; }
        public DbSet<Project_Task> ProjectTask { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>().ToTable("Project");
            modelBuilder.Entity<Project_Task>().ToTable("Project_Task");
        }
    }
}
