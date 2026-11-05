namespace uspto;

internal class IdentifierNormalizationResult
{
    internal bool IsValid = false;
    internal bool IsAmbiguous = false;
    internal string? FailureCode = null;
    internal string CanonicalNumber = string.Empty;
    internal IdentifierKind PatentKind = IdentifierKind.Unknown;
    internal TrademarkIdentifierKind TrademarkKind = TrademarkIdentifierKind.Unknown;
    internal PatentIdentifierCandidateResult[] Candidates = new PatentIdentifierCandidateResult[0];
    internal int CandidateCount = 0;
}

internal static class IdentifierNormalization
{
    internal static IdentifierNormalizationResult NormalizePatent(string input)
    {
        IdentifierNormalizationResult result = new IdentifierNormalizationResult();
        PatentIdentifierCharacterResult characters = PatentIdentifierParser.NormalizeCharacters(input);
        if (!characters.IsValid)
        {
            result.FailureCode = characters.FailureCode;
            return result;
        }

        string normalized = characters.Value;
        if (normalized.StartsWith("PCT", global::System.StringComparison.Ordinal))
        {
            PctApplicationNumberResult pct = PatentIdentifierParser.ParsePctApplicationNumber(input);
            if (!pct.IsValid)
            {
                result.FailureCode = pct.FailureCode;
                return result;
            }

            result.IsValid = true;
            result.CanonicalNumber = pct.CanonicalNumber;
            result.PatentKind = pct.Kind;
            return result;
        }

        string withoutCountry = normalized;
        if (withoutCountry.StartsWith("US", global::System.StringComparison.Ordinal))
        {
            withoutCountry = withoutCountry.Substring(2);
        }

        bool looksLikePublication = false;
        if (withoutCountry.Length == 11 || withoutCountry.Length == 13)
        {
            looksLikePublication = PatentIdentifierParser.ContainsOnlyAsciiDigits(withoutCountry.Substring(0, 4));
        }

        if (looksLikePublication)
        {
            PatentPublicationNumberResult publication = PatentIdentifierParser.ParsePublicationNumber(input);
            if (!publication.IsValid)
            {
                result.FailureCode = publication.FailureCode;
                return result;
            }

            result.IsValid = true;
            result.CanonicalNumber = publication.CanonicalNumber;
            result.PatentKind = publication.Kind;
            return result;
        }

        PatentIdentifierAmbiguityResult ambiguity = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);
        if (ambiguity.IsValid)
        {
            result.IsAmbiguous = ambiguity.IsAmbiguous;
            result.PatentKind = ambiguity.Kind;
            result.Candidates = ambiguity.Candidates;
            result.CandidateCount = ambiguity.CandidateCount;
            if (ambiguity.IsAmbiguous)
            {
                result.FailureCode = "AMBIGUOUS_PATENT_IDENTIFIER";
                return result;
            }

            result.IsValid = true;
            result.CanonicalNumber = ambiguity.CanonicalNumber;
            return result;
        }

        if (ambiguity.FailureCode == "INVALID_NUMERIC_IDENTIFIER")
        {
            result.FailureCode = ambiguity.FailureCode;
            return result;
        }

        if (StartsWithGrantDocumentPrefix(withoutCountry))
        {
            PatentGrantNumberResult prefixedGrant = PatentIdentifierParser.ParseGrantNumber(input);
            if (!prefixedGrant.IsValid)
            {
                result.FailureCode = prefixedGrant.FailureCode;
                return result;
            }

            result.IsValid = true;
            result.CanonicalNumber = prefixedGrant.CanonicalNumber;
            result.PatentKind = prefixedGrant.Kind;
            return result;
        }

        if (ContainsSlash(input))
        {
            PatentApplicationNumberResult application = PatentIdentifierParser.ParseApplicationNumber(input);
            if (!application.IsValid)
            {
                result.FailureCode = application.FailureCode;
                return result;
            }

            result.IsValid = true;
            result.CanonicalNumber = application.CanonicalNumber;
            result.PatentKind = application.Kind;
            return result;
        }

        PatentGrantNumberResult grant = PatentIdentifierParser.ParseGrantNumber(input);
        if (!grant.IsValid)
        {
            result.FailureCode = grant.FailureCode;
            return result;
        }

        result.IsValid = true;
        result.CanonicalNumber = grant.CanonicalNumber;
        result.PatentKind = grant.Kind;
        return result;
    }

    internal static IdentifierNormalizationResult NormalizeTrademark(string input)
    {
        IdentifierNormalizationResult result = new IdentifierNormalizationResult();
        TrademarkIdentifierResult trademark = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);
        if (!trademark.IsValid)
        {
            result.FailureCode = trademark.FailureCode;
            return result;
        }

        result.IsValid = true;
        result.CanonicalNumber = trademark.CanonicalNumber;
        result.TrademarkKind = trademark.Kind;
        return result;
    }

    internal static bool ContainsSlash(string input)
    {
        global::System.ReadOnlySpan<char> characters = global::System.MemoryExtensions.AsSpan(input);
        for (int index = 0; index < characters.Length; index += 1)
        {
            if (characters[index] == '/')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool StartsWithGrantDocumentPrefix(string value)
    {
        return value.StartsWith("RE", global::System.StringComparison.Ordinal) || value.StartsWith("PP", global::System.StringComparison.Ordinal) || value.StartsWith("AI", global::System.StringComparison.Ordinal) || value.StartsWith("D", global::System.StringComparison.Ordinal) || value.StartsWith("H", global::System.StringComparison.Ordinal) || value.StartsWith("X", global::System.StringComparison.Ordinal) || value.StartsWith("T", global::System.StringComparison.Ordinal);
    }
}
