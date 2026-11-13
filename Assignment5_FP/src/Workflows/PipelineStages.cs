namespace ApplicationScoring;

public static class PipelineStages
{
    public static (IReadOnlyList<ValidatedApplication> Applicants, IReadOnlyList<RejectedApplication> Rejections)
        ValidateApplications(IEnumerable<RawApplication> applications)
    {
        var applicants = new List<ValidatedApplication>();
        var rejections = new List<RejectedApplication>();
        var recordNumber = 0;

        foreach (var raw in applications)
        {
            recordNumber++;
            switch (ApplicationFactory.Create(raw))
            {
                case Valid valid:
                    applicants.Add(valid.Value);
                    break;
                case Invalid invalid:
                    rejections.Add(new RejectedApplication(recordNumber, raw.Name, invalid.Reasons));
                    break;
            }
        }

        return (applicants.AsReadOnly(), rejections.AsReadOnly());
    }

    public static IReadOnlyList<ScoredApplication> ComputeScores(
        IEnumerable<ValidatedApplication> applicants,
        Func<ValidatedApplication, decimal> scoringPolicy)
        => Array.AsReadOnly(applicants.ScoreAll(scoringPolicy).ToArray());

    public static AdmissionReport GenerateReport(IEnumerable<ScoredApplication> scoredApplicants)
    {
        var ranked = Array.AsReadOnly(scoredApplicants
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Applicant.Name, StringComparer.Ordinal)
            .ToArray());

        var summary = ranked.Count == 0
            ? "No eligible candidates."
            : FormattableString.Invariant(
                $"Top candidate: {ranked[0].Applicant.Name} with score {ranked[0].Score:0.##}.");
        return new AdmissionReport(ranked, summary);
    }
}
