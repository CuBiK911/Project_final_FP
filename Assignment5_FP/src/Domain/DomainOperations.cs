namespace ApplicationScoring;

public static class ApplicantRepository
{
    public static Maybe<ValidatedApplication> FindByName(IReadOnlyList<ValidatedApplication> applicants, string name)
        => applicants.FirstOrDefault(app => StringComparer.Ordinal.Equals(app.Name, name.Trim())) is { } found
            ? new Some<ValidatedApplication>(found)
            : new None<ValidatedApplication>();

    public static Result<ValidatedApplication, DomainError> RequireApplicant(Maybe<ValidatedApplication> applicant, string name)
        => applicant.Match<Result<ValidatedApplication, DomainError>>(
            app => new Success<ValidatedApplication, DomainError>(app),
            () => new Failure<ValidatedApplication, DomainError>(
                new DomainError(ApplicationError.MissingApplicant, $"Eligible applicant '{name}' not found.")));
}

public static class DomainOperations
{
    public static Result<ValidatedApplication, DomainError> CheckAcademicStanding(ValidatedApplication applicant)
        => applicant.Gpa >= 2.0m
            ? new Success<ValidatedApplication, DomainError>(applicant)
            : new Failure<ValidatedApplication, DomainError>(new DomainError(ApplicationError.InvalidGpa,
                FormattableString.Invariant($"GPA {applicant.Gpa:0.##} is below 2.0.")));

    public static Result<WorkflowState, DomainError> ConfirmState(WorkflowState state, Func<ValidatedApplication, decimal> policy)
        => state switch
        {
            Pending pending => ConfirmPending(pending, policy).Map<Confirmed, WorkflowState, DomainError>(confirmed => confirmed),
            Confirmed confirmed => new Success<WorkflowState, DomainError>(confirmed),
            Rejected => new Failure<WorkflowState, DomainError>(new DomainError(
                ApplicationError.StateTransitionNotAllowed, "Cannot confirm a rejected application.")),
            _ => new Failure<WorkflowState, DomainError>(new DomainError(
                ApplicationError.StateTransitionNotAllowed, "Unknown state."))
        };

    public static Result<ScoredApplication, DomainError> ProcessApplicantChain(
        IReadOnlyList<ValidatedApplication> applicants, string name, Func<ValidatedApplication, decimal> policy)
        => ApplicantRepository.RequireApplicant(ApplicantRepository.FindByName(applicants, name), name)
            .Map(applicant => new Pending(applicant))
            .Bind(pending => ConfirmPending(pending, policy))
            .Map(confirmed => new ScoredApplication(confirmed.Applicant, confirmed.Score));

    private static Result<Confirmed, DomainError> ConfirmPending(Pending pending, Func<ValidatedApplication, decimal> policy)
        => CheckAcademicStanding(pending.Applicant).Map(applicant => new Confirmed(applicant, policy(applicant)));
}
