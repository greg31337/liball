namespace uri;

/// <summary>
/// Provides bounded exact, assigned-name, and namespace-specific URN comparison.
/// </summary>
public class UrnComparer : IUrnComparer
{
    /// <summary>Initializes a stateless URN comparer.</summary>
    public UrnComparer()
    {
    }

    /// <summary>Compares complete URN component text and delimiter presence exactly.</summary>
    /// <param name="left">The first complete URN text.</param>
    /// <param name="right">The second complete URN text.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The exact comparison outcome.</returns>
    public UrnEquivalenceResult CompareUrnExact(
        string left,
        string right,
        UrnParseOptions options)
    {
        return CompareAtLevel(
            left,
            right,
            options,
            UrnEquivalenceLevel.Exact);
    }

    /// <summary>Compares assigned names using generic RFC 8141 equivalence.</summary>
    /// <param name="left">The first complete URN text.</param>
    /// <param name="right">The second complete URN text.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The assigned-name comparison outcome.</returns>
    public UrnEquivalenceResult CompareUrnAssignedName(
        string left,
        string right,
        UrnParseOptions options)
    {
        return CompareAtLevel(
            left,
            right,
            options,
            UrnEquivalenceLevel.AssignedName);
    }

    /// <summary>Compares assigned names with an explicitly supplied namespace handler.</summary>
    /// <param name="left">The first complete URN text.</param>
    /// <param name="right">The second complete URN text.</param>
    /// <param name="options">The validation, namespace-handler, and processing options.</param>
    /// <returns>The namespace-specific comparison outcome.</returns>
    public UrnEquivalenceResult CompareUrnNamespaceSpecific(
        string left,
        string right,
        UrnParseOptions options)
    {
        return CompareAtLevel(
            left,
            right,
            options,
            UrnEquivalenceLevel.NamespaceSpecific);
    }

    internal UrnEquivalenceResult CompareAtLevel(
        string? left,
        string? right,
        UrnParseOptions? options,
        UrnEquivalenceLevel level)
    {
        UrnParser parser = new UrnParser();
        UrnParseResult parsedLeft = parser.ParseNullableText(left, options);
        if (!parsedLeft.Success)
        {
            return UrnComparerCore.CreateFailure(
                level,
                parsedLeft.Diagnostic);
        }

        UrnParseResult parsedRight = parser.ParseNullableText(right, options);
        if (!parsedRight.Success)
        {
            return UrnComparerCore.CreateFailure(
                level,
                parsedRight.Diagnostic);
        }

        if (level == UrnEquivalenceLevel.Exact)
        {
            return UrnComparerCore.CompareExact(
                parsedLeft.Urn,
                parsedRight.Urn);
        }

        if (level == UrnEquivalenceLevel.AssignedName)
        {
            return UrnComparerCore.CompareAssignedName(
                parsedLeft.Urn,
                parsedRight.Urn);
        }

        IUrnNamespaceHandler? handler = null;
        if (options != null)
        {
            handler = options.NamespaceHandler;
        }

        return UrnComparerCore.CompareNamespaceSpecific(
            parsedLeft.Urn,
            parsedRight.Urn,
            handler);
    }
}
