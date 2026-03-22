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
            var viewModel = await BuildTutorDashboardAsync(tutor.Id, tutor.FullName, tutor.Email);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Students(string searchQuery = null)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var students = await BuildStudentCardsAsync(tutor.Id, searchQuery);
            return View(new StudentsListViewModel { SearchQuery = searchQuery, Students = students });
        }

        [HttpGet]
        public async Task<IActionResult> StudentDetails(string id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var student = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id && user.PlatformRole == PlatformRoles.Student);
            if (student == null) return NotFound();

            var groupMemberships = await _dbContext.StudentGroupMembers.AsNoTracking()
                .Where(member => member.StudentId == student.Id && member.StudentGroup.TutorId == tutor.Id)
                .Include(member => member.StudentGroup)
                .OrderBy(member => member.StudentGroup.Name)
                .ToListAsync();

            var submissions = await _dbContext.StudentSubmissions.AsNoTracking()
                .Where(submission => submission.StudentId == student.Id && submission.LearningTest.TutorId == tutor.Id)
                .Include(submission => submission.LearningTest)
                .OrderByDescending(submission => submission.SubmittedAtUtc)
                .ToListAsync();

            return View(new StudentDetailsViewModel
            {
                StudentId = student.Id,
                FullName = string.IsNullOrWhiteSpace(student.FullName) ? student.Email : student.FullName,
                Email = student.Email,
                GradeLabel = student.GradeLabel,
                Groups = groupMemberships.Select(member => new GroupOptionViewModel { Id = member.StudentGroupId, Name = member.StudentGroup.Name }).ToList(),
                Submissions = submissions.Select(submission => new SubmissionSummaryViewModel
                {
                    Id = submission.Id,
                    StudentName = string.IsNullOrWhiteSpace(student.FullName) ? student.Email : student.FullName,
                    TestTitle = submission.LearningTest.Title,
                    AutoScore = submission.AutoScore,
                    MaxScore = submission.MaxScore,
                    TutorScore = submission.TutorScore,
                    SubmittedAtUtc = submission.SubmittedAtUtc,
                    NeedsManualReview = submission.NeedsManualReview,
                    IsReviewed = submission.ReviewedAtUtc.HasValue,
                    ExamType = submission.LearningTest.ExamType
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateStudent() => View(await BuildCreateStudentModelAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel model)
        {
            if (!ModelState.IsValid) return View(await BuildCreateStudentModelAsync(model));
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
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(await BuildCreateStudentModelAsync(model));
            }

            foreach (var groupId in model.GroupIds.Distinct())
                _dbContext.StudentGroupMembers.Add(new StudentGroupMember { StudentGroupId = groupId, StudentId = student.Id });

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Ученик зарегистрирован и готов к работе.";
            return RedirectToAction(nameof(Students));
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(string id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var student = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id && user.PlatformRole == PlatformRoles.Student);
            if (student == null) return NotFound();

            var groupIds = await _dbContext.StudentGroupMembers.AsNoTracking()
                .Where(member => member.StudentId == student.Id && member.StudentGroup.TutorId == tutor.Id)
                .Select(member => member.StudentGroupId)
                .ToListAsync();

            var model = new EditStudentViewModel { StudentId = student.Id, FullName = student.FullName, Email = student.Email, GradeLabel = student.GradeLabel, GroupIds = groupIds };
            return View(await BuildEditStudentModelAsync(model, tutor.Id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(EditStudentViewModel model)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var student = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == model.StudentId && user.PlatformRole == PlatformRoles.Student);
            if (student == null) return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != student.Id) ModelState.AddModelError(nameof(model.Email), "Этот email уже занят другим пользователем.");
            if (!ModelState.IsValid) return View(await BuildEditStudentModelAsync(model, tutor.Id));

            student.FullName = model.FullName?.Trim();
            student.Email = model.Email?.Trim();
            student.UserName = model.Email?.Trim();
            student.GradeLabel = model.GradeLabel?.Trim();
            await _userManager.UpdateAsync(student);

            var selectedGroupIds = model.GroupIds?.Distinct().ToList() ?? new List<int>();
            var memberships = await _dbContext.StudentGroupMembers.Where(member => member.StudentId == student.Id && member.StudentGroup.TutorId == tutor.Id).ToListAsync();
            var existingGroupIds = memberships.Select(member => member.StudentGroupId).ToList();
            _dbContext.StudentGroupMembers.RemoveRange(memberships.Where(member => !selectedGroupIds.Contains(member.StudentGroupId)));
            foreach (var groupId in selectedGroupIds.Where(groupId => !existingGroupIds.Contains(groupId)))
                _dbContext.StudentGroupMembers.Add(new StudentGroupMember { StudentGroupId = groupId, StudentId = student.Id });

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Профиль ученика обновлен.";
            return RedirectToAction(nameof(StudentDetails), new { id = student.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudentFromGroup(string studentId, int groupId)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var membership = await _dbContext.StudentGroupMembers.Include(member => member.StudentGroup)
                .FirstOrDefaultAsync(member => member.StudentId == studentId && member.StudentGroupId == groupId && member.StudentGroup.TutorId == tutor.Id);
            if (membership == null) return NotFound();
            _dbContext.StudentGroupMembers.Remove(membership);
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Ученик удален из группы.";
            return RedirectToAction(nameof(StudentDetails), new { id = studentId });
        }

        [HttpGet]
        public async Task<IActionResult> ReviewSubmission(int id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var viewModel = await BuildReviewSubmissionViewModelAsync(id, tutor.Id);
            if (viewModel == null) return NotFound();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSubmission(ReviewSubmissionInputModel model)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var submission = await _dbContext.StudentSubmissions.Include(item => item.LearningTest).Include(item => item.Answers).ThenInclude(answer => answer.LearningTestQuestion)
                .FirstOrDefaultAsync(item => item.Id == model.SubmissionId && item.LearningTest.TutorId == tutor.Id);
            if (submission == null) return NotFound();

            var inputById = model.Answers.ToDictionary(answer => answer.AnswerId, answer => answer);
            decimal manualScore = 0;
            foreach (var answer in submission.Answers)
            {
                if (!IsManualQuestion(answer.LearningTestQuestion)) continue;
                if (!inputById.TryGetValue(answer.Id, out var answerInput)) continue;
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
                if (reviewViewModel == null) return NotFound();
                reviewViewModel.TutorFeedback = model.TutorFeedback;
                foreach (var answerViewModel in reviewViewModel.Answers.Where(answer => answer.CanEditPoints))
                    if (inputById.TryGetValue(answerViewModel.AnswerId, out var answerInput)) answerViewModel.AwardedPoints = answerInput.AwardedPoints;
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
        public async Task<IActionResult> CreateGroup() => View(await BuildCreateGroupModelAsync());

        [HttpGet]
        public async Task<IActionResult> EditGroup(int id)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var group = await _dbContext.StudentGroups.AsNoTracking().Where(item => item.Id == id && item.TutorId == tutor.Id).Include(item => item.Members).FirstOrDefaultAsync();
            if (group == null) return NotFound();
            var model = new CreateGroupViewModel { GroupId = group.Id, Name = group.Name, Description = group.Description, StudentIds = group.Members.Select(member => member.StudentId).ToList() };
            return View("CreateGroup", await BuildCreateGroupModelAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid) return View(await BuildCreateGroupModelAsync(model));
            var tutor = await _userManager.GetUserAsync(User);
            var selectedStudents = model.StudentIds?.Distinct().ToList() ?? new List<string>();

            if (model.GroupId.HasValue)
            {
                var existingGroup = await _dbContext.StudentGroups.Include(group => group.Members).FirstOrDefaultAsync(group => group.Id == model.GroupId.Value && group.TutorId == tutor.Id);
                if (existingGroup == null) return NotFound();
                existingGroup.Name = model.Name;
                existingGroup.Description = model.Description;
                var existingMemberIds = existingGroup.Members.Select(member => member.StudentId).ToList();
                _dbContext.StudentGroupMembers.RemoveRange(existingGroup.Members.Where(member => !selectedStudents.Contains(member.StudentId)).ToList());
                foreach (var studentId in selectedStudents.Where(studentId => !existingMemberIds.Contains(studentId)))
                    _dbContext.StudentGroupMembers.Add(new StudentGroupMember { StudentGroupId = existingGroup.Id, StudentId = studentId });
                await _dbContext.SaveChangesAsync();
                TempData["StatusMessage"] = "Состав группы обновлен.";
                return RedirectToAction(nameof(Index));
            }

            var group = new StudentGroup { Name = model.Name, Description = model.Description, TutorId = tutor.Id };
            _dbContext.StudentGroups.Add(group);
            await _dbContext.SaveChangesAsync();
            foreach (var studentId in selectedStudents)
                _dbContext.StudentGroupMembers.Add(new StudentGroupMember { StudentGroupId = group.Id, StudentId = studentId });
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Группа создана.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateTest() => View(await BuildCreateTestModelAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTest(CreateTestViewModel model)
        {
            model.Questions = model.Questions?.Where(question => !string.IsNullOrWhiteSpace(question.Prompt)).ToList() ?? new List<QuestionEditorViewModel>();
            if (!model.Questions.Any()) ModelState.AddModelError(string.Empty, "Добавьте хотя бы один вопрос.");
            if (!ModelState.IsValid) return View(await BuildCreateTestModelAsync(model));

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
            foreach (var groupId in model.AssignedGroupIds.Distinct()) _dbContext.TestAssignments.Add(new TestAssignment { LearningTestId = test.Id, StudentGroupId = groupId });
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Тест сохранен и назначен выбранным группам.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateCreativeTask() => View(await BuildCreateCreativeTaskModelAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCreativeTask(CreateCreativeTaskViewModel model)
        {
            if (!ModelState.IsValid) return View(await BuildCreateCreativeTaskModelAsync(model));
            var tutor = await _userManager.GetUserAsync(User);
            var test = new LearningTest
            {
                Title = model.Title,
                ExamType = model.ExamType,
                MechanicType = "Творческое задание",
                ModuleName = "Творческий цех",
                Description = model.Theme,
                IsPublished = true,
                TutorId = tutor.Id,
                CreatedAtUtc = DateTime.UtcNow,
                TimeLimitMinutes = model.TimeLimitMinutes,
                Questions = new List<LearningTestQuestion>
                {
                    new LearningTestQuestion
                    {
                        Order = 1,
                        Prompt = model.Prompt,
                        QuestionType = "Essay",
                        Explanation = model.Explanation,
                        MaxPoints = model.MaxPoints
                    }
                }
            };

            _dbContext.LearningTests.Add(test);
            await _dbContext.SaveChangesAsync();
            foreach (var groupId in model.AssignedGroupIds.Distinct())
            {
                _dbContext.TestAssignments.Add(new TestAssignment { LearningTestId = test.Id, StudentGroupId = groupId });
            }

            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Творческое задание создано и назначено ученикам.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLessonVisibility(int assignmentId)
        {
            var tutor = await _userManager.GetUserAsync(User);
            var assignment = await _dbContext.LessonAssignments.Include(item => item.StudentGroup).FirstOrDefaultAsync(item => item.Id == assignmentId && item.StudentGroup.TutorId == tutor.Id);
            if (assignment == null) return NotFound();
            assignment.IsVisibleToStudent = !assignment.IsVisibleToStudent;
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = assignment.IsVisibleToStudent ? "Урок снова виден ученикам." : "Урок скрыт из кабинетов учеников.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToQuestion(ReplyStudentQuestionInputModel model)
        {
            var tutor = await _userManager.GetUserAsync(User);
            if (!ModelState.IsValid)
            {
                TempData["StatusMessage"] = "Не удалось сохранить ответ ученику.";
                return RedirectToAction(nameof(Index));
            }

            var question = await _dbContext.StudentQuestions.FirstOrDefaultAsync(item => item.Id == model.QuestionId && item.TutorId == tutor.Id);
            if (question == null) return NotFound();
            question.TutorReply = model.TutorReply?.Trim();
            question.RepliedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Ответ ученику сохранен.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<TutorDashboardViewModel> BuildTutorDashboardAsync(string tutorId, string tutorFullName, string tutorEmail)
        {
            var groups = await _dbContext.StudentGroups.AsNoTracking().Where(group => group.TutorId == tutorId).Include(group => group.Members).ThenInclude(member => member.Student).OrderBy(group => group.Name).ToListAsync();
            var tests = await _dbContext.LearningTests.AsNoTracking().Where(test => test.TutorId == tutorId).Include(test => test.Assignments).Include(test => test.Submissions).Include(test => test.Questions).OrderByDescending(test => test.CreatedAtUtc).ToListAsync();
            var students = await BuildStudentCardsAsync(tutorId, null);
            var submissions = await _dbContext.StudentSubmissions.AsNoTracking().Where(submission => submission.LearningTest.TutorId == tutorId).Include(submission => submission.Student).Include(submission => submission.LearningTest).OrderByDescending(submission => submission.SubmittedAtUtc).Take(12).ToListAsync();
            var lessonAssignments = await _dbContext.LessonAssignments.AsNoTracking().Where(item => item.StudentGroup.TutorId == tutorId).Include(item => item.StudentGroup).Include(item => item.Lesson).OrderBy(item => item.Lesson.DisplayOrder).ToListAsync();
            var lessonProgress = await _dbContext.StudentLessonProgresses.AsNoTracking().ToListAsync();
            var tips = await _dbContext.StudyTips.AsNoTracking().OrderBy(item => item.DisplayOrder).ToListAsync();
            var memes = await _dbContext.DailyMemes.AsNoTracking().OrderBy(item => item.DisplayOrder).Take(3).ToListAsync();
            var memeIds = memes.Select(item => item.Id).ToList();
            var likeCounts = await _dbContext.StudentMemeLikes.AsNoTracking().Where(item => memeIds.Contains(item.DailyMemeId)).GroupBy(item => item.DailyMemeId).Select(group => new { DailyMemeId = group.Key, Count = group.Count() }).ToListAsync();
            var questions = await _dbContext.StudentQuestions.AsNoTracking().Where(item => item.TutorId == tutorId).Include(item => item.Student).OrderByDescending(item => item.CreatedAtUtc).Take(8).ToListAsync();

            var sectionExamTypes = new[] { "ЕГЭ", "ОГЭ", "Итоговое сочинение", "Устное собеседование" };
            var sections = sectionExamTypes.Select(examType => new DashboardSectionViewModel
            {
                Key = SanitizeKey(examType),
                Title = examType,
                Description = PlatformCatalog.GetExamDescription(examType),
                AccentLabel = examType == "ЕГЭ" ? "3:30" : examType == "ОГЭ" ? "3:55" : "Трек",
                MockExamCount = tests.Count(test => test.ExamType == examType && test.IsMockExam),
                PracticeCount = PlatformCatalog.BuildPracticeTasks(examType == "Устное собеседование" ? "ОГЭ" : examType).Sum(item => item.TaskCount),
                LessonCount = lessonAssignments.Count(item => item.Lesson.ExamType == examType),
                PracticeTasks = PlatformCatalog.BuildPracticeTasks(examType == "Устное собеседование" ? "ОГЭ" : examType)
                    .Take(examType == "ЕГЭ" ? 26 : 12)
                    .Select(item => new PracticeTaskLineViewModel { TaskNumber = item.TaskNumber, Label = item.Label, TaskCount = item.TaskCount })
                    .ToList(),
                FeaturedTests = tests.Where(test => test.ExamType == examType).Take(6).Select(test => new StudentAssignedTestViewModel
                {
                    Id = test.Id,
                    Title = test.Title,
                    ExamType = test.ExamType,
                    MechanicType = test.MechanicType,
                    GroupName = test.Assignments.Count == 0 ? "без группы" : $"Групп: {test.Assignments.Count}",
                    QuestionCount = test.Questions.Count,
                    TimeLimitMinutes = test.TimeLimitMinutes,
                    AlreadySubmitted = false,
                    IsMockExam = test.IsMockExam,
                    IsCreativeTask = test.MechanicType == "Творческое задание",
                    ModuleName = test.ModuleName
                }).ToList()
            }).ToList();

            return new TutorDashboardViewModel
            {
                TutorName = string.IsNullOrWhiteSpace(tutorFullName) ? tutorEmail : tutorFullName,
                PendingReviewCount = submissions.Count(submission => submission.NeedsManualReview || !submission.ReviewedAtUtc.HasValue),
                TotalStudentsCount = students.Count,
                TotalGroupsCount = groups.Count,
                TotalLessonsCount = lessonAssignments.Count,
                TotalCreativeTaskCount = tests.Count(test => test.MechanicType == "Творческое задание"),
                DailyQuote = PlatformCatalog.GetDailyQuote(DateTime.Today),
                DailyCatImageUrl = $"https://cataas.com/cat?width=720&height=400&seed=tutor-{DateTime.Today:yyyyMMdd}",
                Sections = sections,
                Students = students.Select(student => new StudentOptionViewModel { Id = student.Id, Email = student.Email, GradeLabel = student.GradeLabel, Label = BuildStudentLabel(student.FullName, student.Email, student.GradeLabel), GroupNames = student.GroupNames }).ToList(),
                Groups = groups.Select(group => new TutorGroupCardViewModel { Id = group.Id, Name = group.Name, Description = group.Description, StudentNames = group.Members.Select(member => string.IsNullOrWhiteSpace(member.Student.FullName) ? member.Student.Email : member.Student.FullName).OrderBy(name => name).ToList() }).ToList(),
                Tests = tests.Select(test => new TutorTestCardViewModel { Id = test.Id, Title = test.Title, ExamType = test.ExamType, MechanicType = test.MechanicType, ModuleName = test.ModuleName, IsPublished = test.IsPublished, GroupCount = test.Assignments.Count, SubmissionCount = test.Submissions.Count }).ToList(),
                RecentSubmissions = submissions.Select(submission => new SubmissionSummaryViewModel { Id = submission.Id, StudentName = string.IsNullOrWhiteSpace(submission.Student.FullName) ? submission.Student.Email : submission.Student.FullName, TestTitle = submission.LearningTest.Title, AutoScore = submission.AutoScore, MaxScore = submission.MaxScore, TutorScore = submission.TutorScore, SubmittedAtUtc = submission.SubmittedAtUtc, NeedsManualReview = submission.NeedsManualReview, IsReviewed = submission.ReviewedAtUtc.HasValue, ExamType = submission.LearningTest.ExamType }).ToList(),
                Lessons = lessonAssignments.Select(assignment =>
                {
                    var completedCount = lessonProgress.Count(item => item.LessonAssignmentId == assignment.Id && item.IsCompleted);
                    return new LessonCardViewModel
                    {
                        AssignmentId = assignment.Id,
                        LessonId = assignment.LessonId,
                        ExamType = assignment.Lesson.ExamType,
                        Title = assignment.Lesson.Title,
                        Theme = assignment.Lesson.Theme,
                        Notes = assignment.Lesson.Notes,
                        Homework = assignment.Lesson.Homework,
                        LessonFormat = assignment.Lesson.LessonFormat,
                        GroupName = assignment.StudentGroup.Name,
                        IsVisibleToStudent = assignment.IsVisibleToStudent,
                        Summary = $"{assignment.Lesson.Summary} Завершили: {completedCount} ученик(а)."
                    };
                }).ToList(),
                Tips = tips.Select(tip => new StudyTipViewModel { Id = tip.Id, ExamType = tip.ExamType, Title = tip.Title, Description = tip.Description }).ToList(),
                Memes = memes.Select(meme => new MemeCardViewModel { Id = meme.Id, Title = meme.Title, Caption = meme.Caption, Theme = meme.Theme, ImageUrl = meme.ImageUrl, LikeCount = likeCounts.FirstOrDefault(item => item.DailyMemeId == meme.Id)?.Count ?? 0 }).ToList(),
                StudentQuestions = questions.Select(question => new StudentQuestionViewModel { Id = question.Id, StudentName = string.IsNullOrWhiteSpace(question.Student.FullName) ? question.Student.Email : question.Student.FullName, Topic = question.Topic, Message = question.Message, TutorReply = question.TutorReply, CreatedAtUtc = question.CreatedAtUtc, RepliedAtUtc = question.RepliedAtUtc }).ToList(),
                CreativeIdeas = BuildCreativeIdeas()
            };
        }

        private async Task<List<StudentCardViewModel>> BuildStudentCardsAsync(string tutorId, string searchQuery)
        {
            var studentsQuery = _dbContext.Users.AsNoTracking().Where(user => user.PlatformRole == PlatformRoles.Student || string.IsNullOrWhiteSpace(user.PlatformRole));
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedQuery = searchQuery.Trim();
                studentsQuery = studentsQuery.Where(user => user.FullName.Contains(normalizedQuery) || user.Email.Contains(normalizedQuery) || user.GradeLabel.Contains(normalizedQuery));
            }

            var students = await studentsQuery.OrderBy(user => user.FullName).ThenBy(user => user.Email).ToListAsync();
            var groupMemberships = await _dbContext.StudentGroupMembers.AsNoTracking().Where(member => member.StudentGroup.TutorId == tutorId).Include(member => member.StudentGroup).ToListAsync();
            var submissions = await _dbContext.StudentSubmissions.AsNoTracking().Where(submission => submission.LearningTest.TutorId == tutorId).ToListAsync();

            return students.Select(student => new StudentCardViewModel
            {
                Id = student.Id,
                FullName = string.IsNullOrWhiteSpace(student.FullName) ? student.Email : student.FullName,
                Email = student.Email,
                GradeLabel = student.GradeLabel,
                GroupNames = groupMemberships.Where(member => member.StudentId == student.Id).Select(member => member.StudentGroup.Name).OrderBy(name => name).ToList(),
                SubmissionCount = submissions.Count(submission => submission.StudentId == student.Id),
                ReviewedSubmissionCount = submissions.Count(submission => submission.StudentId == student.Id && submission.ReviewedAtUtc.HasValue)
            }).ToList();
        }

        private async Task<CreateStudentViewModel> BuildCreateStudentModelAsync(CreateStudentViewModel model = null)
        {
            model ??= new CreateStudentViewModel();
            var tutor = await _userManager.GetUserAsync(User);
            model.AvailableGroups = await _dbContext.StudentGroups.AsNoTracking().Where(group => group.TutorId == tutor.Id).OrderBy(group => group.Name).Select(group => new GroupOptionViewModel { Id = group.Id, Name = group.Name }).ToListAsync();
            return model;
        }

        private async Task<EditStudentViewModel> BuildEditStudentModelAsync(EditStudentViewModel model, string tutorId)
        {
            model.AvailableGroups = await _dbContext.StudentGroups.AsNoTracking().Where(group => group.TutorId == tutorId).OrderBy(group => group.Name).Select(group => new GroupOptionViewModel { Id = group.Id, Name = group.Name }).ToListAsync();
            return model;
        }

        private async Task<CreateGroupViewModel> BuildCreateGroupModelAsync(CreateGroupViewModel model = null)
        {
            model ??= new CreateGroupViewModel();
            model.AvailableStudents = await _dbContext.Users.AsNoTracking().Where(user => user.PlatformRole == PlatformRoles.Student || string.IsNullOrWhiteSpace(user.PlatformRole)).OrderBy(user => user.FullName).ThenBy(user => user.Email).Select(user => new StudentOptionViewModel { Id = user.Id, Email = user.Email, GradeLabel = user.GradeLabel, Label = BuildStudentLabel(user.FullName, user.Email, user.GradeLabel) }).ToListAsync();
            return model;
        }

        private async Task<CreateTestViewModel> BuildCreateTestModelAsync(CreateTestViewModel model = null)
        {
            model ??= new CreateTestViewModel();
            var tutor = await _userManager.GetUserAsync(User);
            model.AvailableGroups = await _dbContext.StudentGroups.AsNoTracking().Where(group => group.TutorId == tutor.Id).OrderBy(group => group.Name).Select(group => new GroupOptionViewModel { Id = group.Id, Name = group.Name }).ToListAsync();
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

        private async Task<CreateCreativeTaskViewModel> BuildCreateCreativeTaskModelAsync(CreateCreativeTaskViewModel model = null)
        {
            model ??= new CreateCreativeTaskViewModel();
            var tutor = await _userManager.GetUserAsync(User);
            model.AvailableGroups = await _dbContext.StudentGroups.AsNoTracking().Where(group => group.TutorId == tutor.Id).OrderBy(group => group.Name).Select(group => new GroupOptionViewModel { Id = group.Id, Name = group.Name }).ToListAsync();
            model.AvailableExamTypes = PlatformCatalog.ExamTypes.Where(item => item != "Произвольный модуль").ToList();
            return model;
        }

        private async Task<ReviewSubmissionViewModel> BuildReviewSubmissionViewModelAsync(int submissionId, string tutorId)
        {
            var submission = await _dbContext.StudentSubmissions.AsNoTracking().Where(item => item.Id == submissionId && item.LearningTest.TutorId == tutorId).Include(item => item.Student).Include(item => item.LearningTest).Include(item => item.Answers).ThenInclude(answer => answer.LearningTestQuestion).FirstOrDefaultAsync();
            if (submission == null) return null;

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
                Answers = submission.Answers.OrderBy(answer => answer.LearningTestQuestion.Order).Select(answer => new SubmissionAnswerReviewViewModel
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
                }).ToList()
            };
        }

        private static List<CreativeIdeaViewModel> BuildCreativeIdeas()
        {
            return new List<CreativeIdeaViewModel>
            {
                new CreativeIdeaViewModel { ExamType = "ЕГЭ", Title = "Проблема искренних чувств", Prompt = "Напиши два аргумента и объясни связь между ними на тему спада искренних чувств в современном времени." },
                new CreativeIdeaViewModel { ExamType = "ЕГЭ", Title = "Проблема ревности", Prompt = "Собери комментарий к проблеме ревности: позиция автора, два примера, связь и собственный вывод." },
                new CreativeIdeaViewModel { ExamType = "ОГЭ", Title = "Литературный диалог", Prompt = "Пусть герой литературы объяснит свой поступок и тем самым раскроет правило русского языка или нравственный выбор." },
                new CreativeIdeaViewModel { ExamType = "ОГЭ", Title = "Песни и тропы", Prompt = "Объясни на строках песни, где находятся эпитет, гипербола, литота и метафора, а затем придумай свои примеры." }
            };
        }

        private static bool IsManualQuestion(LearningTestQuestion question)
        {
            return question.QuestionType == "Essay" || question.QuestionType == "AudioPrompt" || question.QuestionType == "OpenText" || string.IsNullOrWhiteSpace(question.CorrectAnswer);
        }

        private static string BuildStudentLabel(string fullName, string email, string gradeLabel)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} | {1}{2}", string.IsNullOrWhiteSpace(fullName) ? email : fullName, email, string.IsNullOrWhiteSpace(gradeLabel) ? string.Empty : $" | {gradeLabel}");
        }

        private static string SanitizeKey(string value)
        {
            return (value ?? string.Empty).ToLowerInvariant().Replace(" ", "-");
        }
    }
}

