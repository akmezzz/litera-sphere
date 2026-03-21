using System;
using System.Collections.Generic;
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
                await EnsureUserAsync(userManager, "sofia@student.local", "Student123!", "София Миронова", PlatformRoles.Student, "11 класс")
            };

            if (await dbContext.StudentGroups.AnyAsync())
            {
                return;
            }

            var ogeGroup = new StudentGroup
            {
                Name = "ОГЭ: интенсив",
                Description = "Группа для подготовки к устному собеседованию и изложению.",
                TutorId = tutor.Id
            };

            var egeGroup = new StudentGroup
            {
                Name = "ЕГЭ: сочинение и тест",
                Description = "Группа для 11 класса с пробниками и редактурой сочинений.",
                TutorId = tutor.Id
            };

            dbContext.StudentGroups.AddRange(ogeGroup, egeGroup);
            await dbContext.SaveChangesAsync();

            dbContext.StudentGroupMembers.AddRange(
                new StudentGroupMember { StudentGroupId = ogeGroup.Id, StudentId = students[0].Id },
                new StudentGroupMember { StudentGroupId = egeGroup.Id, StudentId = students[1].Id },
                new StudentGroupMember { StudentGroupId = egeGroup.Id, StudentId = students[2].Id });

            var punctuationTest = new LearningTest
            {
                Title = "Пунктуационный лабиринт: БСП и СПП",
                ExamType = "ЕГЭ",
                MechanicType = "Пунктуационный лабиринт",
                ModuleName = "Детектив ошибок",
                Description = "Тренажер по постановке знаков препинания в сложных предложениях.",
                IsPublished = true,
                TutorId = tutor.Id,
                TimeLimitMinutes = 25,
                Questions = new List<LearningTestQuestion>
                {
                    new LearningTestQuestion
                    {
                        Order = 1,
                        Prompt = "Выберите правильный знак: Я понял __ пора начинать подготовку.",
                        QuestionType = "SingleChoice",
                        OptionsText = "запятая\nдвоеточие\nтире\nничего",
                        CorrectAnswer = "двоеточие",
                        Explanation = "Вторая часть поясняет первую.",
                        MaxPoints = 1
                    },
                    new LearningTestQuestion
                    {
                        Order = 2,
                        Prompt = "Кратко объясните, почему выбран именно этот знак препинания.",
                        QuestionType = "OpenText",
                        CorrectAnswer = "",
                        Explanation = "Ответ проверяется репетитором.",
                        MaxPoints = 2
                    }
                }
            };

            var mockExam = new LearningTest
            {
                Title = "Пробник ЕГЭ: тест + задание 27",
                ExamType = "ЕГЭ",
                MechanicType = "Пробник",
                ModuleName = "Контрольные тесты",
                Description = "Модель пробника: задания 1-26 автоматически, сочинение отправляется репетитору на проверку.",
                IsPublished = true,
                IsMockExam = true,
                TutorId = tutor.Id,
                TimeLimitMinutes = 210,
                Questions = new List<LearningTestQuestion>
                {
                    new LearningTestQuestion
                    {
                        Order = 1,
                        Prompt = "Укажите вариант, где во всех словах пропущена одна и та же буква.",
                        QuestionType = "SingleChoice",
                        OptionsText = "пр..града, пр..образить\nпр..ступить, пр..мьера\nпр..одолеть, пр..вратить\nпр..клонный, пр..чудливый",
                        CorrectAnswer = "пр..одолеть, пр..вратить",
                        Explanation = "Во всех словах пишется буква Е: преодолеть, превратить.",
                        MaxPoints = 1
                    },
                    new LearningTestQuestion
                    {
                        Order = 2,
                        Prompt = "Напишите мини-сочинение по тексту и обозначьте проблему, позицию автора и собственный комментарий.",
                        QuestionType = "Essay",
                        Explanation = "Для задания 27 нужна ручная или ИИ-проверка.",
                        MaxPoints = 22
                    }
                }
            };

            dbContext.LearningTests.AddRange(punctuationTest, mockExam);
            await dbContext.SaveChangesAsync();

            dbContext.TestAssignments.AddRange(
                new TestAssignment { LearningTestId = punctuationTest.Id, StudentGroupId = egeGroup.Id },
                new TestAssignment { LearningTestId = mockExam.Id, StudentGroupId = egeGroup.Id },
                new TestAssignment { LearningTestId = punctuationTest.Id, StudentGroupId = ogeGroup.Id });

            await dbContext.SaveChangesAsync();
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
