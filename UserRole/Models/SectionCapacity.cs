using System.ComponentModel.DataAnnotations;

namespace UserRoles.Models
{
    public class SectionCapacity
    {
        [Key]
        [StringLength(10)]
        public string GradeLevel { get; set; } // "1", "2", "3", "4", "5", "6"

        public int CurrentSection { get; set; } = 1;

        public int StudentsInCurrentSection { get; set; } = 0;
    }
}