using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationScoring
{
    public static class ApplicantRepository
    {
        
        public static Maybe<ValidatedApplication> FindByName(IReadOnlyList<ValidatedApplication> applicants, string name)
        {
            var found = applicants.FirstOrDefault(a => a.Name == name);
            
            return found is null ? new None<ValidatedApplication>() : new Some<ValidatedApplication>(found);
        }

        
        public static Result<ValidatedApplication, DomainError> RequireApplicant(Maybe<ValidatedApplication> maybe, string name)
        {
            return maybe.Match(
                onSome: v => (Result<ValidatedApplication, DomainError>)new Success<ValidatedApplication, DomainError>(v),
                onNone: () => new Failure<ValidatedApplication, DomainError>(new DomainError(ApplicationError.MissingApplicant, $"Applicant '{name}' not found."))
            );
        }
    }

    public static class DomainOperations
    {
       
        public static Result<ValidatedApplication, DomainError> CheckAcademicStanding(ValidatedApplication app)
        {
            return app.Gpa >= 2.0m 
                ? new Success<ValidatedApplication, DomainError>(app) 
                : new Failure<ValidatedApplication, DomainError>(new DomainError(ApplicationError.InvalidGpa, $"GPA {app.Gpa} is below 2.0."));
        }

      
        public static Result<WorkflowState, DomainError> ConfirmState(WorkflowState state, Func<ValidatedApplication, decimal> policy)
        {
            return state switch
            {
                Pending p => new Success<WorkflowState, DomainError>(new Confirmed(p.Applicant, policy(p.Applicant))),
                Confirmed c => new Success<WorkflowState, DomainError>(c), // Idempotent
                Rejected r => new Failure<WorkflowState, DomainError>(new DomainError(ApplicationError.StateTransitionNotAllowed, "Cannot confirm a rejected application.")),
                _ => new Failure<WorkflowState, DomainError>(new DomainError(ApplicationError.StateTransitionNotAllowed, "Unknown state."))
            };
        }

        
        public static Result<ScoredApplication, DomainError> ProcessApplicantChain(
            IReadOnlyList<ValidatedApplication> applicants, 
            string name, 
            Func<ValidatedApplication, decimal> policy)
        {
            Result<ValidatedApplication, DomainError> lookup = ApplicantRepository.RequireApplicant(
                ApplicantRepository.FindByName(applicants, name), name);

         
            Result<ValidatedApplication, DomainError> validated = lookup.Bind(CheckAcademicStanding);

            Result<ScoredApplication, DomainError> scored = validated.Map(app => 
                new ScoredApplication(app, policy(app))
            );

            return scored;
        }
    }
}