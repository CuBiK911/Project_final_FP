namespace ApplicationScoring;

public static class ScoringPolicies
{
    private static readonly Func<ValidatedApplication, decimal> Academic = ScoringFactory.CreateScorer()(200m)(1m);
    private static readonly Func<ValidatedApplication, decimal> Holistic = ScoringFactory.CreateScorer()(100m)(5m);

    public static decimal AcademicPolicy(ValidatedApplication applicant) => Academic(applicant);

    public static decimal HolisticPolicy(ValidatedApplication applicant) => Holistic(applicant);
}
