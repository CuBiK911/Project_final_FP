using Xunit;

namespace ApplicationScoring.Tests;

internal static class TestApplications
{
    public static ValidatedApplication Valid(string name, string age, string gpa, string hours)
        => Assert.IsType<Valid>(ApplicationFactory.Create(name, age, gpa, hours, "false")).Value;
}
