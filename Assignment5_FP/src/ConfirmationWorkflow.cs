using System;
using System.Collections.Generic;

namespace ApplicationScoring
{
   
    public abstract record WorkflowState;
    public sealed record Pending(ValidatedApplication Applicant) : WorkflowState;
    public sealed record Confirmed(ValidatedApplication Applicant, decimal Score) : WorkflowState;
    public sealed record Rejected(string Reason) : WorkflowState;

    public static class ConfirmationWorkflow
    {
        
        public static WorkflowState Confirm(WorkflowState currentState, Func<ValidatedApplication, decimal> policy)
        {
            return currentState switch
            {
                Pending p => new Confirmed(p.Applicant, policy(p.Applicant)),
                Confirmed c => c,  
                Rejected r => r,   
                _ => throw new InvalidOperationException($"Unknown state: {currentState.GetType().Name}")
            };
        }
        public static WorkflowState Reject(WorkflowState currentState, string reason)
        {
            return currentState switch
            {
                Pending p => new Rejected(reason),
                Confirmed c => c, 
                Rejected r => r,   
                _ => throw new InvalidOperationException($"Unknown state: {currentState.GetType().Name}")
            };
        }
    }
}