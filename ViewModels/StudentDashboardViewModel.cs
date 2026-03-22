using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TutorPlatform.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }
        public string GradeLabel { get; set; }
        public string DailyQuote { get; set; }
        public string DailyCatImageUrl { get; set; }
        public int PreparationMinutes { get; set; }
        public int CompletedLessonsCount { get; set; }
        public int CompletedTestCount { get; set; }
        public int LikedMemesCount { get; set; }
        public List<string> GroupNames { get; set; } = new List<string>();
        public List<DashboardSectionViewModel> Sections { get; set; } = new List<DashboardSectionViewModel>();
        public List<StudentAssignedTestViewModel> AssignedTests { get; set; } = new List<StudentAssignedTestViewModel>();
        public List<StudentResultViewModel> Results { get; set; } = new List<StudentResultViewModel>();
        public List<LessonCardViewModel> ActiveLessons { get; set; } = new List<LessonCardViewModel>();
        public List<LessonCardViewModel> HomeworkLessons { get; set; } = new List<LessonCardViewModel>();
        public List<StudyTipViewModel> Tips { get; set; } = new List<StudyTipViewModel>();
        public List<MemeCardViewModel> Memes { get; set; } = new List<MemeCardViewModel>();
        public List<StudentQuestionViewModel> Questions { get; set; } = new List<StudentQuestionViewModel>();
    }

    public class StudentAssignedTestViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ExamType { get; set; }
        public string MechanicType { get; set; }
        public string GroupName { get; set; }
        public int QuestionCount { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public bool AlreadySubmitted { get; set; }
        public bool IsMockExam { get; set; }
        public bool IsCreativeTask { get; set; }
        public string ModuleName { get; set; }
    }

    public class StudentResultViewModel
    {
        public string Title { get; set; }
        public string ExamType { get; set; }
        public decimal AutoScore { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? TutorScore { get; set; }
        public string TutorFeedback { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public bool NeedsManualReview { get; set; }
        public bool IsReviewed { get; set; }
    }

    public class TakeTestViewModel
    {
        public int TestId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ExamType { get; set; }
        public string MechanicType { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public List<QuestionAttemptViewModel> Questions { get; set; } = new List<QuestionAttemptViewModel>();
    }

    public class QuestionAttemptViewModel
    {
        public int QuestionId { get; set; }
        public int Order { get; set; }
        public string Prompt { get; set; }
        public string QuestionType { get; set; }
        public string Explanation { get; set; }
        public decimal MaxPoints { get; set; }
        public List<string> Options { get; set; } = new List<string>();
    }

    public class SubmitTestViewModel
    {
        public int TestId { get; set; }
        public List<SubmittedAnswerInputModel> Answers { get; set; } = new List<SubmittedAnswerInputModel>();
    }

    public class SubmittedAnswerInputModel
    {
        public int QuestionId { get; set; }

        [Display(Name = "Ответ")]
        public string SubmittedValue { get; set; }
    }

    public class StudentQuestionInputModel
    {
        [Required]
        [StringLength(120)]
        [Display(Name = "Тема")]
        public string Topic { get; set; }

        [Required]
        [StringLength(2000)]
        [Display(Name = "Вопрос или пожелание")]
        public string Message { get; set; }
    }
}
