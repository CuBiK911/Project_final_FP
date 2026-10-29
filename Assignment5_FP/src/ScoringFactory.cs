using System;

namespace ApplicationScoring
{
    public static class ScoringFactory
    {
       
        public static Func<decimal, Func<decimal, Func<ValidatedApplication, decimal>>> CreateScorer()
        {
            return gpaWeight => hoursWeight => app => (app.Gpa * gpaWeight) + (app.Hours * hoursWeight);
        }
    }
}