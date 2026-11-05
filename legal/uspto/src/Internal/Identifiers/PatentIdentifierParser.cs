namespace uspto;

internal class PatentIdentifierCharacterResult
{
    internal string Value = string.Empty;
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal int SeparatorCount = 0;
    internal int LetterCount = 0;
    internal int DigitCount = 0;
    internal int InvalidCharacterIndex = 0;
    internal char InvalidCharacter = '\0';
    internal bool HasInvalidCharacter = false;
}

internal class PatentApplicationNumberResult
{
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal IdentifierKind Kind = IdentifierKind.Unknown;
    internal string NormalizedInput = string.Empty;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal string CountryCode = string.Empty;
    internal string SeriesCode = string.Empty;
    internal string SerialNumber = string.Empty;
    internal bool HadCountryPrefix = false;
}

internal class PatentPublicationNumberResult
{
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal IdentifierKind Kind = IdentifierKind.Unknown;
    internal string NormalizedInput = string.Empty;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal string CountryCode = string.Empty;
    internal string PublicationNumber = string.Empty;
    internal string Year = string.Empty;
    internal string SequenceNumber = string.Empty;
    internal string KindCode = string.Empty;
    internal bool HasKindCode = false;
    internal bool HadCountryPrefix = false;
    internal bool IsRepublication = false;
    internal bool IsCorrectedPublication = false;
}

internal class PatentGrantNumberResult
{
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal IdentifierKind Kind = IdentifierKind.Unknown;
    internal string NormalizedInput = string.Empty;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal string CountryCode = string.Empty;
    internal string DocumentPrefix = string.Empty;
    internal string NumberDigits = string.Empty;
    internal string KindCode = string.Empty;
    internal string GrantCategory = string.Empty;
    internal bool HasKindCode = false;
    internal bool HadCountryPrefix = false;
    internal bool WasPadded = false;
}

internal class PctApplicationNumberResult
{
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal IdentifierKind Kind = IdentifierKind.Unknown;
    internal string NormalizedInput = string.Empty;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal string StandardizedNumber = string.Empty;
    internal string StandardizedDisplayNumber = string.Empty;
    internal string CountryCode = string.Empty;
    internal string ReceivingOfficeCode = string.Empty;
    internal string Year = string.Empty;
    internal string SequenceNumber = string.Empty;
    internal string ExpandedYear = string.Empty;
    internal string ExpandedSequenceNumber = string.Empty;
    internal bool IsLegacy = false;
}

internal class PatentIdentifierCandidateResult
{
    internal IdentifierKind Kind = IdentifierKind.Unknown;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal string CountryCode = string.Empty;
    internal string Explanation = string.Empty;
}

internal class PatentIdentifierAmbiguityResult
{
    internal bool IsValid = false;
    internal bool IsAmbiguous = false;
    internal string? FailureCode = null;
    internal IdentifierKind Kind = IdentifierKind.Unknown;
    internal string NormalizedInput = string.Empty;
    internal string NumericValue = string.Empty;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal PatentIdentifierCandidateResult[] Candidates = new PatentIdentifierCandidateResult[0];
    internal int CandidateCount = 0;
    internal bool HadCountryPrefix = false;
}

internal static class PatentIdentifierParser
{
    internal static PatentIdentifierCharacterResult NormalizeCharacters(string input)
    {
        PatentIdentifierCharacterResult result = new PatentIdentifierCharacterResult();
        if (input == null)
        {
            result.FailureCode = "REQUIRED";
            return result;
        }

        global::System.ReadOnlySpan<char> characters = global::System.MemoryExtensions.AsSpan(input);
        global::System.Text.StringBuilder normalized = new global::System.Text.StringBuilder(characters.Length);
        for (int index = 0; index < characters.Length; index += 1)
        {
            char character = characters[index];
            if (character >= '0' && character <= '9')
            {
                normalized.Append(character);
                result.DigitCount += 1;
            }
            else if (character >= 'A' && character <= 'Z')
            {
                normalized.Append(character);
                result.LetterCount += 1;
            }
            else if (character >= 'a' && character <= 'z')
            {
                normalized.Append((char)(character - 'a' + 'A'));
                result.LetterCount += 1;
            }
            else if (IsPermittedSeparator(character))
            {
                result.SeparatorCount += 1;
            }
            else
            {
                result.FailureCode = "INVALID_CHARACTER";
                result.InvalidCharacterIndex = index;
                result.InvalidCharacter = character;
                result.HasInvalidCharacter = true;
                return result;
            }
        }

        if (normalized.Length == 0)
        {
            result.FailureCode = "EMPTY_IDENTIFIER";
            return result;
        }

        result.Value = normalized.ToString();
        result.IsValid = true;
        return result;
    }

