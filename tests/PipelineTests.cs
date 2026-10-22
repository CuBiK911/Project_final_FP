using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ApplicationScoring.Tests
{
    public class PipelineTests
    {
        private readonly IReadOnlyList<RawApplication> _normalDataset = new List<RawApplication>
        {
            new("Alice", "20", "3.0", "110", "false"), 
            new("Charlie", "21", "4.0", "0", "false")  
        }.AsReadOnly();
        
        
    [Fact]
       public void Factory_ValidInput_CreatesUsableValidatedValue()
{
    
    var outcome = ApplicationFactory.Create("Alice", "20", "3.0", "110", "false");

    Assert.IsType<Valid>(outcome);
    var valid = (Valid)outcome;
    Assert.Equal("Alice", valid.Value.Name);
    Assert.Equal(20, valid.Value.Age);
    Assert.Equal(3.0m, valid.Value.Gpa);
    Assert.Equal(110, valid.Value.Hours);
}
        [Fact] 
        public void NormalWorkflow_ProducesDocumentedResult()
        {
            var report = Workflow.Run(_normalDataset, ScoringPolicies.AcademicPolicy);
            Assert.Equal(2, report.RankedApplicants.Count);
           
            Assert.Equal("Charlie", report.RankedApplicants[0].Applicant.Name);
            Assert.Equal(800m, report.RankedApplicants[0].Score);
        }

        [Fact] 
        public void EmptyDataset_ProducesEmptyResultWithoutCrashing()
        {
            var emptyDataset = new List<RawApplication>().AsReadOnly();
            var report = Workflow.Run(emptyDataset, ScoringPolicies.AcademicPolicy);
            Assert.Empty(report.RankedApplicants);
            Assert.Equal("No eligible candidates.", report.Summary);
        }

        [Fact] 
        public void BoundaryValue_FollowsInclusionRule()
        {
            
            var boundaryDataset = new List<RawApplication> { new("BoundaryUser", "18", "2.5", "10", "false") }.AsReadOnly();
            var report = Workflow.Run(boundaryDataset, ScoringPolicies.AcademicPolicy);
            Assert.Single(report.RankedApplicants);
        }

        [Fact] 
        public void InvalidAndMissingInput_AreRejected()
        {
            var invalidDataset = new List<RawApplication>
            {
                new(null, "20", "3.0", "10", "false"),          
                new("Bob", "seventeen", "3.0", "10", "false"),  
                new("Diana", "20", "5.0", "10", "false"),       
                new("Eve", "20", "3.0", "10", "maybe")          
            }.AsReadOnly();

            var (valid, errors) = ApplicationParser.ParseAndValidate(invalidDataset);
            
            Assert.Empty(valid);
            Assert.Equal(4, errors.Count);
        }

        [Fact]
        public void TwoPolicies_ProduceDifferentResults()
        {
            var academicReport = Workflow.Run(_normalDataset, ScoringPolicies.AcademicPolicy);
            var holisticReport = Workflow.Run(_normalDataset, ScoringPolicies.HolisticPolicy);

            
            Assert.Equal("Charlie", academicReport.RankedApplicants[0].Applicant.Name);
            
            
            Assert.Equal("Alice", holisticReport.RankedApplicants[0].Applicant.Name);
        }

        [Fact] 
        public void PurityAndStages_CallerOwnedInputRemainsUnchanged()
        {
            var inputList = new List<ValidatedApplication> { new("TestUser", 20, 3.5m, 20) };
            IReadOnlyList<ValidatedApplication> callerOwned = inputList.AsReadOnly();
            
            var originalCount = callerOwned.Count;
            var originalFirst = callerOwned[0];

            var filtered = PipelineStages.FilterEligible(callerOwned);
            var scored = PipelineStages.ComputeScores(filtered, ScoringPolicies.AcademicPolicy);

          
            Assert.Equal(originalCount, callerOwned.Count);
            Assert.Same(originalFirst, callerOwned[0]);
            
           
            Assert.NotSame(callerOwned, filtered);
        }
    }
}