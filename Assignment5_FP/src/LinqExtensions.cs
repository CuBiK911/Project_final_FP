using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationScoring
{
    public static class LinqExtensions
    {
       
        public static IEnumerable<ScoredApplication> ScoreAll(this IEnumerable<ValidatedApplication> apps, Func<ValidatedApplication, decimal> policy)
        {
            return apps.Select(app => new ScoredApplication(app, policy(app)));
        }

        
        public static double CalculateAverageGpa(this IEnumerable<ValidatedApplication> apps)
        {
            return apps.Any() ? apps.Average(a => (double)a.Gpa) : 0.0;
        }
    }
}