    internal static PatentApplicationNumberResult ParseApplicationNumber(string input)
    {
        PatentApplicationNumberResult result = new PatentApplicationNumberResult();
        PatentIdentifierCharacterResult characters = NormalizeCharacters(input);
        if (!characters.IsValid)
        {
            result.FailureCode = characters.FailureCode;
            return result;
        }

        string normalized = characters.Value;
        result.NormalizedInput = normalized;
        if (normalized.StartsWith("US", global::System.StringComparison.Ordinal))
        {
            result.HadCountryPrefix = true;
            normalized = normalized.Substring(2);
        }
        else if (characters.LetterCount != 0)
        {
            result.FailureCode = "UNSUPPORTED_APPLICATION_PREFIX";
            return result;
        }

        if (normalized.Length != 8)
        {
            result.FailureCode = "INVALID_APPLICATION_FORMAT";
            return result;
        }

        for (int index = 0; index < normalized.Length; index += 1)
        {
            if (normalized[index] < '0' || normalized[index] > '9')
            {
                result.FailureCode = "INVALID_APPLICATION_FORMAT";
                return result;
            }
        }

        string seriesCode = normalized.Substring(0, 2);
        string serialNumber = normalized.Substring(2, 6);
        if (!IsSupportedApplicationSeries(seriesCode))
        {
            result.FailureCode = "UNSUPPORTED_APPLICATION_SERIES";
            return result;
        }

        if (!ContainsNonzeroDigit(serialNumber))
        {
            result.FailureCode = "INVALID_APPLICATION_SERIAL";
            return result;
        }

        result.Kind = IdentifierKind.PatentApplication;
        result.CanonicalNumber = seriesCode + serialNumber;
        result.DisplayNumber = seriesCode + "/" + serialNumber.Substring(0, 3) + "," + serialNumber.Substring(3, 3);
        result.CountryCode = "US";
        result.SeriesCode = seriesCode;
        result.SerialNumber = serialNumber;
        result.IsValid = true;
        return result;
    }

    internal static PatentPublicationNumberResult ParsePublicationNumber(string input)
    {
        PatentPublicationNumberResult result = new PatentPublicationNumberResult();
        PatentIdentifierCharacterResult characters = NormalizeCharacters(input);
        if (!characters.IsValid)
        {
            result.FailureCode = characters.FailureCode;
            return result;
        }

        string normalized = characters.Value;
        result.NormalizedInput = normalized;
        if (normalized.StartsWith("US", global::System.StringComparison.Ordinal))
        {
            result.HadCountryPrefix = true;
            normalized = normalized.Substring(2);
        }
        else if (normalized.Length >= 2 && normalized[0] >= 'A' && normalized[0] <= 'Z' && normalized[1] >= 'A' && normalized[1] <= 'Z')
        {
            result.FailureCode = "UNSUPPORTED_PUBLICATION_COUNTRY";
            return result;
        }

        string kindCode = string.Empty;
        bool hasKindCode = false;
        if (normalized.Length == 13)
        {
            kindCode = normalized.Substring(11, 2);
            hasKindCode = true;
        }
        else if (normalized.Length != 11)
        {
            result.FailureCode = "INVALID_PUBLICATION_FORMAT";
            return result;
        }

        string publicationNumber = normalized.Substring(0, 11);
        for (int index = 0; index < publicationNumber.Length; index += 1)
        {
            if (publicationNumber[index] < '0' || publicationNumber[index] > '9')
            {
                result.FailureCode = "INVALID_PUBLICATION_FORMAT";
                return result;
            }
        }

        if (hasKindCode && !IsSupportedPublicationKind(kindCode))
        {
            result.FailureCode = "UNSUPPORTED_PUBLICATION_KIND";
            return result;
        }

        string year = publicationNumber.Substring(0, 4);
        string sequenceNumber = publicationNumber.Substring(4, 7);
        if (!IsValidPublicationYear(year))
        {
            result.FailureCode = "INVALID_PUBLICATION_YEAR";
            return result;
        }

        if (!ContainsNonzeroDigit(sequenceNumber))
        {
            result.FailureCode = "INVALID_PUBLICATION_SEQUENCE";
            return result;
        }

        string kindSuffix = string.Empty;
        if (hasKindCode)
        {
            kindSuffix = kindCode;
        }

        result.Kind = IdentifierKind.PatentPublication;
        result.CanonicalNumber = "US" + publicationNumber + kindSuffix;
        result.DisplayNumber = "US " + year + "/" + sequenceNumber;
        if (hasKindCode)
        {
            result.DisplayNumber += " " + kindCode;
        }

        result.CountryCode = "US";
        result.PublicationNumber = publicationNumber;
        result.Year = year;
        result.SequenceNumber = sequenceNumber;
        result.KindCode = kindCode;
        result.HasKindCode = hasKindCode;
        result.HadCountryPrefix = result.NormalizedInput.StartsWith("US", global::System.StringComparison.Ordinal);
        result.IsRepublication = kindCode == "A2";
        result.IsCorrectedPublication = kindCode == "A9";
        result.IsValid = true;
        return result;
    }

