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

            var outcome = ApplicationFactory.Create("Alice", "20", "3.0", "110", "false");
            if (outcome is not Valid v)
            {
                Console.WriteLine("Factory failed unexpectedly.");
                return;
            }
            WorkflowState state = new Pending(v.Value);

            Console.WriteLine($"Initial state: {state.GetType().Name}");

           
            state = ConfirmationWorkflow.Confirm(state, ScoringPolicies.AcademicPolicy);
            Console.WriteLine($"After confirmation: {state.GetType().Name}");

            state = ConfirmationWorkflow.Confirm(state, ScoringPolicies.AcademicPolicy);
            Console.WriteLine($"After repeated confirmation: {state.GetType().Name} (safe rule: returns existing outcome)");

           
            state = ConfirmationWorkflow.Reject(state, "Manual rejection");
            Console.WriteLine($"After rejection attempt: {state.GetType().Name} (safe rule: cannot reject confirmed)");
        

            Console.WriteLine("--- File Boundary ---");
            DemonstrateFileBoundary("sample_input.csv");
            DemonstrateFileBoundary("missing_file.csv");
            DemonstrateFileBoundary("malformed_file.csv");

            var rawAlice = new RawApplication("Alice", "20", "3.0", "110", "false");
            var rawBob = new RawApplication("Bob", "22", "1.5", "50", "false"); 
            var rawCharlie = new RawApplication("Charlie", "21", "4.0", "0", "false");

            var validApps = new List<ValidatedApplication>();

            
            void AddIfValid(RawApplication raw)
            {
                if (ApplicationFactory.Create(raw.Name, raw.Age, raw.Gpa, raw.Hours, raw.HasCriminalRecord) is Valid v)
                {
                    validApps.Add(v.Value);
                }
            }

            AddIfValid(rawAlice);
            AddIfValid(rawBob);
            AddIfValid(rawCharlie);
            
            var apps = validApps.AsReadOnly();
          
             Console.WriteLine("\n--- Domain Chain (Lookup, Bind, Map) ---");
            DemonstrateChain(apps, "Alice", ScoringPolicies.AcademicPolicy); 
            DemonstrateChain(apps, "Bob", ScoringPolicies.AcademicPolicy);   
            DemonstrateChain(apps, "Unknown", ScoringPolicies.AcademicPolicy); 

          
            Console.WriteLine("\n--- State Transition ---");
            WorkflowState rejectedState = new Rejected("Manual rejection");
            var confirmResult = DomainOperations.ConfirmState(rejectedState, ScoringPolicies.AcademicPolicy);
            PrintResult(confirmResult);
        

           

          
            Console.WriteLine("--- Loading Data ---");
            var rawStream = DataLoader.LoadAndStreamRawApplications("sample_input.csv");
            
            
            Console.WriteLine($"Transformation count BEFORE enumeration: {DataLoader.TransformationCount}");
            
            var ValidApps = rawStream
                .Select(r => ApplicationFactory.Create(r.Name, r.Age, r.Gpa, r.Hours, r.HasCriminalRecord))
                .Where(outcome => outcome is Valid)
                .Select(outcome => ((Valid)outcome).Value)
                .ToList(); 

            Console.WriteLine($"Transformation count AFTER enumeration: {DataLoader.TransformationCount}");
            Console.WriteLine($"Loaded {validApps.Count} valid applicants.\n");

            
            Console.WriteLine("--- Curried Scoring ---");
            var scorer = ScoringFactory.CreateScorer();
            
          
            var academicPolicy = scorer(200m)(1m);
            var holisticPolicy = scorer(100m)(5m);

            var alice = validApps.First(a => a.Name == "Alice");
            Console.WriteLine($"Alice Academic Score: {academicPolicy(alice)}");
            Console.WriteLine($"Alice Holistic Score: {holisticPolicy(alice)}\n");

         
            Console.WriteLine("--- LINQ Reports ---");
            
         
            var scoredApps = validApps.ScoreAll(academicPolicy).ToList();
            Console.WriteLine($"Average GPA of valid applicants: {validApps.CalculateAverageGpa():F2}");
            
            Console.WriteLine("\nAge Group Report:");
            Console.WriteLine(ReportGenerator.GenerateAgeGroupReport(validApps));

            Console.WriteLine("\nHigh Achievers Report:");
            Console.WriteLine(ReportGenerator.GenerateHighAchieversReport(scoredApps));

            
            Console.WriteLine("\n--- Native Integration ---");
            var nativeResult = NativeScoringAdapter.GetExperienceMultiplier(alice.Hours);
            switch (nativeResult)
        {
    case Success<int, DomainError> success:
        Console.WriteLine($"Alice's native experience multiplier: {success.Value}");
        break;
    case Failure<int, DomainError> failure:
        Console.WriteLine($"Native failure: {failure.Error.Message}");
        break;
      }

        
            var failResult = NativeScoringAdapter.GetExperienceMultiplier(-5);
           switch (failResult)
      {
    case Success<int, DomainError> success:
        Console.WriteLine($"Unexpected success: {success.Value}");
        break;
    case Failure<int, DomainError> failure:
        Console.WriteLine($"Expected native failure: {failure.Error.Message}");
        break;
     }
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
}