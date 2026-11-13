using System.Reflection;
using Xunit;

namespace ApplicationScoring.Tests;

internal static class TestRunner
{
    public static int Main()
    {
        var methods = typeof(TestRunner).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(FactAttribute), inherit: false))
            .OrderBy(method => method.DeclaringType!.Name, StringComparer.Ordinal)
            .ThenBy(method => method.Name, StringComparer.Ordinal);

        var passed = 0;
        var failed = 0;

        foreach (var method in methods)
        {
            var name = $"{method.DeclaringType!.Name}.{method.Name}";
            var data = method.GetCustomAttributes<InlineDataAttribute>()
                .SelectMany(attribute => attribute.GetData(method))
                .ToArray();
            if (method.IsDefined(typeof(TheoryAttribute), inherit: false) && data.Length == 0)
            {
                Console.WriteLine($"FAIL {name}: no test cases.");
                failed++;
                continue;
            }

            var cases = data.Length == 0 ? [Array.Empty<object>()] : data;
            for (var i = 0; i < cases.Length; i++)
            {
                var label = cases.Length == 1 ? name : $"{name} [{i + 1}]";
                try
                {
                    method.Invoke(Activator.CreateInstance(method.DeclaringType), cases[i]);
                    Console.WriteLine($"PASS {label}");
                    passed++;
                }
                catch (Exception ex)
                {
                    var failure = ex is TargetInvocationException { InnerException: { } cause } ? cause : ex;
                    Console.WriteLine($"FAIL {label}: {failure.Message}");
                    failed++;
                }
            }
        }

        Console.WriteLine($"{passed} passed, {failed} failed.");
        return failed == 0 && passed > 0 ? 0 : 1;
    }
}
