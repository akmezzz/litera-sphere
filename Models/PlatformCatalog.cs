using System.Collections.Generic;

namespace TutorPlatform.Models
{
    public static class PlatformCatalog
    {
        public static IReadOnlyList<FeatureModule> FeatureModules { get; } = new[]
        {
            new FeatureModule("Голос эфира", "Устное собеседование, чтение и монологи", "Запись и анализ аудиоответов, работа с монологом, карточки-опоры."),
            new FeatureModule("Детектив ошибок", "Редактирование и анализ", "Перестановка абзацев, поиск ошибок, классификация связок и пунктуации."),
            new FeatureModule("Тренажер ФИПИ", "Интерактивные тесты", "Орфография, корни, синтаксис, лексика и средства выразительности."),
            new FeatureModule("Творческий цех", "Итоговое сочинение", "Аргументация, подбор цитат, антиплагиат и редактура стиля.")
        };

        public static IReadOnlyList<ExamTrack> ExamTracks { get; } = new[]
        {
            new ExamTrack("ЕГЭ", "Автоматическая проверка заданий 1-26, отдельная работа с заданием 27, таймер на 3 часа 30 минут."),
            new ExamTrack("ОГЭ", "Аудиомодуль для изложения, тестовая часть 2-12, сочинение 13.1-13.3, таймер на 3 часа 55 минут.")
        };

        public static IReadOnlyList<string> ExamTypes { get; } = new[] { "ОГЭ", "ЕГЭ", "Итоговое сочинение", "Произвольный модуль" };

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
}
