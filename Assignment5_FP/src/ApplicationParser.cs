using System;
using System.Collections.Generic;

namespace ApplicationScoring
{
    public static class ApplicationParser
    {
        public static (List<ValidatedApplication> Valid, List<string> Errors) ParseAndValidate(IReadOnlyList<RawApplication> rawInputs)
        {
            var valid = new List<ValidatedApplication>();
            var errors = new List<string>();

            foreach (var raw in rawInputs)
            {
                
                if (string.IsNullOrWhiteSpace(raw.Name))
                {
                    errors.Add($"Missing name for record.");
                    continue;
                }

                if (!int.TryParse(raw.Age, out int age))
                {
                    errors.Add($"Invalid age '{raw.Age}' for {raw.Name}.");
                    continue;
                }

                if (!decimal.TryParse(raw.Gpa, out decimal gpa))
                {
                    errors.Add($"Invalid GPA '{raw.Gpa}' for {raw.Name}.");
                    continue;
                }

                if (!int.TryParse(raw.Hours, out int hours))
                {
                    errors.Add($"Invalid hours '{raw.Hours}' for {raw.Name}.");
                    continue;
                }

                if (!bool.TryParse(raw.HasCriminalRecord, out bool hasRecord))
                {
                    errors.Add($"Invalid criminal record flag '{raw.HasCriminalRecord}' for {raw.Name}.");
                    continue;
                }

               
                if (gpa < 0 || gpa > 4.0m)
                {
                    errors.Add($"GPA {gpa} out of bounds [0, 4.0] for {raw.Name}.");
                    continue;
                }

              
                valid.Add(new ValidatedApplication(raw.Name, age, gpa, hours));
            }

            return (valid, errors);
        }
    }
}