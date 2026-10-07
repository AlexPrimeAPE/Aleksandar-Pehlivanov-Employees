using Employees.Core;

namespace Employees.Api;

public static class UploadLimits
{
    public const long MaxBytes = 10 * 1024 * 1024;
}

public sealed record AnalyzeResponse(IReadOnlyList<PairResult> Pairs, IReadOnlyList<ParseError> Errors, int RowsRead);

public static class AnalyzeEndpoint
{
    private static readonly string[] AllowedExtensions = [".csv", ".txt"];

    public static void MapAnalyzeEndpoint(this IEndpointRouteBuilder routes) =>
        routes.MapPost("/api/analyze", Analyze)
              .DisableAntiforgery();

    private static IResult Analyze(IFormFile file, string? dateOrder, TimeProvider clock)
    {
        if (file.Length == 0)
            return Problem(StatusCodes.Status400BadRequest, "The file is empty.", "Choose a CSV file with at least one row.");

        if (file.Length > UploadLimits.MaxBytes)
            return Problem(StatusCodes.Status413PayloadTooLarge, "The file is too large.", "The maximum size is 10 MB.");

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
            return Problem(StatusCodes.Status415UnsupportedMediaType, "Unsupported file type.", "Please upload a .csv or .txt file.");

        if (!TryReadDateOrder(dateOrder, out var order))
            return Problem(StatusCodes.Status400BadRequest, "Unknown date order.", "Use 'dmy' (day first) or 'mdy' (month first).");

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        using var reader = new StreamReader(file.OpenReadStream());
        var parsed = CsvReader.Read(reader, today, new DateParser(order));
        var pairs = PairFinder.FindLongestPairs(parsed.Assignments);

        return Results.Ok(new AnalyzeResponse(pairs, parsed.Errors, parsed.Assignments.Count));
    }

    private static bool TryReadDateOrder(string? value, out NumericDateOrder order)
    {
        order = NumericDateOrder.DayFirst;

        if (string.IsNullOrEmpty(value) || value.Equals("dmy", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("mdy", StringComparison.OrdinalIgnoreCase))
        {
            order = NumericDateOrder.MonthFirst;
            return true;
        }

        return false;
    }

    private static IResult Problem(int status, string title, string detail) =>
        Results.Problem(statusCode: status, title: title, detail: detail);
}
