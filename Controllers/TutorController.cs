using System;
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

            var students = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.PlatformRole == PlatformRoles.Student)
                .OrderBy(user => user.FullName)
                .Select(user => new StudentOptionViewModel
                {
                    Id = user.Id,
                    Label = $"{user.FullName} ({user.Email})"
                })
                .ToListAsync();

            var viewModel = new TutorDashboardViewModel
            {
                TutorName = tutor.FullName,
                Students = students,
                Groups = groups.Select(group => new TutorGroupCardViewModel
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    StudentNames = group.Members.Select(member => member.Student.FullName).OrderBy(name => name).ToList()
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
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateGroup()
        {
            return View(await BuildCreateGroupModelAsync());
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
            var group = new StudentGroup
            {
                Name = model.Name,
                Description = model.Description,
                TutorId = tutor.Id
            };

            _dbContext.StudentGroups.Add(group);
            await _dbContext.SaveChangesAsync();

            var selectedStudents = model.StudentIds?.Distinct().ToList() ?? new System.Collections.Generic.List<string>();
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
                ?? new System.Collections.Generic.List<QuestionEditorViewModel>();

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

        private async Task<CreateGroupViewModel> BuildCreateGroupModelAsync(CreateGroupViewModel model = null)
        {
            model ??= new CreateGroupViewModel();
            model.AvailableStudents = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.PlatformRole == PlatformRoles.Student)
                .OrderBy(user => user.FullName)
                .Select(user => new StudentOptionViewModel
                {
                    Id = user.Id,
                    Label = $"{user.FullName} ({user.GradeLabel})"
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
    }
}
