using System;

namespace uri;

/// <summary>
/// Provides bounded exact, syntax-normalized, and scheme-profile URI comparison.
/// </summary>
public class UriComparer : IUriComparer
{
    /// <summary>Initializes a stateless URI comparer.</summary>
    public UriComparer()
    {
    }

    /// <summary>Compares serialized component text and delimiter presence exactly.</summary>
    /// <param name="left">The first URI-reference text.</param>
    /// <param name="right">The second URI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    public UriComparisonResult CompareExact(
        string left,
        string right,
        UriComparisonOptions options)
    {
        return CompareAtLevel(left, right, options, UriComparisonLevel.Exact);
    }

    /// <summary>Compares after generic syntax normalization.</summary>
    /// <param name="left">The first URI-reference text.</param>
    /// <param name="right">The second URI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    public UriComparisonResult CompareSyntax(
        string left,
        string right,
        UriComparisonOptions options)
    {
        return CompareAtLevel(left, right, options, UriComparisonLevel.Syntax);
    }

    /// <summary>Compares after generic and explicit or built-in profile normalization.</summary>
    /// <param name="left">The first URI-reference text.</param>
    /// <param name="right">The second URI-reference text.</param>
    /// <param name="options">The comparison options and optional profile.</param>
    /// <returns>The comparison outcome.</returns>
    public UriComparisonResult CompareWithProfile(
        string left,
        string right,
        UriComparisonOptions options)
    {
        return CompareAtLevel(
            left,
            right,
            options,
            UriComparisonLevel.SchemeProfile);
    }

    internal UriComparisonResult CompareAtLevel(
        string? left,
        string? right,
        UriComparisonOptions? options,
        UriComparisonLevel level)
    {
        UriDiagnostic? optionsFailure = GetComparisonOptionsDiagnostic(
            options,
            level);
        if (optionsFailure != null)
        {
            return CreateComparisonFailure(level, optionsFailure);
        }

        if (left == null || right == null || options == null)
        {
            return CreateComparisonFailure(
                level,
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriValidationResult leftValidation = ValidateComparisonText(left, options);
        if (!leftValidation.Success)
        {
            return CreateComparisonFailure(level, leftValidation.Diagnostic);
        }

        UriValidationResult rightValidation = ValidateComparisonText(right, options);
        if (!rightValidation.Success)
        {
            return CreateComparisonFailure(level, rightValidation.Diagnostic);
        }

        UriReference comparedLeft = NormalizeComparisonReference(
            leftValidation.Reference,
            options,
            level);
        UriReference comparedRight = NormalizeComparisonReference(
            rightValidation.Reference,
            options,
            level);

        if (level != UriComparisonLevel.Exact)
        {
            UriCompositionResult leftOutput = UriComposerCore.RecomposeParsedReference(
                comparedLeft,
                options.Limits);
            if (!leftOutput.Success)
            {
                return CreateComparisonFailure(level, leftOutput.Diagnostic);
            }

            UriCompositionResult rightOutput = UriComposerCore.RecomposeParsedReference(
                comparedRight,
                options.Limits);
            if (!rightOutput.Success)
            {
                return CreateComparisonFailure(level, rightOutput.Diagnostic);
            }
        }

        UriComparisonCoreResult comparison = UriComparerCore.CompareExact(
            comparedLeft,
            comparedRight);
        return new UriComparisonResult(
            true,
            comparison.Equal,
            level,
            comparison.DifferentComponent,
            null);
    }

    internal UriDiagnostic? GetComparisonOptionsDiagnostic(
        UriComparisonOptions? options,
        UriComparisonLevel level)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        if (level == UriComparisonLevel.SchemeProfile &&
            options.Profile != null &&
            !UriNormalizerCore.IsValidSchemeProfile(options.Profile))
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        return null;
    }

    internal UriValidationResult ValidateComparisonText(
        string text,
        UriComparisonOptions options)
    {
        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = options.Limits;
        return UriValidatorCore.ValidateReference(text.AsSpan(), validationOptions);
    }

    internal UriReference NormalizeComparisonReference(
        UriReference reference,
        UriComparisonOptions options,
        UriComparisonLevel level)
    {
        if (level == UriComparisonLevel.Exact)
        {
            return reference;
        }

        if (level == UriComparisonLevel.Syntax)
        {
            return UriNormalizerCore.NormalizeSyntaxReference(reference);
        }

        if (options.Profile == null)
        {
            return UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(reference);
        }

        return UriNormalizerCore.NormalizeWithSchemeProfile(
            reference,
            options.Profile);
    }

    internal UriComparisonResult CreateComparisonFailure(
        UriComparisonLevel level,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriComparisonResult(
            false,
            false,
            level,
            UriComponent.None,
            failure);
    }
}
