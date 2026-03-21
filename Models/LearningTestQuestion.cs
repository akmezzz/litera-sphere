using System.ComponentModel.DataAnnotations;

namespace TutorPlatform.Models
{
    public class LearningTestQuestion
    {
        public int Id { get; set; }
        public int LearningTestId { get; set; }
        public LearningTest LearningTest { get; set; }

        public int Order { get; set; }

        [Required]
        [StringLength(3000)]
        public string Prompt { get; set; }

        [Required]
        [StringLength(32)]
        public string QuestionType { get; set; }

        [StringLength(3000)]
        public string OptionsText { get; set; }

        [StringLength(1000)]
        public string CorrectAnswer { get; set; }

        [StringLength(2000)]
        public string Explanation { get; set; }

        public decimal MaxPoints { get; set; } = 1;
    }
}
