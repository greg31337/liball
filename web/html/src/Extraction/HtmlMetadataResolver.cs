using System;
using System.Collections.Generic;
using System.Text;

namespace html;

internal class HtmlMetadataCandidateResolution
{
    internal HtmlMetadataCandidate[] candidates;
    internal long duplicateCount;

    internal HtmlMetadataCandidateResolution(
        HtmlMetadataCandidate[] candidatesValue, long duplicateCountValue)
    {
        this.candidates = candidatesValue;
        this.duplicateCount = duplicateCountValue;
    }

    internal HtmlMetadataCandidate[] GetCandidates()
    {
        HtmlMetadataCandidate[] result =
            new HtmlMetadataCandidate[this.candidates.Length];
        Array.Copy(this.candidates, result, this.candidates.Length);
        return result;
    }

    internal long GetDuplicateCount()
    {
        return this.duplicateCount;
    }
}

internal static class HtmlMetadataResolver
{
    internal const int LowQualityAuthorPenalty = 50;

    internal static HtmlMetadataCandidateResolution Resolve(
        HtmlMetadataCandidate[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlMetadataCandidate[] normalized =
            new HtmlMetadataCandidate[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "Metadata candidates cannot contain null.", "values");
            }

            normalized[index] = NormalizeCandidate(values[index]);
            index = index + 1;
        }

        Array.Sort(normalized, HtmlMetadataCandidateComparer.Compare);
        HashSet<string>[] seen =
            new HashSet<string>[(int)HtmlMetadataField.ImageUrl + 1];
        List<HtmlMetadataCandidate> unique =
            new List<HtmlMetadataCandidate>(normalized.Length);
        long duplicateCount = 0;
        index = 0;
        while (index < normalized.Length)
        {
            HtmlMetadataCandidate candidate = normalized[index];
            int fieldIndex = (int)candidate.GetField();
            HashSet<string> fieldValues = seen[fieldIndex];
            if (fieldValues == null)
            {
                fieldValues = new HashSet<string>(StringComparer.Ordinal);
                seen[fieldIndex] = fieldValues;
            }

            if (fieldValues.Add(candidate.GetValue()))
            {
                unique.Add(candidate);
            }
            else if (duplicateCount < long.MaxValue)
            {
                duplicateCount = duplicateCount + 1;
            }

            index = index + 1;
        }

        return new HtmlMetadataCandidateResolution(unique.ToArray(),
            duplicateCount);
    }

    internal static HtmlMetadataCandidate NormalizeCandidate(
        HtmlMetadataCandidate candidate)
    {
        HtmlMetadataField field = candidate.GetField();
        string value = candidate.GetValue();
        switch (field)
        {
            case HtmlMetadataField.Title:
            case HtmlMetadataField.Description:
            case HtmlMetadataField.SiteName:
            case HtmlMetadataField.Author:
            case HtmlMetadataField.PublishedTime:
            case HtmlMetadataField.ModifiedTime:
                value = NormalizeText(value);
                break;
            case HtmlMetadataField.Language:
                value = NormalizeLanguage(value);
                break;
            case HtmlMetadataField.Direction:
                value = AsciiLower(value.Trim());
                break;
        }

        int precedence = candidate.GetPrecedence();
        if (field == HtmlMetadataField.Author && LooksLikeUrl(value))
        {
            precedence = precedence > int.MaxValue - LowQualityAuthorPenalty
                ? int.MaxValue : precedence + LowQualityAuthorPenalty;
        }

        if (value == candidate.GetValue()
            && precedence == candidate.GetPrecedence())
        {
            return candidate;
        }

        return new HtmlMetadataCandidate(field, value,
            candidate.GetRawValue(), candidate.WasRawValueTruncated(),
            candidate.GetSource(), precedence,
            candidate.GetDocumentOrder(), candidate.GetRetentionOrder());
    }

    internal static string NormalizeText(string value)
    {
        string result = HtmlTextContent.Normalize(value);
        if (!CanNormalize(result))
        {
            return result;
        }

        return result.Normalize(NormalizationForm.FormC);
    }

    internal static bool CanNormalize(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsLowSurrogate(current))
            {
                return false;
            }

            if (char.IsHighSurrogate(current))
            {
                index = index + 1;
                if (index >= value.Length
                    || !char.IsLowSurrogate(value[index]))
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal static string NormalizeTextValue(string value)
    {
        return NormalizeText(value);
    }

    internal static string NormalizeLanguage(string? value)
    {
        string? normalized = HtmlTextContent.Normalize(value)
            .Replace('_', '-');
        string[] parts = normalized.Split('-');
        StringBuilder result = new StringBuilder(normalized.Length);
        bool extension = false;
        int index = 0;
        while (index < parts.Length)
        {
            if (index > 0)
            {
                result.Append('-');
            }

            string part = parts[index];
            if (index == 0 || extension)
            {
                result.Append(AsciiLower(part));
            }
            else if (part.Length == 1 && IsAsciiAlphaNumeric(part))
            {
                result.Append(AsciiLower(part));
                extension = true;
            }
            else if (part.Length == 4 && IsAsciiLetters(part))
            {
                result.Append(Ascii.ToUpper(part[0]));
                result.Append(AsciiLower(part.Substring(1)));
            }
            else if (part.Length == 2 && IsAsciiLetters(part)
                || part.Length == 3 && IsAsciiDigits(part))
            {
                result.Append(AsciiUpper(part));
            }
            else
            {
                result.Append(AsciiLower(part));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal static string NormalizeLanguageValue(string? value)
    {
        return NormalizeLanguage(value);
    }

    internal static bool LooksLikeUrl(string value)
    {
        if (value.StartsWith("/", StringComparison.Ordinal)
            || value.StartsWith("./", StringComparison.Ordinal)
            || value.StartsWith("../", StringComparison.Ordinal)
            || value.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        HtmlUrlSchemeResult result = HtmlUrls.ExtractScheme(value);
        return result.IsComplete() && result.HasScheme();
    }

    internal static bool IsAsciiLetters(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!(current >= 'A' && current <= 'Z')
                && !(current >= 'a' && current <= 'z'))
            {
                return false;
            }

            index = index + 1;
        }

        return value.Length > 0;
    }

    internal static bool IsAsciiDigits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return value.Length > 0;
    }

    internal static bool IsAsciiAlphaNumeric(string value)
    {
        return IsAsciiLetters(value) || IsAsciiDigits(value);
    }

    internal static string AsciiLower(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Ascii.ToLower(result[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static string AsciiUpper(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Ascii.ToUpper(result[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
