using System.Collections.Generic;
using System.Text;

namespace unicode;

/// <summary>
/// Maps and validates one Unicode label without performing Punycode conversion or DNS processing.
/// </summary>
public class UnicodeIdna : IUnicodeIdna
{
    internal const string EmptyRule = "Idna.Empty";
    internal const string InvalidScalarRule = "Idna.InvalidScalar";
    internal const string LabelSeparatorRule = "Idna.LabelSeparator";
    internal const string HyphenRule = "Idna.Hyphen";
    internal const string LeadingMarkRule = "Idna.LeadingMark";
    internal const string DisallowedCodePointRule = "Idna.DisallowedCodePoint";
    internal const string ContextualRule = "Idna.Contextual";
    internal const string BidiRule = "Idna.Bidi";

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeIdna()
    {
    }

    /// <summary>
    /// Applies nontransitional Unicode compatibility mapping and NFC normalization to one label.
    /// </summary>
    /// <param name="label">The UTF-16 label to prepare.</param>
    /// <returns>The mapped and normalized Unicode label.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    public string PrepareLabel(string label)
    {
        System.ArgumentNullException.ThrowIfNull(label);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(label);
        StringBuilder builder = new StringBuilder(label.Length);
        int index = 0;
        while (index < scalars.Length)
        {
            builder.Append(MapLabelCodePoint(scalars[index].CodePoint));
            index = index + 1;
        }

        return UnicodeNormalization.Instance.Normalize(builder.ToString(), UnicodeNormalizationForm.Nfc);
    }

    /// <summary>
    /// Prepares and validates one Unicode label with contextual, bidirectional, and security checks.
    /// </summary>
    /// <param name="label">The UTF-16 label to validate.</param>
    /// <returns>The prepared label, validation diagnostics, and security observations.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    public UnicodeIdnaValidationResult ValidateLabel(string label)
    {
        System.ArgumentNullException.ThrowIfNull(label);

        string prepared = PrepareLabel(label);
        List<UnicodeIdnaDiagnostic> diagnostics = new List<UnicodeIdnaDiagnostic>();
        AddInvalidScalarDiagnostics(label, diagnostics);
        if (diagnostics.Count == 0)
        {
            AddLabelDiagnostics(prepared, diagnostics);
        }

        UnicodeSecurityDiagnostic[] securityDiagnostics = UnicodeSecurity.Instance.GetSecurityDiagnostics(prepared);
        UnicodeRestrictionLevel restrictionLevel = UnicodeSecurity.Instance.GetRestrictionLevel(prepared);
        UnicodeIdnaStatus status = UnicodeIdnaStatus.Valid;
        if (diagnostics.Count != 0)
        {
            status = diagnostics[0].Status;
        }

        return new UnicodeIdnaValidationResult(
            diagnostics.Count == 0,
            status,
            prepared,
            diagnostics.ToArray(),
            securityDiagnostics,
            restrictionLevel);
    }

    /// <summary>
    /// Reports whether one Unicode label passes validation after Unicode preparation.
    /// </summary>
    /// <param name="label">The UTF-16 label to validate.</param>
    /// <returns><c>true</c> when the prepared label is valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    public bool IsLabelValid(string label)
    {
        return ValidateLabel(label).IsValid;
    }

    internal static string MapLabelCodePoint(int codePoint)
    {
        if (codePoint == 0x3002 || codePoint == 0xFF0E || codePoint == 0xFF61)
        {
            return ".";
        }

        if (codePoint == 0x1E9E)
        {
            return "\u00DF";
        }

        if (codePoint == 0x00DF || codePoint == 0x03C2 || codePoint == 0x200C || codePoint == 0x200D)
        {
            return UnicodeScalar.Instance.ToUtf16String(codePoint);
        }

        if (UnicodeSecurity.IsBidiControl(codePoint))
        {
            return UnicodeScalar.Instance.ToUtf16String(codePoint);
        }

        string scalar = UnicodeScalar.Instance.ToUtf16String(codePoint);
        return UnicodeCase.Instance.ToNfkcCaseFold(scalar);
    }

