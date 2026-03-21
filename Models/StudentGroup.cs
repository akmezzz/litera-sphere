using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TutorPlatform.Models
{
    public class StudentGroup
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string TutorId { get; set; }
        public ApplicationUser Tutor { get; set; }

        public ICollection<StudentGroupMember> Members { get; set; } = new List<StudentGroupMember>();
        public ICollection<TestAssignment> Assignments { get; set; } = new List<TestAssignment>();
    }
}
