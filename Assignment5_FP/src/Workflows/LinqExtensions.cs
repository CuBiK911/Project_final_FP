namespace ApplicationScoring;

public static class LinqExtensions
{
    public static IEnumerable<ScoredApplication> ScoreAll(
        this IEnumerable<ValidatedApplication> apps, Func<ValidatedApplication, decimal> policy)
    {
        foreach (var applicant in apps)
            yield return SafeWorkFlow.Score(applicant, policy);
    }

    public static double CalculateAverageGpa(this IEnumerable<ValidatedApplication> apps)
        => apps.Select(app => (double)app.Gpa).DefaultIfEmpty(0).Average();
}
