using System.Collections.Generic;

namespace unicode;

/// <summary>
/// Provides Unicode identifier classification, normalization, comparison, and validation.
/// </summary>
public class UnicodeIdentifiers : IUnicodeIdentifiers
{
    internal static readonly UnicodeIdentifiers Instance = new UnicodeIdentifiers();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeIdentifiers()
    {
    }
    /// <summary>
    /// The stable diagnostic rule identifier for empty.
    /// </summary>
    internal const string EmptyRule = "Identifier.Empty";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Start.
    /// </summary>
    internal const string InvalidStartRule = "Identifier.InvalidStart";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Continue.
    /// </summary>
    internal const string InvalidContinueRule = "Identifier.InvalidContinue";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Scalar.
    /// </summary>
    internal const string InvalidScalarRule = "Identifier.InvalidScalar";
    /// <summary>
    /// The stable diagnostic rule identifier for pattern Syntax.
    /// </summary>
    internal const string PatternSyntaxRule = "Identifier.PatternSyntax";
    /// <summary>
    /// The stable diagnostic rule identifier for pattern White Space.
    /// </summary>
    internal const string PatternWhiteSpaceRule = "Identifier.PatternWhiteSpace";
    /// <summary>
    /// The stable diagnostic rule identifier for needs Normalization.
    /// </summary>
    internal const string NeedsNormalizationRule = "Identifier.NeedsNormalization";

