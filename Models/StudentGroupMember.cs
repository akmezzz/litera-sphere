namespace TutorPlatform.Models
{
    public class StudentGroupMember
    {
        public int StudentGroupId { get; set; }
        public StudentGroup StudentGroup { get; set; }

        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }
    }
}
