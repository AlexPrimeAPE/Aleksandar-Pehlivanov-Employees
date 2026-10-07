using System.Globalization;

namespace Employees.Core;

public enum NumericDateOrder { DayFirst, MonthFirst }

public sealed class DateParser
{
    private static readonly string[] Iso =
    {
        "yyyy-M-d", "yyyy/M/d", "yyyy.M.d", "yyyyMMdd",
        "yyyy-M-d H:mm", "yyyy-M-d H:mm:ss", "yyyy-M-d H:mm:ss.FFFFFFF",
        "yyyy-M-dTH:mm:ss", "yyyy-M-dTH:mm:ss.FFFFFFF",
        "yyyy-M-dTH:mm:ssK", "yyyy-M-dTH:mm:ss.FFFFFFFK",
    };

    private static readonly string[] DayFirst =
    {
        "d/M/yyyy", "d-M-yyyy", "d.M.yyyy",
        "d/M/yyyy H:mm", "d/M/yyyy H:mm:ss", "d.M.yyyy H:mm", "d.M.yyyy H:mm:ss",
    };

    private static readonly string[] MonthFirst =
    {
        "M/d/yyyy", "M-d-yyyy", "M.d.yyyy",
        "M/d/yyyy H:mm", "M/d/yyyy H:mm:ss", "M/d/yyyy h:mm tt", "M/d/yyyy h:mm:ss tt",
    };

    private static readonly string[] MonthNames =
    {
        "d MMM yyyy", "d MMMM yyyy", "d-MMM-yyyy", "d-MMMM-yyyy", "d/MMM/yyyy",
        "MMM d, yyyy", "MMMM d, yyyy", "MMM d yyyy", "MMMM d yyyy", "MMM-d-yyyy",
        "ddd, d MMM yyyy", "dddd, d MMMM yyyy", "ddd, MMM d, yyyy", "dddd, MMMM d, yyyy",
        "yyyy MMM d", "yyyy-MMM-d",
    };

    public static readonly string[] DefaultFormats =
        Iso.Concat(DayFirst).Concat(MonthFirst).Concat(MonthNames).ToArray();

    private readonly string[] _formats;

    public DateParser(NumericDateOrder order = NumericDateOrder.DayFirst, IEnumerable<string>? formats = null)
    {
        _formats = formats?.ToArray() ?? OrderedFormats(order);
    }

    public bool TryParse(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var text = value.Trim();
        foreach (var format in _formats)
        {
            var parsed = DateTimeOffset.TryParseExact(
                text, format, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var result);

            if (parsed)
            {
                date = DateOnly.FromDateTime(result.DateTime);
                return true;
            }
        }
        return false;
    }

    private static string[] OrderedFormats(NumericDateOrder order) =>
        order == NumericDateOrder.DayFirst
            ? DefaultFormats
            : Iso.Concat(MonthFirst).Concat(DayFirst).Concat(MonthNames).ToArray();
}
