namespace Employees.Core;

public static class PairFinder
{
    private readonly record struct EmployeePair(int EmpId1, int EmpId2);

    private sealed record Worker(int EmpId, List<DatePeriod> Periods);

    public static IReadOnlyList<PairResult> FindLongestPairs(IEnumerable<Assignment> assignments) =>
        TopPairs(FindAllPairs(assignments));

    private static IReadOnlyList<PairResult> FindAllPairs(IEnumerable<Assignment> assignments)
    {
        var projectsByPair = new Dictionary<EmployeePair, List<CommonProject>>();

        foreach (var project in assignments.GroupBy(a => a.ProjectId))
            CollectCommonProjects(project.Key, project, projectsByPair);

        return projectsByPair
            .Select(entry => new PairResult(
                entry.Key.EmpId1,
                entry.Key.EmpId2,
                entry.Value.Sum(p => p.DaysWorked),
                entry.Value.OrderByDescending(p => p.DaysWorked).ThenBy(p => p.ProjectId).ToList()))
            .OrderByDescending(p => p.TotalDays).ThenBy(p => p.EmpId1).ThenBy(p => p.EmpId2)
            .ToList();
    }

    private static IReadOnlyList<PairResult> TopPairs(IReadOnlyList<PairResult> allPairs) =>
        allPairs.Count == 0
            ? allPairs
            : allPairs.TakeWhile(p => p.TotalDays == allPairs[0].TotalDays).ToList();

    private static void CollectCommonProjects(
        int projectId,
        IEnumerable<Assignment> projectRows,
        Dictionary<EmployeePair, List<CommonProject>> projectsByPair)
    {
        var workers = projectRows
            .GroupBy(a => a.EmpId)
            .Select(g => new Worker(g.Key, MergePeriods(g)))
            .OrderBy(w => w.EmpId)
            .ToList();

        for (var i = 0; i < workers.Count; i++)
        {
            for (var j = i + 1; j < workers.Count; j++)
            {
                var days = CountSharedDays(workers[i].Periods, workers[j].Periods);
                if (days == 0) continue;

                var pair = new EmployeePair(workers[i].EmpId, workers[j].EmpId);
                if (!projectsByPair.TryGetValue(pair, out var projects))
                    projectsByPair[pair] = projects = new List<CommonProject>();

                projects.Add(new CommonProject(pair.EmpId1, pair.EmpId2, projectId, days));
            }
        }
    }

    private static List<DatePeriod> MergePeriods(IEnumerable<Assignment> rows)
    {
        var merged = new List<DatePeriod>();

        foreach (var period in rows.Select(r => r.Period).OrderBy(p => p.From))
        {
            if (merged.Count > 0 && period.From <= merged[^1].To)
            {
                var last = merged[^1];
                merged[^1] = last with { To = period.To > last.To ? period.To : last.To };
            }
            else
            {
                merged.Add(period);
            }
        }

        return merged;
    }

    private static int CountSharedDays(List<DatePeriod> first, List<DatePeriod> second)
    {
        int i = 0, j = 0, total = 0;

        while (i < first.Count && j < second.Count)
        {
            total += first[i].SharedDaysWith(second[j]);

            if (first[i].To < second[j].To) i++;
            else j++;
        }

        return total;
    }
}
