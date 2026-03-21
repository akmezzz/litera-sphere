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
    [Authorize(Roles = PlatformRoles.Tutor)]
    public class TutorController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public TutorController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var tutor = await _userManager.GetUserAsync(User);
            var groups = await _dbContext.StudentGroups
                .AsNoTracking()
                .Where(group => group.TutorId == tutor.Id)
                .Include(group => group.Members)
                    .ThenInclude(member => member.Student)
                .OrderBy(group => group.Name)
                .ToListAsync();

            var tests = await _dbContext.LearningTests
                .AsNoTracking()
                .Where(test => test.TutorId == tutor.Id)
                .Include(test => test.Assignments)
                .Include(test => test.Submissions)
                .OrderByDescending(test => test.CreatedAtUtc)
                .ToListAsync();

            var studentMemberships = await _dbContext.StudentGroupMembers
                .AsNoTracking()
                .Include(member => member.StudentGroup)
                .ToListAsync();

            var students = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.PlatformRole == PlatformRoles.Student || string.IsNullOrWhiteSpace(user.PlatformRole))
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .ToListAsync();

            var submissions = await _dbContext.StudentSubmissions
                .AsNoTracking()
                .Where(submission => submission.LearningTest.TutorId == tutor.Id)
                .Include(submission => submission.Student)
                .Include(submission => submission.LearningTest)
                .OrderByDescending(submission => submission.SubmittedAtUtc)
                .Take(8)
                .ToListAsync();

            var viewModel = new TutorDashboardViewModel
            {
                TutorName = string.IsNullOrWhiteSpace(tutor.FullName) ? tutor.Email : tutor.FullName,
                Students = students.Select(user => new StudentOptionViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    GradeLabel = user.GradeLabel,
                    Label = BuildStudentLabel(user.FullName, user.Email, user.GradeLabel),
                    GroupNames = studentMemberships
                        .Where(member => member.StudentId == user.Id)
                        .Select(member => member.StudentGroup.Name)
                        .OrderBy(name => name)
                        .ToList()
                }).ToList(),
                PendingReviewCount = submissions.Count(submission => submission.NeedsManualReview || !submission.ReviewedAtUtc.HasValue),
                Groups = groups.Select(group => new TutorGroupCardViewModel
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    StudentNames = group.Members
                        .Select(member => string.IsNullOrWhiteSpace(member.Student.FullName) ? member.Student.Email : member.Student.FullName)
                        .OrderBy(name => name)
                        .ToList()
                }).ToList(),
                Tests = tests.Select(test => new TutorTestCardViewModel
                {
                    Id = test.Id,
                    Title = test.Title,
                    ExamType = test.ExamType,
                    MechanicType = test.MechanicType,
                    ModuleName = test.ModuleName,
                    IsPublished = test.IsPublished,
                    GroupCount = test.Assignments.Count,
                    SubmissionCount = test.Submissions.Count
                }).ToList(),
                RecentSubmissions = submissions.Select(submission => new SubmissionSummaryViewModel
                {
                    Id = submission.Id,
                    StudentName = string.IsNullOrWhiteSpace(submission.Student.FullName) ? submission.Student.Email : submission.Student.FullName,
                    TestTitle = submission.LearningTest.Title,
                    AutoScore = submission.AutoScore,
                    MaxScore = submission.MaxScore,
                    TutorScore = submission.TutorScore,
                    SubmittedAtUtc = submission.SubmittedAtUtc,
                    NeedsManualReview = submission.NeedsManualReview,
                    IsReviewed = submission.ReviewedAtUtc.HasValue
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStudent()
        {
            return View(await BuildCreateStudentModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(await BuildCreateStudentModelAsync(model));
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email уже существует.");
                return View(await BuildCreateStudentModelAsync(model));
            }

            var student = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName?.Trim(),
                PlatformRole = PlatformRoles.Student,
                GradeLabel = model.GradeLabel?.Trim()
            };

            var result = await _userManager.CreateAsync(student, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(await BuildCreateStudentModelAsync(model));
            }

            foreach (var groupId in model.GroupIds.Distinct())
            {
                _dbContext.StudentGroupMembers.Add(new StudentGroupMember
                {
                    StudentGroupId = groupId,
                    StudentId = student.Id
                });
            }

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Ученик зарегистрирован и готов к работе.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ReviewSubmission(int id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var viewModel = await BuildReviewSubmissionViewModelAsync(id, tutor.Id);
            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSubmission(ReviewSubmissionInputModel model)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var submission = await _dbContext.StudentSubmissions
                .Include(item => item.LearningTest)
                .Include(item => item.Answers)
                    .ThenInclude(answer => answer.LearningTestQuestion)
                .FirstOrDefaultAsync(item => item.Id == model.SubmissionId && item.LearningTest.TutorId == tutor.Id);

            if (submission == null)
            {
                return NotFound();
            }

            var inputById = model.Answers.ToDictionary(answer => answer.AnswerId, answer => answer);
            decimal manualScore = 0;

            foreach (var answer in submission.Answers)
            {
                var isManualQuestion = IsManualQuestion(answer.LearningTestQuestion);
                if (!isManualQuestion)
                {
                    continue;
                }

                if (!inputById.TryGetValue(answer.Id, out var answerInput))
                {
                    continue;
                }

                if (answerInput.AwardedPoints < 0 || answerInput.AwardedPoints > answer.LearningTestQuestion.MaxPoints)
                {
                    ModelState.AddModelError(string.Empty, $"Баллы за задание {answer.LearningTestQuestion.Order} должны быть от 0 до {answer.LearningTestQuestion.MaxPoints}.");
                    break;
                }

                answer.AwardedPoints = answerInput.AwardedPoints;
                manualScore += answer.AwardedPoints;
            }

            if (!ModelState.IsValid)
            {
                var reviewViewModel = await BuildReviewSubmissionViewModelAsync(model.SubmissionId, tutor.Id);
                if (reviewViewModel == null)
                {
                    return NotFound();
                }

                reviewViewModel.TutorFeedback = model.TutorFeedback;
                foreach (var answerViewModel in reviewViewModel.Answers.Where(answer => answer.CanEditPoints))
                {
                    if (inputById.TryGetValue(answerViewModel.AnswerId, out var answerInput))
                    {
                        answerViewModel.AwardedPoints = answerInput.AwardedPoints;
                    }
                }

                reviewViewModel.TutorScore = submission.AutoScore + reviewViewModel.Answers.Where(answer => answer.CanEditPoints).Sum(answer => answer.AwardedPoints);
                return View(reviewViewModel);
            }

            submission.TutorScore = submission.AutoScore + manualScore;
            submission.TutorFeedback = model.TutorFeedback?.Trim();
            submission.ReviewedAtUtc = DateTime.UtcNow;
            submission.NeedsManualReview = false;

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Баллы и комментарий сохранены. Ученик увидит их в своем кабинете.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateGroup()
        {
            return View(await BuildCreateGroupModelAsync());
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(int id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var group = await _dbContext.StudentGroups
                .AsNoTracking()
                .Where(item => item.Id == id && item.TutorId == tutor.Id)
                .Include(item => item.Members)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return NotFound();
            }

            var model = new CreateGroupViewModel
            {
                GroupId = group.Id,
                Name = group.Name,
                Description = group.Description,
                StudentIds = group.Members.Select(member => member.StudentId).ToList()
            };

            return View("CreateGroup", await BuildCreateGroupModelAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(await BuildCreateGroupModelAsync(model));
            }

            var tutor = await _userManager.GetUserAsync(User);
            var selectedStudents = model.StudentIds?.Distinct().ToList() ?? new List<string>();

            if (model.GroupId.HasValue)
            {
                var existingGroup = await _dbContext.StudentGroups
                    .Include(group => group.Members)
                    .FirstOrDefaultAsync(group => group.Id == model.GroupId.Value && group.TutorId == tutor.Id);

                if (existingGroup == null)
                {
                    return NotFound();
                }

                existingGroup.Name = model.Name;
                existingGroup.Description = model.Description;

                var existingMemberIds = existingGroup.Members.Select(member => member.StudentId).ToList();
                var membersToRemove = existingGroup.Members.Where(member => !selectedStudents.Contains(member.StudentId)).ToList();
                _dbContext.StudentGroupMembers.RemoveRange(membersToRemove);

                foreach (var studentId in selectedStudents.Where(studentId => !existingMemberIds.Contains(studentId)))
                {
                    _dbContext.StudentGroupMembers.Add(new StudentGroupMember
                    {
                        StudentGroupId = existingGroup.Id,
                        StudentId = studentId
                    });
                }

                await _dbContext.SaveChangesAsync();
                TempData["StatusMessage"] = "Состав группы обновлен.";
                return RedirectToAction(nameof(Index));
            }

            var group = new StudentGroup
            {
                Name = model.Name,
                Description = model.Description,
                TutorId = tutor.Id
            };

            _dbContext.StudentGroups.Add(group);
            await _dbContext.SaveChangesAsync();

            foreach (var studentId in selectedStudents)
            {
                _dbContext.StudentGroupMembers.Add(new StudentGroupMember
                {
                    StudentGroupId = group.Id,
                    StudentId = studentId
                });
            }

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Группа создана.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateTest()
        {
            return View(await BuildCreateTestModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTest(CreateTestViewModel model)
        {
            model.Questions = model.Questions?.Where(question => !string.IsNullOrWhiteSpace(question.Prompt)).ToList()
                ?? new List<QuestionEditorViewModel>();

            if (!model.Questions.Any())
            {
                ModelState.AddModelError(string.Empty, "Добавьте хотя бы один вопрос.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildCreateTestModelAsync(model));
            }

            var tutor = await _userManager.GetUserAsync(User);
            var test = new LearningTest
            {
                Title = model.Title,
                ExamType = model.ExamType,
                MechanicType = model.MechanicType,
                ModuleName = model.ModuleName,
                Description = model.Description,
                IsMockExam = model.IsMockExam,
                IsPublished = model.IsPublished,
                TimeLimitMinutes = model.TimeLimitMinutes,
                TutorId = tutor.Id,
                CreatedAtUtc = DateTime.UtcNow,
                Questions = model.Questions.Select((question, index) => new LearningTestQuestion
                {
                    Order = index + 1,
                    Prompt = question.Prompt,
                    QuestionType = question.QuestionType,
                    OptionsText = question.OptionsText,
                    CorrectAnswer = question.CorrectAnswer,
                    Explanation = question.Explanation,
                    MaxPoints = question.MaxPoints
                }).ToList()
            };

            _dbContext.LearningTests.Add(test);
            await _dbContext.SaveChangesAsync();

            foreach (var groupId in model.AssignedGroupIds.Distinct())
            {
                _dbContext.TestAssignments.Add(new TestAssignment
                {
                    LearningTestId = test.Id,
                    StudentGroupId = groupId
                });
            }

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Тест сохранен и назначен выбранным группам.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<CreateStudentViewModel> BuildCreateStudentModelAsync(CreateStudentViewModel model = null)
        {
            model ??= new CreateStudentViewModel();
            var tutor = await _userManager.GetUserAsync(User);
            model.AvailableGroups = await _dbContext.StudentGroups
                .AsNoTracking()
                .Where(group => group.TutorId == tutor.Id)
                .OrderBy(group => group.Name)
                .Select(group => new GroupOptionViewModel
                {
                    Id = group.Id,
                    Name = group.Name
                })
                .ToListAsync();

            return model;
        }

        private async Task<CreateGroupViewModel> BuildCreateGroupModelAsync(CreateGroupViewModel model = null)
        {
            model ??= new CreateGroupViewModel();
            model.AvailableStudents = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.PlatformRole == PlatformRoles.Student || string.IsNullOrWhiteSpace(user.PlatformRole))
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .Select(user => new StudentOptionViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    GradeLabel = user.GradeLabel,
                    Label = BuildStudentLabel(user.FullName, user.Email, user.GradeLabel)
                })
                .ToListAsync();

            return model;
        }

        private async Task<CreateTestViewModel> BuildCreateTestModelAsync(CreateTestViewModel model = null)
        {
            model ??= new CreateTestViewModel();
            var tutor = await _userManager.GetUserAsync(User);
            model.AvailableGroups = await _dbContext.StudentGroups
                .AsNoTracking()
                .Where(group => group.TutorId == tutor.Id)
                .OrderBy(group => group.Name)
                .Select(group => new GroupOptionViewModel
                {
                    Id = group.Id,
                    Name = group.Name
                })
                .ToListAsync();

            model.AvailableExamTypes = PlatformCatalog.ExamTypes.ToList();
            model.AvailableMechanics = PlatformCatalog.Mechanics.ToList();
            model.AvailableQuestionTypes = PlatformCatalog.QuestionTypes.ToList();

            if (!model.Questions.Any())
            {
                model.Questions.Add(new QuestionEditorViewModel());
                model.Questions.Add(new QuestionEditorViewModel());
            }

            return model;
        }

        private async Task<ReviewSubmissionViewModel> BuildReviewSubmissionViewModelAsync(int submissionId, string tutorId)
        {
            var submission = await _dbContext.StudentSubmissions
                .AsNoTracking()
                .Where(item => item.Id == submissionId && item.LearningTest.TutorId == tutorId)
                .Include(item => item.Student)
                .Include(item => item.LearningTest)
                .Include(item => item.Answers)
                    .ThenInclude(answer => answer.LearningTestQuestion)
                .FirstOrDefaultAsync();

            if (submission == null)
            {
                return null;
            }

            return new ReviewSubmissionViewModel
            {
                SubmissionId = submission.Id,
                StudentName = string.IsNullOrWhiteSpace(submission.Student.FullName) ? submission.Student.Email : submission.Student.FullName,
                StudentEmail = submission.Student.Email,
                StudentGradeLabel = submission.Student.GradeLabel,
                TestTitle = submission.LearningTest.Title,
                ExamType = submission.LearningTest.ExamType,
                MechanicType = submission.LearningTest.MechanicType,
                AutoScore = submission.AutoScore,
                MaxScore = submission.MaxScore,
                NeedsManualReview = submission.NeedsManualReview,
                SubmittedAtUtc = submission.SubmittedAtUtc,
                TutorScore = submission.TutorScore,
                TutorFeedback = submission.TutorFeedback,
                ReviewedAtUtc = submission.ReviewedAtUtc,
                Answers = submission.Answers
                    .OrderBy(answer => answer.LearningTestQuestion.Order)
                    .Select(answer => new SubmissionAnswerReviewViewModel
                    {
                        AnswerId = answer.Id,
                        Order = answer.LearningTestQuestion.Order,
                        Prompt = answer.LearningTestQuestion.Prompt,
                        QuestionType = answer.LearningTestQuestion.QuestionType,
                        CorrectAnswer = answer.LearningTestQuestion.CorrectAnswer,
                        SubmittedValue = answer.SubmittedValue,
                        Explanation = answer.LearningTestQuestion.Explanation,
                        MaxPoints = answer.LearningTestQuestion.MaxPoints,
                        AwardedPoints = answer.AwardedPoints,
                        IsAutoCorrect = answer.IsAutoCorrect,
                        CanEditPoints = IsManualQuestion(answer.LearningTestQuestion)
                    })
                    .ToList()
            };
        }

        private static bool IsManualQuestion(LearningTestQuestion question)
        {
            return question.QuestionType == "Essay"
                || question.QuestionType == "AudioPrompt"
                || question.QuestionType == "OpenText"
                || string.IsNullOrWhiteSpace(question.CorrectAnswer);
        }

        private static string BuildStudentLabel(string fullName, string email, string gradeLabel)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} | {1}{2}",
                string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                email,
                string.IsNullOrWhiteSpace(gradeLabel) ? string.Empty : $" | {gradeLabel}");
        }
    }
}
