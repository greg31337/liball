namespace uspto;

internal static class CitedIdentifierNormalizer
{
    internal static CitedReferenceIdentifier NormalizeCitedIdentifier(string? verbatimText, string? parsedIdentifier, string? publicationNumber, string? kindCode, string? countryCode, bool nonPatentLiteratureIndicator, bool hasNonPatentLiteratureIndicator)
    {
        CitedReferenceIdentifier result = new CitedReferenceIdentifier();
        result.VerbatimText = verbatimText;
        if (hasNonPatentLiteratureIndicator && nonPatentLiteratureIndicator)
        {
            result.IsNonPatentLiterature = true;
            result.IsClassified = true;
            return result;
        }

        string? candidate = null;
        bool hasSourcePatentEvidence = false;
        if (!global::System.String.IsNullOrWhiteSpace(parsedIdentifier))
        {
            candidate = parsedIdentifier;
            hasSourcePatentEvidence = true;
        }
        else if (!global::System.String.IsNullOrWhiteSpace(publicationNumber))
        {
            candidate = BuildStructuredPatentCandidate(publicationNumber, kindCode, countryCode);
            hasSourcePatentEvidence = true;
        }
        else if (LooksLikeExplicitUsPatent(verbatimText))
        {
            candidate = verbatimText;
        }

        if (candidate != null && TryNormalizeCitedPatent(candidate, result))
        {
            return result;
        }

        if (hasSourcePatentEvidence || (hasNonPatentLiteratureIndicator && !nonPatentLiteratureIndicator) || LooksLikeForeignPatentDocument(verbatimText))
        {
            result.IsPatentDocument = true;
            result.IsClassified = true;
            return result;
        }

        if (HasNonPatentLiteratureMarker(verbatimText))
        {
            result.IsNonPatentLiterature = true;
            result.IsClassified = true;
        }

        return result;
    }

    internal static string BuildStructuredPatentCandidate(string publicationNumber, string? kindCode, string? countryCode)
    {
        string result = publicationNumber.Trim();
        if (!global::System.String.IsNullOrWhiteSpace(countryCode))
        {
            string country = countryCode.Trim();
            if (!result.StartsWith(country, global::System.StringComparison.OrdinalIgnoreCase))
            {
                result = country + result;
            }
        }

        if (!global::System.String.IsNullOrWhiteSpace(kindCode))
        {
            string kind = kindCode.Trim();
            if (!result.EndsWith(kind, global::System.StringComparison.OrdinalIgnoreCase))
            {
                result += kind;
            }
        }

        return result;
    }

    internal static bool TryNormalizeCitedPatent(string candidate, CitedReferenceIdentifier result)
    {
        IdentifierNormalizationResult normalized = IdentifierNormalization.NormalizePatent(candidate);
        if (!normalized.IsValid)
        {
            return false;
        }

        if (normalized.PatentKind != IdentifierKind.PatentPublication && normalized.PatentKind != IdentifierKind.PatentGrant)
        {
            return false;
        }

        result.CanonicalPatentIdentifier = normalized.CanonicalNumber;
        result.PatentIdentifierKind = normalized.PatentKind;
        result.IsPatentDocument = true;
        result.IsClassified = true;
        return true;
    }

    internal static bool LooksLikeExplicitUsPatent(string? value)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (!trimmed.StartsWith("US", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int index = 2;
        while (index < trimmed.Length && !IsAsciiDigit(trimmed[index]))
        {
            char character = trimmed[index];
            if (!IsCitationSeparator(character))
            {
                return false;
            }

            index += 1;
        }

        return index < trimmed.Length;
    }

    internal static bool LooksLikeForeignPatentDocument(string? value)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length < 3 || !IsAsciiLetter(trimmed[0]) || !IsAsciiLetter(trimmed[1]))
        {
            return false;
        }

        string office = trimmed.Substring(0, 2).ToUpperInvariant();
        if (!IsKnownForeignPatentOffice(office))
        {
            return false;
        }

        int index = 2;
        while (index < trimmed.Length && !IsAsciiDigit(trimmed[index]))
        {
            if (!IsCitationSeparator(trimmed[index]))
            {
                return false;
            }

            index += 1;
        }

        int digitCount = 0;
        while (index < trimmed.Length)
        {
            if (IsAsciiDigit(trimmed[index]))
            {
                digitCount += 1;
            }

            index += 1;
        }

        return digitCount >= 5;
    }

    internal static bool HasNonPatentLiteratureMarker(string? value)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        return trimmed.Equals("NPL", global::System.StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("NPL ", global::System.StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("NPL:", global::System.StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("NON-PATENT ", global::System.StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("NONPATENT ", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsKnownForeignPatentOffice(string office)
    {
        return office == "AU" || office == "CA" || office == "CN" || office == "DE" || office == "EP" || office == "FR" || office == "GB" || office == "JP" || office == "KR" || office == "WO";
    }

    internal static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal static bool IsAsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
    }

    internal static bool IsCitationSeparator(char character)
    {
        return character == ' ' || character == '\t' || character == '\r' || character == '\n' || character == '-' || character == '/' || character == '.' || character == ',';
    }
}
