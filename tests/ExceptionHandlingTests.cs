using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ApplicationScoring.Tests
{
    public class ExceptionHandlingTests
    {
        private readonly IReadOnlyList<ValidatedApplication> _apps = new List<ValidatedApplication>
        {
            new ValidatedApplication("Alice", 20, 3.0m, 110),
            new ValidatedApplication("Bob", 22, 1.5m, 50)
        }.AsReadOnly();

        [Fact] 
        public void CompleteSuccess_ChainProducesCorrectValue()
        {
            var result = DomainOperations.ProcessApplicantChain(_apps, "Alice", ScoringPolicies.AcademicPolicy);
            Assert.IsType<Success<ScoredApplication, DomainError>>(result);
            var score = ((Success<ScoredApplication, DomainError>)result).Value.Score;
            Assert.Equal(710m, score);
        }

        [Fact] 
        public void DomainFailure_InvalidGpa_ReturnsError()
        {
            var result = DomainOperations.ProcessApplicantChain(_apps, "Bob", ScoringPolicies.AcademicPolicy);
            Assert.IsType<Failure<ScoredApplication, DomainError>>(result);
            var error = ((Failure<ScoredApplication, DomainError>)result).Error;
            Assert.Equal(ApplicationError.InvalidGpa, error.Code);
        }

        [Fact] 
        public void DomainFailure_MissingApplicant_ReturnsError()
        {
            var result = DomainOperations.ProcessApplicantChain(_apps, "Unknown", ScoringPolicies.AcademicPolicy);
            Assert.IsType<Failure<ScoredApplication, DomainError>>(result);
            var error = ((Failure<ScoredApplication, DomainError>)result).Error;
            Assert.Equal(ApplicationError.MissingApplicant, error.Code);
        }

        [Fact] 
        public void DomainFailure_StateTransitionRejected_ReturnsError()
        {
            WorkflowState rejected = new Rejected("Test");
            var result = DomainOperations.ConfirmState(rejected, ScoringPolicies.AcademicPolicy);
            Assert.IsType<Failure<WorkflowState, DomainError>>(result);
        }

        [Fact] 
        public void LookupPresent_ReturnsSome()
        {
            var result = ApplicantRepository.FindByName(_apps, "Alice");
            Assert.IsType<Some<ValidatedApplication>>(result);
        }

        [Fact] 
        public void LookupAbsent_ReturnsNone()
        {
            var result = ApplicantRepository.FindByName(_apps, "Unknown");
            Assert.IsType<None<ValidatedApplication>>(result);
        }

        [Fact] 
        public void MapSuccess_TransformationApplied()
        {
            Result<int, string> success = new Success<int, string>(5);
            var result = success.Map(x => x * 2);
            Assert.IsType<Success<int, string>>(result);
            Assert.Equal(10, ((Success<int, string>)result).Value);
        }

        [Fact] 
        public void BindSuccess_ContinuationApplied()
        {
            Result<int, string> success = new Success<int, string>(5);
            var result = success.Bind(x => new Success<int, string>(x + 1));
            Assert.IsType<Success<int, string>>(result);
            Assert.Equal(6, ((Success<int, string>)result).Value);
        }

        [Fact] 
        public void FailurePropagation_MapSkipsFunction()
        {
            bool mapCalled = false;
            Result<int, string> failure = new Failure<int, string>("error");
            failure.Map(x => { mapCalled = true; return x; });
            Assert.False(mapCalled);
        }

        [Fact] 
        public void FailurePropagation_BindSkipsFunction()
        {
            bool bindCalled = false;
            Result<int, string> failure = new Failure<int, string>("error");
            failure.Bind(x => { bindCalled = true; return new Success<int, string>(x); });
            Assert.False(bindCalled);
        }

        [Fact] 
        public void MissingFile_ReturnsFileNotFound()
        {
            var result = FileBoundary.LoadApplications("non_existent_file.csv");
            Assert.IsType<Failure<IReadOnlyList<RawApplication>, DomainError>>(result);
            var error = ((Failure<IReadOnlyList<RawApplication>, DomainError>)result).Error;
            Assert.Equal(ApplicationError.FileNotFound, error.Code);
        }

        [Fact] 
        public void MalformedFile_ReturnsMalformedFile()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Header\nBadData");
                var result = FileBoundary.LoadApplications(tempFile);
                Assert.IsType<Failure<IReadOnlyList<RawApplication>, DomainError>>(result);
                var error = ((Failure<IReadOnlyList<RawApplication>, DomainError>)result).Error;
                Assert.Equal(ApplicationError.MalformedFile, error.Code);
            }
            finally { File.Delete(tempFile); }
        }
    }
}