namespace ApplicationScoring;

public abstract record WorkflowState
{
    private protected WorkflowState() { }
}

public sealed record Pending(ValidatedApplication Applicant) : WorkflowState;

public sealed record Confirmed : WorkflowState
{
    internal Confirmed(ValidatedApplication applicant, decimal score)
    {
        Applicant = applicant;
        Score = score;
    }

    public ValidatedApplication Applicant { get; }
    public decimal Score { get; }
}

public sealed record Rejected(string Reason) : WorkflowState;

public static class ConfirmationWorkflow
{
    public static WorkflowState Confirm(WorkflowState currentState, Func<ValidatedApplication, decimal> policy)
        => DomainOperations.ConfirmState(currentState, policy).Match(state => state, _ => currentState);

    public static WorkflowState Reject(WorkflowState currentState, string reason)
        => currentState is Pending ? new Rejected(reason) : currentState;
}
