using System.Collections.Generic;

namespace ApplicationScoring
{
   
    public record RawApplication(string? Name, string? Age, string? Gpa, string? Hours, string? HasCriminalRecord);

   

    
    public record ScoredApplication(ValidatedApplication Applicant, decimal Score);

    
    public record AdmissionReport(IReadOnlyList<ScoredApplication> RankedApplicants, string Summary);
  
    public record ValidatedApplication
    {
        internal ValidatedApplication(string name, int age, decimal gpa, int hours)
        {
            Name = name;
            Age = age;
            Gpa = gpa;
            Hours = hours;
        }

        public string Name { get; }
        public int Age { get; }
        public decimal Gpa { get; }
        public int Hours { get; }
    }

    public abstract record ValidationOutcome;

  
    public sealed record Valid(ValidatedApplication Value) : ValidationOutcome;

    public sealed record Invalid(IReadOnlyList<string> Reasons) : ValidationOutcome;

}