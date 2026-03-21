using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TutorPlatform.ViewModels
{
    public class TutorDashboardViewModel
    {
        public string TutorName { get; set; }
        public int PendingReviewCount { get; set; }
        public List<TutorGroupCardViewModel> Groups { get; set; } = new List<TutorGroupCardViewModel>();
        public List<TutorTestCardViewModel> Tests { get; set; } = new List<TutorTestCardViewModel>();
        public List<StudentOptionViewModel> Students { get; set; } = new List<StudentOptionViewModel>();
        public List<SubmissionSummaryViewModel> RecentSubmissions { get; set; } = new List<SubmissionSummaryViewModel>();
    }

    public class TutorGroupCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> StudentNames { get; set; } = new List<string>();
    }

    public class TutorTestCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ExamType { get; set; }
        public string MechanicType { get; set; }
        public string ModuleName { get; set; }
        public bool IsPublished { get; set; }
        public int GroupCount { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class StudentOptionViewModel
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Email { get; set; }
        public string GradeLabel { get; set; }
        public List<string> GroupNames { get; set; } = new List<string>();
    }

    public class SubmissionSummaryViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string TestTitle { get; set; }
        public decimal AutoScore { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? TutorScore { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public bool NeedsManualReview { get; set; }
        public bool IsReviewed { get; set; }
    }

    public class ReviewSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string StudentGradeLabel { get; set; }
        public string TestTitle { get; set; }
        public string ExamType { get; set; }
        public string MechanicType { get; set; }
        public decimal AutoScore { get; set; }
        public decimal MaxScore { get; set; }
        public bool NeedsManualReview { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public decimal? TutorScore { get; set; }
        public string TutorFeedback { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public List<SubmissionAnswerReviewViewModel> Answers { get; set; } = new List<SubmissionAnswerReviewViewModel>();
        public decimal ManualScore => Answers.Where(answer => answer.CanEditPoints).Sum(answer => answer.AwardedPoints);
    }

    public class SubmissionAnswerReviewViewModel
    {
        public int AnswerId { get; set; }
        public int Order { get; set; }
        public string Prompt { get; set; }
        public string QuestionType { get; set; }
        public string CorrectAnswer { get; set; }
        public string SubmittedValue { get; set; }
        public string Explanation { get; set; }
        public decimal MaxPoints { get; set; }
        public decimal AwardedPoints { get; set; }
        public bool IsAutoCorrect { get; set; }
        public bool CanEditPoints { get; set; }
    }

    public class ReviewSubmissionInputModel
    {
        public int SubmissionId { get; set; }

        [StringLength(2000)]
        [Display(Name = "Комментарий ученику")]
        public string TutorFeedback { get; set; }

        public List<ReviewAnswerInputModel> Answers { get; set; } = new List<ReviewAnswerInputModel>();
    }

    public class ReviewAnswerInputModel
    {
        public int AnswerId { get; set; }
        public decimal AwardedPoints { get; set; }
    }

    public class CreateStudentViewModel
    {
        [Required]
        [StringLength(120)]
        [Display(Name = "Имя ученика")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(60, MinimumLength = 6)]
        [Display(Name = "Временный пароль")]
        public string Password { get; set; } = "Student123!";

        [Display(Name = "Класс")]
        public string GradeLabel { get; set; }

        [Display(Name = "Сразу добавить в группы")]
        public List<int> GroupIds { get; set; } = new List<int>();

        public List<GroupOptionViewModel> AvailableGroups { get; set; } = new List<GroupOptionViewModel>();
    }

    public class CreateGroupViewModel
    {
        public int? GroupId { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Название группы")]
        public string Name { get; set; }

        [StringLength(500)]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Ученики")]
        public List<string> StudentIds { get; set; } = new List<string>();

        public List<StudentOptionViewModel> AvailableStudents { get; set; } = new List<StudentOptionViewModel>();
        public bool IsEditMode => GroupId.HasValue;
        public string FormTitle => IsEditMode ? "Редактировать группу" : "Создать группу";
        public string SubmitLabel => IsEditMode ? "Сохранить изменения" : "Сохранить группу";
    }

    public class CreateTestViewModel
    {
        [Required]
        [StringLength(180)]
        [Display(Name = "Название теста")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Тип экзамена")]
        public string ExamType { get; set; }

        [Required]
        [Display(Name = "Механика")]
        public string MechanicType { get; set; }

        [Display(Name = "Модуль")]
        public string ModuleName { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Это пробник")]
        public bool IsMockExam { get; set; }

        [Display(Name = "Опубликовать сразу")]
        public bool IsPublished { get; set; } = true;

        [Range(1, 600)]
        [Display(Name = "Таймер, минут")]
        public int? TimeLimitMinutes { get; set; }

        [Display(Name = "Группы для назначения")]
        public List<int> AssignedGroupIds { get; set; } = new List<int>();

        public List<GroupOptionViewModel> AvailableGroups { get; set; } = new List<GroupOptionViewModel>();
        public List<string> AvailableExamTypes { get; set; } = new List<string>();
        public List<string> AvailableMechanics { get; set; } = new List<string>();
        public List<string> AvailableQuestionTypes { get; set; } = new List<string>();
        public List<QuestionEditorViewModel> Questions { get; set; } = new List<QuestionEditorViewModel>();
    }

    public class GroupOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class QuestionEditorViewModel
    {
        [Display(Name = "Формулировка")]
        public string Prompt { get; set; }

        [Display(Name = "Тип")]
        public string QuestionType { get; set; } = "OpenText";

        [Display(Name = "Варианты ответа")]
        public string OptionsText { get; set; }

        [Display(Name = "Правильный ответ")]
        public string CorrectAnswer { get; set; }

        [Display(Name = "Комментарий для проверки")]
        public string Explanation { get; set; }

        [Range(0.5, 50)]
        [Display(Name = "Баллы")]
        public decimal MaxPoints { get; set; } = 1;
    }
}
