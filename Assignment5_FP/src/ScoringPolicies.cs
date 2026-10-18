using System;

namespace ApplicationScoring
{
    public static class ScoringPolicies
    {
        
        public static decimal AcademicPolicy(ValidatedApplication a) 
            => (a.Gpa * 200) + (a.Hours * 1);

        
        public static decimal HolisticPolicy(ValidatedApplication a) 
            => (a.Gpa * 100) + (a.Hours * 5);
    }
}