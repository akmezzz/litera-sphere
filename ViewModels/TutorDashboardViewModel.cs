using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TutorPlatform.ViewModels
{
    public class TutorDashboardViewModel
    {
        public string TutorName { get; set; }
        public List<TutorGroupCardViewModel> Groups { get; set; } = new List<TutorGroupCardViewModel>();
        public List<TutorTestCardViewModel> Tests { get; set; } = new List<TutorTestCardViewModel>();
        public List<StudentOptionViewModel> Students { get; set; } = new List<StudentOptionViewModel>();
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
    }

    public class CreateGroupViewModel
    {
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
