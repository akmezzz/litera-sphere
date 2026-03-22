namespace TutorPlatform.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string ExamType { get; set; }
        public string Title { get; set; }
        public string Theme { get; set; }
        public string Summary { get; set; }
        public string Notes { get; set; }
        public string Homework { get; set; }
        public string LessonFormat { get; set; }
        public int DisplayOrder { get; set; }
    }
}
