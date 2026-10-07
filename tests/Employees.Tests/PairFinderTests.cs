using Employees.Core;
using Xunit;

namespace Employees.Tests;

public class PairFinderTests
{
    private static readonly DateOnly Today = new(2024, 1, 1);

    private static ParseResult Parse(string csv) => CsvReader.Read(new StringReader(csv), Today);

    [Fact]
    public void Sums_overlap_across_projects_and_counts_both_boundary_days()
    {
        var rows = Parse("""
            1, 10, 2020-01-01, 2020-01-10
            2, 10, 2020-01-06, 2020-01-20
            1, 20, 2021-01-01, 2021-01-03
            2, 20, 2021-01-01, 2021-01-03
            3, 10, 2020-01-09, 2020-01-09
            """);

        var pair = PairFinder.FindLongestPairs(rows.Assignments).Single();

        Assert.Equal((1, 2), (pair.EmpId1, pair.EmpId2));
        Assert.Equal(5 + 3, pair.TotalDays);
        Assert.Equal(2, pair.Projects.Count);
    }

    [Fact]
    public void Returns_every_pair_that_ties_for_the_longest_time()
    {
        var rows = Parse("""
            1, 1, 2020-01-01, 2020-01-10
            2, 1, 2020-01-01, 2020-01-10
            3, 2, 2021-01-01, 2021-01-10
            4, 2, 2021-01-01, 2021-01-10
            5, 3, 2022-01-01, 2022-01-05
            6, 3, 2022-01-01, 2022-01-05
            """);

        var pairs = PairFinder.FindLongestPairs(rows.Assignments);

        Assert.Equal(new[] { (1, 2), (3, 4) }, pairs.Select(p => (p.EmpId1, p.EmpId2)));
        Assert.All(pairs, p => Assert.Equal(10, p.TotalDays));
    }

    [Fact]
    public void Null_DateTo_means_today()
    {
        var rows = Parse("1, 1, 2023-12-30, NULL\n2, 1, 2023-12-01, NULL");

        Assert.Equal(3, PairFinder.FindLongestPairs(rows.Assignments).Single().TotalDays);
    }

    [Fact]
    public void Overlapping_rows_of_the_same_employee_are_not_counted_twice()
    {
        var rows = Parse("1, 1, 2020-01-01, 2020-01-10\n1, 1, 2020-01-05, 2020-01-15\n2, 1, 2020-01-01, 2020-01-15");

        Assert.Equal(15, PairFinder.FindLongestPairs(rows.Assignments).Single().TotalDays);
    }

    [Fact]
    public void Employees_on_different_projects_do_not_count()
    {
        var rows = Parse("1, 1, 2020-01-01, 2020-01-10\n2, 2, 2020-01-01, 2020-01-10");

        Assert.Empty(PairFinder.FindLongestPairs(rows.Assignments));
    }

    [Theory]
    [InlineData("2020-03-04")]
    [InlineData("4/3/2020")]
    [InlineData("04.03.2020")]
    [InlineData("4 Mar 2020")]
    [InlineData("March 4, 2020")]
    [InlineData("20200304")]
    [InlineData("2020-03-04T10:15:00")]
    [InlineData("2020-03-04T23:30:00-05:00")]
    [InlineData("2020-3-4")]
    [InlineData("4-Mar-2020")]
    [InlineData("Wed, 4 Mar 2020")]
    public void Supports_many_date_formats(string input)
    {
        Assert.True(new DateParser().TryParse(input, out var date));
        Assert.Equal(new DateOnly(2020, 3, 4), date);
    }

    [Fact]
    public void Unambiguous_us_dates_parse_even_when_day_first_is_the_default()
    {
        Assert.True(new DateParser().TryParse("12/31/2020", out var date));
        Assert.Equal(new DateOnly(2020, 12, 31), date);
    }

    [Theory]
    [InlineData(NumericDateOrder.DayFirst, "2020-04-03")]
    [InlineData(NumericDateOrder.MonthFirst, "2020-03-04")]
    public void Unclear_dates_follow_the_chosen_order(NumericDateOrder order, string expected)
    {
        Assert.True(new DateParser(order).TryParse("03/04/2020", out var date));
        Assert.Equal(DateOnly.Parse(expected), date);
    }

    [Fact]
    public void Us_date_with_am_pm_time_parses()
    {
        Assert.True(new DateParser(NumericDateOrder.MonthFirst).TryParse("3/4/2020 11:30 PM", out var date));
        Assert.Equal(new DateOnly(2020, 3, 4), date);
    }

    [Fact]
    public void Bad_rows_are_reported_and_do_not_stop_the_import()
    {
        var result = Parse("EmpID,ProjectID,DateFrom,DateTo\n1,1,garbage,NULL\n2,1,2020-01-02,2020-01-01\n3,1,2020-01-01,NULL");

        Assert.Single(result.Assignments);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Quoted_fields_are_handled_by_CsvHelper()
    {
        var result = Parse("\"1\",\"5\",\"2020-01-01\",\"2020-01-02\"\n2,5,2020-01-01,2020-01-02");

        Assert.Equal(2, result.Assignments.Count);
    }

    [Fact]
    public void Three_way_tie_returns_all_three_pairs()
    {
        var rows = Parse("""
            1, 1, 2020-01-01, 2020-01-10
            2, 1, 2020-01-01, 2020-01-10
            3, 1, 2020-01-01, 2020-01-10
            """);

        var pairs = PairFinder.FindLongestPairs(rows.Assignments);

        Assert.Equal(new[] { (1, 2), (1, 3), (2, 3) }, pairs.Select(p => (p.EmpId1, p.EmpId2)));
        Assert.All(pairs, p => Assert.Equal(10, p.TotalDays));
    }
}
