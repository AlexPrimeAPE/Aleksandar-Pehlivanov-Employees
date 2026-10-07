# Employees

Finds the pair of employees who worked together on common projects for the longest total time.
If several pairs tie, all of them are shown. The result is a single grid with the green total row(s); in a tie, every tied pair is in it, each with its own green total.
Input: CSV `EmpID, ProjectID, DateFrom, DateTo` (header optional, `NULL` or empty DateTo = today).

## Run
Only the .NET 8 SDK is needed. No npm, no build step for the UI.

- **Visual Studio / Rider:** open `Employees.sln` and press **Run** (F5). The browser opens at http://localhost:5080.
- **CLI:** `dotnet run --project src/Employees.Api`, then open http://localhost:5080.
- **Tests:** `dotnet test`

The page has two built-in sample files (`src/Employees.Api/wwwroot/samples`), including one with a tie.

## Structure
- `Employees.Core`: parsing and the algorithm (`CsvReader`, `DateParser`, `PairFinder`). No web dependencies.
- `Employees.Api`: one upload endpoint, error handling, and the static React UI.
- `Employees.Tests`: unit tests for Core and integration tests for the endpoint.

## Design decisions
- **Algorithm:** group by project, merge each employee's overlapping rows (no double counting), then compare two employees' sorted periods side by side. Totals are summed across projects.
- **Days are inclusive** of the first and last day (one shared day = 1). Change `DatePeriod.SharedDaysWith` if exclusive is expected.
- **Ties:** every pair with the top total is returned, ordered by employee id. The UI shows them in green with a "Tied" badge.
- **Dates:** `DateParser` supports ISO, European, US, month names and times/offsets. Unambiguous values work under either order; unclear ones like `03/04/2020` follow the day-first / month-first switch in the UI. Two-digit years are not supported (the century would be a guess).
- **Error handling:** uploads are limited to 10 MB and to `.csv`/`.txt`. Bad requests return `ProblemDetails` with a clear message. A global exception handler logs unexpected errors and returns a generic message. Bad CSV rows are skipped and listed in the UI.
- **UI:** React with no build step (React, ReactDOM and htm are vendored in `wwwroot/lib`), drag and drop, sample files, accessible status/alerts, light and dark mode.
- **Today** is injected (`TimeProvider`), so NULL handling is testable.
