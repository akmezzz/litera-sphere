using System;

namespace TutorPlatform.Models
{
    public class StudentMemeLike
    {
        public int Id { get; set; }
        public int DailyMemeId { get; set; }
        public DailyMeme DailyMeme { get; set; }

        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }

        public DateTime LikedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
