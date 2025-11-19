using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserRoles.Models
{
    public class SubjectEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(450)]
        public string? ProfessorId { get; set; }

        public DateTime EnrolledDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; } = null!;

        [ForeignKey("ProfessorId")]
        public virtual Users? Professor { get; set; }
    }
}