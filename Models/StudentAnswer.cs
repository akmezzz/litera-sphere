namespace TutorPlatform.Models
{
    public class StudentAnswer
    {
        public int Id { get; set; }
        public int StudentSubmissionId { get; set; }
        public StudentSubmission StudentSubmission { get; set; }

        public int LearningTestQuestionId { get; set; }
        public LearningTestQuestion LearningTestQuestion { get; set; }

        public string SubmittedValue { get; set; }
        public bool IsAutoCorrect { get; set; }
        public decimal AwardedPoints { get; set; }
        public string TutorComment { get; set; }
    }
}


