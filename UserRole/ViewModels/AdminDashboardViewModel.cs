using System.ComponentModel.DataAnnotations;
using UserRoles.Controllers;
using UserRoles.Models;

namespace UserRoles.ViewModels
{
    // Dashboard ViewModel
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int PendingEnrollments { get; set; }
        public int TotalProfessors { get; set; }
        public int TotalSections { get; set; }

        public List<GradeLevelStats> StudentsPerGradeLevel { get; set; } = new();
        public List<GradeLevelStats> ProfessorsPerGradeLevel { get; set; } = new();
        public List<Enrollment> RecentEnrollments { get; set; } = new();
    }

    public class GradeLevelStats
    {
        public string GradeLevel { get; set; }
        public int StudentCount { get; set; }
        public int ProfessorCount { get; set; }
    }

    // Add Professor ViewModel
    public class AddProfessorViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be between {2} and {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade level is required.")]
        [Display(Name = "Assigned Grade Level")]
        public string GradeLevel { get; set; } = string.Empty;

        [Display(Name = "Assigned Section")]
        public int? Section { get; set; }

        [Display(Name = "Assigned Subject")]
        public string? Subject { get; set; } // Required for grades 4-6
    }

    // Edit Professor ViewModel
    public class EditProfessorViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade level is required.")]
        [Display(Name = "Assigned Grade Level")]
        public string GradeLevel { get; set; } = string.Empty;

        [Display(Name = "Assigned Section")]
        public int? Section { get; set; }

        [Display(Name = "Assigned Subject")]
        public string? Subject { get; set; } // Required for grades 4-6
    }

    // Section Management ViewModels
    public class SectionManagementViewModel
    {
        public List<GradeLevelSections> GradeLevels { get; set; } = new();
    }

    public class GradeLevelSections
    {
        public string GradeLevel { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public List<SectionInfo> Sections { get; set; } = new();
    }

    public class SectionInfo
    {
        public int SectionNumber { get; set; }
        public int StudentCount { get; set; }
    }

    // Class Schedule ViewModel
    public class ClassScheduleViewModel
    {
        public string GradeLevel { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty; // "Morning" or "Afternoon"
        public List<ProfessorScheduleInfo> Professors { get; set; } = new(); // For detailed schedule view
    }

    // Professor Schedule Information
    public class ProfessorScheduleInfo
    {
        public string ProfessorName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int? Section { get; set; }
        public string Room { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
    }

    // Professor Dashboard ViewModel
    public class ProfessorDashboardViewModel
    {
        public string ProfessorName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public int Section { get; set; }
        public string AssignedRoom { get; set; } = string.Empty;
        public List<Enrollment> Students { get; set; } = new();
        public ClassScheduleViewModel ClassSchedule { get; set; } = new();
        public int TotalStudents { get; set; }

        // NEW: List of all assignments
        public List<UserRoles.Controllers.ProfessorAssignmentInfo> Assignments { get; set; } = new();
    }
}