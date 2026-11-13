namespace ApplicationScoring;

internal static class Program
{
    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "data", "sample_input.csv");

    public static int Main(string[] args)
    {
        try
        {
            return args switch
            {
                [] or ["demo"] => Demo(),
                ["rank", var input, var policy, var output] => Rank(input, policy, output),
                ["applicant", var input, var policy, var name] => Applicant(input, policy, name),
                ["--help"] or ["-h"] => ShowUsage(Console.Out, 0),
                _ => ShowUsage(Console.Error, 2)
            };
        }
        catch (IOException ex)
        {
            try { Console.Error.WriteLine($"Console I/O failed: {ex.Message}"); }
            catch (IOException) { }
            return 1;
        }
    }

    private static int Rank(string input, string policyName, string output)
    {
        return ResolvePolicy(policyName)
            .Bind(policy => CheckOutputPath(input, output).Map(_ => policy))
            .Bind(policy => FileBoundary.LoadApplications(input)
                .Map(rows => Workflow.RunDetailed(rows, policy)))
            .Map(report => ReportText.Format(policyName, report))
            .Bind(report => FileBoundary.SaveReport(output, report).Map(path => (Report: report, Path: path)))
            .Match(
                saved =>
                {
                    Console.Write(saved.Report);
                    Console.WriteLine($"Exported to {saved.Path}");
                    return 0;
                },
                PrintError);
    }

    private static int Applicant(string input, string policyName, string name)
    {
        return ResolvePolicy(policyName)
            .Bind(policy => FileBoundary.LoadApplications(input)
                .Map(rows => PipelineStages.ValidateApplications(rows).Applicants)
                .Bind(applicants => DomainOperations.ProcessApplicantChain(applicants, name, policy)))
            .Match(
                scored =>
                {
                    Console.WriteLine(FormattableString.Invariant(
                        $"{scored.Applicant.Name} confirmed with a score of {scored.Score:0.##}."));
                    return 0;
                },
                PrintError);
    }

    private static int Demo()
    {
        return FileBoundary.LoadApplications(SamplePath).Match(rows =>
        {
            Console.Write(ReportText.Format("academic", Workflow.RunDetailed(rows, ScoringPolicies.AcademicPolicy)));
            Console.WriteLine();
            Console.Write(ReportText.Format("holistic", Workflow.RunDetailed(rows, ScoringPolicies.HolisticPolicy)));

            Console.WriteLine();
            Console.WriteLine("Deferred scoring");
            var (applicants, _) = PipelineStages.ValidateApplications(rows);
            var calls = 0;
            var deferredScores = applicants.ScoreAll(app =>
            {
                calls++;
                return ScoringPolicies.AcademicPolicy(app);
            });
            Console.WriteLine($"Policy calls before enumeration: {calls}");
            var snapshot = deferredScores.ToArray();
            Console.WriteLine($"Policy calls after snapshot: {calls}. Scores saved in memory: {snapshot.Length}.");

            Console.WriteLine();
            Console.WriteLine("Confirmation checks");
            var lowGpa = ApplicationFactory.Create("Sample applicant", "22", "1.5", "10", "false");
            if (lowGpa is Valid valid)
                ShowOutcome(DomainOperations.ProcessApplicantChain([valid.Value], valid.Value.Name, ScoringPolicies.AcademicPolicy));
            ShowOutcome(DomainOperations.ProcessApplicantChain(applicants, "Unknown", ScoringPolicies.AcademicPolicy));
            ShowOutcome(DomainOperations.ConfirmState(new Rejected("Manual rejection"), ScoringPolicies.AcademicPolicy));
            return 0;
        }, PrintError);
    }

    private static Result<Func<ValidatedApplication, decimal>, DomainError> ResolvePolicy(string name)
        => name.ToLowerInvariant() switch
        {
            "academic" => new Success<Func<ValidatedApplication, decimal>, DomainError>(ScoringPolicies.AcademicPolicy),
            "holistic" => new Success<Func<ValidatedApplication, decimal>, DomainError>(ScoringPolicies.HolisticPolicy),
            _ => new Failure<Func<ValidatedApplication, decimal>, DomainError>(
                new DomainError(ApplicationError.UnknownPolicy, $"Unknown policy '{name}'. Use academic or holistic."))
        };

    private static Result<string, DomainError> CheckOutputPath(string input, string output)
    {
        try
        {
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            if (comparer.Equals(Path.GetFullPath(input), Path.GetFullPath(output)))
                return new Failure<string, DomainError>(new DomainError(
                    ApplicationError.FileWriteFailed, "Choose a report path different from the input CSV."));
            return new Success<string, DomainError>(output);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            return new Failure<string, DomainError>(new DomainError(ApplicationError.FileWriteFailed, ex.Message));
        }
    }

    private static void ShowOutcome<T>(Result<T, DomainError> outcome)
        => Console.WriteLine(outcome.Match(value => $"Success: {value}", error => $"{error.Code}: {error.Message}"));

    private static int PrintError(DomainError error)
    {
        Console.Error.WriteLine($"{error.Code}: {error.Message}");
        return error.Code == ApplicationError.UnknownPolicy ? 2 : 1;
    }

    private static int ShowUsage(TextWriter output, int exitCode)
    {
        output.WriteLine("""
            University Application Scoring
              demo
              rank <input.csv> <academic|holistic> <report.txt>
              applicant <input.csv> <academic|holistic> <name>
            """);
        return exitCode;
    }
}

