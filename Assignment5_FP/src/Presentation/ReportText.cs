using System.Text;

namespace ApplicationScoring;

internal static class ReportText
{
    public static string Format(string policy, WorkflowReport report)
    {
        var text = new StringBuilder();
        text.AppendLine($"Admissions ranking ({policy})");
        text.AppendLine(report.Ranking.Summary);
        var position = 0;
        foreach (var entry in report.Ranking.RankedApplicants)
            text.AppendLine(FormattableString.Invariant($"{++position}. {entry.Applicant.Name} | Score {entry.Score:0.##}"));

        text.AppendLine();
        text.AppendLine($"Rejected applications ({report.Rejections.Count})");
        foreach (var item in report.Rejections)
        {
            var name = string.IsNullOrWhiteSpace(item.Name) ? "(missing name)" : item.Name;
            text.AppendLine($"Record {item.RecordNumber}, {name}: {string.Join(" ", item.Reasons)}");
        }

        text.AppendLine();
        text.AppendLine("Age groups (eligible applicants)");
        text.AppendLine(ReportGenerator.FormatAgeGroups(report.AgeGroups));
        text.AppendLine();
        text.AppendLine("High achievers (GPA 3.5 or above, up to 5)");
        text.AppendLine(ReportGenerator.FormatHighAchievers(report.HighAchievers));
        return text.ToString();
    }
}
