using System;

namespace uri;

/// <summary>
/// Provides bounded assigned-name separation and explicit q/f locator transfer.
/// </summary>
public class UrnLocatorUtility : IUrnLocatorUtility
{
    /// <summary>Initializes a stateless URN locator utility.</summary>
    public UrnLocatorUtility()
    {
    }

    /// <summary>Applies a URN q-component and f-component to a resolved locator.</summary>
    /// <param name="urn">The complete source URN text.</param>
    /// <param name="locator">The resolved URI-reference text.</param>
    /// <param name="options">The collision and processing options.</param>
    /// <returns>The locator text after applying selected URN components.</returns>
    /// <exception cref="UriResolutionException">Validation, collision policy, or a configured limit fails.</exception>
    public string ApplyUrnComponentsToLocator(
        string urn,
        string locator,
        UrnLocatorMergeOptions options)
    {
        UriDiagnostic? optionFailure =
            UrnLocatorCore.ValidateMergeOptions(options);
        if (optionFailure != null || options == null)
        {
            throw CreateResolutionException(optionFailure);
        }

        if (urn == null || locator == null)
        {
            throw CreateResolutionException(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        UrnParseOptions urnOptions = new UrnParseOptions();
        urnOptions.Limits = options.Limits;
        UrnParser parser = new UrnParser();
        UrnParseResult parsedUrn = parser.TryParseUrn(urn, urnOptions);
        if (!parsedUrn.Success)
        {
            throw CreateResolutionException(parsedUrn.Diagnostic);
        }

        UriValidationOptions locatorOptions = new UriValidationOptions();
        locatorOptions.Limits = options.Limits;
        UriValidationResult parsedLocator =
            UriValidatorCore.ValidateReference(
                locator.AsSpan(),
                locatorOptions);
        if (!parsedLocator.Success)
        {
            throw CreateResolutionException(parsedLocator.Diagnostic);
        }

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                parsedUrn.Urn,
                parsedLocator.Reference,
                options);
        if (!result.Success)
        {
            throw CreateResolutionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Returns the assigned-name portion without r-, q-, or f-components.</summary>
    /// <param name="urn">The complete source URN text.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The lossless assigned-name text.</returns>
    /// <exception cref="UrnSyntaxException">Validation or a configured limit fails.</exception>
    public string SeparateAssignedName(
        string urn,
        UrnParseOptions options)
    {
        UrnParser parser = new UrnParser();
        UrnParseResult parsed = parser.TryParseUrn(urn, options);
        if (!parsed.Success || options == null)
        {
            throw CreateSyntaxException(parsed.Diagnostic);
        }

        UriTextResult result = UrnLocatorCore.SeparateAssignedName(
            parsed.Urn,
            options.Limits);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Text;
    }

    internal UriResolutionException CreateResolutionException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UriResolutionException(failure);
    }

    internal UrnSyntaxException CreateSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UrnSyntaxException(failure);
    }
}
