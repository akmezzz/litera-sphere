using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorPlatform.Models;

namespace TutorPlatform.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await dbContext.Database.EnsureCreatedAsync();
            await EnsureExtendedSchemaAsync(dbContext);

            var tutor = await EnsureUserAsync(userManager,
                "tutor@literasphere.local",
                "Tutor123!",
                "Мария Петрова",
                PlatformRoles.Tutor,
                "11 класс");

            var students = new[]
            {
                await EnsureUserAsync(userManager, "anna@student.local", "Student123!", "Анна Смирнова", PlatformRoles.Student, "9 класс"),
                await EnsureUserAsync(userManager, "ivan@student.local", "Student123!", "Иван Крылов", PlatformRoles.Student, "11 класс"),
                await EnsureUserAsync(userManager, "sofia@student.local", "Student123!", "София Миронова", PlatformRoles.Student, "11 класс"),
                await EnsureUserAsync(userManager, "egor@student.local", "Student123!", "Егор Беляев", PlatformRoles.Student, "10 класс"),
                await EnsureUserAsync(userManager, "polina@student.local", "Student123!", "Полина Орлова", PlatformRoles.Student, "9 класс")
            };

            var ogeGroup = await EnsureGroupAsync(dbContext, tutor.Id, "ОГЭ: интенсив", "Группа для подготовки к изложению, сочинению и устному собеседованию.");
            var egeGroup = await EnsureGroupAsync(dbContext, tutor.Id, "ЕГЭ: сочинение и тест", "Группа для 11 класса с пробниками, аргументацией и тестовой частью.");
            var writingGroup = await EnsureGroupAsync(dbContext, tutor.Id, "Итоговое сочинение", "Отдельный поток по аргументации, тезисам и литературным примерам.");

            await EnsureMembershipAsync(dbContext, ogeGroup.Id, students[0].Id);
            await EnsureMembershipAsync(dbContext, ogeGroup.Id, students[4].Id);
            await EnsureMembershipAsync(dbContext, egeGroup.Id, students[1].Id);
            await EnsureMembershipAsync(dbContext, egeGroup.Id, students[2].Id);
            await EnsureMembershipAsync(dbContext, egeGroup.Id, students[3].Id);
            await EnsureMembershipAsync(dbContext, writingGroup.Id, students[1].Id);
            await EnsureMembershipAsync(dbContext, writingGroup.Id, students[2].Id);
            await dbContext.SaveChangesAsync();

            await RemoveLegacySeededContentAsync(dbContext, tutor.Id);
            await SeedMemesAsync(dbContext);
        }

        private static async Task RemoveLegacySeededContentAsync(ApplicationDbContext dbContext, string tutorId)
        {
            var seededTestTitles = new[]
            {
                "Пробник ЕГЭ: вариант 1",
                "Типовые задания ЕГЭ: номер 16 и 21",
                "Пробник ОГЭ: вариант 1",
                "ОГЭ: диктор на радио",
                "Творческое задание: ревность и искренние чувства"
            };

            var seededLessonTitles = new[]
            {
                "Задание 27: комментарий без воды",
                "Пунктуация: причастный и деепричастный оборот",
                "Изложение: три микротемы без паники",
                "Устное собеседование: диктор эфира",
                "Аргументация: дом, память и нравственный выбор"
            };

            var seededTipTitles = new[]
            {
                "Один пробник в день",
                "Разминка перед сочинением",
                "Изложение без паники",
                "Чтение как у диктора",
                "Отдых тоже часть подготовки",
                "Лови ошибки сразу"
            };

            var seededQuestionTopics = new[]
            {
                "Комментарий в сочинении",
                "Изложение"
            };

            var seededTestIds = await dbContext.LearningTests
                .Where(test => test.TutorId == tutorId && seededTestTitles.Contains(test.Title))
                .Select(test => test.Id)
                .ToListAsync();

            if (seededTestIds.Any())
            {
                var answers = await dbContext.StudentAnswers.Where(answer => seededTestIds.Contains(answer.StudentSubmission.LearningTestId)).ToListAsync();
                var submissions = await dbContext.StudentSubmissions.Where(submission => seededTestIds.Contains(submission.LearningTestId)).ToListAsync();
                var assignments = await dbContext.TestAssignments.Where(assignment => seededTestIds.Contains(assignment.LearningTestId)).ToListAsync();
                var questions = await dbContext.LearningTestQuestions.Where(question => seededTestIds.Contains(question.LearningTestId)).ToListAsync();
                var tests = await dbContext.LearningTests.Where(test => seededTestIds.Contains(test.Id)).ToListAsync();

                dbContext.StudentAnswers.RemoveRange(answers);
                dbContext.StudentSubmissions.RemoveRange(submissions);
                dbContext.TestAssignments.RemoveRange(assignments);
                dbContext.LearningTestQuestions.RemoveRange(questions);
                dbContext.LearningTests.RemoveRange(tests);
            }

            var seededLessonIds = await dbContext.Lessons
                .Where(lesson => seededLessonTitles.Contains(lesson.Title))
                .Select(lesson => lesson.Id)
                .ToListAsync();

            if (seededLessonIds.Any())
            {
                var lessonAssignmentIds = await dbContext.LessonAssignments
                    .Where(assignment => seededLessonIds.Contains(assignment.LessonId))
                    .Select(assignment => assignment.Id)
                    .ToListAsync();

                var progresses = await dbContext.StudentLessonProgresses.Where(progress => lessonAssignmentIds.Contains(progress.LessonAssignmentId)).ToListAsync();
                var assignments = await dbContext.LessonAssignments.Where(assignment => seededLessonIds.Contains(assignment.LessonId)).ToListAsync();
                var lessons = await dbContext.Lessons.Where(lesson => seededLessonIds.Contains(lesson.Id)).ToListAsync();

                dbContext.StudentLessonProgresses.RemoveRange(progresses);
                dbContext.LessonAssignments.RemoveRange(assignments);
                dbContext.Lessons.RemoveRange(lessons);
            }

            var seededTips = await dbContext.StudyTips.Where(tip => seededTipTitles.Contains(tip.Title)).ToListAsync();
            var seededQuestions = await dbContext.StudentQuestions.Where(question => seededQuestionTopics.Contains(question.Topic)).ToListAsync();
            dbContext.StudyTips.RemoveRange(seededTips);
            dbContext.StudentQuestions.RemoveRange(seededQuestions);

            await dbContext.SaveChangesAsync();
        }

        private static async Task EnsureExtendedSchemaAsync(ApplicationDbContext dbContext)
        {
            await EnsureStudentSubmissionSchemaAsync(dbContext);

            if (dbContext.Database.ProviderName != null && dbContext.Database.ProviderName.Contains("Sqlite"))
            {
                var sqliteCommands = new[]
                {
                    @"CREATE TABLE IF NOT EXISTS StudyTips (Id INTEGER PRIMARY KEY AUTOINCREMENT, ExamType TEXT NULL, Title TEXT NULL, Description TEXT NULL, DisplayOrder INTEGER NOT NULL DEFAULT 0);",
                    @"CREATE TABLE IF NOT EXISTS DailyMemes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NULL, Caption TEXT NULL, ImageUrl TEXT NULL, Theme TEXT NULL, DisplayOrder INTEGER NOT NULL DEFAULT 0);",
                    @"CREATE TABLE IF NOT EXISTS StudentMemeLikes (Id INTEGER PRIMARY KEY AUTOINCREMENT, DailyMemeId INTEGER NOT NULL, StudentId TEXT NOT NULL, LikedAtUtc TEXT NOT NULL, FOREIGN KEY (DailyMemeId) REFERENCES DailyMemes(Id) ON DELETE CASCADE, FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS IX_StudentMemeLikes_DailyMemeId_StudentId ON StudentMemeLikes (DailyMemeId, StudentId);",
                    @"CREATE TABLE IF NOT EXISTS Lessons (Id INTEGER PRIMARY KEY AUTOINCREMENT, ExamType TEXT NULL, Title TEXT NULL, Theme TEXT NULL, Summary TEXT NULL, Notes TEXT NULL, Homework TEXT NULL, LessonFormat TEXT NULL, DisplayOrder INTEGER NOT NULL DEFAULT 0);",
                    @"CREATE TABLE IF NOT EXISTS LessonAssignments (Id INTEGER PRIMARY KEY AUTOINCREMENT, LessonId INTEGER NOT NULL, StudentGroupId INTEGER NOT NULL, IsVisibleToStudent INTEGER NOT NULL DEFAULT 1, AssignedAtUtc TEXT NOT NULL, FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE, FOREIGN KEY (StudentGroupId) REFERENCES StudentGroups(Id) ON DELETE CASCADE);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS IX_LessonAssignments_LessonId_StudentGroupId ON LessonAssignments (LessonId, StudentGroupId);",
                    @"CREATE TABLE IF NOT EXISTS StudentLessonProgresses (Id INTEGER PRIMARY KEY AUTOINCREMENT, LessonAssignmentId INTEGER NOT NULL, StudentId TEXT NOT NULL, IsCompleted INTEGER NOT NULL DEFAULT 0, OpenedAtUtc TEXT NULL, CompletedAtUtc TEXT NULL, FOREIGN KEY (LessonAssignmentId) REFERENCES LessonAssignments(Id) ON DELETE CASCADE, FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS IX_StudentLessonProgresses_LessonAssignmentId_StudentId ON StudentLessonProgresses (LessonAssignmentId, StudentId);",
                    @"CREATE TABLE IF NOT EXISTS StudentQuestions (Id INTEGER PRIMARY KEY AUTOINCREMENT, StudentId TEXT NOT NULL, TutorId TEXT NOT NULL, Topic TEXT NULL, Message TEXT NULL, TutorReply TEXT NULL, CreatedAtUtc TEXT NOT NULL, RepliedAtUtc TEXT NULL, FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT, FOREIGN KEY (TutorId) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT);"
                };

                foreach (var command in sqliteCommands)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(command);
                }

                return;
            }

            if (dbContext.Database.ProviderName != null && dbContext.Database.ProviderName.Contains("SqlServer"))
            {
                var sqlServerCommands = new[]
                {
                    @"IF OBJECT_ID('StudyTips', 'U') IS NULL CREATE TABLE StudyTips (Id int IDENTITY(1,1) PRIMARY KEY, ExamType nvarchar(32) NULL, Title nvarchar(200) NULL, Description nvarchar(max) NULL, DisplayOrder int NOT NULL DEFAULT 0);",
                    @"IF OBJECT_ID('DailyMemes', 'U') IS NULL CREATE TABLE DailyMemes (Id int IDENTITY(1,1) PRIMARY KEY, Title nvarchar(200) NULL, Caption nvarchar(max) NULL, ImageUrl nvarchar(500) NULL, Theme nvarchar(120) NULL, DisplayOrder int NOT NULL DEFAULT 0);",
                    @"IF OBJECT_ID('StudentMemeLikes', 'U') IS NULL CREATE TABLE StudentMemeLikes (Id int IDENTITY(1,1) PRIMARY KEY, DailyMemeId int NOT NULL, StudentId nvarchar(450) NOT NULL, LikedAtUtc datetime2 NOT NULL, CONSTRAINT FK_StudentMemeLikes_DailyMemes FOREIGN KEY (DailyMemeId) REFERENCES DailyMemes(Id), CONSTRAINT FK_StudentMemeLikes_AspNetUsers FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id));",
                    @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentMemeLikes_DailyMemeId_StudentId') CREATE UNIQUE INDEX IX_StudentMemeLikes_DailyMemeId_StudentId ON StudentMemeLikes (DailyMemeId, StudentId);",
                    @"IF OBJECT_ID('Lessons', 'U') IS NULL CREATE TABLE Lessons (Id int IDENTITY(1,1) PRIMARY KEY, ExamType nvarchar(32) NULL, Title nvarchar(200) NULL, Theme nvarchar(200) NULL, Summary nvarchar(max) NULL, Notes nvarchar(max) NULL, Homework nvarchar(max) NULL, LessonFormat nvarchar(64) NULL, DisplayOrder int NOT NULL DEFAULT 0);",
                    @"IF OBJECT_ID('LessonAssignments', 'U') IS NULL CREATE TABLE LessonAssignments (Id int IDENTITY(1,1) PRIMARY KEY, LessonId int NOT NULL, StudentGroupId int NOT NULL, IsVisibleToStudent bit NOT NULL DEFAULT 1, AssignedAtUtc datetime2 NOT NULL, CONSTRAINT FK_LessonAssignments_Lessons FOREIGN KEY (LessonId) REFERENCES Lessons(Id), CONSTRAINT FK_LessonAssignments_StudentGroups FOREIGN KEY (StudentGroupId) REFERENCES StudentGroups(Id));",
                    @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LessonAssignments_LessonId_StudentGroupId') CREATE UNIQUE INDEX IX_LessonAssignments_LessonId_StudentGroupId ON LessonAssignments (LessonId, StudentGroupId);",
                    @"IF OBJECT_ID('StudentLessonProgresses', 'U') IS NULL CREATE TABLE StudentLessonProgresses (Id int IDENTITY(1,1) PRIMARY KEY, LessonAssignmentId int NOT NULL, StudentId nvarchar(450) NOT NULL, IsCompleted bit NOT NULL DEFAULT 0, OpenedAtUtc datetime2 NULL, CompletedAtUtc datetime2 NULL, CONSTRAINT FK_StudentLessonProgresses_LessonAssignments FOREIGN KEY (LessonAssignmentId) REFERENCES LessonAssignments(Id), CONSTRAINT FK_StudentLessonProgresses_AspNetUsers FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id));",
                    @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentLessonProgresses_LessonAssignmentId_StudentId') CREATE UNIQUE INDEX IX_StudentLessonProgresses_LessonAssignmentId_StudentId ON StudentLessonProgresses (LessonAssignmentId, StudentId);",
                    @"IF OBJECT_ID('StudentQuestions', 'U') IS NULL CREATE TABLE StudentQuestions (Id int IDENTITY(1,1) PRIMARY KEY, StudentId nvarchar(450) NOT NULL, TutorId nvarchar(450) NOT NULL, Topic nvarchar(200) NULL, Message nvarchar(max) NULL, TutorReply nvarchar(max) NULL, CreatedAtUtc datetime2 NOT NULL, RepliedAtUtc datetime2 NULL, CONSTRAINT FK_StudentQuestions_Student FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id), CONSTRAINT FK_StudentQuestions_Tutor FOREIGN KEY (TutorId) REFERENCES AspNetUsers(Id));"
                };

                foreach (var command in sqlServerCommands)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(command);
                }
            }
        }

        private static async Task EnsureStudentSubmissionSchemaAsync(ApplicationDbContext dbContext)
        {
            if (dbContext.Database.ProviderName != null && dbContext.Database.ProviderName.Contains("Sqlite"))
            {
                await EnsureSqliteColumnAsync(dbContext, "StudentSubmissions", "TutorScore", "REAL NULL");
                await EnsureSqliteColumnAsync(dbContext, "StudentSubmissions", "TutorFeedback", "TEXT NULL");
                await EnsureSqliteColumnAsync(dbContext, "StudentSubmissions", "ReviewedAtUtc", "TEXT NULL");
                await EnsureSqliteColumnAsync(dbContext, "StudentAnswers", "TutorComment", "TEXT NULL");
                return;
            }

            if (dbContext.Database.ProviderName != null && dbContext.Database.ProviderName.Contains("SqlServer"))
            {
                await dbContext.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('StudentSubmissions', 'TutorScore') IS NULL
    ALTER TABLE StudentSubmissions ADD TutorScore decimal(9,2) NULL;");
                await dbContext.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('StudentSubmissions', 'TutorFeedback') IS NULL
    ALTER TABLE StudentSubmissions ADD TutorFeedback nvarchar(2000) NULL;");
                await dbContext.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('StudentSubmissions', 'ReviewedAtUtc') IS NULL
    ALTER TABLE StudentSubmissions ADD ReviewedAtUtc datetime2 NULL;");
                await dbContext.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('StudentAnswers', 'TutorComment') IS NULL
    ALTER TABLE StudentAnswers ADD TutorComment nvarchar(2000) NULL;");
            }
        }

        private static async Task SeedTipsAsync(ApplicationDbContext dbContext)
        {
            if (await dbContext.StudyTips.AnyAsync()) return;

            var tips = new[]
            {
                new StudyTip { ExamType = "ЕГЭ", Title = "Один пробник в день", Description = "Решай один полноценный пробник, а затем разбирай 3-5 самых частых ошибок. Так тревога превращается в понятный план.", DisplayOrder = 1 },
                new StudyTip { ExamType = "ЕГЭ", Title = "Разминка перед сочинением", Description = "Перед заданием 27 выпиши проблему, позицию автора и один жизненный пример. Это помогает не зависнуть у пустого листа.", DisplayOrder = 2 },
                new StudyTip { ExamType = "ОГЭ", Title = "Изложение без паники", Description = "Во время прослушивания фиксируй только смысловые опоры: герой, действие, вывод. Красивую форму допишешь позже.", DisplayOrder = 3 },
                new StudyTip { ExamType = "ОГЭ", Title = "Чтение как у диктора", Description = "Перед чтением вслух сделай один спокойный вдох, отметь паузы карандашом и читай с чуть более медленным темпом.", DisplayOrder = 4 },
                new StudyTip { ExamType = "Общее", Title = "Отдых тоже часть подготовки", Description = "После 50 минут учебы встань, разомни плечи и убери экран на 10 минут. Мозг запоминает лучше в ритме, а не в перегрузе.", DisplayOrder = 5 },
                new StudyTip { ExamType = "Общее", Title = "Лови ошибки сразу", Description = "Если правило снова дало сбой, занеси его в мини-конспект. Повтор через день закрепляет материал лучше, чем зубрежка в ночь.", DisplayOrder = 6 }
            };

            dbContext.StudyTips.AddRange(tips);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedMemesAsync(ApplicationDbContext dbContext)
        {
            if (await dbContext.DailyMemes.AnyAsync()) return;

            dbContext.DailyMemes.AddRange(
                new DailyMeme
                {
                    Title = "Котик и запятые",
                    Caption = "Когда наконец понял, где ставить запятую в сложноподчиненном, и смотришь на мир как филолог-легенда.",
                    ImageUrl = "https://cataas.com/cat/says/%D0%97%D0%B0%D0%BF%D1%8F%D1%82%D1%8B%D0%B5?fontSize=32&fontColor=white",
                    Theme = "Учебный мем",
                    DisplayOrder = 1
                },
                new DailyMeme
                {
                    Title = "Мем про пробник",
                    Caption = "Я после пробника: сначала драматично, потом разобрал ошибки и внезапно вырос на 8 баллов.",
                    ImageUrl = "https://cataas.com/cat/says/%D0%9F%D1%80%D0%BE%D0%B1%D0%BD%D0%B8%D0%BA?fontSize=32&fontColor=white",
                    Theme = "Котики",
                    DisplayOrder = 2
                },
                new DailyMeme
                {
                    Title = "Литературный кот",
                    Caption = "Когда Печорин снова все усложнил, а ты все равно обязан написать сильный комментарий.",
                    ImageUrl = "https://cataas.com/cat/says/%D0%9F%D0%B5%D1%87%D0%BE%D1%80%D0%B8%D0%BD?fontSize=30&fontColor=white",
                    Theme = "Литература",
                    DisplayOrder = 3
                });

            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedLessonsAsync(ApplicationDbContext dbContext, int ogeGroupId, int egeGroupId, int writingGroupId)
        {
            if (!await dbContext.Lessons.AnyAsync())
            {
                dbContext.Lessons.AddRange(
                    new Lesson
                    {
                        ExamType = "ЕГЭ",
                        Title = "Задание 27: комментарий без воды",
                        Theme = "Проблема, позиция автора, два примера и связь",
                        Summary = "Учимся быстро собирать каркас сочинения ЕГЭ: проблема, позиция автора, два примера-иллюстрации и анализ связи между ними.",
                        Notes = "1. Сначала назови проблему в одном точном предложении. 2. Затем сформулируй позицию автора. 3. Подбери два примера из текста. 4. Объясни, как они связаны: дополнение, сопоставление, противопоставление или причина-следствие.",
                        Homework = "Вставь пропущенные формулировки в каркас комментария, реши 3 тренировочных задания по связи примеров и напиши мини-комментарий на 120-150 слов.",
                        LessonFormat = "Конспект",
                        DisplayOrder = 1
                    },
                    new Lesson
                    {
                        ExamType = "ЕГЭ",
                        Title = "Пунктуация: причастный и деепричастный оборот",
                        Theme = "Обособление и смысловая интонация",
                        Summary = "Разбираем, как работают причастные и деепричастные обороты на понятных примерах, в том числе в строках песен и живой речи.",
                        Notes = "Причастный оборот отвечает на вопрос какой? и описывает предмет. Деепричастный оборот отвечает на вопрос что делая? и добавляет действие. Если слышишь добавочное действие героя, почти всегда нужен деепричастный оборот и обособление.",
                        Homework = "Заполни пропуски в конспекте, найди обороты в 5 предложениях и придумай 2 собственных примера из современной речи или песен.",
                        LessonFormat = "Тема урока",
                        DisplayOrder = 2
                    },
                    new Lesson
                    {
                        ExamType = "ОГЭ",
                        Title = "Изложение: как держать смысловой каркас",
                        Theme = "Ключевые микротемы и сжатие текста",
                        Summary = "Ученик учится ловить микротемы, отбрасывать второстепенное и собирать спокойное, структурное изложение без паники.",
                        Notes = "Во время первого прослушивания фиксируй тему и три главные мысли. Во время второго прослушивания отмечай детали, которые помогают связать абзацы. Сжатие строится на обобщении, исключении и упрощении синтаксиса.",
                        Homework = "Составь план из трех микротем, выполни мини-изложение на 70 слов и допиши пропущенные слова в памятке по сжатию текста.",
                        LessonFormat = "Конспект",
                        DisplayOrder = 3
                    },
                    new Lesson
                    {
                        ExamType = "ОГЭ",
                        Title = "Устное собеседование: диктор эфира",
                        Theme = "Чтение вслух, паузы, интонация и монолог",
                        Summary = "Тренировка чтения вслух в роли диктора радиопрограммы: держим темп, паузы и доброжелательный голос.",
                        Notes = "Перед чтением посмотри на имена, даты и длинные слова. Отметь логические ударения. После чтения быстро оцени: был ли темп ровным, не пропадали ли окончания слов, хватало ли дыхания на длинные фразы.",
                        Homework = "Прочитай текст дважды, выпиши 3 совета самому себе для следующего чтения и ответь на вопросы монолога по картинкам.",
                        LessonFormat = "Практика",
                        DisplayOrder = 4
                    },
                    new Lesson
                    {
                        ExamType = "Итоговое сочинение",
                        Title = "Аргументация: дом, память и нравственный выбор",
                        Theme = "Как подбирать литературу и личный опыт под тему",
                        Summary = "Разбираем сильные аргументы, чтобы ученик не писал общими словами, а выводил четкую мысль на литературе и личном опыте.",
                        Notes = "Хороший аргумент содержит тезис, конкретный эпизод и вывод, который возвращает нас к теме. Не пересказывай весь сюжет: бери только один точный момент, который работает на мысль.",
                        Homework = "Подбери 2 литературных аргумента на тему дома и один пример из личного опыта, затем оформи вывод в 3 предложениях.",
                        LessonFormat = "Творческая мастерская",
                        DisplayOrder = 5
                    });

                await dbContext.SaveChangesAsync();
            }

            var lessons = await dbContext.Lessons.AsNoTracking().ToListAsync();
            foreach (var lesson in lessons.Where(lesson => lesson.ExamType == "ЕГЭ"))
            {
                await EnsureLessonAssignmentAsync(dbContext, lesson.Id, egeGroupId);
            }

            foreach (var lesson in lessons.Where(lesson => lesson.ExamType == "ОГЭ"))
            {
                await EnsureLessonAssignmentAsync(dbContext, lesson.Id, ogeGroupId);
            }

            foreach (var lesson in lessons.Where(lesson => lesson.ExamType == "Итоговое сочинение"))
            {
                await EnsureLessonAssignmentAsync(dbContext, lesson.Id, writingGroupId);
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedTestsAsync(ApplicationDbContext dbContext, string tutorId, int ogeGroupId, int egeGroupId, int writingGroupId)
        {
            if (await dbContext.LearningTests.AnyAsync()) return;

            var tests = new List<(LearningTest Test, int[] GroupIds)>
            {
                (new LearningTest
                {
                    Title = "Пробник ЕГЭ: вариант 1",
                    ExamType = "ЕГЭ",
                    MechanicType = "Пробник",
                    ModuleName = "Контрольные тесты",
                    Description = "Полноформатный пробник ЕГЭ: тестовая часть оценивается автоматически, сочинение проверяет репетитор.",
                    IsPublished = true,
                    IsMockExam = true,
                    TutorId = tutorId,
                    TimeLimitMinutes = 210,
                    Questions = new List<LearningTestQuestion>
                    {
                        new LearningTestQuestion { Order = 1, Prompt = "Укажите предложение, в котором верно передана главная информация текста.", QuestionType = "SingleChoice", OptionsText = "Вариант 1\nВариант 2\nВариант 3\nВариант 4", CorrectAnswer = "Вариант 2", Explanation = "Автопроверка по ключу.", MaxPoints = 1 },
                        new LearningTestQuestion { Order = 2, Prompt = "Выберите ряд, в котором во всех словах пропущена безударная проверяемая гласная.", QuestionType = "SingleChoice", OptionsText = "касательная, обнажить\nпроверять, молчаливый\nозарение, касание\nвытерпеть, блестящий", CorrectAnswer = "проверять, молчаливый", Explanation = "Тестовая часть идет автоматически.", MaxPoints = 1 },
                        new LearningTestQuestion { Order = 3, Prompt = "Напишите сочинение по тексту: сформулируйте проблему, позицию автора, комментарий и собственное отношение.", QuestionType = "Essay", Explanation = "Репетитор выставляет баллы только за письменную часть и итог автоматически суммируется.", MaxPoints = 22 }
                    }
                }, new[] { egeGroupId, writingGroupId }),
                (new LearningTest
                {
                    Title = "Типовые задания ЕГЭ: номер 16 и 21",
                    ExamType = "ЕГЭ",
                    MechanicType = "Пунктуационный лабиринт",
                    ModuleName = "Типовые задания",
                    Description = "Связка типовых пунктуационных заданий для ежедневной отработки номеров.",
                    IsPublished = true,
                    TutorId = tutorId,
                    TimeLimitMinutes = 30,
                    Questions = new List<LearningTestQuestion>
                    {
                        new LearningTestQuestion { Order = 1, Prompt = "Расставьте знаки препинания: Когда стемнело __ город зажег огни __ и площадь задышала музыкой.", QuestionType = "SingleChoice", OptionsText = "запятая, запятая\nзапятая, тире\nтире, запятая\nдвоеточие, запятая", CorrectAnswer = "запятая, запятая", Explanation = "Проверяем границы придаточного и сочинительной связи.", MaxPoints = 1 },
                        new LearningTestQuestion { Order = 2, Prompt = "Объясните постановку знаков препинания в одном-двух предложениях.", QuestionType = "OpenText", Explanation = "Пояснение проверяется вручную репетитором.", MaxPoints = 2 }
                    }
                }, new[] { egeGroupId }),
                (new LearningTest
                {
                    Title = "Пробник ОГЭ: вариант 1",
                    ExamType = "ОГЭ",
                    MechanicType = "Пробник",
                    ModuleName = "Контрольные тесты",
                    Description = "Пробник ОГЭ: тестовая часть проверяется автоматически, изложение и сочинение оценивает репетитор.",
                    IsPublished = true,
                    IsMockExam = true,
                    TutorId = tutorId,
                    TimeLimitMinutes = 235,
                    Questions = new List<LearningTestQuestion>
                    {
                        new LearningTestQuestion { Order = 1, Prompt = "Выберите верное утверждение о тексте.", QuestionType = "SingleChoice", OptionsText = "Утверждение 1\nУтверждение 2\nУтверждение 3\nУтверждение 4", CorrectAnswer = "Утверждение 3", Explanation = "Автопроверка.", MaxPoints = 1 },
                        new LearningTestQuestion { Order = 2, Prompt = "Напишите сжатое изложение по прослушанному тексту.", QuestionType = "Essay", Explanation = "Репетитор выставляет балл за изложение вручную.", MaxPoints = 6 },
                        new LearningTestQuestion { Order = 3, Prompt = "Выберите одну из тем 13.1, 13.2 или 13.3 и напишите сочинение-рассуждение.", QuestionType = "Essay", Explanation = "Сочинение также проверяется вручную.", MaxPoints = 7 }
                    }
                }, new[] { ogeGroupId }),
                (new LearningTest
                {
                    Title = "ОГЭ: диктор на радио",
                    ExamType = "Устное собеседование",
                    MechanicType = "Подкаст-диктор",
                    ModuleName = "Голос эфира",
                    Description = "Практика чтения вслух и устного монолога в образе ведущего радиопрограммы.",
                    IsPublished = true,
                    TutorId = tutorId,
                    TimeLimitMinutes = 20,
                    Questions = new List<LearningTestQuestion>
                    {
                        new LearningTestQuestion { Order = 1, Prompt = "Прочитайте текст вслух как диктор утреннего эфира и опишите, где сделали паузы.", QuestionType = "AudioPrompt", Explanation = "Пока ответ сохраняется текстом: ученик может описать, что получилось, а репетитор дать совет.", MaxPoints = 3 },
                        new LearningTestQuestion { Order = 2, Prompt = "Подготовьте монолог по картинке и коротко опишите, как выстроили вступление, основную часть и вывод.", QuestionType = "OpenText", Explanation = "Репетитор оценивает структуру и выразительность.", MaxPoints = 3 }
                    }
                }, new[] { ogeGroupId }),
                (new LearningTest
                {
                    Title = "Творческое задание: ревность и искренние чувства",
                    ExamType = "ЕГЭ",
                    MechanicType = "Творческое задание",
                    ModuleName = "Творческий цех",
                    Description = "Напиши два аргумента и покажи связь между ними на тему спада искренних чувств в современном времени.",
                    IsPublished = true,
                    TutorId = tutorId,
                    TimeLimitMinutes = 35,
                    Questions = new List<LearningTestQuestion>
                    {
                        new LearningTestQuestion { Order = 1, Prompt = "Сформулируй проблему, подбери два аргумента и объясни связь между ними. Можно опереться на литературу, жизнь или личный опыт.", QuestionType = "Essay", Explanation = "Репетитор оценивает глубину мысли и структуру аргументации.", MaxPoints = 10 }
                    }
                }, new[] { egeGroupId, writingGroupId })
            };

            var createdTests = new List<(LearningTest Test, int[] GroupIds)>();
            foreach (var item in tests)
            {
                var exists = await dbContext.LearningTests.AnyAsync(test => test.TutorId == tutorId && test.Title == item.Test.Title);
                if (exists)
                {
                    continue;
                }

                dbContext.LearningTests.Add(item.Test);
                createdTests.Add(item);
            }

            if (createdTests.Any())
            {
                await dbContext.SaveChangesAsync();

                foreach (var item in createdTests)
                {
                    foreach (var groupId in item.GroupIds)
                    {
                        dbContext.TestAssignments.Add(new TestAssignment { LearningTestId = item.Test.Id, StudentGroupId = groupId });
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        private static async Task SeedQuestionsAsync(ApplicationDbContext dbContext, string tutorId, ApplicationUser[] students)
        {
            if (await dbContext.StudentQuestions.AnyAsync()) return;

            dbContext.StudentQuestions.AddRange(
                new StudentQuestion
                {
                    StudentId = students[1].Id,
                    TutorId = tutorId,
                    Topic = "Комментарий в сочинении",
                    Message = "Я запутался, как лучше показать связь между примерами. Можно брать противопоставление, если один пример усиливает другой?",
                    TutorReply = "Да, можно, если ты прямо объясняешь, что второй пример раскрывает проблему под другим углом. Завтра на уроке разберем еще 2 шаблона 🙂",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    RepliedAtUtc = DateTime.UtcNow.AddHours(-20)
                },
                new StudentQuestion
                {
                    StudentId = students[0].Id,
                    TutorId = tutorId,
                    Topic = "Изложение",
                    Message = "Мне сложно удерживать вторую микротему, когда слушаю текст. Можно дать еще один короткий алгоритм?",
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-9)
                });

            await dbContext.SaveChangesAsync();
        }

        private static async Task<StudentGroup> EnsureGroupAsync(ApplicationDbContext dbContext, string tutorId, string name, string description)
        {
            var group = await dbContext.StudentGroups.FirstOrDefaultAsync(item => item.TutorId == tutorId && item.Name == name);
            if (group != null)
            {
                group.Description = description;
                await dbContext.SaveChangesAsync();
                return group;
            }

            group = new StudentGroup { Name = name, Description = description, TutorId = tutorId };
            dbContext.StudentGroups.Add(group);
            await dbContext.SaveChangesAsync();
            return group;
        }

        private static async Task EnsureMembershipAsync(ApplicationDbContext dbContext, int groupId, string studentId)
        {
            var exists = await dbContext.StudentGroupMembers.AnyAsync(item => item.StudentGroupId == groupId && item.StudentId == studentId);
            if (!exists)
            {
                dbContext.StudentGroupMembers.Add(new StudentGroupMember { StudentGroupId = groupId, StudentId = studentId });
            }
        }

        private static async Task EnsureLessonAssignmentAsync(ApplicationDbContext dbContext, int lessonId, int groupId)
        {
            var exists = await dbContext.LessonAssignments.AnyAsync(item => item.LessonId == lessonId && item.StudentGroupId == groupId);
            if (!exists)
            {
                dbContext.LessonAssignments.Add(new LessonAssignment { LessonId = lessonId, StudentGroupId = groupId, IsVisibleToStudent = true, AssignedAtUtc = DateTime.UtcNow });
            }
        }

        private static async Task EnsureSqliteColumnAsync(ApplicationDbContext dbContext, string tableName, string columnName, string columnDefinition)
        {
            await using var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info('{tableName}')";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            await dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string fullName,
            string role,
            string gradeLabel)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (user.PlatformRole != role || user.FullName != fullName || user.GradeLabel != gradeLabel)
                {
                    user.PlatformRole = role;
                    user.FullName = fullName;
                    user.GradeLabel = gradeLabel;
                    await userManager.UpdateAsync(user);
                }

                return user;
            }

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PlatformRole = role,
                GradeLabel = gradeLabel
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Не удалось создать пользователя {email}: {errors}");
            }

            return user;
        }
    }
}





