using Xunit;

namespace ApplicationScoring.Tests;

public class WorkflowBoundaryTests
{
    private const string Header = "Name,Age,GPA,Hours,HasCriminalRecord";

    [Fact]
    public void MissingFile_ProducesExplicitFailure()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.csv");
        var failure = Assert.IsType<Failure<IReadOnlyList<RawApplication>, DomainError>>(
            FileBoundary.LoadApplications(missing));
        Assert.Equal(ApplicationError.FileNotFound, failure.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Wrong,Age,GPA,Hours,HasCriminalRecord\nA,20,3.0,0,false")]
    [InlineData(Header + "\nA,20,3.0")]
    [InlineData(Header + "\n\"Unclosed,20,3.0,0,false")]
    public void MalformedFile_ProducesExplicitFailure(string csv)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, csv);
            var failure = Assert.IsType<Failure<IReadOnlyList<RawApplication>, DomainError>>(
                FileBoundary.LoadApplications(path));
            Assert.Equal(ApplicationError.MalformedFile, failure.Error.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CsvToReport_HandlesQuotedNameAndReplacesExistingReport()
    {
        var input = Path.GetTempFileName();
        var output = Path.GetTempFileName();
        try
        {
            File.WriteAllText(input, Header + "\n\"Doe, Jane\",25,4.0,0,false\nOther,20,3.5,-1,false");
            File.WriteAllText(output, "old report");
            var exported = FileBoundary.LoadApplications(input)
                .Map(rows => Workflow.RunDetailed(rows, ScoringPolicies.AcademicPolicy))
                .Bind(report => FileBoundary.SaveReport(output, report.Ranking.Summary));
            Assert.IsType<Success<string, DomainError>>(exported);
            Assert.Equal("Top candidate: Doe, Jane with score 800.", File.ReadAllText(output));
        }
        finally
        {
            File.Delete(input);
            File.Delete(output);
        }
    }

    [Fact]
    public void OutputDirectoryMissing_ReturnsWriteFailure()
    {
        var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "report.txt");
        var failure = Assert.IsType<Failure<string, DomainError>>(FileBoundary.SaveReport(output, "report"));
        Assert.Equal(ApplicationError.FileWriteFailed, failure.Error.Code);
    }

    [Fact]
    public void HeaderOnly_IsAValidEmptyDataset()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, Header);
            var result = Assert.IsType<Success<IReadOnlyList<RawApplication>, DomainError>>(
                FileBoundary.LoadApplications(path));
            Assert.Empty(result.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Missing", ApplicationError.MissingApplicant)]
    [InlineData("Low GPA", ApplicationError.InvalidGpa)]
    public void ApplicantFailure_SkipsScoring(string name, ApplicationError expected)
    {
        var applicant = TestApplications.Valid("Low GPA", "20", "1.99", "0");
        var outcome = DomainOperations.ProcessApplicantChain([applicant], name,
            _ => throw new InvalidOperationException("Unexpected scoring"));
        Assert.Equal(expected, Assert.IsType<Failure<ScoredApplication, DomainError>>(outcome).Error.Code);
    }

    [Fact]
    public void RejectedState_CannotBeConfirmedOrScored()
    {
        var rejected = new Rejected("Manual rejection");
        var outcome = DomainOperations.ConfirmState(rejected,
            _ => throw new InvalidOperationException("Unexpected scoring"));
        Assert.Equal(ApplicationError.StateTransitionNotAllowed,
            Assert.IsType<Failure<WorkflowState, DomainError>>(outcome).Error.Code);
        Assert.Equal("Manual rejection", rejected.Reason);
    }

    [Fact]
    public void Confirmation_AllowsGpaAtThresholdAndIsIdempotent()
    {
        var pending = new Pending(TestApplications.Valid("Alex", "18", "2.0", "0"));
        var first = Assert.IsType<Success<WorkflowState, DomainError>>(
            DomainOperations.ConfirmState(pending, ScoringPolicies.AcademicPolicy));
        var confirmed = Assert.IsType<Confirmed>(first.Value);
        Assert.Equal(400m, confirmed.Score);

        var second = Assert.IsType<Success<WorkflowState, DomainError>>(
            DomainOperations.ConfirmState(confirmed, _ => throw new InvalidOperationException("Unexpected rescoring")));
        Assert.Same(confirmed, second.Value);
    }
}
