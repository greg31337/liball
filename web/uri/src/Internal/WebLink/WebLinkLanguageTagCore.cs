using System.Collections.Generic;

namespace uri;

internal static class WebLinkLanguageTagCore
{
    internal const string InvalidLanguageTagMessage = "The Web Link language tag is not well-formed RFC 5646 syntax.";
    internal const string RecoveredInvalidLanguageTagMessage = "Recovery retained but did not apply an invalid Web Link language tag.";

    internal static readonly string[] GrandfatheredTags = new string[]
    {
        "en-GB-oed",
        "i-ami",
        "i-bnn",
        "i-default",
        "i-enochian",
        "i-hak",
        "i-klingon",
        "i-lux",
        "i-mingo",
        "i-navajo",
        "i-pwn",
        "i-tao",
        "i-tay",
        "i-tsu",
        "sgn-BE-FR",
        "sgn-BE-NL",
        "sgn-CH-DE",
        "art-lojban",
        "cel-gaulish",
        "no-bok",
        "no-nyn",
        "zh-guoyu",
        "zh-hakka",
        "zh-min",
        "zh-min-nan",
        "zh-xiang"
    };

    internal static WebLinkLanguageTagValidationResult ValidateTag(
        string tag)
    {
        if (tag.Length == 0)
        {
            return CreateInvalidResult(0, 0);
        }

        int maximumSubtags = tag.Length / 2 + 1;
        int[] starts = new int[maximumSubtags];
        int[] lengths = new int[maximumSubtags];
        int count = 0;
        int subtagStart = 0;
        int offset = 0;
        while (offset < tag.Length)
        {
            char value = tag[offset];
            if (value == '-')
            {
                if (offset == subtagStart)
                {
                    return CreateInvalidResult(offset, 1);
                }

                starts[count] = subtagStart;
                lengths[count] = offset - subtagStart;
                count = count + 1;
                subtagStart = offset + 1;
            }
            else if (!Ascii.IsAlphaNumeric(value))
            {
                return CreateInvalidResult(offset, 1);
            }

            offset = offset + 1;
        }

        if (subtagStart == tag.Length)
        {
            return CreateInvalidResult(tag.Length, 0);
        }

        starts[count] = subtagStart;
        lengths[count] = tag.Length - subtagStart;
        count = count + 1;
        if (IsGrandfatheredTag(tag))
        {
            return CreateValidResult();
        }

        if (IsPrivateUseSingleton(tag, starts[0], lengths[0]))
        {
            return ValidatePrivateUseTag(tag, starts, lengths, count, 0);
        }

        return ValidateNormalTag(tag, starts, lengths, count);
    }

    internal static WebLinkLanguageTagValidationResult ValidateNormalTag(
        string tag,
        int[] starts,
        int[] lengths,
        int count)
    {
        int index = 0;
        int languageLength = lengths[0];
        if (!IsAlphaSubtag(tag, starts[0], languageLength) ||
            languageLength < 2 || languageLength > 8)
        {
            return CreateInvalidResult(starts[0], languageLength);
        }

        index = 1;
        if (languageLength <= 3)
        {
            int extlangCount = 0;
            while (index < count &&
                extlangCount < 3 &&
                lengths[index] == 3 &&
                IsAlphaSubtag(tag, starts[index], lengths[index]))
            {
                index = index + 1;
                extlangCount = extlangCount + 1;
            }
        }

        if (index < count &&
            lengths[index] == 4 &&
            IsAlphaSubtag(tag, starts[index], lengths[index]))
        {
            index = index + 1;
        }

        if (index < count &&
            (lengths[index] == 2 &&
                IsAlphaSubtag(tag, starts[index], lengths[index]) ||
             lengths[index] == 3 &&
                IsDigitSubtag(tag, starts[index], lengths[index])))
        {
            index = index + 1;
        }

        List<string> variants = new List<string>();
        while (index < count &&
            IsVariantSubtag(tag, starts[index], lengths[index]))
        {
            string variant = tag.Substring(starts[index], lengths[index]);
            if (ContainsIgnoreCase(variants, variant))
            {
                return CreateInvalidResult(starts[index], lengths[index]);
            }

            variants.Add(variant);
            index = index + 1;
        }

        bool[] singletons = new bool[36];
        while (index < count &&
            IsExtensionSingleton(tag, starts[index], lengths[index]))
        {
            int singleton = GetSingletonIndex(tag[starts[index]]);
            if (singletons[singleton])
            {
                return CreateInvalidResult(starts[index], lengths[index]);
            }

            singletons[singleton] = true;
            int singletonIndex = index;
            index = index + 1;
            int extensionCount = 0;
            while (index < count &&
                lengths[index] >= 2 && lengths[index] <= 8 &&
                IsAlphaNumericSubtag(
                    tag,
                    starts[index],
                    lengths[index]))
            {
                index = index + 1;
                extensionCount = extensionCount + 1;
            }

            if (extensionCount == 0)
            {
                return CreateInvalidResult(
                    starts[singletonIndex] + lengths[singletonIndex],
                    0);
            }
        }

        if (index < count &&
            IsPrivateUseSingleton(tag, starts[index], lengths[index]))
        {
            return ValidatePrivateUseTag(
                tag,
                starts,
                lengths,
                count,
                index);
        }

        if (index < count)
        {
            return CreateInvalidResult(starts[index], lengths[index]);
        }

        return CreateValidResult();
    }

