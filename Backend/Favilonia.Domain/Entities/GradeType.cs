namespace Favilonia.Domain.Entities;

public static class GradeType
{
    public const string ControlWork = "ControlWork";   // Контрольная работа
    public const string Homework    = "Homework";      // Домашнее задание
    public const string OralAnswer  = "OralAnswer";    // Устный ответ
    public const string Test        = "Test";          // Тест
    public const string Other       = "Other";         // Другое

    public static readonly IReadOnlyCollection<string> All =
        new[] { ControlWork, Homework, OralAnswer, Test, Other };
}