    internal static PatentGrantNumberResult ParseGrantNumber(string input)
    {
        PatentGrantNumberResult result = new PatentGrantNumberResult();
        PatentIdentifierCharacterResult characters = NormalizeCharacters(input);
        if (!characters.IsValid)
        {
            result.FailureCode = characters.FailureCode;
            return result;
        }

        string normalized = characters.Value;
        result.NormalizedInput = normalized;
        if (normalized.StartsWith("US", global::System.StringComparison.Ordinal))
        {
            result.HadCountryPrefix = true;
            normalized = normalized.Substring(2);
        }

        string prefix = string.Empty;
        if (normalized.StartsWith("RE", global::System.StringComparison.Ordinal) || normalized.StartsWith("PP", global::System.StringComparison.Ordinal) || normalized.StartsWith("AI", global::System.StringComparison.Ordinal))
        {
            prefix = normalized.Substring(0, 2);
            normalized = normalized.Substring(2);
        }
        else if (normalized.Length != 0 && (normalized[0] == 'D' || normalized[0] == 'H' || normalized[0] == 'X' || normalized[0] == 'T'))
        {
            prefix = normalized.Substring(0, 1);
            normalized = normalized.Substring(1);
        }
        else if (normalized.Length != 0 && normalized[0] >= 'A' && normalized[0] <= 'Z')
        {
            result.FailureCode = "UNSUPPORTED_GRANT_PREFIX";
            return result;
        }

        int digitEnd = 0;
        while (digitEnd < normalized.Length && normalized[digitEnd] >= '0' && normalized[digitEnd] <= '9')
        {
            digitEnd += 1;
        }

        if (digitEnd == 0)
        {
            result.FailureCode = "INVALID_GRANT_FORMAT";
            return result;
        }

        string numberDigits = normalized.Substring(0, digitEnd);
        string kindCode = normalized.Substring(digitEnd);
        int maximumDigits = GrantMaximumDigits(prefix);
        if (numberDigits.Length > maximumDigits)
        {
            result.FailureCode = "INVALID_GRANT_FORMAT";
            return result;
        }

        if (!ContainsNonzeroDigit(numberDigits))
        {
            result.FailureCode = "INVALID_GRANT_NUMBER";
            return result;
        }

        if (!IsSupportedGrantKind(prefix, kindCode))
        {
            result.FailureCode = "UNSUPPORTED_GRANT_KIND";
            return result;
        }

        int minimumDigits = GrantMinimumDigits(prefix);
        string paddedDigits = PadGrantDigits(numberDigits, minimumDigits);
        string displayDigits = FormatGrantDigits(paddedDigits);
        string kindSuffix = string.Empty;
        if (kindCode.Length != 0)
        {
            kindSuffix = " " + kindCode;
        }

        result.Kind = IdentifierKind.PatentGrant;
        result.CanonicalNumber = "US" + prefix + paddedDigits + kindCode;
        result.DisplayNumber = "US " + prefix + displayDigits + kindSuffix;
        result.CountryCode = "US";
        result.DocumentPrefix = prefix;
        result.NumberDigits = paddedDigits;
        result.KindCode = kindCode;
        result.GrantCategory = GrantCategory(prefix);
        result.HasKindCode = kindCode.Length != 0;
        result.WasPadded = paddedDigits.Length != numberDigits.Length;
        result.IsValid = true;
        return result;
    }

