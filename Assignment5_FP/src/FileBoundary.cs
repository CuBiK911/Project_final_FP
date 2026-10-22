using System;
using System.Collections.Generic;
using System.IO;

namespace ApplicationScoring
{
    public static class FileBoundary
    {
        
        public static Result<IReadOnlyList<RawApplication>, DomainError> LoadApplications(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                        new DomainError(ApplicationError.FileNotFound, $"File not found: {filePath}"));
                }

                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                {
                    return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                        new DomainError(ApplicationError.MalformedFile, "File is empty."));
                }

                var apps = new List<RawApplication>();
                for (int i = 1; i < lines.Length; i++) 
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length != 5)
                    {
                        return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                            new DomainError(ApplicationError.MalformedFile, $"Malformed CSV at line {i + 1}."));
                    }
                    apps.Add(new RawApplication(parts[0], parts[1], parts[2], parts[3], parts[4]));
                }
                return new Success<IReadOnlyList<RawApplication>, DomainError>(apps.AsReadOnly());
            }
            catch (Exception ex)
            {
                return new Failure<IReadOnlyList<RawApplication>, DomainError>(
                    new DomainError(ApplicationError.MalformedFile, $"Operational error: {ex.Message}"));
            }
        }
    }
}