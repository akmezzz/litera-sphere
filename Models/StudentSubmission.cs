using System;
using System.Collections.Generic;

namespace TutorPlatform.Models
{
    public class StudentSubmission
    {
        public int Id { get; set; }
        public int LearningTestId { get; set; }
        public LearningTest LearningTest { get; set; }

        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
        public decimal AutoScore { get; set; }
        public decimal MaxScore { get; set; }
        public bool NeedsManualReview { get; set; }

        public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
    }
}
