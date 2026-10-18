using System;
using System.Collections.Generic;

namespace ApplicationScoring
{
    public static class ApplicationFactory
    {
        
        public static ValidationOutcome Create(string? name, string? age, string? gpa, string? hours, string? hasCriminalRecord)
        {
            var reasons = new List<string>();

            
            if (string.IsNullOrWhiteSpace(name))
            {
                reasons.Add("Name is required.");
            }

            
            if (!int.TryParse(age, out int parsedAge))
            {
                reasons.Add($"Age '{age}' is not a valid integer.");
            }
            else if (parsedAge < 18 || parsedAge > 25)
            {
                reasons.Add($"Age {parsedAge} is outside the valid range [18, 25].");
            }

            
            if (!decimal.TryParse(gpa, out decimal parsedGpa))
            {
                reasons.Add($"GPA '{gpa}' is not a valid decimal.");
            }
            else if (parsedGpa < 0 || parsedGpa > 4.0m)
            {
                reasons.Add($"GPA {parsedGpa} is outside the valid range [0, 4.0].");
            }

          
            if (!int.TryParse(hours, out int parsedHours))
            {
                reasons.Add($"Hours '{hours}' is not a valid integer.");
            }
            else if (parsedHours < 0)
            {
                reasons.Add($"Hours {parsedHours} cannot be negative.");
            }

           
            if (!bool.TryParse(hasCriminalRecord, out bool hasRecord))
            {
                reasons.Add($"Criminal record flag '{hasCriminalRecord}' is not a valid boolean.");
            }
            else if (hasRecord)
            {
                reasons.Add("Applicant has a criminal record and is not eligible.");
            }

          
            if (reasons.Count > 0)
            {
                return new Invalid(reasons.AsReadOnly());
            }

            return new Valid(new ValidatedApplication(name!, parsedAge, parsedGpa, parsedHours));
        }
    }
}