using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ApplicationScoring.Tests
{
    public class SafeWorkflowTests
    {
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
        public void Factory_InvalidAge_RejectsAndReportsReason()
        {
            var outcome = ApplicationFactory.Create("Bob", "17", "3.0", "10", "false");

            Assert.IsType<Invalid>(outcome);
            var invalid = (Invalid)outcome;
            Assert.Contains(invalid.Reasons, r => r.Contains("Age") && r.Contains("18"));
        }

        [Fact] 
        public void Factory_CriminalRecord_RejectsAndReportsReason()
        {
            var outcome = ApplicationFactory.Create("Eve", "21", "3.2", "50", "true");

            Assert.IsType<Invalid>(outcome);
            var invalid = (Invalid)outcome;
            Assert.Contains(invalid.Reasons, r => r.Contains("criminal record"));
        }

        [Fact] 
        public void DistinctCases_BothReachableAndPatternMatched()
        {
            var validOutcome = ApplicationFactory.Create("Alice", "20", "3.0", "110", "false");
            var invalidOutcome = ApplicationFactory.Create("Bob", "17", "3.0", "10", "false");

            var validResult = validOutcome switch
            {
                Valid v => "accepted",
                Invalid i => "rejected",
                _ => "unknown"
            };

            var invalidResult = invalidOutcome switch
            {
                Valid v => "accepted",
                Invalid i => "rejected",
                _ => "unknown"
            };

            Assert.Equal("accepted", validResult);
            Assert.Equal("rejected", invalidResult);
        }

        [Fact] 
        public void NormalOperation_ValidatedValueProducesCorrectResult()
        {
            var dataset = new List<RawApplication>
            {
                new("Alice", "20", "3.0", "110", "false"),
                new("Charlie", "21", "4.0", "0", "false")
            }.AsReadOnly();

            var report = SafeWorkFlow.Run(dataset, ScoringPolicies.AcademicPolicy);

            Assert.Equal(2, report.RankedApplicants.Count);
            Assert.Equal("Charlie", report.RankedApplicants[0].Applicant.Name);
            Assert.Equal(800m, report.RankedApplicants[0].Score);
        }

        [Fact] 
        public void MissingPrerequisite_InvalidSequenceRejected()
        {
            var raw = new RawApplication("Test", "20", "3.0", "110", "false");

           
            
            var outcome = SafeWorkFlow.Validate(raw);
            Assert.IsType<Valid>(outcome);

            var valid = ((Valid)outcome).Value;
            var scored = SafeWorkFlow.Score(valid, ScoringPolicies.AcademicPolicy);
            Assert.Equal(710m, scored.Score);
        }

        [Fact]
        public void RepeatedOperation_FollowsDocumentedSafeRule()
        {
            var validApp = new ValidatedApplication("Alice", 20, 3.0m, 110);
            WorkflowState state = new Pending(validApp);

           
            state = ConfirmationWorkflow.Confirm(state, ScoringPolicies.AcademicPolicy);
            Assert.IsType<Confirmed>(state);
            var first = (Confirmed)state;

           
            state = ConfirmationWorkflow.Confirm(state, ScoringPolicies.HolisticPolicy);
            Assert.IsType<Confirmed>(state);
            var second = (Confirmed)state;

            
            Assert.Equal(first.Score, second.Score);
        }

        [Fact] 
        public void BoundaryValue_ExactThresholdFollowsRule()
        {
           
            var outcome = ApplicationFactory.Create("Boundary", "18", "2.5", "10", "false");
            Assert.IsType<Valid>(outcome);

            
            var below = ApplicationFactory.Create("Below", "17", "2.5", "10", "false");
            Assert.IsType<Invalid>(below);

            
            var max = ApplicationFactory.Create("Max", "25", "2.5", "10", "false");
            Assert.IsType<Valid>(max);

            
            var above = ApplicationFactory.Create("Above", "26", "2.5", "10", "false");
            Assert.IsType<Invalid>(above);
        }

        [Fact] 
        public void StatePreservation_FailedTransitionLeavesOriginalUnchanged()
        {
            var validApp = new ValidatedApplication("Alice", 20, 3.0m, 110);
            WorkflowState original = new Pending(validApp);

            var rejected = ConfirmationWorkflow.Reject(original, "Test rejection");
            Assert.IsType<Rejected>(rejected);

            
            Assert.IsType<Pending>(original);
            var pending = (Pending)original;
            Assert.Equal("Alice", pending.Applicant.Name);
        }
    }
}