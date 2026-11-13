using Microsoft.VisualBasic.FileIO;

namespace ApplicationScoring;

internal static class CsvApplications
{
    private static readonly IReadOnlyList<string> Header =
        Array.AsReadOnly(new[] { "Name", "Age", "GPA", "Hours", "HasCriminalRecord" });

    public static Result<IReadOnlyList<RawApplication>, DomainError> Parse(string csv)
    {
        using var parser = new TextFieldParser(new StringReader(csv))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        try
        {
            if (parser.ReadFields() is not { } header ||
                !header.SequenceEqual(Header, StringComparer.OrdinalIgnoreCase))
            {
                return Malformed("Expected CSV header: " + string.Join(",", Header) + ".");
            }

            var rows = new List<RawApplication>();
            while (!parser.EndOfData)
            {
                var lineNumber = parser.LineNumber;
                var fields = parser.ReadFields();
                if (fields is not { Length: 5 })
                    return Malformed($"CSV line {lineNumber} must contain exactly five fields.");

                rows.Add(new RawApplication(fields[0], fields[1], fields[2], fields[3], fields[4]));
            }

            return new Success<IReadOnlyList<RawApplication>, DomainError>(rows.AsReadOnly());
        }
        catch (MalformedLineException ex)
        {
            return Malformed($"Malformed CSV quoting at line {ex.LineNumber}.");
        }
    }

    private static Failure<IReadOnlyList<RawApplication>, DomainError> Malformed(string message)
        => new(new DomainError(ApplicationError.MalformedFile, message));
}
