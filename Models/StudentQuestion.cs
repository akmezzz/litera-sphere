using System;

namespace TutorPlatform.Models
{
    public class StudentQuestion
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }
        public string TutorId { get; set; }
        public string Topic { get; set; }
        public string Message { get; set; }
        public string TutorReply { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RepliedAtUtc { get; set; }
    }
}
