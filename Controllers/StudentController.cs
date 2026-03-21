using System;
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

            var submittedIds = submissions.Select(submission => submission.LearningTestId).ToHashSet();

            var viewModel = new StudentDashboardViewModel
            {
                StudentName = string.IsNullOrWhiteSpace(student.FullName) ? student.Email : student.FullName,
                GradeLabel = string.IsNullOrWhiteSpace(student.GradeLabel) ? "класс не указан" : student.GradeLabel,
                GroupNames = groups,
                AssignedTests = assignments.Select(assignment => new StudentAssignedTestViewModel
                {
                    Id = assignment.LearningTest.Id,
                    Title = assignment.LearningTest.Title,
                    ExamType = assignment.LearningTest.ExamType,
                    MechanicType = assignment.LearningTest.MechanicType,
                    GroupName = assignment.StudentGroup.Name,
                    QuestionCount = assignment.LearningTest.Questions.Count,
                    TimeLimitMinutes = assignment.LearningTest.TimeLimitMinutes,
                    AlreadySubmitted = submittedIds.Contains(assignment.LearningTestId)
                }).ToList(),
                Results = submissions.Select(submission => new StudentResultViewModel
                {
                    Title = submission.LearningTest.Title,
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
                    isAutoCorrect = string.Equals(
                        submittedValue,
                        question.CorrectAnswer?.Trim(),
                        StringComparison.OrdinalIgnoreCase);

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

            TempData["StatusMessage"] = string.Format(
                CultureInfo.InvariantCulture,
                "Работа отправлена. Автоматический результат: {0}/{1}.",
                submission.AutoScore,
                submission.MaxScore);

            return RedirectToAction(nameof(Index));
        }
    }
}
