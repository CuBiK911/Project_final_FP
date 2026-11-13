using Xunit;

namespace ApplicationScoring.Tests;

public class PipelineTests
{
    private static RawApplication[] Sample() =>
    [
        new("Alice", "20", "3.0", "110", "false"),
        new("Bob", "17", "3.5", "10", "false"),
        new("Charlie", "21", "4.0", "0", "false"),
        new("Diana", "22", "-1.0", "15", "false"),
        new("Eve", "21", "3.2", "50", "true")
    ];

    [Fact]
    public void AcademicWorkflow_RanksEligibleRowsAndExplainsRejections()
    {
        var report = Workflow.RunDetailed(Sample(), ScoringPolicies.AcademicPolicy);

        Assert.Collection(report.Ranking.RankedApplicants,
            first => Assert.Equal(("Charlie", 800m), (first.Applicant.Name, first.Score)),
            second => Assert.Equal(("Alice", 710m), (second.Applicant.Name, second.Score)));
        Assert.Equal(new[] { "Bob", "Diana", "Eve" }, report.Rejections.Select(item => item.Name));
        Assert.Equal(new[] { 2, 4, 5 }, report.Rejections.Select(item => item.RecordNumber));
        Assert.Equal("Charlie", Assert.Single(report.HighAchievers).Name);
        Assert.Equal(new AgeGroupRow("21-25", 1, 4), report.AgeGroups[0]);
    }

    [Fact]
    public void HolisticWorkflow_ChangesTheRankingUsingTheSameRows()
    {
        var report = Workflow.RunDetailed(Sample(), ScoringPolicies.HolisticPolicy);

        Assert.Collection(report.Ranking.RankedApplicants,
            first => Assert.Equal(("Alice", 850m), (first.Applicant.Name, first.Score)),
            second => Assert.Equal(("Charlie", 400m), (second.Applicant.Name, second.Score)));
        Assert.Equal(3, report.Rejections.Count);
    }

    [Fact]
    public void EmptyDataset_ProducesEmptyReportWithoutScoring()
    {
        var report = Workflow.RunDetailed([], _ => throw new InvalidOperationException("Unexpected scoring"));

        Assert.Empty(report.Ranking.RankedApplicants);
        Assert.Empty(report.Rejections);
        Assert.Empty(report.AgeGroups);
        Assert.Empty(report.HighAchievers);
        Assert.Equal("No eligible candidates.", report.Ranking.Summary);
    }

    [Fact]
    public void InvalidRows_SkipScoringAndLeaveCallerOwnedRowsUntouched()
    {
        var rows = Sample().ToList();
        var original = rows.ToArray();
        var scored = new List<string>();
        var report = Workflow.RunDetailed(rows, applicant =>
        {
            scored.Add(applicant.Name);
            return 0m;
        });

        Assert.Equal(original, rows);
        Assert.Equal(new[] { "Alice", "Charlie" }, scored);
        rows.Clear();
        Assert.Equal(2, report.Ranking.RankedApplicants.Count);
        Assert.Equal(3, report.Rejections.Count);
    }

    [Fact]
    public void Factory_AcceptsInclusiveBoundariesAndTrimsName()
    {
        var lower = TestApplications.Valid("  Alex  ", "18", "0", "0");
        var upper = TestApplications.Valid("Alex", "25", "4.0", "0");

        Assert.Equal(("Alex", 18, 0m, 0), (lower.Name, lower.Age, lower.Gpa, lower.Hours));
        Assert.Equal(("Alex", 25, 4m, 0), (upper.Name, upper.Age, upper.Gpa, upper.Hours));
    }

    [Theory]
    [InlineData(null, "20", "3.0", "10", "false", "Name")]
    [InlineData("A", "twenty", "3.0", "10", "false", "Age")]
    [InlineData("A", "20", "3,5", "10", "false", "GPA")]
    [InlineData("A", "20", "4.1", "10", "false", "GPA")]
    [InlineData("A", "20", "3.0", "-1", "false", "Hours")]
    [InlineData("A", "20", "3.0", "10", "maybe", "Criminal record flag")]
    public void Factory_RejectsInvalidFields(string? name, string age, string gpa, string hours, string flag, string field)
    {
        var result = Assert.IsType<Invalid>(ApplicationFactory.Create(name, age, gpa, hours, flag));
        Assert.StartsWith(field, Assert.Single(result.Reasons));
    }

    [Fact]
    public void Ranking_TiesUseOrdinalApplicantName()
    {
        RawApplication[] rows = [new("Zoe", "20", "3.5", "0", "false"), new("Ana", "21", "3.5", "0", "false")];
        var report = Workflow.RunDetailed(rows, ScoringPolicies.AcademicPolicy);

        Assert.Equal(new[] { "Ana", "Zoe" }, report.Ranking.RankedApplicants.Select(item => item.Applicant.Name));
        Assert.Equal(new[] { "Ana", "Zoe" }, report.HighAchievers.Select(item => item.Name));
        Assert.Equal(new[] { "18-20", "21-25" }, report.AgeGroups.Select(item => item.Group));
    }
}