    internal static void AddInvalidScalarDiagnostics(
        string label,
        List<UnicodeIdnaDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(label);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(label);
        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                diagnostics.Add(new UnicodeIdnaDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    UnicodeIdnaStatus.InvalidScalar,
                    InvalidScalarRule,
                    "Input contains invalid UTF-16 scalar data."));
            }

            index = index + 1;
        }
    }

    internal static void AddLabelDiagnostics(
        string prepared,
        List<UnicodeIdnaDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(prepared);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(prepared);
        if (scalars.Length == 0)
        {
            diagnostics.Add(new UnicodeIdnaDiagnostic(
                0,
                0,
                -1,
                UnicodeIdnaStatus.Empty,
                EmptyRule,
                "Unicode label must not be empty."));
            return;
        }

        AddHyphenDiagnostics(scalars, diagnostics);
        AddLeadingMarkDiagnostic(scalars, diagnostics);

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].CodePoint == 0x002E)
            {
                diagnostics.Add(new UnicodeIdnaDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    UnicodeIdnaStatus.LabelSeparator,
                    LabelSeparatorRule,
                    "One-label validation does not allow a label separator."));
            }
            else
            {
                if (!UnicodeContextualRules.RequiresContext(scalars[index].CodePoint))
                {
                    if (!IsIdnaCodePointAllowed(scalars[index].CodePoint))
                    {
                        diagnostics.Add(new UnicodeIdnaDiagnostic(
                            scalars[index].Utf16Index,
                            scalars[index].Utf16Length,
                            scalars[index].CodePoint,
                            UnicodeIdnaStatus.DisallowedCodePoint,
                            DisallowedCodePointRule,
                            "Code point is disallowed in an IDNA-adjacent Unicode label."));
                    }
                }
            }

            if (UnicodeContextualRules.RequiresContext(scalars[index].CodePoint))
            {
                if (!UnicodeContextualRules.IsValidContext(scalars, index))
                {
                    diagnostics.Add(new UnicodeIdnaDiagnostic(
                        scalars[index].Utf16Index,
                        scalars[index].Utf16Length,
                        scalars[index].CodePoint,
                        UnicodeIdnaStatus.ContextualRule,
                        ContextualRule,
                        "Context-dependent code point does not satisfy its required contextual rule."));
                }
            }

            index = index + 1;
        }

        AddBidiDiagnostics(prepared, diagnostics);
    }

    internal static void AddHyphenDiagnostics(
        UnicodeScalarRecord[] scalars,
        List<UnicodeIdnaDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (scalars[0].CodePoint == 0x002D)
        {
            AddHyphenDiagnostic(scalars[0], diagnostics, "Unicode label must not begin with a hyphen.");
        }

        if (scalars[scalars.Length - 1].CodePoint == 0x002D)
        {
            AddHyphenDiagnostic(scalars[scalars.Length - 1], diagnostics, "Unicode label must not end with a hyphen.");
        }

        if (scalars.Length >= 4)
        {
            if (scalars[2].CodePoint == 0x002D && scalars[3].CodePoint == 0x002D)
            {
                AddHyphenDiagnostic(scalars[2], diagnostics, "Unicode label reserves hyphens in both the third and fourth positions.");
            }
        }
    }

    internal static void AddHyphenDiagnostic(
        UnicodeScalarRecord scalar,
        List<UnicodeIdnaDiagnostic> diagnostics,
        string message)
    {
        System.ArgumentNullException.ThrowIfNull(scalar);
        System.ArgumentNullException.ThrowIfNull(diagnostics);
        System.ArgumentNullException.ThrowIfNull(message);

        diagnostics.Add(new UnicodeIdnaDiagnostic(
            scalar.Utf16Index,
            scalar.Utf16Length,
            scalar.CodePoint,
            UnicodeIdnaStatus.Hyphen,
            HyphenRule,
            message));
    }

    internal static void AddLeadingMarkDiagnostic(
        UnicodeScalarRecord[] scalars,
        List<UnicodeIdnaDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(scalars[0].CodePoint);
        if (category == UnicodeGeneralCategory.NonspacingMark ||
            category == UnicodeGeneralCategory.SpacingMark ||
            category == UnicodeGeneralCategory.EnclosingMark)
        {
            diagnostics.Add(new UnicodeIdnaDiagnostic(
                scalars[0].Utf16Index,
                scalars[0].Utf16Length,
                scalars[0].CodePoint,
                UnicodeIdnaStatus.LeadingMark,
                LeadingMarkRule,
                "Unicode label must not begin with a combining mark."));
        }
    }

    internal static void AddBidiDiagnostics(
        string prepared,
        List<UnicodeIdnaDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(prepared);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeSecurityDiagnostic[] bidiDiagnostics = UnicodeSecurity.Instance.GetIdentifierBidiDiagnostics(prepared);
        int index = 0;
        while (index < bidiDiagnostics.Length)
        {
            diagnostics.Add(new UnicodeIdnaDiagnostic(
                bidiDiagnostics[index].Utf16Index,
                bidiDiagnostics[index].Utf16Length,
                bidiDiagnostics[index].CodePoint,
                UnicodeIdnaStatus.BidiRule,
                BidiRule,
                bidiDiagnostics[index].Message));
            index = index + 1;
        }
    }

    internal static bool IsIdnaCodePointAllowed(int codePoint)
    {
        if (UnicodePrecis.IsPrecisValidException(codePoint))
        {
            return true;
        }

        if (UnicodePrecis.IsPrecisDisallowedException(codePoint))
        {
            return false;
        }

        if (codePoint == 0x002D)
        {
            return true;
        }

        if (codePoint >= 0x0030 && codePoint <= 0x0039)
        {
            return true;
        }

        if (codePoint >= 0x0061 && codePoint <= 0x007A)
        {
            return true;
        }

        if (codePoint <= 0x007F)
        {
            return false;
        }

        UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(codePoint);
        if (category == UnicodeGeneralCategory.OtherNotAssigned)
        {
            return false;
        }

        if (UnicodePrecis.IsOldHangulJamo(codePoint))
        {
            return false;
        }

        if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.DefaultIgnorableCodePoint) ||
            UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.NoncharacterCodePoint) ||
            UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.WhiteSpace))
        {
            return false;
        }

        if (IsIdnaIgnorableBlock(codePoint))
        {
            return false;
        }

        if (category == UnicodeGeneralCategory.Control ||
            category == UnicodeGeneralCategory.Format ||
            category == UnicodeGeneralCategory.PrivateUse ||
            category == UnicodeGeneralCategory.Surrogate ||
            codePoint == UnicodeScalar.ReplacementCharacter)
        {
            return false;
        }

        string scalar = UnicodeScalar.Instance.ToUtf16String(codePoint);
        string normalized = UnicodeNormalization.Instance.Normalize(scalar, UnicodeNormalizationForm.Nfkc);
        string folded = UnicodeCase.Instance.ToNfkcCaseFold(normalized);
        return string.Equals(scalar, folded, System.StringComparison.Ordinal);
    }

    internal static bool IsIdnaIgnorableBlock(int codePoint)
    {
        if (codePoint >= 0x20D0 && codePoint <= 0x20FF)
        {
            return true;
        }

        if (codePoint >= 0x1D100 && codePoint <= 0x1D1FF)
        {
            return true;
        }

        return codePoint >= 0x1D200 && codePoint <= 0x1D24F;
    }
}