    internal static WebLinkLanguageTagValidationResult ValidatePrivateUseTag(
        string tag,
        int[] starts,
        int[] lengths,
        int count,
        int privateUseIndex)
    {
        int index = privateUseIndex + 1;
        if (index == count)
        {
            return CreateInvalidResult(
                starts[privateUseIndex] + lengths[privateUseIndex],
                0);
        }

        while (index < count)
        {
            if (lengths[index] < 1 ||
                lengths[index] > 8 ||
                !IsAlphaNumericSubtag(
                    tag,
                    starts[index],
                    lengths[index]))
            {
                return CreateInvalidResult(starts[index], lengths[index]);
            }

            index = index + 1;
        }

        return CreateValidResult();
    }

    internal static UriDiagnostic? ValidateParameterLanguage(
        WebLinkParameter parameter,
        bool recovery)
    {
        WebLinkLanguageTagValidationResult validation = ValidateTag(
            parameter.Value);
        if (validation.Valid)
        {
            return null;
        }

        int offset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            validation.ErrorOffset);
        int endOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            validation.ErrorOffset + validation.ErrorLength);
        return CreateLanguageDiagnostic(
            recovery,
            offset,
            endOffset - offset);
    }

    internal static UriDiagnostic? ValidateExtendedLanguage(
        WebLinkParameter parameter,
        bool recovery)
    {
        if (parameter.Language.Length == 0)
        {
            return null;
        }

        WebLinkLanguageTagValidationResult validation = ValidateTag(
            parameter.Language);
        if (validation.Valid)
        {
            return null;
        }

        int languageOffset = parameter.CharacterSet.Length + 1;
        int offset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            languageOffset + validation.ErrorOffset);
        int endOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            languageOffset + validation.ErrorOffset +
                validation.ErrorLength);
        return CreateLanguageDiagnostic(
            recovery,
            offset,
            endOffset - offset);
    }

    internal static bool IsGrandfatheredTag(string tag)
    {
        int index = 0;
        while (index < GrandfatheredTags.Length)
        {
            if (AsciiCase.EqualsIgnoreCase(tag, GrandfatheredTags[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsAlphaSubtag(
        string tag,
        int start,
        int length)
    {
        int index = 0;
        while (index < length)
        {
            if (!Ascii.IsAlphabetic(tag[start + index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsDigitSubtag(
        string tag,
        int start,
        int length)
    {
        int index = 0;
        while (index < length)
        {
            if (!Ascii.IsDigit(tag[start + index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAlphaNumericSubtag(
        string tag,
        int start,
        int length)
    {
        int index = 0;
        while (index < length)
        {
            if (!Ascii.IsAlphaNumeric(tag[start + index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsVariantSubtag(
        string tag,
        int start,
        int length)
    {
        if (length >= 5 && length <= 8)
        {
            return IsAlphaNumericSubtag(tag, start, length);
        }

        return length == 4 &&
            Ascii.IsDigit(tag[start]) &&
            IsAlphaNumericSubtag(tag, start, length);
    }

    internal static bool IsExtensionSingleton(
        string tag,
        int start,
        int length)
    {
        return length == 1 &&
            Ascii.IsAlphaNumeric(tag[start]) &&
            AsciiCase.FoldToLower(tag[start]) != 'x';
    }

    internal static bool IsPrivateUseSingleton(
        string tag,
        int start,
        int length)
    {
        return length == 1 &&
            AsciiCase.FoldToLower(tag[start]) == 'x';
    }

    internal static bool ContainsIgnoreCase(
        List<string> values,
        string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (AsciiCase.EqualsIgnoreCase(values[index], value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int GetSingletonIndex(char value)
    {
        char folded = AsciiCase.FoldToLower(value);
        if (Ascii.IsDigit(folded))
        {
            return folded - '0';
        }

        return folded - 'a' + 10;
    }

    internal static UriDiagnostic CreateLanguageDiagnostic(
        bool recovery,
        int offset,
        int length)
    {
        if (recovery)
        {
            return DiagnosticFactory.CreateWarning(
                DiagnosticDomain.WebLink,
                UriDiagnosticCode.InvalidLanguageTag,
                UriComponent.LanguageTag,
                offset,
                length,
                RecoveredInvalidLanguageTagMessage);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidLanguageTag,
            UriComponent.LanguageTag,
            offset,
            length,
            InvalidLanguageTagMessage);
    }

    internal static WebLinkLanguageTagValidationResult CreateValidResult()
    {
        return new WebLinkLanguageTagValidationResult(true, 0, 0);
    }

    internal static WebLinkLanguageTagValidationResult CreateInvalidResult(
        int offset,
        int length)
    {
        return new WebLinkLanguageTagValidationResult(
            false,
            offset,
            length);
    }
}
