using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationScoring
{
    public static class SafeWorkFlow
    {
       
        public static ValidationOutcome Validate(RawApplication raw)
        {
            return ApplicationFactory.Create(raw.Name, raw.Age, raw.Gpa, raw.Hours, raw.HasCriminalRecord);
        }

       
        public static ScoredApplication Score(ValidatedApplication app, Func<ValidatedApplication, decimal> policy)
        {
            return new ScoredApplication(app, policy(app));
        }

       
        public static AdmissionReport GenerateReport(IReadOnlyList<ScoredApplication> scoredApps)
        {
            var ranked = scoredApps
                .OrderByDescending(s => s.Score)
                .ToList()
                .AsReadOnly();

            var summary = ranked.Any()
                ? $"Top candidate: {ranked[0].Applicant.Name} with score {ranked[0].Score}."
                : "No eligible candidates.";

            return new AdmissionReport(ranked, summary);
        }

      
        public static AdmissionReport Run(IReadOnlyList<RawApplication> rawInputs, Func<ValidatedApplication, decimal> policy)
        {
            var validApps = new List<ValidatedApplication>();

            foreach (var raw in rawInputs)
            {
                var outcome = Validate(raw);

              
                switch (outcome)
                {
                    case Valid v:
                        validApps.Add(v.Value);
                        break;
                    case Invalid inv:
                        Console.WriteLine($"Rejected: {string.Join(", ", inv.Reasons)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected validation outcome: {outcome.GetType().Name}");
                }
            }

          
            var scored = validApps
                .Select(app => Score(app, policy))
                .ToList()
                .AsReadOnly();

            return GenerateReport(scored);
        }
    }
}