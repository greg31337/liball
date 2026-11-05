namespace unicode;

/// <summary>
/// Contains the prepared Unicode label, validation outcome, and security observations.
/// </summary>
public class UnicodeIdnaValidationResult
{
    /// <summary>Whether the label passed every required validation rule.</summary>
    public bool IsValid;
    /// <summary>The first validation status, or <see cref="UnicodeIdnaStatus.Valid"/>.</summary>
    public UnicodeIdnaStatus Status;
    /// <summary>The mapped and NFC-normalized Unicode label.</summary>
    public string PreparedLabel;
    /// <summary>The validation failures in prepared-label order.</summary>
    public UnicodeIdnaDiagnostic[] Diagnostics;
    /// <summary>Non-fatal UTS #39 security observations for the prepared label.</summary>
    public UnicodeSecurityDiagnostic[] SecurityDiagnostics;
    /// <summary>The UTS #39 restriction level of the prepared label.</summary>
    public UnicodeRestrictionLevel RestrictionLevel;

    /// <summary>
    /// Initializes a new label-validation result.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="preparedLabel">The prepared Unicode label.</param>
    /// <param name="diagnostics">The validation diagnostics.</param>
    /// <param name="securityDiagnostics">The non-fatal security observations.</param>
    /// <param name="restrictionLevel">The UTS #39 restriction level.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when any array or string argument is <c>null</c>.</exception>
    public UnicodeIdnaValidationResult(
        bool isValid,
        UnicodeIdnaStatus status,
        string preparedLabel,
        UnicodeIdnaDiagnostic[] diagnostics,
        UnicodeSecurityDiagnostic[] securityDiagnostics,
        UnicodeRestrictionLevel restrictionLevel)
    {
        System.ArgumentNullException.ThrowIfNull(preparedLabel);
        System.ArgumentNullException.ThrowIfNull(diagnostics);
        System.ArgumentNullException.ThrowIfNull(securityDiagnostics);

        IsValid = isValid;
        Status = status;
        PreparedLabel = preparedLabel;
        Diagnostics = diagnostics;
        SecurityDiagnostics = securityDiagnostics;
        RestrictionLevel = restrictionLevel;
    }
}
