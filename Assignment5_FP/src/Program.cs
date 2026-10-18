using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApplicationScoring
{
    public static class Workflow
    {
       
        public static AdmissionReport Run(IReadOnlyList<RawApplication> rawInputs, Func<ValidatedApplication, decimal> policy)
        {
            var (valid, _) = ApplicationParser.ParseAndValidate(rawInputs);
            var eligible = PipelineStages.FilterEligible(valid);
            var scored = PipelineStages.ComputeScores(eligible, policy);
            return PipelineStages.GenerateReport(scored);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
           
            var lines = File.ReadAllLines("sample_input.csv").Skip(1);
            var dataset = lines.Select(line => {
                var parts = line.Split(',');
                return new RawApplication(parts[0], parts[1], parts[2], parts[3], parts[4]);
            }).ToList().AsReadOnly();

            Console.WriteLine("--- Running with Academic Policy ---");
            var academicReport = Workflow.Run(dataset, ScoringPolicies.AcademicPolicy);
            PrintReport(academicReport);

            Console.WriteLine("\n--- Running with Holistic Policy ---");
            var holisticReport = Workflow.Run(dataset, ScoringPolicies.HolisticPolicy);
            PrintReport(holisticReport);

            
            Console.WriteLine("\n--- Validating Invalid Input ---");
            var invalidDataset = new List<RawApplication>
            {
                new(null, "20", "3.0", "10", "false"),
                new("Frank", "twenty", "3.0", "10", "false")
            }.AsReadOnly();
            
            var (_, errors) = ApplicationParser.ParseAndValidate(invalidDataset);
            foreach(var err in errors) Console.WriteLine($"ERROR: {err}");

            var validApp = new ValidatedApplication("Alice", 20, 3.0m, 110);
            WorkflowState state = new Pending(validApp);

            Console.WriteLine($"Initial state: {state.GetType().Name}");

           
            state = ConfirmationWorkflow.Confirm(state, ScoringPolicies.AcademicPolicy);
            Console.WriteLine($"After confirmation: {state.GetType().Name}");

            state = ConfirmationWorkflow.Confirm(state, ScoringPolicies.AcademicPolicy);
            Console.WriteLine($"After repeated confirmation: {state.GetType().Name} (safe rule: returns existing outcome)");

           
            state = ConfirmationWorkflow.Reject(state, "Manual rejection");
            Console.WriteLine($"After rejection attempt: {state.GetType().Name} (safe rule: cannot reject confirmed)");
        }

        static void PrintReport(AdmissionReport report)
        {
            Console.WriteLine(report.Summary);
            foreach (var app in report.RankedApplicants)
            {
                Console.WriteLine($"- {app.Applicant.Name}: Score {app.Score}");
            }
        }
    }
}