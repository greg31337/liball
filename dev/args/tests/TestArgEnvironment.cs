using System.Collections.Generic;

namespace args.Tests;

internal class TestArgEnvironment : IArgEnvironment
{
    internal Dictionary<string, string?> Values = new Dictionary<string, string?>();

    internal TestArgEnvironment()
    {
    }

    internal TestArgEnvironment(string name, string? value)
    {
        Values[name] = value;
    }

    internal TestArgEnvironment Set(string name, string value)
    {
        Values[name] = value;
        return this;
    }

    public string? GetEnvironmentVariable(string name)
    {
        if (Values.ContainsKey(name))
        {
            return Values[name];
        }

        return null;
    }
}
