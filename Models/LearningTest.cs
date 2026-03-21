using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TutorPlatform.Models
{
    public class LearningTest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(180)]
        public string Title { get; set; }

        [Required]
        [StringLength(32)]
        public string ExamType { get; set; }

        [Required]
        [StringLength(64)]
        public string MechanicType { get; set; }

        [StringLength(80)]
        public string ModuleName { get; set; }

        [StringLength(2400)]
        public string Description { get; set; }

        public bool IsPublished { get; set; }
        public bool IsMockExam { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public string TutorId { get; set; }
        public ApplicationUser Tutor { get; set; }

        public ICollection<LearningTestQuestion> Questions { get; set; } = new List<LearningTestQuestion>();
        public ICollection<TestAssignment> Assignments { get; set; } = new List<TestAssignment>();
        public ICollection<StudentSubmission> Submissions { get; set; } = new List<StudentSubmission>();
    }
}
