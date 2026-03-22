using System;

namespace TutorPlatform.Models
{
    public class StudentLessonProgress
    {
        public int Id { get; set; }
        public int LessonAssignmentId { get; set; }
        public LessonAssignment LessonAssignment { get; set; }

        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? OpenedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
