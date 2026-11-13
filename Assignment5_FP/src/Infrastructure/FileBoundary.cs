using System.Text;

namespace ApplicationScoring;

public static class FileBoundary
{
    public static Result<IReadOnlyList<RawApplication>, DomainError> LoadApplications(string filePath)
    {
        try
        {
            var csv = File.ReadAllText(filePath, new UTF8Encoding(false, true));
            return CsvApplications.Parse(csv);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                new DomainError(ApplicationError.FileNotFound, $"Input file not found: {filePath}"));
        }
        catch (DecoderFallbackException)
        {
            return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                new DomainError(ApplicationError.MalformedFile, $"Invalid text encoding in '{filePath}'."));
        }
        catch (Exception ex) when (IsPathOrIoError(ex))
        {
            return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                new DomainError(ApplicationError.FileReadFailed, $"Could not read '{filePath}': {ex.Message}"));
        }
    }

    public static Result<string, DomainError> SaveReport(string filePath, string report)
    {
        string? temporaryPath = null;
        try
        {
            var destination = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(destination)
                ?? throw new ArgumentException("Expected a report filename.");
            temporaryPath = Path.Combine(directory, $".admissions-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporaryPath, report);
            File.Move(temporaryPath, destination, overwrite: true);
            return new Success<string, DomainError>(destination);
        }
        catch (Exception ex) when (IsPathOrIoError(ex))
        {
            return new Failure<string, DomainError>(
                new DomainError(ApplicationError.FileWriteFailed, $"Could not save '{filePath}': {ex.Message}"));
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    private static bool IsPathOrIoError(Exception ex)
        => ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;
}
