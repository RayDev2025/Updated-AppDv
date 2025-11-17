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
        public string? Subject { get; set; }

        [StringLength(50)]
        public string? AssignedRoom { get; set; }

        // NEW: Time schedule fields
        [StringLength(10)]
        public string? StartTime { get; set; } // Format: "HH:mm" (e.g., "08:00")

        [StringLength(10)]
        public string? EndTime { get; set; } // Format: "HH:mm" (e.g., "09:00")

        [StringLength(20)]
        public string? DayOfWeek { get; set; } // "Monday", "Tuesday", etc., or "Monday,Wednesday,Friday"

        // Navigation property
        [ForeignKey("ProfessorId")]
        public Users Professor { get; set; } = null!;
    }
}