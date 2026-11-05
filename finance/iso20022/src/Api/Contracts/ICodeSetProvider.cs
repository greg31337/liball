namespace iso20022;

/// <summary>Looks up internal and external codes using explicit edition and active-date policy.</summary>
public interface ICodeSetProvider
{
    /// <summary>Looks up one exact code in the selected internal or external code-set edition.</summary>
    /// <param name="request">The code-set source, edition policy, value, and active date.</param><returns>The lookup result and selected provenance.</returns>
    CodeLookupResult LookupCode(CodeLookupRequest? request);
}
