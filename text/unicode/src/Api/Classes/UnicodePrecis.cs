using System.Collections.Generic;
using System.Text;

namespace unicode;

/// <summary>
/// Prepares and validates username, nickname, and opaque strings with PRECIS-inspired profiles.
/// </summary>
public class UnicodePrecis : IUnicodePrecis
{
    internal static readonly UnicodePrecis Instance = new UnicodePrecis();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodePrecis()
    {
    }
    /// <summary>
    /// The stable diagnostic rule identifier for empty.
    /// </summary>
    internal const string EmptyRule = "Precis.Empty";
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Scalar.
    /// </summary>
    internal const string InvalidScalarRule = "Precis.InvalidScalar";
    /// <summary>
    /// The stable diagnostic rule identifier for disallowed Space.
    /// </summary>
    internal const string DisallowedSpaceRule = "Precis.DisallowedSpace";
    /// <summary>
    /// The stable diagnostic rule identifier for disallowed Control.
    /// </summary>
    internal const string DisallowedControlRule = "Precis.DisallowedControl";
    /// <summary>
    /// The stable diagnostic rule identifier for disallowed Identifier.
    /// </summary>
    internal const string DisallowedIdentifierRule = "Precis.DisallowedIdentifier";
    /// <summary>
    /// The stable diagnostic rule identifier for needs Preparation.
    /// </summary>
    internal const string NeedsPreparationRule = "Precis.NeedsPreparation";
    /// <summary>
    /// The stable diagnostic rule identifier for a disallowed code point.
    /// </summary>
    internal const string DisallowedCodePointRule = "Precis.DisallowedCodePoint";
    /// <summary>
    /// The stable diagnostic rule identifier for a contextual rule failure.
    /// </summary>
    internal const string ContextualRule = "Precis.Contextual";
    /// <summary>
    /// The stable diagnostic rule identifier for a bidirectional rule failure.
    /// </summary>
    internal const string BidiRule = "Precis.Bidi";

    /// <summary>
    /// Prepares text with the default case-mapped username profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareUsername(string text)
    {
        return Prepare(text, UnicodePrecisProfile.CreateUsernameCaseMapped());
    }

    /// <summary>
    /// Prepares text with the default nickname profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareNickname(string text)
    {
        return Prepare(text, UnicodePrecisProfile.CreateNickname());
    }

    /// <summary>
    /// Prepares text with the default opaque-string profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareOpaqueString(string text)
    {
        return Prepare(text, UnicodePrecisProfile.CreateOpaqueString());
    }

    /// <summary>
    /// Maps, case-converts, normalizes, and validates text according to a PRECIS-style profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public string Prepare(string text, UnicodePrecisProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        string prepared = text;
        string previous = "";
        int pass = 0;
        while (pass < 4)
        {
            previous = prepared;
            prepared = ApplyProfileRules(prepared, profile);
            if (string.Equals(prepared, previous, System.StringComparison.Ordinal))
            {
                break;
            }

            pass = pass + 1;
        }