    internal static PctApplicationNumberResult ParsePctApplicationNumber(string input)
    {
        PctApplicationNumberResult result = new PctApplicationNumberResult();
        PatentIdentifierCharacterResult characters = NormalizeCharacters(input);
        if (!characters.IsValid)
        {
            result.FailureCode = characters.FailureCode;
            return result;
        }

        string normalized = characters.Value;
        result.NormalizedInput = normalized;
        if (!normalized.StartsWith("PCT", global::System.StringComparison.Ordinal))
        {
            result.FailureCode = "INVALID_PCT_PREFIX";
            return result;
        }

        bool isLegacy = normalized.Length == 12;
        if (!isLegacy && normalized.Length != 15)
        {
            result.FailureCode = "INVALID_PCT_FORMAT";
            return result;
        }

        string receivingOfficeCode = normalized.Substring(3, 2);
        if (!IsAsciiUppercaseLetter(receivingOfficeCode[0]) || !IsAsciiUppercaseLetter(receivingOfficeCode[1]))
        {
            result.FailureCode = "INVALID_PCT_RECEIVING_OFFICE";
            return result;
        }

        int yearLength = 4;
        int sequenceLength = 6;
        if (isLegacy)
        {
            yearLength = 2;
            sequenceLength = 5;
        }

        string year = normalized.Substring(5, yearLength);
        string sequenceNumber = normalized.Substring(5 + yearLength, sequenceLength);
        if (!ContainsOnlyAsciiDigits(year) || !IsValidPctYear(year, isLegacy))
        {
            result.FailureCode = "INVALID_PCT_YEAR";
            return result;
        }

        if (!ContainsOnlyAsciiDigits(sequenceNumber))
        {
            result.FailureCode = "INVALID_PCT_FORMAT";
            return result;
        }

        if (!ContainsNonzeroDigit(sequenceNumber))
        {
            result.FailureCode = "INVALID_PCT_SEQUENCE";
            return result;
        }

        string expandedYear = year;
        string expandedSequenceNumber = sequenceNumber;
        if (isLegacy)
        {
            expandedYear = ExpandLegacyPctYear(year);
            expandedSequenceNumber = "0" + sequenceNumber;
        }

        result.Kind = IdentifierKind.PctApplication;
        result.CanonicalNumber = "PCT" + receivingOfficeCode + year + sequenceNumber;
        result.DisplayNumber = "PCT/" + receivingOfficeCode + year + "/" + sequenceNumber;
        result.StandardizedNumber = "PCT" + receivingOfficeCode + expandedYear + expandedSequenceNumber;
        result.StandardizedDisplayNumber = "PCT/" + receivingOfficeCode + expandedYear + "/" + expandedSequenceNumber;
        result.CountryCode = receivingOfficeCode;
        result.ReceivingOfficeCode = receivingOfficeCode;
        result.Year = year;
        result.SequenceNumber = sequenceNumber;
        result.ExpandedYear = expandedYear;
        result.ExpandedSequenceNumber = expandedSequenceNumber;
        result.IsLegacy = isLegacy;
        result.IsValid = true;
        return result;
    }

