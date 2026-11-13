namespace ApplicationScoring;

public static class Workflow
{
    public static AdmissionReport Run(IReadOnlyList<RawApplication> rawInputs, Func<ValidatedApplication, decimal> policy)
        => RunDetailed(rawInputs, policy).Ranking;

    public static WorkflowReport RunDetailed(
        IEnumerable<RawApplication> rawInputs, Func<ValidatedApplication, decimal> policy)
    {
        var (applicants, rejections) = PipelineStages.ValidateApplications(rawInputs);
        var scored = PipelineStages.ComputeScores(applicants, policy);
        var ranking = PipelineStages.GenerateReport(scored);

        return new WorkflowReport(
            ranking,
            rejections,
            ReportGenerator.AgeGroups(applicants),
            ReportGenerator.HighAchievers(scored));
    }
}
