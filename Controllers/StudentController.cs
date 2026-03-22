using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorPlatform.Data;
using TutorPlatform.Models;
using TutorPlatform.ViewModels;

namespace TutorPlatform.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var student = await _userManager.GetUserAsync(User);
            if (student.PlatformRole != PlatformRoles.Student)
            {
                return RedirectToAction("Index", "Tutor");
            }

            var assignments = await _dbContext.TestAssignments
                .AsNoTracking()
                .Where(assignment => assignment.StudentGroup.Members.Any(member => member.StudentId == student.Id))
                .Include(assignment => assignment.StudentGroup)
                .Include(assignment => assignment.LearningTest)
                    .ThenInclude(test => test.Questions)
                .OrderByDescending(assignment => assignment.AssignedAtUtc)
                .ToListAsync();

            var submissions = await _dbContext.StudentSubmissions
                .AsNoTracking()
                .Where(submission => submission.StudentId == student.Id)
                .Include(submission => submission.LearningTest)
                .OrderByDescending(submission => submission.SubmittedAtUtc)
                .ToListAsync();

            var groups = await _dbContext.StudentGroupMembers
                .AsNoTracking()
                .Where(member => member.StudentId == student.Id)
                .Include(member => member.StudentGroup)
                .Select(member => member.StudentGroup.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            var lessonAssignments = await _dbContext.LessonAssignments
                .AsNoTracking()
                .Where(assignment => assignment.IsVisibleToStudent && assignment.StudentGroup.Members.Any(member => member.StudentId == student.Id))
                .Include(assignment => assignment.StudentGroup)
                .Include(assignment => assignment.Lesson)
                .OrderBy(assignment => assignment.Lesson.DisplayOrder)
                .ToListAsync();

            var lessonProgress = await _dbContext.StudentLessonProgresses
                .AsNoTracking()
                .Where(progress => progress.StudentId == student.Id)
                .ToListAsync();

            var relevantExamTypes = assignments.Select(assignment => assignment.LearningTest.ExamType)
                .Concat(lessonAssignments.Select(assignment => assignment.Lesson.ExamType))
                .Where(examType => !string.IsNullOrWhiteSpace(examType))
                .Distinct()
                .ToList();

            if (!relevantExamTypes.Any())
            {
                relevantExamTypes = student.GradeLabel != null && student.GradeLabel.Contains("9", StringComparison.OrdinalIgnoreCase)
                    ? new List<string> { "ОГЭ", "Устное собеседование" }
                    : new List<string> { "ЕГЭ", "Итоговое сочинение" };
            }

            var submittedIds = submissions.Select(submission => submission.LearningTestId).ToHashSet();
            var groupedAssignments = assignments
                .GroupBy(assignment => assignment.LearningTestId)
                .Select(group => group.First())
                .ToList();

            var assignedTests = groupedAssignments.Select(assignment => new StudentAssignedTestViewModel
            {
                Id = assignment.LearningTest.Id,
                Title = assignment.LearningTest.Title,
                ExamType = assignment.LearningTest.ExamType,
                MechanicType = assignment.LearningTest.MechanicType,
                GroupName = assignment.StudentGroup.Name,
                QuestionCount = assignment.LearningTest.Questions.Count,
                TimeLimitMinutes = assignment.LearningTest.TimeLimitMinutes,
                AlreadySubmitted = submittedIds.Contains(assignment.LearningTestId),
                IsMockExam = assignment.LearningTest.IsMockExam,
                IsCreativeTask = assignment.LearningTest.MechanicType == "Творческое задание",
                ModuleName = assignment.LearningTest.ModuleName
            }).ToList();

            var lessonCards = lessonAssignments.Select(assignment =>
            {
                var progress = lessonProgress.FirstOrDefault(item => item.LessonAssignmentId == assignment.Id);
                return new LessonCardViewModel
                {
                    AssignmentId = assignment.Id,
                    LessonId = assignment.LessonId,
                    ExamType = assignment.Lesson.ExamType,
                    Title = assignment.Lesson.Title,
                    Theme = assignment.Lesson.Theme,
                    Summary = assignment.Lesson.Summary,
                    Notes = assignment.Lesson.Notes,
                    Homework = assignment.Lesson.Homework,
                    LessonFormat = assignment.Lesson.LessonFormat,
                    GroupName = assignment.StudentGroup.Name,
                    IsVisibleToStudent = assignment.IsVisibleToStudent,
                    IsCompleted = progress?.IsCompleted ?? false,
                    CompletedAtUtc = progress?.CompletedAtUtc
                };
            }).ToList();

            var sections = relevantExamTypes.Select(examType => new DashboardSectionViewModel
            {
                Key = SanitizeKey(examType),
                Title = examType,
                Description = PlatformCatalog.GetExamDescription(examType),
                AccentLabel = examType == "ЕГЭ" ? "3:30" : examType == "ОГЭ" ? "3:55" : "Трек",
                MockExamCount = assignedTests.Count(test => test.ExamType == examType && test.IsMockExam),
                PracticeCount = PlatformCatalog.BuildPracticeTasks(examType == "Устное собеседование" ? "ОГЭ" : examType).Sum(item => item.TaskCount),
                LessonCount = lessonCards.Count(lesson => lesson.ExamType == examType),
                PracticeTasks = PlatformCatalog.BuildPracticeTasks(examType == "Устное собеседование" ? "ОГЭ" : examType)
                    .Take(examType == "ЕГЭ" ? 26 : 12)
                    .Select(item => new PracticeTaskLineViewModel { TaskNumber = item.TaskNumber, Label = item.Label, TaskCount = item.TaskCount })
                    .ToList(),
                FeaturedTests = assignedTests.Where(test => test.ExamType == examType).Take(6).ToList()
            }).ToList();

            var tips = await _dbContext.StudyTips
                .AsNoTracking()
                .Where(tip => tip.ExamType == "Общее" || relevantExamTypes.Contains(tip.ExamType) || (tip.ExamType == "ОГЭ" && relevantExamTypes.Contains("Устное собеседование")))
                .OrderBy(tip => tip.DisplayOrder)
                .Select(tip => new StudyTipViewModel { Id = tip.Id, ExamType = tip.ExamType, Title = tip.Title, Description = tip.Description })
                .ToListAsync();

            var memeLikes = await _dbContext.StudentMemeLikes.AsNoTracking().Where(item => item.StudentId == student.Id).ToListAsync();
            var memes = await _dbContext.DailyMemes.AsNoTracking().OrderBy(item => item.DisplayOrder).Take(3).ToListAsync();
            var memeIds = memes.Select(meme => meme.Id).ToList();
            var likeCounts = await _dbContext.StudentMemeLikes.AsNoTracking()
                .Where(item => memeIds.Contains(item.DailyMemeId))
                .GroupBy(item => item.DailyMemeId)
                .Select(group => new { DailyMemeId = group.Key, Count = group.Count() })
                .ToListAsync();

            var questions = await _dbContext.StudentQuestions.AsNoTracking()
                .Where(question => question.StudentId == student.Id)
                .OrderByDescending(question => question.CreatedAtUtc)
                .Select(question => new StudentQuestionViewModel
                {
                    Id = question.Id,
                    StudentName = string.IsNullOrWhiteSpace(student.FullName) ? student.Email : student.FullName,
                    Topic = question.Topic,
                    Message = question.Message,
                    TutorReply = question.TutorReply,
                    CreatedAtUtc = question.CreatedAtUtc,
                    RepliedAtUtc = question.RepliedAtUtc
                }).ToListAsync();

            var preparationMinutes = submissions.Sum(submission => submission.LearningTest.TimeLimitMinutes ?? 35)
                + lessonCards.Count(lesson => lesson.IsCompleted) * 25
                + lessonCards.Count(lesson => !lesson.IsCompleted) * 10;

            var viewModel = new StudentDashboardViewModel
            {
                StudentName = string.IsNullOrWhiteSpace(student.FullName) ? student.Email : student.FullName,
                GradeLabel = string.IsNullOrWhiteSpace(student.GradeLabel) ? "класс не указан" : student.GradeLabel,
                GroupNames = groups,
                DailyQuote = PlatformCatalog.GetDailyQuote(DateTime.Today),
                DailyCatImageUrl = $"https://cataas.com/cat?width=720&height=400&seed=litera-{DateTime.Today:yyyyMMdd}",
                PreparationMinutes = preparationMinutes,
                CompletedLessonsCount = lessonCards.Count(lesson => lesson.IsCompleted),
                CompletedTestCount = submissions.Count,
                LikedMemesCount = memeLikes.Count,
                Sections = sections,
                AssignedTests = assignedTests,
                ActiveLessons = lessonCards,
                HomeworkLessons = lessonCards.Where(lesson => !lesson.IsCompleted).ToList(),
                Tips = tips,
                Memes = memes.Select(meme => new MemeCardViewModel
                {
                    Id = meme.Id,
                    Title = meme.Title,
                    Caption = meme.Caption,
                    Theme = meme.Theme,
                    ImageUrl = meme.ImageUrl,
                    LikeCount = likeCounts.FirstOrDefault(item => item.DailyMemeId == meme.Id)?.Count ?? 0,
                    IsLikedByCurrentStudent = memeLikes.Any(item => item.DailyMemeId == meme.Id)
                }).ToList(),
                Questions = questions,
                Results = submissions.Select(submission => new StudentResultViewModel
                {
                    Title = submission.LearningTest.Title,
                    ExamType = submission.LearningTest.ExamType,
                    AutoScore = submission.AutoScore,
                    MaxScore = submission.MaxScore,
                    TutorScore = submission.TutorScore,
                    TutorFeedback = submission.TutorFeedback,
                    SubmittedAtUtc = submission.SubmittedAtUtc,
                    ReviewedAtUtc = submission.ReviewedAtUtc,
                    NeedsManualReview = submission.NeedsManualReview,
                    IsReviewed = submission.ReviewedAtUtc.HasValue
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMemeLike(int id)
        {
            var student = await _userManager.GetUserAsync(User);
            var existingLike = await _dbContext.StudentMemeLikes.FirstOrDefaultAsync(item => item.DailyMemeId == id && item.StudentId == student.Id);
            if (existingLike == null)
            {
                _dbContext.StudentMemeLikes.Add(new StudentMemeLike { DailyMemeId = id, StudentId = student.Id, LikedAtUtc = DateTime.UtcNow });
            }
            else
            {
                _dbContext.StudentMemeLikes.Remove(existingLike);
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteLesson(int assignmentId)
        {
            var student = await _userManager.GetUserAsync(User);
            var assignment = await _dbContext.LessonAssignments
                .Include(item => item.StudentGroup)
                    .ThenInclude(group => group.Members)
                .FirstOrDefaultAsync(item => item.Id == assignmentId && item.IsVisibleToStudent);
            if (assignment == null || !assignment.StudentGroup.Members.Any(member => member.StudentId == student.Id))
            {
                return NotFound();
            }

            var progress = await _dbContext.StudentLessonProgresses.FirstOrDefaultAsync(item => item.LessonAssignmentId == assignmentId && item.StudentId == student.Id);
            if (progress == null)
            {
                progress = new StudentLessonProgress { LessonAssignmentId = assignmentId, StudentId = student.Id, OpenedAtUtc = DateTime.UtcNow };
                _dbContext.StudentLessonProgresses.Add(progress);
            }

            progress.IsCompleted = true;
            progress.CompletedAtUtc = DateTime.UtcNow;
            progress.OpenedAtUtc ??= DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Урок отмечен как пройденный, а конспект сохранен в кабинете.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendQuestion(StudentQuestionInputModel model)
        {
            var student = await _userManager.GetUserAsync(User);
            if (!ModelState.IsValid)
            {
                TempData["StatusMessage"] = "Не удалось отправить вопрос. Проверь тему и текст сообщения.";
                return RedirectToAction(nameof(Index));
            }

            var tutorId = await _dbContext.StudentGroupMembers
                .Where(member => member.StudentId == student.Id)
                .Select(member => member.StudentGroup.TutorId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(tutorId))
            {
                TempData["StatusMessage"] = "Сначала ученика нужно прикрепить к группе репетитора.";
                return RedirectToAction(nameof(Index));
            }

            _dbContext.StudentQuestions.Add(new StudentQuestion
            {
                StudentId = student.Id,
                TutorId = tutorId,
                Topic = model.Topic?.Trim(),
                Message = model.Message?.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Вопрос отправлен репетитору.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> TakeTest(int id)
        {
            var student = await _userManager.GetUserAsync(User);
            var test = await _dbContext.LearningTests
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Include(item => item.Questions)
                .Include(item => item.Assignments)
                    .ThenInclude(assignment => assignment.StudentGroup)
                        .ThenInclude(group => group.Members)
                .FirstOrDefaultAsync();

            if (test == null || !test.Assignments.Any(assignment => assignment.StudentGroup.Members.Any(member => member.StudentId == student.Id)))
            {
                return NotFound();
            }

            var existingSubmission = await _dbContext.StudentSubmissions
                .AsNoTracking()
                .AnyAsync(submission => submission.LearningTestId == id && submission.StudentId == student.Id);

            if (existingSubmission)
            {
                TempData["StatusMessage"] = "Этот тест уже отправлен.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new TakeTestViewModel
            {
                TestId = test.Id,
                Title = test.Title,
                Description = test.Description,
                ExamType = test.ExamType,
                MechanicType = test.MechanicType,
                TimeLimitMinutes = test.TimeLimitMinutes,
                Questions = test.Questions
                    .OrderBy(question => question.Order)
                    .Select(question => new QuestionAttemptViewModel
                    {
                        QuestionId = question.Id,
                        Order = question.Order,
                        Prompt = question.Prompt,
                        QuestionType = question.QuestionType,
                        Explanation = question.Explanation,
                        MaxPoints = question.MaxPoints,
                        Options = (question.OptionsText ?? string.Empty)
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(option => option.Trim())
                            .Where(option => !string.IsNullOrWhiteSpace(option))
                            .ToList()
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTest(SubmitTestViewModel model)
        {
            var student = await _userManager.GetUserAsync(User);
            var test = await _dbContext.LearningTests
                .Include(item => item.Questions)
                .FirstOrDefaultAsync(item => item.Id == model.TestId);

            if (test == null)
            {
                return NotFound();
            }

            var existingSubmission = await _dbContext.StudentSubmissions
                .AnyAsync(submission => submission.LearningTestId == model.TestId && submission.StudentId == student.Id);

            if (existingSubmission)
            {
                TempData["StatusMessage"] = "Этот тест уже отправлен.";
                return RedirectToAction(nameof(Index));
            }

            var submission = new StudentSubmission
            {
                LearningTestId = test.Id,
                StudentId = student.Id,
                SubmittedAtUtc = DateTime.UtcNow
            };

            decimal autoScore = 0;
            decimal maxScore = 0;
            var needsManualReview = false;

            foreach (var question in test.Questions.OrderBy(item => item.Order))
            {
                var answerInput = model.Answers.FirstOrDefault(answer => answer.QuestionId == question.Id);
                var submittedValue = answerInput?.SubmittedValue?.Trim() ?? string.Empty;
                var isEssayLike = question.QuestionType == "Essay" || question.QuestionType == "AudioPrompt" || string.IsNullOrWhiteSpace(question.CorrectAnswer);

                var isAutoCorrect = false;
                decimal awardedPoints = 0;
                maxScore += question.MaxPoints;

                if (!isEssayLike)
                {
                    isAutoCorrect = string.Equals(submittedValue, question.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);
                    if (isAutoCorrect)
                    {
                        awardedPoints = question.MaxPoints;
                        autoScore += question.MaxPoints;
                    }
                }
                else
                {
                    needsManualReview = true;
                }

                submission.Answers.Add(new StudentAnswer
                {
                    LearningTestQuestionId = question.Id,
                    SubmittedValue = submittedValue,
                    IsAutoCorrect = isAutoCorrect,
                    AwardedPoints = awardedPoints
                });
            }

            submission.AutoScore = autoScore;
            submission.MaxScore = maxScore;
            submission.NeedsManualReview = needsManualReview;

            _dbContext.StudentSubmissions.Add(submission);
            await _dbContext.SaveChangesAsync();

            TempData["StatusMessage"] = string.Format(CultureInfo.InvariantCulture, "Работа отправлена. Автоматический результат: {0}/{1}.", submission.AutoScore, submission.MaxScore);
            return RedirectToAction(nameof(Index));
        }

        private static string SanitizeKey(string value)
        {
            return (value ?? string.Empty).ToLowerInvariant().Replace(" ", "-");
        }
    }
}
