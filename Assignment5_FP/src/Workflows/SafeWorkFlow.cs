namespace ApplicationScoring;

public static class SafeWorkFlow
{
    public static ValidationOutcome Validate(RawApplication raw) => ApplicationFactory.Create(raw);

    public static ScoredApplication Score(ValidatedApplication app, Func<ValidatedApplication, decimal> policy)
        => new(app, policy(app));

    public static AdmissionReport GenerateReport(IReadOnlyList<ScoredApplication> scoredApps)
        => PipelineStages.GenerateReport(scoredApps);

    public static AdmissionReport Run(IReadOnlyList<RawApplication> rawInputs, Func<ValidatedApplication, decimal> policy)
        => Workflow.Run(rawInputs, policy);
}
