using System;
using System.Collections.Generic;

namespace fake;

internal class TemplateResolverRegistry
{
    internal Dictionary<string, Func<string?, string>> Resolvers;
    internal List<string> OrderedNames;
    internal int MaximumCount;

    internal TemplateResolverRegistry(int maximumCount)
    {
        Guard.NonNegative(maximumCount, "maximumCount");
        Resolvers = new Dictionary<string, Func<string?, string>>(StringComparer.Ordinal);
        OrderedNames = new List<string>();
        MaximumCount = maximumCount;
    }

    internal void Register(string name, Func<string?, string> resolver)
    {
        name = TemplateEngine.ValidateResolverName(name, MaximumCount);
        Guard.NotNull(resolver, "resolver");
        if (TemplateEngine.IsBuiltInToken(name))
        {
            throw new ArgumentException(
                $"The token '{name}' belongs to the built-in provider catalog and cannot be replaced.",
                "name");
        }
        if (Resolvers.ContainsKey(name))
        {
            throw new ArgumentException(
                $"The token '{name}' already has a registered resolver.",
                "name");
        }
        Guard.Count(
            TemplateEngine.Tokens().Length + OrderedNames.Count + 1,
            MaximumCount,
            "resolvers");
        Resolvers.Add(name, resolver);
        OrderedNames.Add(name);
    }

    internal bool Contains(string name)
    {
        return Resolvers.ContainsKey(name);
    }

    internal string Resolve(string name, string? argument)
    {
        string result = Resolvers[name](argument);
        Guard.NotNull(result, "resolver");
        return result;
    }

    internal string[] Names()
    {
        return OrderedNames.ToArray();
    }
}
