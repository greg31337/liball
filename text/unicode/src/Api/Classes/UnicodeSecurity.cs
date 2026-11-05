using System.Collections.Generic;
using System.Text;

namespace unicode;

/// <summary>
/// Provides UTS #39 confusable, script, restriction-level, and identifier security checks.
/// </summary>
public class UnicodeSecurity : IUnicodeSecurity
{
    internal static readonly UnicodeSecurity Instance = new UnicodeSecurity();
    internal const ulong HanBopomofoScriptBit = 4294967296;
    internal const ulong JapaneseScriptBit = 8589934592;
    internal const ulong KoreanScriptBit = 17179869184;
    internal const ulong AllScripts = 18446744073709551615;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeSecurity()
    {
    }
    /// <summary>
    /// The stable diagnostic rule identifier for invisible.
    /// </summary>
    internal const string InvisibleRule = "Security.Invisible";
    /// <summary>
    /// The stable diagnostic rule identifier for default Ignorable.
    /// </summary>
    internal const string DefaultIgnorableRule = "Security.DefaultIgnorable";
    /// <summary>
    /// The stable diagnostic rule identifier for variation Selector.
    /// </summary>
    internal const string VariationSelectorRule = "Security.VariationSelector";
    /// <summary>
    /// The stable diagnostic rule identifier for join Control.
    /// </summary>
    internal const string JoinControlRule = "Security.JoinControl";
    /// <summary>
    /// The stable diagnostic rule identifier for bidi Control.
    /// </summary>
    internal const string BidiControlRule = "Security.BidiControl";
    /// <summary>
    /// The stable diagnostic rule identifier for identifier bidi safety.
    /// </summary>
    internal const string IdentifierBidiRule = "Security.IdentifierBidi";
    /// <summary>
    /// The stable diagnostic rule identifier for label bidi safety.
    /// </summary>
    internal const string LabelBidiRule = "Security.LabelBidi";

    /// <summary>
    /// Returns the NFD-normalized UTS #39 confusable skeleton for text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string GetSkeleton(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        string decomposed = UnicodeNormalization.Instance.Normalize(text, UnicodeNormalizationForm.Nfd);
        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(decomposed);
        List<int> codePoints = new List<int>();
        int index = 0;

        while (index < scalars.Length)
        {
            if (!IsDefaultIgnorable(scalars[index].CodePoint))
            {
                codePoints.Add(scalars[index].CodePoint);
            }

            index = index + 1;
        }

