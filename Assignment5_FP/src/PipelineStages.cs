using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationScoring
{
    public static class PipelineStages
    {
        
        public static IReadOnlyList<ValidatedApplication> FilterEligible(IReadOnlyList<ValidatedApplication> applicants)
        {
          
            return applicants
                .Where(a => a.Age >= 18 && a.Age <= 25 )
                .ToList()
                .AsReadOnly();
        }

    
        public static IReadOnlyList<ScoredApplication> ComputeScores(
            IReadOnlyList<ValidatedApplication> applicants, 
            Func<ValidatedApplication, decimal> scoringPolicy)
        {
            return applicants
                .Select(a => new ScoredApplication(a, scoringPolicy(a)))
                .ToList()
                .AsReadOnly();
        }

       
        public static AdmissionReport GenerateReport(IReadOnlyList<ScoredApplication> scoredApplicants)
        {
            var ranked = scoredApplicants
                .OrderByDescending(s => s.Score)
                .ToList()
                .AsReadOnly();

            var summary = ranked.Any() 
                ? $"Top candidate: {ranked[0].Applicant.Name} with score {ranked[0].Score}." 
                : "No eligible candidates.";

            return new AdmissionReport(ranked, summary);
        }
    }
}