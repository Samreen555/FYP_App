using FYP_App.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<GlobalSettings> GlobalSettings { get; set; }
        public DbSet<FypRegistration> FypRegistrations { get; set; }
        public DbSet<DefenseSchedule> DefenseSchedules { get; set; }
        public DbSet<Panel> Panels { get; set; }
        public DbSet<PanelMember> PanelMembers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<ProjectGrade> ProjectGrades { get; set; }
        public DbSet<MeetingLog> MeetingLogs { get; set; }
        public DbSet<DefenseEvaluation> DefenseEvaluations { get; set; }
        

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
           
            builder.Entity<UserProfile>()
                .HasIndex(u => u.UserId)
                .IsUnique();
        }
    }
}