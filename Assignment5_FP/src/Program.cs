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
           /*
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
        */

            Console.WriteLine("--- File Boundary ---");
            DemonstrateFileBoundary("sample_input.csv");
            DemonstrateFileBoundary("missing_file.csv");
            DemonstrateFileBoundary("malformed_file.csv");

            var validApps = new List<ValidatedApplication>
            {
                new ValidatedApplication("Alice", 20, 3.0m, 110),
                new ValidatedApplication("Bob", 22, 1.5m, 50), 
                new ValidatedApplication("Charlie", 21, 4.0m, 0)
            }.AsReadOnly();

          
            Console.WriteLine("\n--- Domain Chain (Lookup, Bind, Map) ---");
            DemonstrateChain(validApps, "Alice", ScoringPolicies.AcademicPolicy); 
            DemonstrateChain(validApps, "Bob", ScoringPolicies.AcademicPolicy);   
            DemonstrateChain(validApps, "Unknown", ScoringPolicies.AcademicPolicy); 

        
            Console.WriteLine("\n--- State Transition ---");
            WorkflowState rejectedState = new Rejected("Manual rejection");
            var confirmResult = DomainOperations.ConfirmState(rejectedState, ScoringPolicies.AcademicPolicy);
            PrintResult(confirmResult);
        

       

       

        
         static void DemonstrateFileBoundary(string filePath)
        {
            Console.WriteLine($"\nLoading: {filePath}");
            var result = FileBoundary.LoadApplications(filePath);
            PrintResult(result);
        }
         static void DemonstrateChain(IReadOnlyList<ValidatedApplication> apps, string name, Func<ValidatedApplication, decimal> policy)
        {
            Console.WriteLine($"\nProcessing: {name}");
            var result = DomainOperations.ProcessApplicantChain(apps, name, policy);
            PrintResult(result);
        }
         static void PrintResult<T, E>(Result<T, E> result)
        {
            
 switch (result)
    {
        case Success<T, E> success:
          
            if (success.Value is System.Collections.IEnumerable collection && success.Value is not string)
            {
                int count = 0;
                foreach (var _ in (System.Collections.IEnumerable)success.Value) count++;
                Console.WriteLine($"  Success: Loaded {count} records.");
            }
            else
            {
                Console.WriteLine($"  Success: {success.Value}");
            }
            break;
            
        case Failure<T, E> failure:
            Console.WriteLine($"  Failure: {failure.Error}");
            break;
    }

        }

      /*  static void PrintReport(AdmissionReport report)
        {
            Console.WriteLine(report.Summary);
            foreach (var app in report.RankedApplicants)
            {
                Console.WriteLine($"- {app.Applicant.Name}: Score {app.Score}");
            }
        }*/
        }
    }
}