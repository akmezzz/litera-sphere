using System;

namespace TutorPlatform.Models
{
    public class TestAssignment
    {
        public int Id { get; set; }
        public int LearningTestId { get; set; }
        public LearningTest LearningTest { get; set; }

        public int StudentGroupId { get; set; }
        public StudentGroup StudentGroup { get; set; }

        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
