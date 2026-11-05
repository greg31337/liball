namespace unicode;

/// <summary>
/// Combines identifier validity with Unicode security flags, scripts, and diagnostics.
/// </summary>
public class UnicodeSecurityCheckResult
{
    /// <summary>
    /// Whether both identifier validation and the configured security checks passed.
    /// </summary>
    public bool IsSafe;
    /// <summary>
    /// The combined security conditions found in the identifier.
    /// </summary>
    public UnicodeSecurityFlag Flags;
    /// <summary>
    /// The UTS #39 restriction level assigned to the identifier.
    /// </summary>
    public UnicodeRestrictionLevel RestrictionLevel;
    /// <summary>
    /// The normalized UTS #39 confusable skeleton.
    /// </summary>
    public string Skeleton;
    /// <summary>
    /// The significant scripts used by the text, excluding Common and Inherited.
    /// </summary>
    public UnicodeScript[] Scripts;
    /// <summary>
    /// The underlying identifier-profile validation result.
    /// </summary>
    public UnicodeIdentifierValidationResult IdentifierValidation;
    /// <summary>
    /// The Unicode security diagnostics in input order.
    /// </summary>
    public UnicodeSecurityDiagnostic[] SecurityDiagnostics;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="isSafe">Whether the identifier passed the security checks.</param>
    /// <param name="flags">The combined security flags.</param>
    /// <param name="restrictionLevel">The UTS #39 restriction level.</param>
    /// <param name="skeleton">The UTS #39 confusable skeleton.</param>
    /// <param name="scripts">The significant scripts used by the identifier.</param>
    /// <param name="identifierValidation">The identifier-profile validation result.</param>
    /// <param name="securityDiagnostics">The security diagnostics.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>skeleton</c>, <c>scripts</c>, <c>identifierValidation</c>, <c>securityDiagnostics</c> is <c>null</c>.</exception>
    public UnicodeSecurityCheckResult(
        bool isSafe,
        UnicodeSecurityFlag flags,
        UnicodeRestrictionLevel restrictionLevel,
        string skeleton,
        UnicodeScript[] scripts,
        UnicodeIdentifierValidationResult identifierValidation,
        UnicodeSecurityDiagnostic[] securityDiagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(skeleton);
        System.ArgumentNullException.ThrowIfNull(scripts);
        System.ArgumentNullException.ThrowIfNull(identifierValidation);
        System.ArgumentNullException.ThrowIfNull(securityDiagnostics);

        IsSafe = isSafe;
        Flags = flags;
        RestrictionLevel = restrictionLevel;
        Skeleton = skeleton;
        Scripts = scripts;
        IdentifierValidation = identifierValidation;
        SecurityDiagnostics = securityDiagnostics;
    }
}
