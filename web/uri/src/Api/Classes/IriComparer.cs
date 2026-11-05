using System;

namespace uri;

/// <summary>
/// Provides bounded exact, NFC-normalized, and mapped-URI IRI comparison.
/// </summary>
public class IriComparer : IIriComparer
{
    /// <summary>Initializes a stateless IRI comparer.</summary>
    public IriComparer()
    {
    }

    /// <summary>Compares lossless Unicode component text and delimiter presence.</summary>
    /// <param name="left">The first IRI-reference text.</param>
    /// <param name="right">The second IRI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    public UriComparisonResult CompareIriExact(
        string left,
        string right,
        UriComparisonOptions options)
    {
        UriDiagnostic? precondition = GetComparisonPrecondition(
            left,
            right,
            options);
        if (precondition != null)
        {
            return IriComparerCore.CreateFailure(
                UriComparisonLevel.Exact,
                precondition);
        }

        return IriComparerCore.CompareExact(
            left.AsSpan(),
            right.AsSpan(),
            options.Limits);
    }

    /// <summary>Compares IRI components after Unicode Normalization Form C.</summary>
    /// <param name="left">The first IRI-reference text.</param>
    /// <param name="right">The second IRI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    public UriComparisonResult CompareIriUnicode(
        string left,
        string right,
        UriComparisonOptions options)
    {
        UriDiagnostic? precondition = GetComparisonPrecondition(
            left,
            right,
            options);
        if (precondition != null)
        {
            return IriComparerCore.CreateFailure(
                UriComparisonLevel.IriUnicode,
                precondition);
        }

        return IriComparerCore.CompareUnicode(
            left.AsSpan(),
            right.AsSpan(),
            options.Limits);
    }

    /// <summary>Compares after mapping to and syntax-normalizing URI references.</summary>
    /// <param name="left">The first IRI-reference text.</param>
    /// <param name="right">The second IRI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    public UriComparisonResult CompareIriAsUri(
        string left,
        string right,
        UriComparisonOptions options)
    {
        UriDiagnostic? precondition = GetComparisonPrecondition(
            left,
            right,
            options);
        if (precondition != null)
        {
            return IriComparerCore.CreateFailure(
                UriComparisonLevel.IriMappedUri,
                precondition);
        }

        return IriComparerCore.CompareAsUri(
            left.AsSpan(),
            right.AsSpan(),
            options.Limits);
    }

    internal UriDiagnostic? GetComparisonPrecondition(
        string? left,
        string? right,
        UriComparisonOptions? options)
    {
        if (left == null ||
            right == null ||
            options == null ||
            options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        return null;
    }
}
