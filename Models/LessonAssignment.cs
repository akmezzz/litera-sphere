using System;

namespace TutorPlatform.Models
{
    public class LessonAssignment
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }

        public int StudentGroupId { get; set; }
        public StudentGroup StudentGroup { get; set; }

        public bool IsVisibleToStudent { get; set; } = true;
        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
