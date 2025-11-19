using System.ComponentModel.DataAnnotations;

namespace UserRoles.ViewModels
{
    public class EnrollStudentViewModel
    {
        public int EnrollmentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public int Section { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // For Grades 1-3: One professor teaches all subjects
        public string? SingleProfessorId { get; set; }

        // For Grades 4-6: Multiple professors for different subjects
        public Dictionary<string, string> SubjectProfessors { get; set; } = new();

        // Available professors per subject
        public Dictionary<string, List<ProfessorOption>> AvailableProfessors { get; set; } = new();
    }

    public class ProfessorOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }
}