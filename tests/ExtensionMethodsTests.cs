using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ApplicationScoring.Tests
{
    public class Assignment8Tests
    {
        private readonly IReadOnlyList<ValidatedApplication> _apps = new List<ValidatedApplication>
        {
            new ValidatedApplication("Alice", 20, 3.5m, 110),
            new ValidatedApplication("Bob", 22, 3.8m, 40),
            new ValidatedApplication("Charlie", 19, 3.5m, 110) 
        }.AsReadOnly();

        [Fact] 
        public void Extensions_CalculateAverageGpa_ReturnsCorrectValue()
        {
            double avg = _apps.CalculateAverageGpa();
            Assert.Equal(3.6, Math.Round(avg, 1));
        }

        [Fact] 
        public void Reports_HighAchievers_UsesDeterministicThenBy()
        {
            var scored = _apps.ScoreAll(a => a.Gpa * 100).ToList();
            var report = ReportGenerator.GenerateHighAchieversReport(scored);
            
          
            int aliceIndex = report.IndexOf("Alice");
            int charlieIndex = report.IndexOf("Charlie");
            Assert.True(aliceIndex < charlieIndex, "Alice should appear before Charlie due to ThenBy(Name)");
        }

        [Fact] 
        public void ConfiguredFunctions_PartialApplication_ProducesDifferentResults()
        {
            var scorer = ScoringFactory.CreateScorer();
            var policyA = scorer(10m)(1m);
            var policyB = scorer(1m)(10m);

            var app = new ValidatedApplication("Test", 20, 5.0m, 5);
            
            
            var app2 = new ValidatedApplication("Test2", 20, 2.0m, 10);
            
            Assert.Equal(30m, policyA(app2)); 
            Assert.Equal(102m, policyB(app2)); 
        }

       [Fact] 
public void DeferredExecution_DoesNotProcessBeforeEnumeration()
{
    DataLoader.ResetCounter();

    
    string tempFile = Path.GetTempFileName();
    File.WriteAllText(tempFile, "Name,Age,GPA,Hours,HasCriminalRecord\nAlice,20,3.0,10,false\nBob,21,3.5,20,false");

    try
    {
        
        var enumerable = DataLoader.LoadAndStreamRawApplications(tempFile);

       
        Assert.Equal(0, DataLoader.TransformationCount); 

        
        var list = enumerable.ToList();

       
        Assert.True(DataLoader.TransformationCount > 0); 
    }
    finally
    {
       
        if (File.Exists(tempFile)) File.Delete(tempFile);
    }
}

        [Fact] 
        public void ResourceLifetime_FileIsDisposedAfterLoading()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Header\nA,20,3.0,10,false");

            var enumerable = DataLoader.LoadAndStreamRawApplications(tempFile);
            var list = enumerable.ToList(); 

            
            Exception ex = Record.Exception(() => File.Delete(tempFile));
            Assert.Null(ex); 
        }

        [Fact] 
        public void NativeIntegration_ValidInput_ReturnsExpectedMultiplier()
        {
            
            var result = NativeScoringAdapter.GetExperienceMultiplier(110);
            
            Assert.IsType<Success<int, DomainError>>(result);
            Assert.Equal(3, ((Success<int, DomainError>)result).Value);
        }

        [Fact] 
        public void NativeIntegration_InvalidInput_ReturnsFailureWithoutCallingNative()
        {
            var result = NativeScoringAdapter.GetExperienceMultiplier(-5);
            
            Assert.IsType<Failure<int, DomainError>>(result);
            var error = ((Failure<int, DomainError>)result).Error;
            Assert.Equal(ApplicationError.InvalidGpa, error.Code);
        }
    }
}