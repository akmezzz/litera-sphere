using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TutorPlatform.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }
        public string GradeLabel { get; set; }
        public List<string> GroupNames { get; set; } = new List<string>();
        public List<StudentAssignedTestViewModel> AssignedTests { get; set; } = new List<StudentAssignedTestViewModel>();
        public List<StudentResultViewModel> Results { get; set; } = new List<StudentResultViewModel>();
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
    }

    public class StudentResultViewModel
    {
        public string Title { get; set; }
        public decimal AutoScore { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public bool NeedsManualReview { get; set; }
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
}
