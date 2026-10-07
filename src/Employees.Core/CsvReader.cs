using System.Globalization;
using CsvHelper.Configuration;
using HelperException = CsvHelper.CsvHelperException;
using HelperReader = CsvHelper.CsvReader;

namespace Employees.Core;

public static class CsvReader
{
    private const int MaxMalformedRows = 100;

    public static ParseResult Read(TextReader input, DateOnly today, DateParser? dateParser = null)
    {
        dateParser ??= new DateParser();
        var assignments = new List<Assignment>();
        var errors = new List<ParseError>();

        using var csv = new HelperReader(input, CreateConfiguration());
        var isFirstRow = true;
        var malformedRows = 0;

        while (true)
        {
            try
            {
                if (!csv.Read()) break;
            }
            catch (HelperException)
            {
                errors.Add(new ParseError(csv.Parser.Row, "This row is not valid CSV (for example, a quote is never closed)."));
                if (++malformedRows >= MaxMalformedRows) break;
                continue;
            }

            var line = csv.Parser.Row;
            var fields = csv.Parser.Record!;

            if (isFirstRow)
            {
                isFirstRow = false;
                if (LooksLikeHeader(fields)) continue;
            }

            var assignment = ParseRow(fields, today, dateParser, out var error);
            if (assignment is not null)
                assignments.Add(assignment);
            else
                errors.Add(new ParseError(line, error!));
        }

        return new ParseResult(assignments, errors);
    }

    private static CsvConfiguration CreateConfiguration() => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        TrimOptions = TrimOptions.Trim,
        IgnoreBlankLines = true,
        MissingFieldFound = null,
        BadDataFound = null,
    };

    private static bool LooksLikeHeader(string[] fields) =>
        !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    private static Assignment? ParseRow(string[] fields, DateOnly today, DateParser dateParser, out string? error)
    {
        error = null;

        if (fields.Length != 4)
        {
            error = "Expected 4 columns: EmpID, ProjectID, DateFrom, DateTo.";
            return null;
        }

        if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var empId))
        {
            error = $"EmpID '{fields[0]}' is not a whole number.";
            return null;
        }

        if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var projectId))
        {
            error = $"ProjectID '{fields[1]}' is not a whole number.";
            return null;
        }

        if (!dateParser.TryParse(fields[2], out var from))
        {
            error = $"DateFrom '{fields[2]}' is not a date format we recognise.";
            return null;
        }

        var to = today;
        var dateToIsMissing = fields[3].Length == 0 || fields[3].Equals("NULL", StringComparison.OrdinalIgnoreCase);
        if (!dateToIsMissing && !dateParser.TryParse(fields[3], out to))
        {
            error = $"DateTo '{fields[3]}' is not a date format we recognise.";
            return null;
        }

        if (to < from)
        {
            error = "DateTo is earlier than DateFrom.";
            return null;
        }

        return new Assignment(empId, projectId, new DatePeriod(from, to));
    }
}
