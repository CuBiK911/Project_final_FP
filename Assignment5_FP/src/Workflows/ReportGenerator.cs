namespace ApplicationScoring;

public static class ReportGenerator
{
    public static IReadOnlyList<AgeGroupRow> AgeGroups(IEnumerable<ValidatedApplication> apps)
        => Array.AsReadOnly(apps
            .GroupBy(app => app.Age <= 20 ? "18-20" : "21-25")
            .Select(group => new AgeGroupRow(group.Key, group.Count(), group.CalculateAverageGpa()))
            .OrderByDescending(row => row.AverageGpa)
            .ThenBy(row => row.Group, StringComparer.Ordinal)
            .ToArray());

    public static IReadOnlyList<HighAchieverRow> HighAchievers(IEnumerable<ScoredApplication> scoredApps)
        => Array.AsReadOnly(scoredApps
            .Where(item => item.Applicant.Gpa >= 3.5m)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Applicant.Name, StringComparer.Ordinal)
            .Take(5)
            .Select(item => new HighAchieverRow(item.Applicant.Name, item.Applicant.Gpa, item.Score))
            .ToArray());

    public static string GenerateAgeGroupReport(IEnumerable<ValidatedApplication> apps)
        => FormatAgeGroups(AgeGroups(apps));

    public static string GenerateHighAchieversReport(IEnumerable<ScoredApplication> scoredApps)
        => FormatHighAchievers(HighAchievers(scoredApps));

    internal static string FormatAgeGroups(IReadOnlyList<AgeGroupRow> rows)
        => rows.Count == 0 ? "No data available." : string.Join(Environment.NewLine, rows.Select(row =>
            FormattableString.Invariant($"{row.Group}: {row.Count} applicants, Avg GPA: {row.AverageGpa:F2}")));

    internal static string FormatHighAchievers(IReadOnlyList<HighAchieverRow> rows)
        => rows.Count == 0 ? "No high achievers found." : string.Join(Environment.NewLine, rows.Select(row =>
            FormattableString.Invariant($"{row.Name} (GPA: {row.Gpa:0.##}, Score: {row.Score:0.##})")));
}
