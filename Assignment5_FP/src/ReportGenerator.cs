using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationScoring
{
    public static class ReportGenerator
    {
       
        public static string GenerateAgeGroupReport(IEnumerable<ValidatedApplication> apps)
        {
            var groups = apps
                .GroupBy(a => a.Age <= 20 ? "18-20" : "21-25")
                .Select(g => new 
                { 
                    Group = g.Key, 
                    Count = g.Count(), 
                    AvgGpa = g.Average(x => (double)x.Gpa) 
                })
                .OrderByDescending(x => x.AvgGpa)
                .ThenBy(x => x.Group) 
                .ToList(); 

            return groups.Any() 
                ? string.Join("\n", groups.Select(x => $"{x.Group}: {x.Count} applicants, Avg GPA: {x.AvgGpa:F2}")) 
                : "No data available.";
        }

       
        public static string GenerateHighAchieversReport(IEnumerable<ScoredApplication> scoredApps)
        {
            var top = scoredApps
                .Where(a => a.Applicant.Gpa >= 3.5m)
                .OrderByDescending(a => a.Score)
                .ThenBy(a => a.Applicant.Name)
                .Select(a => $"{a.Applicant.Name} (GPA: {a.Applicant.Gpa}, Score: {a.Score})")
                .Take(5)
                .ToList(); 

            return top.Any() ? string.Join("\n", top) : "No high achievers found.";
        }
    }
}