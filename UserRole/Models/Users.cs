using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UserRoles.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "student";

        // Professor-specific fields
        [StringLength(10)]
        public string? AssignedGradeLevel { get; set; }

        public int? AssignedSection { get; set; }

        [StringLength(50)]
        public string? AssignedRoom { get; set; }

        [StringLength(50)]
        public string? AssignedSubject { get; set; } // For grades 4-6: Math, Science, English, Filipino, Social Studies, MAPEH

        // Security properties
        public string? PasswordHistory { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEndTime { get; set; }
        public DateTime? LastPasswordChange { get; set; }
    }
}