    internal static PatentIdentifierAmbiguityResult DetectBareNumericPatentCandidates(string input)
    {
        PatentIdentifierAmbiguityResult result = new PatentIdentifierAmbiguityResult();
        PatentIdentifierCharacterResult characters = NormalizeCharacters(input);
        if (!characters.IsValid)
        {
            result.FailureCode = characters.FailureCode;
            return result;
        }

        if (ContainsNonWhitespaceSeparator(input))
        {
            result.FailureCode = "NOT_BARE_NUMERIC_IDENTIFIER";
            return result;
        }

        string normalized = characters.Value;
        result.NormalizedInput = normalized;
        if (normalized.StartsWith("US", global::System.StringComparison.Ordinal))
        {
            result.HadCountryPrefix = true;
            normalized = normalized.Substring(2);
        }

        if (!ContainsOnlyAsciiDigits(normalized))
        {
            result.FailureCode = "NOT_BARE_NUMERIC_IDENTIFIER";
            return result;
        }

        result.NumericValue = normalized;
        if (normalized.Length != 7 && normalized.Length != 8)
        {
            result.FailureCode = "INVALID_NUMERIC_IDENTIFIER_LENGTH";
            return result;
        }

        string applicationInput = normalized;
        if (normalized.Length == 7)
        {
            applicationInput = "0" + normalized;
        }

        PatentApplicationNumberResult application = ParseApplicationNumber(applicationInput);
        PatentGrantNumberResult grant = new PatentGrantNumberResult();
        if (normalized.Length == 7 || normalized[0] != '0')
        {
            grant = ParseGrantNumber(normalized);
        }

        int candidateCount = 0;
        if (application.IsValid)
        {
            candidateCount += 1;
        }

        if (grant.IsValid)
        {
            candidateCount += 1;
        }

        if (candidateCount == 0)
        {
            result.FailureCode = "INVALID_NUMERIC_IDENTIFIER";
            return result;
        }

        PatentIdentifierCandidateResult[] candidates = new PatentIdentifierCandidateResult[candidateCount];
        int candidateIndex = 0;
        if (application.IsValid)
        {
            PatentIdentifierCandidateResult applicationCandidate = new PatentIdentifierCandidateResult();
            applicationCandidate.Kind = IdentifierKind.PatentApplication;
            applicationCandidate.CanonicalNumber = application.CanonicalNumber;
            applicationCandidate.DisplayNumber = application.DisplayNumber;
            applicationCandidate.CountryCode = application.CountryCode;
            applicationCandidate.Explanation = "The digits form a supported application series and serial number.";
            candidates[candidateIndex] = applicationCandidate;
            candidateIndex += 1;
        }

        if (grant.IsValid)
        {
            PatentIdentifierCandidateResult grantCandidate = new PatentIdentifierCandidateResult();
            grantCandidate.Kind = IdentifierKind.PatentGrant;
            grantCandidate.CanonicalNumber = grant.CanonicalNumber;
            grantCandidate.DisplayNumber = grant.DisplayNumber;
            grantCandidate.CountryCode = grant.CountryCode;
            grantCandidate.Explanation = "The digits form a utility patent grant number.";
            candidates[candidateIndex] = grantCandidate;
        }

        result.Candidates = candidates;
        result.CandidateCount = candidateCount;
        result.IsAmbiguous = candidateCount > 1;
        if (result.IsAmbiguous)
        {
            result.Kind = IdentifierKind.Ambiguous;
        }
        else
        {
            result.Kind = candidates[0].Kind;
            result.CanonicalNumber = candidates[0].CanonicalNumber;
            result.DisplayNumber = candidates[0].DisplayNumber;
        }

        result.IsValid = true;
        return result;
    }

