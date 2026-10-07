namespace Employees.Core;

public readonly record struct DatePeriod(DateOnly From, DateOnly To)
{
    public int SharedDaysWith(DatePeriod other)
    {
        var start = From > other.From ? From : other.From;
        var end = To < other.To ? To : other.To;

        return start <= end ? end.DayNumber - start.DayNumber + 1 : 0;
    }
}

public sealed record Assignment(int EmpId, int ProjectId, DatePeriod Period);

public sealed record CommonProject(int EmpId1, int EmpId2, int ProjectId, int DaysWorked);

public sealed record PairResult(int EmpId1, int EmpId2, int TotalDays, IReadOnlyList<CommonProject> Projects);

public sealed record ParseError(int Line, string Message);

public sealed record ParseResult(IReadOnlyList<Assignment> Assignments, IReadOnlyList<ParseError> Errors);
