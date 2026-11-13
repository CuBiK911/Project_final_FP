namespace ApplicationScoring;

public static class ApplicationParser
{
    public static (IReadOnlyList<ValidatedApplication> Valid, IReadOnlyList<string> Errors) ParseAndValidate(
        IReadOnlyList<RawApplication> rawInputs)
    {
        var validation = PipelineStages.ValidateApplications(rawInputs);
        return (validation.Applicants, Array.AsReadOnly(validation.Rejections
            .Select(rejection => $"Record {rejection.RecordNumber}: {string.Join(" ", rejection.Reasons)}")
            .ToArray()));
    }
}
