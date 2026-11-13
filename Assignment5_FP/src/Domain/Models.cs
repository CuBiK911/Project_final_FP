namespace ApplicationScoring;

public sealed record RawApplication(string? Name, string? Age, string? Gpa, string? Hours, string? HasCriminalRecord);

public sealed record ScoredApplication(ValidatedApplication Applicant, decimal Score);

public sealed record AdmissionReport(IReadOnlyList<ScoredApplication> RankedApplicants, string Summary);

public sealed record RejectedApplication(int RecordNumber, string? Name, IReadOnlyList<string> Reasons);

public sealed record AgeGroupRow(string Group, int Count, double AverageGpa);

public sealed record HighAchieverRow(string Name, decimal Gpa, decimal Score);

public sealed record WorkflowReport(
    AdmissionReport Ranking,
    IReadOnlyList<RejectedApplication> Rejections,
    IReadOnlyList<AgeGroupRow> AgeGroups,
    IReadOnlyList<HighAchieverRow> HighAchievers);

public sealed partial record ValidatedApplication
{
    private ValidatedApplication(string name, int age, decimal gpa, int hours)
    {
        Name = name;
        Age = age;
        Gpa = gpa;
        Hours = hours;
    }

    public string Name { get; }
    public int Age { get; }
    public decimal Gpa { get; }
    public int Hours { get; }
}

public abstract record ValidationOutcome
{
    private protected ValidationOutcome() { }
}

public sealed record Valid(ValidatedApplication Value) : ValidationOutcome;

public sealed record Invalid(IReadOnlyList<string> Reasons) : ValidationOutcome;