        return prepared;
    }

    internal static string ApplyProfileRules(string text, UnicodePrecisProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        string prepared = text;
        if (profile.IdentifierClass)
        {
            prepared = MapWidth(prepared);
        }

        if (profile.MapSpaces)
        {
            prepared = MapSpacesToAscii(prepared);
        }

        if (profile.CollapseSpaces)
        {
            prepared = CollapseAsciiSpaces(prepared, profile.TrimSpaces);
        }
        else
        {
            if (profile.TrimSpaces)
            {
                prepared = TrimAsciiSpaces(prepared);
            }
        }

        if (profile.CaseMapped)
        {
            prepared = UnicodeCase.Instance.ToLower(prepared);
        }

        if (profile.Normalize)
        {
            prepared = UnicodeNormalization.Instance.Normalize(prepared, profile.NormalizationForm);
        }

        if (profile.MapSpaces)
        {
            prepared = MapSpacesToAscii(prepared);
        }

        if (profile.CollapseSpaces)
        {
            prepared = CollapseAsciiSpaces(prepared, profile.TrimSpaces);
        }
        else
        {
            if (profile.TrimSpaces)
            {
                prepared = TrimAsciiSpaces(prepared);
            }
        }

        return prepared;
    }

    /// <summary>
    /// Prepares text and applies the profile's case-insensitive comparison policy.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public string GetCompareKey(string text, UnicodePrecisProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        string prepared = Prepare(text, profile);
        if (profile.CaseInsensitive)
        {
            if (!profile.CaseMapped)
            {
                prepared = UnicodeCase.Instance.ToLower(prepared);
                if (profile.Normalize)
                {
                    prepared = UnicodeNormalization.Instance.Normalize(prepared, profile.NormalizationForm);
                }

                if (profile.MapSpaces)
                {
                    prepared = MapSpacesToAscii(prepared);
                }

                if (profile.CollapseSpaces)
                {
                    prepared = CollapseAsciiSpaces(prepared, profile.TrimSpaces);
                }
            }
        }

        return prepared;
    }

    /// <summary>
    /// Reports whether two strings produce the same prepared comparison key under a profile.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c>, <c>profile</c> is <c>null</c>.</exception>
    public bool EqualsPrepared(string left, string right, UnicodePrecisProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);
        System.ArgumentNullException.ThrowIfNull(profile);

        string leftKey = GetCompareKey(left, profile);
        string rightKey = GetCompareKey(right, profile);
        return string.Equals(leftKey, rightKey, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Reports whether preparation would leave text unchanged and valid under the profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public bool IsPrepared(string text, UnicodePrecisProfile profile)
    {
        UnicodePrecisValidationResult result = Validate(text, profile);
        return result.IsValid;
    }

    /// <summary>
    /// Validates text under the selected profile and returns the prepared value and diagnostics.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public UnicodePrecisValidationResult Validate(string text, UnicodePrecisProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        string prepared = Prepare(text, profile);
        List<UnicodePrecisDiagnostic> diagnostics = new List<UnicodePrecisDiagnostic>();
        AddInvalidScalarDiagnostics(text, diagnostics);

        if (diagnostics.Count == 0)
        {
            AddPreparedTextDiagnostics(prepared, profile, diagnostics);
        }

        if (diagnostics.Count == 0)
        {
            if (!string.Equals(text, prepared, System.StringComparison.Ordinal))
            {
                diagnostics.Add(new UnicodePrecisDiagnostic(
                    0,
                    text.Length,
                    -1,
                    UnicodePrecisStatus.NeedsPreparation,
                    NeedsPreparationRule,
                    "Input does not match the prepared form for the selected profile."));
            }
        }

        UnicodePrecisStatus status = GetValidationStatus(diagnostics);
        return new UnicodePrecisValidationResult(
            diagnostics.Count == 0,
            status,
            prepared,
            diagnostics.ToArray());
    }

    internal static void AddInvalidScalarDiagnostics(string text, List<UnicodePrecisDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                diagnostics.Add(new UnicodePrecisDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    UnicodePrecisStatus.InvalidScalar,
                    InvalidScalarRule,
                    "Input contains invalid UTF-16 scalar data."));
            }

            index = index + 1;
        }
    }

    internal static void AddPreparedTextDiagnostics(
        string prepared,
        UnicodePrecisProfile profile,
        List<UnicodePrecisDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(prepared);
        System.ArgumentNullException.ThrowIfNull(profile);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(prepared);
        if (scalars.Length == 0)
        {
            if (!profile.AllowEmpty)
            {
                diagnostics.Add(new UnicodePrecisDiagnostic(
                    0,
                    0,
                    -1,
                    UnicodePrecisStatus.Empty,
                    EmptyRule,
                    "Prepared string must contain at least one scalar value."));
            }
        }

        int index = 0;
        while (index < scalars.Length)
        {
            AddPreparedScalarDiagnostics(scalars[index], profile, diagnostics);
            index = index + 1;
        }

        AddContextualDiagnostics(scalars, diagnostics);
        if (profile.IdentifierClass)
        {
            AddPrecisBidiDiagnostics(prepared, diagnostics);
        }
    }

    internal static void AddPreparedScalarDiagnostics(
        UnicodeScalarRecord scalar,
        UnicodePrecisProfile profile,
        List<UnicodePrecisDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalar);
        System.ArgumentNullException.ThrowIfNull(profile);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (!profile.AllowSpaces)
        {
            if (IsPrecisSpace(scalar.CodePoint))
            {
                diagnostics.Add(new UnicodePrecisDiagnostic(
                    scalar.Utf16Index,
                    scalar.Utf16Length,
                    scalar.CodePoint,
                    UnicodePrecisStatus.DisallowedSpace,
                    DisallowedSpaceRule,
                    "Profile does not allow spaces."));
                return;
            }
        }

        if (IsPrecisControl(scalar.CodePoint))
        {
            diagnostics.Add(new UnicodePrecisDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodePrecisStatus.DisallowedControl,
                DisallowedControlRule,
                "Profile does not allow control characters."));
            return;
        }

        if (UnicodeContextualRules.RequiresContext(scalar.CodePoint))
        {
            return;
        }

        if (!IsPrecisCodePointAllowed(scalar.CodePoint, profile.IdentifierClass))
        {
            diagnostics.Add(new UnicodePrecisDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodePrecisStatus.DisallowedCodePoint,
                DisallowedCodePointRule,
                "Code point is not allowed by the selected PRECIS string class."));
        }
    }

    internal static void AddContextualDiagnostics(
        UnicodeScalarRecord[] scalars,
        List<UnicodePrecisDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        int index = 0;
        while (index < scalars.Length)
        {
            if (UnicodeContextualRules.RequiresContext(scalars[index].CodePoint))
            {
                if (!UnicodeContextualRules.IsValidContext(scalars, index))
                {
                    diagnostics.Add(new UnicodePrecisDiagnostic(
                        scalars[index].Utf16Index,
                        scalars[index].Utf16Length,
                        scalars[index].CodePoint,
                        UnicodePrecisStatus.ContextualRule,
                        ContextualRule,
                        "Context-dependent code point does not satisfy its required contextual rule."));
                }
            }

            index = index + 1;
        }
    }

    internal static void AddPrecisBidiDiagnostics(
        string prepared,
        List<UnicodePrecisDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(prepared);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeSecurityDiagnostic[] bidiDiagnostics = UnicodeSecurity.Instance.GetIdentifierBidiDiagnostics(prepared);
        int index = 0;
        while (index < bidiDiagnostics.Length)
        {
            diagnostics.Add(new UnicodePrecisDiagnostic(
                bidiDiagnostics[index].Utf16Index,
                bidiDiagnostics[index].Utf16Length,
                bidiDiagnostics[index].CodePoint,
                UnicodePrecisStatus.BidiRule,
                BidiRule,
                bidiDiagnostics[index].Message));
            index = index + 1;
        }
    }

    internal static bool IsPrecisCodePointAllowed(int codePoint, bool identifierClass)
    {
        if (IsPrecisValidException(codePoint))
        {
            return true;
        }

        if (IsPrecisDisallowedException(codePoint))
        {
            return false;
        }

        if (codePoint >= 0x0021 && codePoint <= 0x007E)
        {
            return true;
        }

        if (UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.OtherNotAssigned)
        {
            return false;
        }

        if (IsOldHangulJamo(codePoint))
        {
            return false;
        }

        if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.DefaultIgnorableCodePoint))
        {
            return false;
        }

        if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.NoncharacterCodePoint))
        {
            return false;
        }

        if (IsPrecisControl(codePoint))
        {
            return false;
        }

        string scalar = UnicodeScalar.Instance.ToUtf16String(codePoint);
        string compatibility = UnicodeNormalization.Instance.Normalize(scalar, UnicodeNormalizationForm.Nfkc);
        if (!string.Equals(scalar, compatibility, System.StringComparison.Ordinal))
        {
            return !identifierClass;
        }

        UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(codePoint);
        if (IsLetterDigitCategory(category))
        {
            return true;
        }

        if (IsFreeformCategory(category))
        {
            return !identifierClass;
        }

        return false;
    }

    internal static bool IsPrecisValidException(int codePoint)
    {
        if (codePoint == 0x00DF || codePoint == 0x03C2)
        {
            return true;
        }

        if (codePoint == 0x06FD || codePoint == 0x06FE || codePoint == 0x0F0B)
        {
            return true;
        }

        return codePoint == 0x3007;
    }

    internal static bool IsPrecisDisallowedException(int codePoint)
    {
        if (codePoint == 0x0640 || codePoint == 0x07FA)
        {
            return true;
        }

        if (codePoint == 0x302E || codePoint == 0x302F || codePoint == 0x303B)
        {
            return true;
        }

        return codePoint >= 0x3031 && codePoint <= 0x3035;
    }

    internal static bool IsOldHangulJamo(int codePoint)
    {
        if (codePoint >= 0x1100 && codePoint <= 0x11FF)
        {
            return true;
        }

        if (codePoint >= 0xA960 && codePoint <= 0xA97F)
        {
            return true;
        }

        return codePoint >= 0xD7B0 && codePoint <= 0xD7FF;
    }

    internal static bool IsLetterDigitCategory(UnicodeGeneralCategory category)
    {
        if (category == UnicodeGeneralCategory.LowercaseLetter ||
            category == UnicodeGeneralCategory.UppercaseLetter ||
            category == UnicodeGeneralCategory.OtherLetter ||
            category == UnicodeGeneralCategory.ModifierLetter)
        {
            return true;
        }

        if (category == UnicodeGeneralCategory.DecimalNumber ||
            category == UnicodeGeneralCategory.NonspacingMark ||
            category == UnicodeGeneralCategory.SpacingMark)
        {
            return true;
        }

        return false;
    }

    internal static bool IsFreeformCategory(UnicodeGeneralCategory category)
    {
        if (category == UnicodeGeneralCategory.TitlecaseLetter ||
            category == UnicodeGeneralCategory.LetterNumber ||
            category == UnicodeGeneralCategory.OtherNumber ||
            category == UnicodeGeneralCategory.EnclosingMark ||
            category == UnicodeGeneralCategory.SpaceSeparator)
        {
            return true;
        }

        if (category == UnicodeGeneralCategory.MathSymbol ||
            category == UnicodeGeneralCategory.CurrencySymbol ||
            category == UnicodeGeneralCategory.ModifierSymbol ||
            category == UnicodeGeneralCategory.OtherSymbol)
        {
            return true;
        }

        return IsPunctuationCategory(category);
    }

    internal static bool IsPunctuationCategory(UnicodeGeneralCategory category)
    {
        if (category == UnicodeGeneralCategory.ConnectorPunctuation ||
            category == UnicodeGeneralCategory.DashPunctuation ||
            category == UnicodeGeneralCategory.OpenPunctuation ||
            category == UnicodeGeneralCategory.ClosePunctuation)
        {
            return true;
        }

        if (category == UnicodeGeneralCategory.InitialPunctuation ||
            category == UnicodeGeneralCategory.FinalPunctuation ||
            category == UnicodeGeneralCategory.OtherPunctuation)
        {
            return true;
        }

        return false;
    }

    internal static UnicodePrecisStatus GetValidationStatus(List<UnicodePrecisDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (diagnostics.Count == 0)
        {
            return UnicodePrecisStatus.Valid;
        }

        return diagnostics[0].Status;
    }

    internal static bool IsPrecisSpace(int codePoint)
    {
        if (codePoint == 0x0020)
        {
            return true;
        }

        return UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.SpaceSeparator;
    }

    internal static bool IsPrecisControl(int codePoint)
    {
        if (codePoint >= 0x0000)
        {
            if (codePoint <= 0x001F)
            {
                return true;
            }
        }

        if (codePoint >= 0x007F)
        {
            if (codePoint <= 0x009F)
            {
                return true;
            }
        }

        return UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.Control;
    }

    internal static string MapWidth(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;
        while (index < scalars.Length)
        {
            string scalar = UnicodeScalar.Instance.ToUtf16String(scalars[index].CodePoint);
            UnicodeEastAsianWidth width = UnicodeProperties.Instance.GetEastAsianWidth(scalars[index].CodePoint);
            if (width == UnicodeEastAsianWidth.Fullwidth || width == UnicodeEastAsianWidth.Halfwidth)
            {
                scalar = UnicodeNormalization.Instance.Normalize(scalar, UnicodeNormalizationForm.Nfkc);
            }

            builder.Append(scalar);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string MapSpacesToAscii(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;
        while (index < scalars.Length)
        {
            if (IsPrecisSpace(scalars[index].CodePoint))
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append(UnicodeScalar.Instance.ToUtf16String(scalars[index].CodePoint));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string CollapseAsciiSpaces(string text, bool trim)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        StringBuilder builder = new StringBuilder(text.Length);
        bool previousWasSpace = false;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == ' ')
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
            }
            else
            {
                builder.Append(text[index]);
                previousWasSpace = false;
            }

            index = index + 1;
        }

        string collapsed = builder.ToString();
        if (trim)
        {
            return TrimAsciiSpaces(collapsed);
        }

        return collapsed;
    }

    internal static string TrimAsciiSpaces(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        int start = 0;
        while (start < text.Length)
        {
            if (text[start] != ' ')
            {
                break;
            }

            start = start + 1;
        }

        int end = text.Length - 1;
        while (end >= start)
        {
            if (text[end] != ' ')
            {
                break;
            }

            end = end - 1;
        }

        if (end < start)
        {
            return "";
        }

        return text.Substring(start, end - start + 1);
    }
}
