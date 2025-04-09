using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Back_HR.Controllers.UsersManagementControllers;

namespace Back_HR.Models
{
    public class HRContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public HRContext(DbContextOptions<HRContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Candidat> Candidates { get; set; }
        public DbSet<Employe> Employees { get; set; }
        public DbSet<RH> HRs { get; set; }
        public DbSet<Competence> Competences { get; set; }
        public DbSet<JobOffer> JobOffers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<RevokedToken> RevokedTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Required for IdentityDbContext
            base.OnModelCreating(modelBuilder);

            // Configure TPT for User inheritance (separate tables for each type)
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Candidat>().ToTable("Candidates");
            modelBuilder.Entity<Employe>().ToTable("Employees");
            modelBuilder.Entity<RH>().ToTable("HRs");

            modelBuilder.Entity<PerformanceReview>().Property(p => p.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Attendance>().Property(a => a.Id).HasDefaultValueSql("NEWID()");

            // Configure GUID IDs with default values (NEWID for SQL Server)
            modelBuilder.Entity<User>().Property(u => u.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Candidat>().Property(c => c.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Employe>().Property(e => e.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<RH>().Property(r => r.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Competence>().Property(s => s.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<JobOffer>().Property(j => j.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Notification>().Property(n => n.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Survey>().Property(s => s.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<SurveyResponse>().Property(sr => sr.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Application>().Property(a => a.Id).HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<User>()
            .Property(u => u.UserType)
            .HasConversion<int>();

            
            modelBuilder.Entity<Employe>()
                .HasMany(e => e.PerformanceReviews)
                .WithOne(p => p.Employee)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Attendance>()
              .HasOne(a => a.Employee)
              .WithMany(e => e.Attendances)
              .HasForeignKey(a => a.EmployeeId)
              .OnDelete(DeleteBehavior.Cascade);


           

            // Optional: Seed initial data for testing

            // Configure many-to-many between Candidate and Skill
            modelBuilder.Entity<Candidat>()
                .HasMany(c => c.Competences)
                .WithMany(s => s.Candidats)
                .UsingEntity(j => j.ToTable("CandidateSkills"));

            // Configure many-to-many between JobOffer and Skill
            modelBuilder.Entity<JobOffer>()
                .HasMany(j => j.Competences)
                .WithMany(s => s.JobOffres)
                .UsingEntity(j => j.ToTable("JobOfferSkills"));

            // Configure many-to-one between JobOffer and HR
            modelBuilder.Entity<JobOffer>()
                .HasOne(j => j.RHResponsable)
                .WithMany(r => r.Offres)
                .HasForeignKey(j => j.RHId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure many-to-many between Notification and User
            modelBuilder.Entity<Notification>()
                .HasMany(n => n.Users)
                .WithMany(u => u.Notifications)
                .UsingEntity(j => j.ToTable("NotificationUsers"));

            // Configure many-to-many between Survey and Employee
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Employes)
                .WithMany(e => e.SurveysResponded)
                .UsingEntity(j => j.ToTable("SurveyEmployees"));

            // Configure one-to-many between Survey and SurveyResponse
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Responses)
                .WithOne(sr => sr.Survey)
                .HasForeignKey(sr => sr.SurveyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many between Employee and SurveyResponse
            modelBuilder.Entity<Employe>()
                .HasMany(e => e.SurveyResponses)
                .WithOne(sr => sr.Employee)
                .HasForeignKey(sr => sr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many between Candidate and Application
            modelBuilder.Entity<Candidat>()
                .HasMany(c => c.Applications)
                .WithOne(a => a.Candidat)
                .HasForeignKey(a => a.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many between JobOffer and Application
            modelBuilder.Entity<JobOffer>()
                .HasMany(j => j.Applications)
                .WithOne(a => a.JobOffer)
                .HasForeignKey(a => a.JobOfferId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}