using Xunit;

namespace ApplicationScoring.Tests;

public class ExtensionMethodsTests
{
    private readonly ValidatedApplication[] _applicants =
    [
        TestApplications.Valid("Charlie", "19", "3.5", "110"),
        TestApplications.Valid("Alice", "20", "3.5", "110"),
        TestApplications.Valid("Bob", "22", "3.8", "40")
    ];

    [Fact]
    public void CalculateAverageGpa_AggregatesAndSupportsEmptyInput()
    {
        Assert.Equal(3.6, Math.Round(_applicants.CalculateAverageGpa(), 1));
        Assert.Equal(0, Array.Empty<ValidatedApplication>().CalculateAverageGpa());
    }

    [Fact]
    public void HighAchievers_OrdersScoreTiesByName()
    {
        var scored = _applicants.ScoreAll(app => app.Gpa * 100);
        var report = ReportGenerator.HighAchievers(scored);

        Assert.Equal(new[] { "Bob", "Alice", "Charlie" }, report.Select(row => row.Name));
    }

    [Fact]
    public void PartiallyAppliedWeights_ProduceDifferentResults()
    {
        var scorer = ScoringFactory.CreateScorer();
        var applicant = TestApplications.Valid("Test", "20", "2.0", "10");
        Assert.Equal(30m, scorer(10m)(1m)(applicant));
        Assert.Equal(102m, scorer(1m)(10m)(applicant));
    }

    [Fact]
    public void DeferredScoring_ExecutesOnlyWhenEnumerated()
    {
        var calls = 0;
        var deferred = _applicants.ScoreAll(app =>
        {
            calls++;
            return ScoringPolicies.AcademicPolicy(app);
        });

        Assert.Equal(0, calls);
        var snapshot = deferred.ToArray();
        Assert.Equal(_applicants.Length, calls);
        Assert.Equal(810m, snapshot[0].Score);
        Assert.Equal(3, snapshot.Length);
        Assert.Equal(_applicants.Length, calls);
    }

    [Fact]
    public void Loading_ReleasesFileBeforeReturningSnapshot()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "Name,Age,GPA,Hours,HasCriminalRecord\nA,20,3.0,10,false");
            var result = FileBoundary.LoadApplications(path);
            File.Delete(path);
            var loaded = Assert.IsType<Success<IReadOnlyList<RawApplication>, DomainError>>(result);
            Assert.Equal("A", Assert.Single(loaded.Value).Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NativeIntegration_ValidInput_ReturnsExpectedMultiplier()
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(Path.Combine(AppContext.BaseDirectory, "native_scoring.dll")))
            return;

        var result = NativeScoringAdapter.GetExperienceMultiplier(110);
        Assert.Equal(3, Assert.IsType<Success<int, DomainError>>(result).Value);
    }

    [Fact]
    public void NativeIntegration_InvalidInput_ReturnsFailureWithoutCallingNative()
    {
        var result = NativeScoringAdapter.GetExperienceMultiplier(-5);
        Assert.Equal(ApplicationError.InvalidGpa, Assert.IsType<Failure<int, DomainError>>(result).Error.Code);
    }
}
