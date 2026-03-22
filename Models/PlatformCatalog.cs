using System;
using System.Collections.Generic;
using System.Linq;

namespace TutorPlatform.Models
{
    public static class PlatformCatalog
    {
        public static IReadOnlyList<FeatureModule> FeatureModules { get; } = new[]
        {
            new FeatureModule("Голос эфира", "Устное собеседование, чтение и монологи", "Тренировка чтения вслух, изложения, устного ответа и живых речевых формулировок."),
            new FeatureModule("Детектив ошибок", "Редактирование и анализ", "Орфография, пунктуация, связки и логика текста в интерактивной подаче."),
            new FeatureModule("Тренажер ФИПИ", "Интерактивные тесты", "Типовые задания по номерам, пробники и тематические наборы для закрепления."),
            new FeatureModule("Творческий цех", "Сочинение и аргументация", "Комментарий, позиция автора, литературные диалоги и игровые задания на смысл." )
        };

        public static IReadOnlyList<ExamTrack> ExamTracks { get; } = new[]
        {
            new ExamTrack("ЕГЭ", "Автоматическая проверка заданий 1-26, ручная оценка сочинения, таймер 3 часа 30 минут."),
            new ExamTrack("ОГЭ", "Тестовая часть, изложение и сочинение с ручной проверкой, таймер 3 часа 55 минут."),
            new ExamTrack("Итоговое сочинение", "Темы, аргументация, комментарий и работа с собственным стилем."),
            new ExamTrack("Устное собеседование", "Чтение вслух, пересказ, монолог и ответы в формате диктора эфира.")
        };

        public static IReadOnlyList<string> ExamTypes { get; } = new[] { "ОГЭ", "ЕГЭ", "Итоговое сочинение", "Устное собеседование", "Произвольный модуль" };

        public static IReadOnlyList<string> Mechanics { get; } = new[]
        {
            "Подкаст-диктор",
            "Интервью с героем",
            "Эмодзи-план",
            "Битое сочинение",
            "Криминалист связок",
            "Орфографический вирус",
            "Пунктуационный лабиринт",
            "Морской бой: орфография",
            "Сортировщик корней",
            "Синтаксический конструктор",
            "Лексический аукцион",
            "Средства выразительности",
            "Битва цитат",
            "Анти-плагиат",
            "Творческое задание",
            "Пробник"
        };

        public static IReadOnlyList<string> QuestionTypes { get; } = new[]
        {
            "SingleChoice",
            "OpenText",
            "Essay",
            "AudioPrompt",
            "Matching"
        };

        public static IReadOnlyList<PracticeTaskCatalogItem> BuildPracticeTasks(string examType)
        {
            var maxTask = string.Equals(examType, "ЕГЭ", StringComparison.OrdinalIgnoreCase) ? 26 : 12;

            return Enumerable.Range(1, maxTask)
                .Select(number => new PracticeTaskCatalogItem
                {
                    TaskNumber = number,
                    TaskCount = 0,
                    Label = string.Equals(examType, "ЕГЭ", StringComparison.OrdinalIgnoreCase)
                        ? BuildEgeTaskLabel(number)
                        : BuildOgeTaskLabel(number)
                })
                .ToList();
        }

        public static string GetExamDescription(string examType)
        {
            return ExamTracks.FirstOrDefault(track => string.Equals(track.Title, examType, StringComparison.OrdinalIgnoreCase))?.Description
                ?? "Отдельный учебный трек с пробниками, типовыми заданиями и ручной проверкой письменной части.";
        }

        public static string GetDailyQuote(DateTime date)
        {
            var quotes = new[]
            {
                "Один пробник в день и один честный отдых вечером дают больше, чем бесконечная тревога.",
                "Сильный результат складывается из коротких, но регулярных подходов.",
                "Каждая исправленная ошибка сегодня экономит баллы на экзамене завтра.",
                "Спокойствие на экзамене тренируется дома так же, как и правила.",
                "Хороший русский язык любит ясную мысль и уверенный ритм работы."
            };

            return quotes[Math.Abs(date.DayOfYear) % quotes.Length];
        }

        private static string BuildEgeTaskLabel(int number)
        {
            var labels = new Dictionary<int, string>
            {
                [1] = "Информационная обработка текста",
                [2] = "Средства связи предложений",
                [3] = "Лексическое значение слова",
                [4] = "Орфоэпия",
                [5] = "Паронимы",
                [6] = "Лексические нормы",
                [7] = "Грамматические нормы",
                [8] = "Синтаксические нормы",
                [9] = "Правописание корней",
                [10] = "Приставки",
                [11] = "Суффиксы",
                [12] = "Личные окончания",
                [13] = "НЕ и НИ",
                [14] = "Слитное, дефисное, раздельное написание",
                [15] = "Н и НН",
                [16] = "Пунктуация в простом предложении",
                [17] = "Обособленные члены",
                [18] = "Вводные конструкции и обращения",
                [19] = "Сложноподчиненное предложение",
                [20] = "Сложные случаи пунктуации",
                [21] = "Пунктуационный анализ",
                [22] = "Смысловой анализ текста",
                [23] = "Типы речи и логика текста",
                [24] = "Лексика в тексте",
                [25] = "Средства связи и позиция автора",
                [26] = "Средства выразительности"
            };

            return labels.TryGetValue(number, out var label) ? label : "Тренировка по номеру";
        }

        private static string BuildOgeTaskLabel(int number)
        {
            var labels = new Dictionary<int, string>
            {
                [1] = "Изложение и понимание аудиотекста",
                [2] = "Анализ текста",
                [3] = "Орфографический анализ",
                [4] = "Пунктуационный анализ",
                [5] = "Синтаксический анализ",
                [6] = "Орфоэпия и словообразование",
                [7] = "Средства выразительности",
                [8] = "Лексический анализ",
                [9] = "Сочинение 13.1",
                [10] = "Сочинение 13.2",
                [11] = "Сочинение 13.3",
                [12] = "Устное собеседование и монолог"
            };

            return labels.TryGetValue(number, out var label) ? label : "Тренировка по номеру";
        }
    }

    public class FeatureModule
    {
        public FeatureModule(string title, string subtitle, string description)
        {
            Title = title;
            Subtitle = subtitle;
            Description = description;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string Description { get; }
    }

    public class ExamTrack
    {
        public ExamTrack(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public string Title { get; }
        public string Description { get; }
    }

    public class PracticeTaskCatalogItem
    {
        public int TaskNumber { get; set; }
        public int TaskCount { get; set; }
        public string Label { get; set; }
    }
}