    /// <summary>
    /// Reports whether a code point has the Unicode ID_Start property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsIdStart(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.IdStart(), codePoint);
    }

    /// <summary>
    /// Reports whether a code point has the Unicode ID_Continue property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsIdContinue(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.IdContinue(), codePoint);
    }

    /// <summary>
    /// Reports whether a code point has the Unicode XID_Start property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsXidStart(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.XidStart(), codePoint);
    }

    /// <summary>
    /// Reports whether a code point has the Unicode XID_Continue property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsXidContinue(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.XidContinue(), codePoint);
    }

    /// <summary>
    /// Reports whether a code point has the Unicode Pattern_Syntax property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsPatternSyntax(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.PatternSyntax(), codePoint);
    }

    /// <summary>
    /// Reports whether a code point has the Unicode Pattern_White_Space property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsPatternWhiteSpace(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.PatternWhiteSpace(), codePoint);
    }

    /// <summary>
    /// Returns the Unicode code-point ranges with the ID_Start property.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    public UnicodeRangeSet GetIdStartRangeSet()
    {
        return new UnicodeRangeSet(UnicodeIdentifierTables.IdStart());
    }

    /// <summary>
    /// Reports whether text is a valid default XID identifier.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsIdentifier(string text)
    {
        UnicodeIdentifierValidationResult result = ValidateIdentifier(text, UnicodeIdentifierProfile.CreateDefault());
        return result.IsValid;
    }

    /// <summary>
    /// Applies the normalization policy supplied by an identifier profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public string NormalizeIdentifier(string text, IUnicodeIdentifierProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        return profile.Normalize(text);
    }

    /// <summary>
    /// Produces an identifier comparison key using a behavioral profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public string GetIdentifierCompareKey(string text, IUnicodeIdentifierProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        return profile.GetCompareKey(text);
    }

    /// <summary>
    /// Reports whether two identifiers produce the same comparison key under a behavioral profile.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c>, <c>profile</c> is <c>null</c>.</exception>
    public bool EqualsIdentifier(string left, string right, IUnicodeIdentifierProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);
        System.ArgumentNullException.ThrowIfNull(profile);

        string leftKey = GetIdentifierCompareKey(left, profile);
        string rightKey = GetIdentifierCompareKey(right, profile);
        return string.Equals(leftKey, rightKey, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates every scalar against a behavioral identifier profile and returns ordered diagnostics.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public UnicodeIdentifierValidationResult ValidateIdentifier(string text, IUnicodeIdentifierProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<UnicodeIdentifierDiagnostic> diagnostics = new List<UnicodeIdentifierDiagnostic>();

        if (scalars.Length == 0)
        {
            diagnostics.Add(new UnicodeIdentifierDiagnostic(
                0,
                0,
                -1,
                UnicodeGeneralCategory.OtherNotAssigned,
                UnicodeIdentifierStatus.Empty,
                UnicodeDiagnosticSeverity.Error,
                EmptyRule,
                "Identifier must contain at least one scalar value."));
        }

        int index = 0;
        while (index < scalars.Length)
        {
            ValidateIdentifierScalar(scalars[index], index, profile, diagnostics);
            index = index + 1;
        }

        if (scalars.Length > 0)
        {
            if (!profile.IsNormalizationSatisfied(text))
            {
                diagnostics.Add(new UnicodeIdentifierDiagnostic(
                    0,
                    text.Length,
                    -1,
                    UnicodeGeneralCategory.OtherNotAssigned,
                    UnicodeIdentifierStatus.NeedsNormalization,
                    UnicodeDiagnosticSeverity.Error,
                    NeedsNormalizationRule,
                    "Identifier does not satisfy the profile normalization form."));
            }
        }

        UnicodeIdentifierStatus status = GetValidationStatus(diagnostics);
        return new UnicodeIdentifierValidationResult(
            diagnostics.Count == 0,
            status,
            diagnostics.ToArray(),
            scalars);
    }

    internal static void ValidateIdentifierScalar(
        UnicodeScalarRecord scalar,
        int scalarIndex,
        IUnicodeIdentifierProfile profile,
        List<UnicodeIdentifierDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalar);
        System.ArgumentNullException.ThrowIfNull(profile);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (scalar.IsReplacement)
        {
            diagnostics.Add(new UnicodeIdentifierDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodeProperties.Instance.GetGeneralCategory(scalar.CodePoint),
                UnicodeIdentifierStatus.InvalidScalar,
                UnicodeDiagnosticSeverity.Error,
                InvalidScalarRule,
                "Identifier contains invalid UTF-16 scalar input."));
            return;
        }

        if (scalarIndex == 0)
        {
            if (!CanStartIdentifier(profile, scalar.CodePoint))
            {
                diagnostics.Add(new UnicodeIdentifierDiagnostic(
                    scalar.Utf16Index,
                    scalar.Utf16Length,
                    scalar.CodePoint,
                    UnicodeProperties.Instance.GetGeneralCategory(scalar.CodePoint),
                    UnicodeIdentifierStatus.InvalidStart,
                    UnicodeDiagnosticSeverity.Error,
                    InvalidStartRule,
                    "Identifier first scalar does not satisfy the profile start rule."));
            }
        }
        else
        {
            if (!CanContinueIdentifier(profile, scalar.CodePoint))
            {
                diagnostics.Add(new UnicodeIdentifierDiagnostic(
                    scalar.Utf16Index,
                    scalar.Utf16Length,
                    scalar.CodePoint,
                    UnicodeProperties.Instance.GetGeneralCategory(scalar.CodePoint),
                    UnicodeIdentifierStatus.InvalidContinue,
                    UnicodeDiagnosticSeverity.Error,
                    InvalidContinueRule,
                    "Identifier scalar does not satisfy the profile continue rule."));
            }
        }

        if (profile.DisallowsPatternSyntax())
        {
            if (Instance.IsPatternSyntax(scalar.CodePoint))
            {
                diagnostics.Add(new UnicodeIdentifierDiagnostic(
                    scalar.Utf16Index,
                    scalar.Utf16Length,
                    scalar.CodePoint,
                    UnicodeProperties.Instance.GetGeneralCategory(scalar.CodePoint),
                    UnicodeIdentifierStatus.Disallowed,
                    UnicodeDiagnosticSeverity.Error,
                    PatternSyntaxRule,
                    "Identifier scalar is disallowed by Pattern_Syntax."));
            }
        }

        if (profile.DisallowsPatternWhiteSpace())
        {
            if (Instance.IsPatternWhiteSpace(scalar.CodePoint))
            {
                diagnostics.Add(new UnicodeIdentifierDiagnostic(
                    scalar.Utf16Index,
                    scalar.Utf16Length,
                    scalar.CodePoint,
                    UnicodeProperties.Instance.GetGeneralCategory(scalar.CodePoint),
                    UnicodeIdentifierStatus.Disallowed,
                    UnicodeDiagnosticSeverity.Error,
                    PatternWhiteSpaceRule,
                    "Identifier scalar is disallowed by Pattern_White_Space."));
            }
        }
    }

    internal static bool CanStartIdentifier(IUnicodeIdentifierProfile profile, int codePoint)
    {
        System.ArgumentNullException.ThrowIfNull(profile);

        if (profile.AllowsStart(codePoint))
        {
            return true;
        }

        UnicodeIdentifierProfile? builtInProfile = profile as UnicodeIdentifierProfile;
        if (builtInProfile != null)
        {
            if (builtInProfile.UseImmutableRules)
            {
                return false;
            }
        }

        if (profile.AllowsUnassignedCodePoints())
        {
            return UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.OtherNotAssigned;
        }

        return false;
    }

    internal static bool CanContinueIdentifier(IUnicodeIdentifierProfile profile, int codePoint)
    {
        System.ArgumentNullException.ThrowIfNull(profile);

        if (profile.AllowsContinue(codePoint))
        {
            return true;
        }

        UnicodeIdentifierProfile? builtInProfile = profile as UnicodeIdentifierProfile;
        if (builtInProfile != null)
        {
            if (builtInProfile.UseImmutableRules)
            {
                return false;
            }
        }

        if (profile.AllowsUnassignedCodePoints())
        {
            return UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.OtherNotAssigned;
        }

        return false;
    }

    internal static UnicodeIdentifierStatus GetValidationStatus(List<UnicodeIdentifierDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (diagnostics.Count == 0)
        {
            return UnicodeIdentifierStatus.Valid;
        }

        UnicodeIdentifierDiagnostic selected = diagnostics[0];
        int index = 1;
        while (index < diagnostics.Count)
        {
            if ((int)diagnostics[index].Severity > (int)selected.Severity)
            {
                selected = diagnostics[index];
            }

            index = index + 1;
        }

        return selected.Status;
    }
}
