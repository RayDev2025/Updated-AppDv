using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserRoles.Models
{
    public class ProfessorSectionAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string ProfessorId { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string GradeLevel { get; set; } = string.Empty;

        [Required]
        public int Section { get; set; }

        [StringLength(50)]
        public string? Subject { get; set; } // For grades 4-6

        [StringLength(50)]
        public string? AssignedRoom { get; set; }

        // Navigation property
        [ForeignKey("ProfessorId")]
        public Users Professor { get; set; } = null!;
    }
}

