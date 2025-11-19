using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserRoles.Models;

namespace UserRoles.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ADD THESE DbSets:
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<SectionCapacity> SectionCapacities { get; set; }
        public DbSet<ProfessorSectionAssignment> ProfessorSectionAssignments { get; set; }

        public DbSet<SubjectEnrollment> SubjectEnrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SectionCapacity>().HasData(
                new SectionCapacity { GradeLevel = "1", CurrentSection = 1, StudentsInCurrentSection = 0 },
                new SectionCapacity { GradeLevel = "2", CurrentSection = 1, StudentsInCurrentSection = 0 },
                new SectionCapacity { GradeLevel = "3", CurrentSection = 1, StudentsInCurrentSection = 0 },
                new SectionCapacity { GradeLevel = "4", CurrentSection = 1, StudentsInCurrentSection = 0 },
                new SectionCapacity { GradeLevel = "5", CurrentSection = 1, StudentsInCurrentSection = 0 },
                new SectionCapacity { GradeLevel = "6", CurrentSection = 1, StudentsInCurrentSection = 0 }
            );
        }
    }
}