    internal static bool ContainsNonWhitespaceSeparator(string input)
    {
        global::System.ReadOnlySpan<char> characters = global::System.MemoryExtensions.AsSpan(input);
        for (int index = 0; index < characters.Length; index += 1)
        {
            char character = characters[index];
            if (character == '/' || character == ',' || character == '.' || character == '-')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsAsciiUppercaseLetter(char character)
    {
        return character >= 'A' && character <= 'Z';
    }

    internal static bool ContainsOnlyAsciiDigits(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsValidPctYear(string year, bool isLegacy)
    {
        string currentYear = global::System.DateTime.UtcNow.Year.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        if (!isLegacy)
        {
            return global::System.String.CompareOrdinal(year, "1978") >= 0 && global::System.String.CompareOrdinal(year, currentYear) <= 0;
        }

        string currentYearCode = currentYear.Substring(2, 2);
        return global::System.String.CompareOrdinal(year, "78") >= 0 || global::System.String.CompareOrdinal(year, currentYearCode) <= 0;
    }

    internal static string ExpandLegacyPctYear(string year)
    {
        if (global::System.String.CompareOrdinal(year, "78") >= 0)
        {
            return "19" + year;
        }

        return "20" + year;
    }

    internal static bool IsSupportedGrantKind(string prefix, string kindCode)
    {
        if (kindCode.Length == 0)
        {
            return true;
        }

        if (prefix.Length == 0)
        {
            return kindCode == "A" || kindCode == "A1" || kindCode == "B1" || kindCode == "B2" || kindCode == "B9";
        }

        if (prefix == "D")
        {
            return kindCode == "S" || kindCode == "S1";
        }

        if (prefix == "PP")
        {
            return kindCode == "P" || kindCode == "P1" || kindCode == "P2" || kindCode == "P3";
        }

        if (prefix == "RE")
        {
            return kindCode == "E" || kindCode == "E1";
        }

        if (prefix == "H")
        {
            return kindCode == "H" || kindCode == "H1";
        }

        if (prefix == "X")
        {
            return kindCode == "I1";
        }

        if (prefix == "AI")
        {
            return kindCode == "I3";
        }

        if (prefix == "T")
        {
            return kindCode == "I4";
        }

        return false;
    }

    internal static int GrantMinimumDigits(string prefix)
    {
        if (prefix == "PP" || prefix == "RE" || prefix == "AI")
        {
            return 5;
        }

        if (prefix == "D" || prefix == "H" || prefix == "X" || prefix == "T")
        {
            return 6;
        }

        return 7;
    }

    internal static int GrantMaximumDigits(string prefix)
    {
        if (prefix == "D")
        {
            return 7;
        }

        if (prefix == "PP" || prefix == "RE" || prefix == "AI")
        {
            return 6;
        }

        if (prefix == "H" || prefix == "X" || prefix == "T")
        {
            return 6;
        }

        return 8;
    }

    internal static string PadGrantDigits(string numberDigits, int minimumDigits)
    {
        if (numberDigits.Length >= minimumDigits)
        {
            return numberDigits;
        }

        return new string('0', minimumDigits - numberDigits.Length) + numberDigits;
    }

    internal static string FormatGrantDigits(string numberDigits)
    {
        int firstNonzero = 0;
        while (firstNonzero < numberDigits.Length - 1 && numberDigits[firstNonzero] == '0')
        {
            firstNonzero += 1;
        }

        string significant = numberDigits.Substring(firstNonzero);
        global::System.Text.StringBuilder formatted = new global::System.Text.StringBuilder(significant.Length + significant.Length / 3);
        for (int index = 0; index < significant.Length; index += 1)
        {
            int remaining = significant.Length - index;
            if (index != 0 && remaining % 3 == 0)
            {
                formatted.Append(',');
            }

            formatted.Append(significant[index]);
        }

        return formatted.ToString();
    }

    internal static string GrantCategory(string prefix)
    {
        if (prefix == "D")
        {
            return "Design";
        }

        if (prefix == "PP")
        {
            return "Plant";
        }

        if (prefix == "RE")
        {
            return "Reissue";
        }

        if (prefix == "H")
        {
            return "StatutoryInventionRegistration";
        }

        if (prefix == "X")
        {
            return "XPatent";
        }

        if (prefix == "AI")
        {
            return "AdditionalImprovement";
        }

        if (prefix == "T")
        {
            return "DefensivePublication";
        }

        return "Utility";
    }

    internal static bool IsSupportedPublicationKind(string kindCode)
    {
        return kindCode == "A1" || kindCode == "A2" || kindCode == "A9";
    }

    internal static bool IsValidPublicationYear(string year)
    {
        string currentYear = global::System.DateTime.UtcNow.Year.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return global::System.String.CompareOrdinal(year, "2001") >= 0 && global::System.String.CompareOrdinal(year, currentYear) <= 0;
    }

    internal static bool IsSupportedApplicationSeries(string seriesCode)
    {
        if (seriesCode.Length != 2)
        {
            return false;
        }

        bool ordinary = seriesCode[0] == '0' && seriesCode[1] >= '1' && seriesCode[1] <= '9' || seriesCode[0] == '1' && seriesCode[1] >= '0' && seriesCode[1] <= '8';
        if (ordinary)
        {
            return true;
        }

        return seriesCode == "29" || seriesCode == "35" || seriesCode == "60" || seriesCode == "61" || seriesCode == "62" || seriesCode == "63" || seriesCode == "90" || seriesCode == "95" || seriesCode == "96";
    }

    internal static bool ContainsNonzeroDigit(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            if (value[index] >= '1' && value[index] <= '9')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsPermittedSeparator(char character)
    {
        return global::System.Char.IsWhiteSpace(character) || character == '/' || character == ',' || character == '.' || character == '-';
    }
}
