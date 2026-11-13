using System.Globalization;

namespace ApplicationScoring;

public static class ApplicationFactory
{
    public static ValidationOutcome Create(RawApplication raw) => ValidatedApplication.Validate(raw);

    public static ValidationOutcome Create(string? name, string? age, string? gpa, string? hours, string? hasCriminalRecord)
        => Create(new RawApplication(name, age, gpa, hours, hasCriminalRecord));
}

public sealed partial record ValidatedApplication
{
    internal static ValidationOutcome Validate(RawApplication raw)
    {
        var reasons = new List<string>();
        var culture = CultureInfo.InvariantCulture;
        var name = raw.Name?.Trim();

        if (string.IsNullOrEmpty(name))
            reasons.Add("Name is required.");

        if (!int.TryParse(raw.Age, NumberStyles.Integer, culture, out var age))
            reasons.Add($"Age '{raw.Age}' is not a valid integer.");
        else if (age is < 18 or > 25)
            reasons.Add($"Age {age} is outside the valid range [18, 25].");

        if (!decimal.TryParse(raw.Gpa, NumberStyles.Number & ~NumberStyles.AllowThousands, culture, out var gpa))
            reasons.Add($"GPA '{raw.Gpa}' is not a valid decimal.");
        else if (gpa is < 0 or > 4)
            reasons.Add(FormattableString.Invariant($"GPA {gpa} is outside the valid range [0, 4.0]."));

        if (!int.TryParse(raw.Hours, NumberStyles.Integer, culture, out var hours))
            reasons.Add($"Hours '{raw.Hours}' is not a valid integer.");
        else if (hours < 0)
            reasons.Add($"Hours {hours} cannot be negative.");

        if (!bool.TryParse(raw.HasCriminalRecord, out var hasRecord))
            reasons.Add($"Criminal record flag '{raw.HasCriminalRecord}' is not a valid boolean.");
        else if (hasRecord)
            reasons.Add("Applicant has a criminal record and is not eligible.");

        return reasons.Count == 0
            ? new Valid(new ValidatedApplication(name!, age, gpa, hours))
            : new Invalid(reasons.AsReadOnly());
    }
}