        string skeleton = CodePointsToString(GetSkeletonCodePoints(codePoints.ToArray()));
        return UnicodeNormalization.Instance.Normalize(skeleton, UnicodeNormalizationForm.Nfd);
    }

    /// <summary>
    /// Reports whether two strings have the same normalized UTS #39 confusable skeleton.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    public bool AreConfusable(string left, string right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        if (string.Equals(left, right, System.StringComparison.Ordinal))
        {
            return false;
        }

        string leftSkeleton = GetSkeleton(left);
        string rightSkeleton = GetSkeleton(right);
        return string.Equals(leftSkeleton, rightSkeleton, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Reports whether two confusable strings have disjoint resolved script sets.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the strings are UTS #39 mixed-script confusables; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    public bool AreMixedScriptConfusable(string left, string right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        if (!AreConfusable(left, right))
        {
            return false;
        }

        ulong leftScripts = GetResolvedScriptSet(left);
        ulong rightScripts = GetResolvedScriptSet(right);
        return (leftScripts & rightScripts) == 0;
    }

    /// <summary>
    /// Reports whether two strings are mixed-script confusables and each string is single-script.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the strings are UTS #39 whole-script confusables; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    public bool AreWholeScriptConfusable(string left, string right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        if (!AreConfusable(left, right))
        {
            return false;
        }

        ulong leftScripts = GetResolvedScriptSet(left);
        ulong rightScripts = GetResolvedScriptSet(right);
        if (leftScripts == 0 || rightScripts == 0)
        {
            return false;
        }

        return (leftScripts & rightScripts) == 0;
    }

    /// <summary>
    /// Returns confusable flags derived from comparing two distinct strings.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns>The confusable flags produced by the comparison.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    public UnicodeSecurityFlag GetConfusableFlags(string left, string right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        if (!AreConfusable(left, right))
        {
            return UnicodeSecurityFlag.None;
        }

        UnicodeSecurityFlag flags = UnicodeSecurityFlag.Confusable;
        ulong leftScripts = GetResolvedScriptSet(left);
        ulong rightScripts = GetResolvedScriptSet(right);
        if (leftScripts != 0 && rightScripts != 0 && (leftScripts & rightScripts) == 0)
        {
            flags = flags | UnicodeSecurityFlag.WholeScriptConfusable;
        }

        return flags;
    }

    /// <summary>
    /// Returns the UTS #39 Identifier_Status value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The Identifier_Status classification.</returns>
    public UnicodeIdentifierSecurityStatus GetIdentifierStatus(int codePoint)
    {
        if (UnicodeRangeSet.ContainsRanges(UnicodeSecurityTables.AllowedIdentifierStatus(), codePoint))
        {
            return UnicodeIdentifierSecurityStatus.Allowed;
        }

        return UnicodeIdentifierSecurityStatus.Restricted;
    }

    /// <summary>
    /// Returns every UTS #39 Identifier_Type value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the Identifier_Type values.</returns>
    public UnicodeIdentifierType[] GetIdentifierTypes(int codePoint)
    {
        int[] starts = UnicodeSecurityTables.IdentifierTypeStarts();
        int[] ends = UnicodeSecurityTables.IdentifierTypeEnds();
        int[] offsets = UnicodeSecurityTables.IdentifierTypeOffsets();
        int[] lengths = UnicodeSecurityTables.IdentifierTypeLengths();
        int[] indexes = UnicodeSecurityTables.IdentifierTypeIndexes();
        string[] names = UnicodeSecurityTables.IdentifierTypeNames();
        int rangeIndex = 0;
        while (rangeIndex < starts.Length)
        {
            if (codePoint >= starts[rangeIndex] && codePoint <= ends[rangeIndex])
            {
                UnicodeIdentifierType[] types = new UnicodeIdentifierType[lengths[rangeIndex]];
                int typeIndex = 0;
                while (typeIndex < types.Length)
                {
                    string name = names[indexes[offsets[rangeIndex] + typeIndex]];
                    types[typeIndex] = IdentifierTypeFromName(name);
                    typeIndex = typeIndex + 1;
                }

                return types;
            }

            rangeIndex = rangeIndex + 1;
        }

        return new UnicodeIdentifierType[]
        {
            UnicodeIdentifierType.NotCharacter,
        };
    }

    /// <summary>
    /// Reports whether nonempty text is permitted by the UTS #39 General Security Profile, including canonical equivalents.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <returns><c>true</c> when the text is allowed; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsAllowedByGeneralSecurityProfile(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (AreScalarsAllowedByIdentifierStatus(text))
        {
            return true;
        }

        return AreCanonicalSegmentsAllowed(text);
    }

    /// <summary>
    /// Returns distinct significant scripts, excluding Common and Inherited.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeScript[] GetScriptSet(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<UnicodeScript> scripts = new List<UnicodeScript>();
        int scalarIndex = 0;

        while (scalarIndex < scalars.Length)
        {
            UnicodeScript[] extensions = UnicodeProperties.Instance.GetScriptExtensions(scalars[scalarIndex].CodePoint);
            int extensionIndex = 0;

            while (extensionIndex < extensions.Length)
            {
                if (!IsSecurityScriptIgnored(extensions[extensionIndex]))
                {
                    if (!ContainsScript(scripts, extensions[extensionIndex]))
                    {
                        scripts.Add(extensions[extensionIndex]);
                    }
                }

                extensionIndex = extensionIndex + 1;
            }

            scalarIndex = scalarIndex + 1;
        }

        return scripts.ToArray();
    }

    /// <summary>
    /// Reports whether text uses more than one significant script.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsMixedScript(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        List<ulong> scriptSets = GetScriptSetOfScriptSets(text);
        if (scriptSets.Count == 0)
        {
            return false;
        }

        return IntersectScriptSets(scriptSets) == 0;
    }

    /// <summary>
    /// Classifies text with the UTS #39 restriction-level rules.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting classification or status.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeRestrictionLevel GetRestrictionLevel(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return UnicodeRestrictionLevel.Unrestricted;
        }

        if (!IsAllowedByGeneralSecurityProfile(text))
        {
            return UnicodeRestrictionLevel.Unrestricted;
        }

        if (IsAsciiOnly(text))
        {
            return UnicodeRestrictionLevel.Ascii;
        }

        List<ulong> scriptSets = GetScriptSetOfScriptSets(text);
        if (scriptSets.Count == 0)
        {
            return UnicodeRestrictionLevel.SingleScriptRestrictive;
        }

        if (IntersectScriptSets(scriptSets) != 0)
        {
            return UnicodeRestrictionLevel.SingleScriptRestrictive;
        }

        List<ulong> nonLatinSets = RemoveLatinScriptSets(scriptSets);
        if (IsCoveredByScript(nonLatinSets, KoreanScriptBit))
        {
            return UnicodeRestrictionLevel.HighlyRestrictive;
        }

        if (IsCoveredByScript(nonLatinSets, HanBopomofoScriptBit))
        {
            return UnicodeRestrictionLevel.HighlyRestrictive;
        }

        if (IsCoveredByScript(nonLatinSets, JapaneseScriptBit))
        {
            return UnicodeRestrictionLevel.HighlyRestrictive;
        }

        if (HasModeratelyRestrictiveScript(nonLatinSets))
        {
            return UnicodeRestrictionLevel.ModeratelyRestrictive;
        }

        return UnicodeRestrictionLevel.MinimallyRestrictive;
    }

    /// <summary>
    /// Reports invisible and control scalars together with identifier bidi-safety violations.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSecurityDiagnostic[] GetSecurityDiagnostics(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<UnicodeSecurityDiagnostic> diagnostics = new List<UnicodeSecurityDiagnostic>();
        int index = 0;

        while (index < scalars.Length)
        {
            AddSecurityDiagnostics(scalars[index], diagnostics);
            index = index + 1;
        }

        AddRepeatedMarkDiagnostics(scalars, diagnostics);

        UnicodeSecurityDiagnostic[] bidiDiagnostics = GetIdentifierBidiDiagnostics(text);
        index = 0;
        while (index < bidiDiagnostics.Length)
        {
            diagnostics.Add(bidiDiagnostics[index]);
            index = index + 1;
        }

        SortSecurityDiagnostics(diagnostics);
        return diagnostics.ToArray();
    }

    /// <summary>
    /// Reports RFC 5893 Bidi Rule violations when an identifier contains right-to-left text.
    /// </summary>
    /// <param name="text">The UTF-16 identifier to inspect.</param>
    /// <returns>A new array containing identifier bidi-safety diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSecurityDiagnostic[] GetIdentifierBidiDiagnostics(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return GetBidiSafetyDiagnostics(text, false, IdentifierBidiRule, "Identifier");
    }

    /// <summary>
    /// Reports violations of the six RFC 5893 Bidi Rule conditions for a label.
    /// </summary>
    /// <param name="label">The UTF-16 label to inspect.</param>
    /// <returns>A new array containing label bidi-safety diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    public UnicodeSecurityDiagnostic[] GetLabelBidiDiagnostics(string label)
    {
        System.ArgumentNullException.ThrowIfNull(label);

        return GetBidiSafetyDiagnostics(label, true, LabelBidiRule, "Label");
    }

    /// <summary>
    /// Runs behavioral identifier-profile validation and Unicode security checks as one operation.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    public UnicodeSecurityCheckResult CheckIdentifier(string text, IUnicodeIdentifierProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(profile);

        UnicodeIdentifierValidationResult identifierValidation = UnicodeIdentifiers.Instance.ValidateIdentifier(text, profile);
        UnicodeSecurityDiagnostic[] securityDiagnostics = GetSecurityDiagnostics(text);
        UnicodeScript[] scripts = GetScriptSet(text);
        UnicodeRestrictionLevel restrictionLevel = GetRestrictionLevel(text);
        string skeleton = GetSkeleton(text);
        bool mixedScript = IsMixedScript(text);
        UnicodeSecurityFlag flags = AggregateSecurityFlags(securityDiagnostics, mixedScript, restrictionLevel);
        bool isSafe = identifierValidation.IsValid && flags == UnicodeSecurityFlag.None;

        return new UnicodeSecurityCheckResult(
            isSafe,
            flags,
            restrictionLevel,
            skeleton,
            scripts,
            identifierValidation,
            securityDiagnostics);
    }

    /// <summary>
    /// Runs identifier checks and compares the identifier with a protected reference string.
    /// </summary>
    /// <param name="text">The UTF-16 identifier to process.</param>
    /// <param name="reference">The distinct protected string to compare.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>An identifier result whose confusable flags reflect the comparison.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>reference</c>, <c>profile</c> is <c>null</c>.</exception>
    public UnicodeSecurityCheckResult CheckIdentifierAgainst(
        string text,
        string reference,
        IUnicodeIdentifierProfile profile)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(reference);
        System.ArgumentNullException.ThrowIfNull(profile);

        UnicodeSecurityCheckResult result = CheckIdentifier(text, profile);
        UnicodeSecurityFlag flags = result.Flags | GetConfusableFlags(text, reference);
        return new UnicodeSecurityCheckResult(
            result.IdentifierValidation.IsValid && flags == UnicodeSecurityFlag.None,
            flags,
            result.RestrictionLevel,
            result.Skeleton,
            result.Scripts,
            result.IdentifierValidation,
            result.SecurityDiagnostics);
    }

    internal static UnicodeSecurityFlag AggregateSecurityFlags(
        UnicodeSecurityDiagnostic[] diagnostics,
        bool mixedScript,
        UnicodeRestrictionLevel restrictionLevel)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        UnicodeSecurityFlag flags = UnicodeSecurityFlag.None;
        if (mixedScript)
        {
            flags = flags | UnicodeSecurityFlag.MixedScript;
        }

        if (restrictionLevel == UnicodeRestrictionLevel.Unrestricted)
        {
            flags = flags | UnicodeSecurityFlag.Restricted;
        }

        int index = 0;
        while (index < diagnostics.Length)
        {
            flags = flags | diagnostics[index].Flag;
            index = index + 1;
        }

        return flags;
    }

    internal static bool HasSecurityFlag(UnicodeSecurityFlag flags, UnicodeSecurityFlag flag)
    {
        return (flags & flag) == flag;
    }

    internal static void AddSecurityDiagnostics(UnicodeScalarRecord scalar, List<UnicodeSecurityDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalar);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        if (IsInvisible(scalar.CodePoint))
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodeSecurityFlag.Invisible,
                InvisibleRule,
                "Scalar has invisible display behavior."));
        }

        if (IsDefaultIgnorable(scalar.CodePoint))
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodeSecurityFlag.DefaultIgnorable,
                DefaultIgnorableRule,
                "Scalar has the Default_Ignorable_Code_Point property."));
        }

        if (IsVariationSelector(scalar.CodePoint))
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodeSecurityFlag.VariationSelector,
                VariationSelectorRule,
                "Scalar is a variation selector."));
        }

        if (IsJoinControl(scalar.CodePoint))
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodeSecurityFlag.JoinControl,
                JoinControlRule,
                "Scalar is a join control."));
        }

        if (IsBidiControl(scalar.CodePoint))
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalar.Utf16Index,
                scalar.Utf16Length,
                scalar.CodePoint,
                UnicodeSecurityFlag.BidiControl,
                BidiControlRule,
                "Scalar is a bidirectional control."));
        }
    }

    internal static void AddRepeatedMarkDiagnostics(
        UnicodeScalarRecord[] scalars,
        List<UnicodeSecurityDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        List<int> marks = new List<int>();
        int index = 0;
        while (index < scalars.Length)
        {
            int[] decomposed = UnicodeNormalization.DecomposeCodePointCanonical(scalars[index].CodePoint);
            bool repeated = false;
            int decomposedIndex = 0;
            while (decomposedIndex < decomposed.Length)
            {
                UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(decomposed[decomposedIndex]);
                bool isMark = category == UnicodeGeneralCategory.NonspacingMark;
                if (category == UnicodeGeneralCategory.EnclosingMark)
                {
                    isMark = true;
                }

                if (!isMark)
                {
                    marks.Clear();
                    decomposedIndex = decomposedIndex + 1;
                    continue;
                }

                if (ContainsCodePoint(marks, decomposed[decomposedIndex]))
                {
                    repeated = true;
                }
                else
                {
                    marks.Add(decomposed[decomposedIndex]);
                }

                decomposedIndex = decomposedIndex + 1;
            }

            if (repeated)
            {
                diagnostics.Add(new UnicodeSecurityDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    UnicodeSecurityFlag.Invisible,
                    InvisibleRule,
                    "Combining-mark sequence repeats the same nonspacing mark."));
            }

            index = index + 1;
        }
    }

    internal static bool ContainsCodePoint(List<int> codePoints, int codePoint)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int index = 0;
        while (index < codePoints.Count)
        {
            if (codePoints[index] == codePoint)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static UnicodeSecurityDiagnostic[] GetBidiSafetyDiagnostics(
        string text,
        bool enforceLeftToRight,
        string rule,
        string subject)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(rule);
        System.ArgumentNullException.ThrowIfNull(subject);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<UnicodeSecurityDiagnostic> diagnostics = new List<UnicodeSecurityDiagnostic>();
        if (scalars.Length == 0)
        {
            if (enforceLeftToRight)
            {
                diagnostics.Add(new UnicodeSecurityDiagnostic(
                    0,
                    0,
                    -1,
                    UnicodeSecurityFlag.BidiUnsafe,
                    rule,
                    "Label must begin with a left-to-right, right-to-left, or Arabic letter."));
            }

            return diagnostics.ToArray();
        }

        bool hasRightToLeft = false;
        int index = 0;
        while (index < scalars.Length)
        {
            UnicodeBidiClass bidiClass = UnicodeProperties.Instance.GetBidiClass(scalars[index].CodePoint);
            if (IsRightToLeftBidiClass(bidiClass))
            {
                hasRightToLeft = true;
            }

            index = index + 1;
        }

        if (!enforceLeftToRight && !hasRightToLeft)
        {
            return diagnostics.ToArray();
        }

        bool rightToLeft = hasRightToLeft;
        UnicodeBidiClass firstClass = UnicodeProperties.Instance.GetBidiClass(scalars[0].CodePoint);
        bool validFirst = firstClass == UnicodeBidiClass.LeftToRight;
        if (rightToLeft)
        {
            validFirst = firstClass == UnicodeBidiClass.RightToLeft || firstClass == UnicodeBidiClass.ArabicLetter;
        }

        if (!validFirst)
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalars[0].Utf16Index,
                scalars[0].Utf16Length,
                scalars[0].CodePoint,
                UnicodeSecurityFlag.BidiUnsafe,
                rule,
                subject + " begins with a bidi class that is unsafe for its direction."));
        }

        int europeanNumberIndex = -1;
        int arabicNumberIndex = -1;
        index = 0;
        while (index < scalars.Length)
        {
            UnicodeBidiClass bidiClass = UnicodeProperties.Instance.GetBidiClass(scalars[index].CodePoint);
            bool allowed = IsLeftToRightLabelBidiClass(bidiClass);
            if (rightToLeft)
            {
                allowed = IsRightToLeftLabelBidiClass(bidiClass);
            }

            if (!allowed)
            {
                diagnostics.Add(new UnicodeSecurityDiagnostic(
                    scalars[index].Utf16Index,
                    scalars[index].Utf16Length,
                    scalars[index].CodePoint,
                    UnicodeSecurityFlag.BidiUnsafe,
                    rule,
                    subject + " contains a bidi class that is unsafe for its direction."));
            }

            if (bidiClass == UnicodeBidiClass.EuropeanNumber && europeanNumberIndex < 0)
            {
                europeanNumberIndex = index;
            }

            if (bidiClass == UnicodeBidiClass.ArabicNumber && arabicNumberIndex < 0)
            {
                arabicNumberIndex = index;
            }

            index = index + 1;
        }

        int endIndex = scalars.Length - 1;
        while (endIndex > 0)
        {
            UnicodeBidiClass endClass = UnicodeProperties.Instance.GetBidiClass(scalars[endIndex].CodePoint);
            if (endClass != UnicodeBidiClass.NonspacingMark)
            {
                break;
            }

            endIndex = endIndex - 1;
        }

        UnicodeBidiClass finalClass = UnicodeProperties.Instance.GetBidiClass(scalars[endIndex].CodePoint);
        bool validEnd = IsLeftToRightLabelEndClass(finalClass);
        if (rightToLeft)
        {
            validEnd = IsRightToLeftLabelEndClass(finalClass);
        }

        if (!validEnd)
        {
            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalars[endIndex].Utf16Index,
                scalars[endIndex].Utf16Length,
                scalars[endIndex].CodePoint,
                UnicodeSecurityFlag.BidiUnsafe,
                rule,
                subject + " ends with a bidi class that is unsafe for its direction."));
        }

        if (rightToLeft && europeanNumberIndex >= 0 && arabicNumberIndex >= 0)
        {
            int conflictingIndex = arabicNumberIndex;
            if (europeanNumberIndex > arabicNumberIndex)
            {
                conflictingIndex = europeanNumberIndex;
            }

            diagnostics.Add(new UnicodeSecurityDiagnostic(
                scalars[conflictingIndex].Utf16Index,
                scalars[conflictingIndex].Utf16Length,
                scalars[conflictingIndex].CodePoint,
                UnicodeSecurityFlag.BidiUnsafe,
                rule,
                subject + " mixes European and Arabic number bidi classes."));
        }

        return diagnostics.ToArray();
    }

    internal static void SortSecurityDiagnostics(List<UnicodeSecurityDiagnostic> diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        int index = 1;
        while (index < diagnostics.Count)
        {
            UnicodeSecurityDiagnostic diagnostic = diagnostics[index];
            int destination = index;
            while (destination > 0 && diagnostics[destination - 1].Utf16Index > diagnostic.Utf16Index)
            {
                diagnostics[destination] = diagnostics[destination - 1];
                destination = destination - 1;
            }

            diagnostics[destination] = diagnostic;
            index = index + 1;
        }
    }

    internal static bool IsRightToLeftBidiClass(UnicodeBidiClass bidiClass)
    {
        if (bidiClass == UnicodeBidiClass.RightToLeft)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.ArabicLetter)
        {
            return true;
        }

        return bidiClass == UnicodeBidiClass.ArabicNumber;
    }

    internal static bool IsRightToLeftLabelBidiClass(UnicodeBidiClass bidiClass)
    {
        if (IsRightToLeftBidiClass(bidiClass))
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanNumber)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanSeparator)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanTerminator)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.CommonSeparator)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.OtherNeutral)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.BoundaryNeutral)
        {
            return true;
        }

        return bidiClass == UnicodeBidiClass.NonspacingMark;
    }

    internal static bool IsLeftToRightLabelBidiClass(UnicodeBidiClass bidiClass)
    {
        if (bidiClass == UnicodeBidiClass.LeftToRight)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanNumber)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanSeparator)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanTerminator)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.CommonSeparator)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.OtherNeutral)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.BoundaryNeutral)
        {
            return true;
        }

        return bidiClass == UnicodeBidiClass.NonspacingMark;
    }

    internal static bool IsRightToLeftLabelEndClass(UnicodeBidiClass bidiClass)
    {
        if (bidiClass == UnicodeBidiClass.RightToLeft)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.ArabicLetter)
        {
            return true;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanNumber)
        {
            return true;
        }

        return bidiClass == UnicodeBidiClass.ArabicNumber;
    }

    internal static bool IsLeftToRightLabelEndClass(UnicodeBidiClass bidiClass)
    {
        if (bidiClass == UnicodeBidiClass.LeftToRight)
        {
            return true;
        }

        return bidiClass == UnicodeBidiClass.EuropeanNumber;
    }

    internal static int[] GetSkeletonCodePoints(int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int[] current = CopyCodePoints(codePoints);
        int iteration = 0;
        while (iteration < 32)
        {
            int[] next = GetSkeletonCodePointsOnce(current);
            if (CodePointsEqual(current, next))
            {
                return next;
            }

            current = next;
            iteration = iteration + 1;
        }

        return current;
    }

    internal static int[] GetSkeletonCodePointsOnce(int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int[] sourceOffsets = UnicodeSecurityTables.ConfusableSourceOffsets();
        int[] sourceLengths = UnicodeSecurityTables.ConfusableSourceLengths();
        int[] sourceCodePoints = UnicodeSecurityTables.ConfusableSourceCodePoints();
        int[] targetOffsets = UnicodeSecurityTables.ConfusableTargetOffsets();
        int[] targetLengths = UnicodeSecurityTables.ConfusableTargetLengths();
        int[] targetCodePoints = UnicodeSecurityTables.ConfusableTargetCodePoints();
        List<int> skeleton = new List<int>();
        int index = 0;

        while (index < codePoints.Length)
        {
            int mappingIndex = FindConfusableMapping(codePoints, index, sourceOffsets, sourceLengths, sourceCodePoints);
            if (mappingIndex >= 0)
            {
                AddConfusableTarget(skeleton, mappingIndex, targetOffsets, targetLengths, targetCodePoints);
                index = index + sourceLengths[mappingIndex];
                continue;
            }

            skeleton.Add(codePoints[index]);
            index = index + 1;
        }

        return skeleton.ToArray();
    }

    internal static int[] CopyCodePoints(int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int[] copy = new int[codePoints.Length];
        int index = 0;
        while (index < codePoints.Length)
        {
            copy[index] = codePoints[index];
            index = index + 1;
        }

        return copy;
    }

    internal static bool CodePointsEqual(int[] left, int[] right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int FindConfusableMapping(
        int[] codePoints,
        int codePointIndex,
        int[] sourceOffsets,
        int[] sourceLengths,
        int[] sourceCodePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(sourceOffsets);
        System.ArgumentNullException.ThrowIfNull(sourceLengths);
        System.ArgumentNullException.ThrowIfNull(sourceCodePoints);

        int bestIndex = -1;
        int bestLength = 0;
        int mappingIndex = 0;

        while (mappingIndex < sourceLengths.Length)
        {
            int length = sourceLengths[mappingIndex];
            if (length > bestLength)
            {
                if (ConfusableSourceMatches(codePoints, codePointIndex, sourceOffsets[mappingIndex], length, sourceCodePoints))
                {
                    bestIndex = mappingIndex;
                    bestLength = length;
                }
            }

            mappingIndex = mappingIndex + 1;
        }

        return bestIndex;
    }

    internal static bool ConfusableSourceMatches(
        int[] codePoints,
        int codePointIndex,
        int sourceOffset,
        int sourceLength,
        int[] sourceCodePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(sourceCodePoints);

        if (codePointIndex < 0)
        {
            return false;
        }

        if (sourceOffset < 0)
        {
            return false;
        }

        if (sourceLength < 0)
        {
            return false;
        }

        if (codePointIndex > codePoints.Length)
        {
            return false;
        }

        if (sourceOffset > sourceCodePoints.Length)
        {
            return false;
        }

        if (sourceLength > codePoints.Length - codePointIndex)
        {
            return false;
        }

        if (sourceLength > sourceCodePoints.Length - sourceOffset)
        {
            return false;
        }

        int index = 0;
        while (index < sourceLength)
        {
            if (codePoints[codePointIndex + index] != sourceCodePoints[sourceOffset + index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void AddConfusableTarget(
        List<int> destination,
        int mappingIndex,
        int[] targetOffsets,
        int[] targetLengths,
        int[] targetCodePoints)
    {
        System.ArgumentNullException.ThrowIfNull(destination);
        System.ArgumentNullException.ThrowIfNull(targetOffsets);
        System.ArgumentNullException.ThrowIfNull(targetLengths);
        System.ArgumentNullException.ThrowIfNull(targetCodePoints);

        int offset = targetOffsets[mappingIndex];
        int length = targetLengths[mappingIndex];
        int index = 0;

        while (index < length)
        {
            destination.Add(targetCodePoints[offset + index]);
            index = index + 1;
        }
    }

    internal static bool ContainsScript(List<UnicodeScript> scripts, UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(scripts);

        int index = 0;
        while (index < scripts.Count)
        {
            if (scripts[index] == script)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsScript(UnicodeScript[] scripts, UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(scripts);

        int index = 0;
        while (index < scripts.Length)
        {
            if (scripts[index] == script)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsSecurityScriptIgnored(UnicodeScript script)
    {
        if (script == UnicodeScript.Common)
        {
            return true;
        }

        if (script == UnicodeScript.Inherited)
        {
            return true;
        }

        return script == UnicodeScript.Unknown;
    }

    internal static UnicodeIdentifierType IdentifierTypeFromName(string name)
    {
        if (name == "Deprecated")
        {
            return UnicodeIdentifierType.Deprecated;
        }

        if (name == "Default_Ignorable")
        {
            return UnicodeIdentifierType.DefaultIgnorable;
        }

        if (name == "Not_NFKC")
        {
            return UnicodeIdentifierType.NotNfkc;
        }

        if (name == "Not_XID")
        {
            return UnicodeIdentifierType.NotXid;
        }

        if (name == "Exclusion")
        {
            return UnicodeIdentifierType.Exclusion;
        }

        if (name == "Obsolete")
        {
            return UnicodeIdentifierType.Obsolete;
        }

        if (name == "Technical")
        {
            return UnicodeIdentifierType.Technical;
        }

        if (name == "Uncommon_Use")
        {
            return UnicodeIdentifierType.UncommonUse;
        }

        if (name == "Limited_Use")
        {
            return UnicodeIdentifierType.LimitedUse;
        }

        if (name == "Inclusion")
        {
            return UnicodeIdentifierType.Inclusion;
        }

        if (name == "Recommended")
        {
            return UnicodeIdentifierType.Recommended;
        }

        return UnicodeIdentifierType.NotCharacter;
    }

    internal static bool AreScalarsAllowedByIdentifierStatus(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        if (scalars.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                return false;
            }

            if (Instance.GetIdentifierStatus(scalars[index].CodePoint) != UnicodeIdentifierSecurityStatus.Allowed)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreCanonicalSegmentsAllowed(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        string decomposed = UnicodeNormalization.Instance.Normalize(text, UnicodeNormalizationForm.Nfd);
        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(decomposed);
        if (scalars.Length == 0)
        {
            return false;
        }

        int segmentStart = 0;
        int index = 1;
        while (index < scalars.Length)
        {
            if (UnicodeProperties.Instance.GetCombiningClass(scalars[index].CodePoint) == 0)
            {
                if (!IsCanonicalSegmentAllowed(decomposed, segmentStart, scalars[index].Utf16Index))
                {
                    return false;
                }

                segmentStart = scalars[index].Utf16Index;
            }

            index = index + 1;
        }

        return IsCanonicalSegmentAllowed(decomposed, segmentStart, decomposed.Length);
    }

    internal static bool IsCanonicalSegmentAllowed(string decomposed, int startUtf16Index, int endUtf16Index)
    {
        System.ArgumentNullException.ThrowIfNull(decomposed);

        string segment = decomposed.Substring(startUtf16Index, endUtf16Index - startUtf16Index);
        if (AreScalarsAllowedByIdentifierStatus(segment))
        {
            return true;
        }

        string composed = UnicodeNormalization.Instance.Normalize(segment, UnicodeNormalizationForm.Nfc);
        return AreScalarsAllowedByIdentifierStatus(composed);
    }

    internal static List<ulong> GetScriptSetOfScriptSets(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<ulong> scriptSets = new List<ulong>();
        int scalarIndex = 0;
        while (scalarIndex < scalars.Length)
        {
            ulong scriptSet = GetAugmentedScriptSet(scalars[scalarIndex].CodePoint);
            if (scriptSet != AllScripts)
            {
                if (!ContainsInteger(scriptSets, scriptSet))
                {
                    scriptSets.Add(scriptSet);
                }
            }

            scalarIndex = scalarIndex + 1;
        }

        return scriptSets;
    }

    internal static ulong GetResolvedScriptSet(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return IntersectScriptSets(GetScriptSetOfScriptSets(text));
    }

    internal static ulong GetAugmentedScriptSet(int codePoint)
    {
        UnicodeScript[] scripts = UnicodeProperties.Instance.GetScriptExtensions(codePoint);
        ulong scriptSet = 0;
        int index = 0;
        while (index < scripts.Length)
        {
            UnicodeScript script = scripts[index];
            if (script == UnicodeScript.Common || script == UnicodeScript.Inherited)
            {
                return AllScripts;
            }

            scriptSet = scriptSet | (1UL << (int)script);
            if (script == UnicodeScript.Han)
            {
                scriptSet = scriptSet | HanBopomofoScriptBit | JapaneseScriptBit | KoreanScriptBit;
            }

            if (script == UnicodeScript.Hiragana || script == UnicodeScript.Katakana)
            {
                scriptSet = scriptSet | JapaneseScriptBit;
            }

            if (script == UnicodeScript.Hangul)
            {
                scriptSet = scriptSet | KoreanScriptBit;
            }

            index = index + 1;
        }

        return scriptSet;
    }

    internal static ulong IntersectScriptSets(List<ulong> scriptSets)
    {
        System.ArgumentNullException.ThrowIfNull(scriptSets);

        ulong intersection = AllScripts;
        int index = 0;
        while (index < scriptSets.Count)
        {
            intersection = intersection & scriptSets[index];
            index = index + 1;
        }

        return intersection;
    }

    internal static List<ulong> RemoveLatinScriptSets(List<ulong> scriptSets)
    {
        System.ArgumentNullException.ThrowIfNull(scriptSets);

        ulong latinBit = 1UL << (int)UnicodeScript.Latin;
        List<ulong> result = new List<ulong>();
        int index = 0;
        while (index < scriptSets.Count)
        {
            if ((scriptSets[index] & latinBit) == 0)
            {
                result.Add(scriptSets[index]);
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool IsCoveredByScript(List<ulong> scriptSets, ulong scriptBit)
    {
        System.ArgumentNullException.ThrowIfNull(scriptSets);

        if (scriptSets.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < scriptSets.Count)
        {
            if ((scriptSets[index] & scriptBit) == 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasModeratelyRestrictiveScript(List<ulong> scriptSets)
    {
        System.ArgumentNullException.ThrowIfNull(scriptSets);

        ulong intersection = IntersectScriptSets(scriptSets);
        ulong recommended = (1UL << (int)UnicodeScript.Hebrew)
            | (1UL << (int)UnicodeScript.Arabic)
            | (1UL << (int)UnicodeScript.Devanagari)
            | (1UL << (int)UnicodeScript.Hiragana)
            | (1UL << (int)UnicodeScript.Katakana)
            | (1UL << (int)UnicodeScript.Han)
            | (1UL << (int)UnicodeScript.Hangul)
            | (1UL << (int)UnicodeScript.Armenian)
            | (1UL << (int)UnicodeScript.Bengali)
            | (1UL << (int)UnicodeScript.Ethiopic)
            | (1UL << (int)UnicodeScript.Georgian)
            | (1UL << (int)UnicodeScript.Grantha)
            | (1UL << (int)UnicodeScript.Gujarati)
            | (1UL << (int)UnicodeScript.Gurmukhi)
            | (1UL << (int)UnicodeScript.Kannada)
            | (1UL << (int)UnicodeScript.Khmer)
            | (1UL << (int)UnicodeScript.Lao)
            | (1UL << (int)UnicodeScript.Malayalam)
            | (1UL << (int)UnicodeScript.Myanmar)
            | (1UL << (int)UnicodeScript.Oriya)
            | (1UL << (int)UnicodeScript.Sinhala)
            | (1UL << (int)UnicodeScript.Tamil)
            | (1UL << (int)UnicodeScript.Telugu)
            | (1UL << (int)UnicodeScript.Thaana)
            | (1UL << (int)UnicodeScript.Thai)
            | (1UL << (int)UnicodeScript.Tibetan);
        return (intersection & recommended) != 0;
    }

    internal static bool ContainsInteger(List<ulong> values, ulong value)
    {
        System.ArgumentNullException.ThrowIfNull(values);

        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsAllowedByIdentifierStatus(string text)
    {
        return Instance.IsAllowedByGeneralSecurityProfile(text);
    }

    internal static bool IsAsciiOnly(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        if (scalars.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].CodePoint > 0x7F)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsInvisible(int codePoint)
    {
        if (IsDefaultIgnorable(codePoint))
        {
            return true;
        }

        if (IsVariationSelector(codePoint))
        {
            return true;
        }

        if (IsJoinControl(codePoint))
        {
            return true;
        }

        if (IsBidiControl(codePoint))
        {
            return true;
        }

        if (codePoint == 0x200B)
        {
            return true;
        }

        if (codePoint == 0x2060)
        {
            return true;
        }

        return codePoint == 0xFEFF;
    }

    internal static bool IsDefaultIgnorable(int codePoint)
    {
        return UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.DefaultIgnorableCodePoint);
    }

    internal static bool IsVariationSelector(int codePoint)
    {
        if (codePoint >= 0x180B)
        {
            if (codePoint <= 0x180D)
            {
                return true;
            }
        }

        if (codePoint == 0x180F)
        {
            return true;
        }

        if (codePoint >= 0xFE00)
        {
            if (codePoint <= 0xFE0F)
            {
                return true;
            }
        }

        if (codePoint >= 0xE0100)
        {
            return codePoint <= 0xE01EF;
        }

        return false;
    }

    internal static bool IsJoinControl(int codePoint)
    {
        if (codePoint == 0x200C)
        {
            return true;
        }

        return codePoint == 0x200D;
    }

    internal static bool IsBidiControl(int codePoint)
    {
        if (codePoint == 0x061C)
        {
            return true;
        }

        if (codePoint == 0x200E)
        {
            return true;
        }

        if (codePoint == 0x200F)
        {
            return true;
        }

        if (codePoint >= 0x202A)
        {
            if (codePoint <= 0x202E)
            {
                return true;
            }
        }

        if (codePoint >= 0x2066)
        {
            return codePoint <= 0x2069;
        }

        return false;
    }

    internal static string CodePointsToString(int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < codePoints.Length)
        {
            builder.Append(UnicodeScalar.Instance.ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }
}
