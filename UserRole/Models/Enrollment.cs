using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserRoles.Models
{
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // CHANGED FROM int TO string

        [Required]
        [StringLength(100)]
        public string StudentName { get; set; }

        [Required]
        [StringLength(10)]
        public string GradeLevel { get; set; } // "1", "2", "3", "4", "5", "6"

        public int? Section { get; set; }

        [Required]
        [StringLength(255)]
        public string BirthCertificate { get; set; }

        [Required]
        [StringLength(255)]
        public string Form137 { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; } = "pending"; // "pending", "approved", "rejected"

        [StringLength(100)]
        public string ParentName { get; set; }

        [StringLength(20)]
        public string ContactNumber { get; set; }

        public string Address { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual Users User { get; set; }
    }
}