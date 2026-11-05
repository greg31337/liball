namespace countrycodes;

internal static class ValidationEngine
{
    internal static ValidationResult ValidateAlpha2(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeAlpha2Text(value, false);
        if (normalized.CanonicalValue == null)
        {
            return CreateFixedCodeSyntaxFailure(
                value,
                CountryCodeConstants.Alpha2Length,
                false);
        }

        return ValidateCountryState(normalized, policy);
    }

    internal static ValidationResult ValidateAlpha3(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeAlpha3Text(value, false);
        if (normalized.CanonicalValue == null)
        {
            return CreateFixedCodeSyntaxFailure(
                value,
                CountryCodeConstants.Alpha3Length,
                false);
        }

        return ValidateCountryState(normalized, policy);
    }

    internal static ValidationResult ValidateNumeric3(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeNumericText(value, false);
        if (normalized.CanonicalValue == null)
        {
            return CreateFixedCodeSyntaxFailure(
                value,
                CountryCodeConstants.Numeric3Length,
                true);
        }

        return ValidateCountryState(normalized, policy);
    }

    internal static ValidationResult ValidateAlpha4(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeAlpha4Text(value, false);
        if (normalized.CanonicalValue == null)
        {
            return CreateFixedCodeSyntaxFailure(
                value,
                CountryCodeConstants.Alpha4Length,
                false);
        }

        return ValidateCountryState(normalized, policy);
    }

    internal static ValidationResult ValidateAnyCountryCode(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeAnyText(value, false);
        if (normalized.CanonicalValue == null)
        {
            return CreateAnyCodeSyntaxFailure(value);
        }

        return ValidateCountryState(normalized, policy);
    }

    internal static ValidationResult ValidateSubdivision(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeSubdivisionText(value, false);
        if (normalized.CanonicalValue == null)
        {
            return CreateSubdivisionSyntaxFailure(value);
        }

        string canonical = normalized.CanonicalValue;
        if (policy == ValidationPolicy.SyntaxOnly)
        {
            return CreateValidResult(canonical);
        }

        string prefix = canonical.Substring(
            0,
            CountryCodeConstants.SubdivisionCountryCodeLength);
        CodeNormalizationResult country =
            NormalizationEngine.NormalizeAlpha2Text(prefix, false);
        if (country.AssignmentStatus == CodeAssignmentStatus.Unassigned)
        {
            return CreateFailureResult(
                canonical,
                ValidationFailureKind.UnknownCountryPrefix,
                false,
                0,
                0);
        }

        if (!IsCountryStateAccepted(country, policy))
        {
            return CreateFailureResult(
                canonical,
                GetCountryStateFailure(country.AssignmentStatus),
                false,
                0,
                0);
        }

        if (SubdivisionIndex.FindSubdivisionByCode(canonical) != null)
        {
            return CreateValidResult(canonical);
        }

        if (SubdivisionIndex.FindSubdivisionAliasByCode(canonical) != null)
        {
            if (policy == ValidationPolicy.AnyKnownIso ||
                policy == ValidationPolicy.AllowHistorical)
            {
                return CreateValidResult(canonical);
            }

            return CreateFailureResult(
                canonical,
                ValidationFailureKind.Deprecated,
                false,
                0,
                0);
        }

        return CreateFailureResult(
            canonical,
            ValidationFailureKind.Unknown,
            false,
            0,
            0);
    }

    internal static ValidationResult ValidateLocale(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized = NormalizationEngine.NormalizeLocale(value);
        if (normalized.CanonicalValue == null)
        {
            return CreateLocaleSyntaxFailure(value);
        }

        string canonical = normalized.CanonicalValue;
        if (policy == ValidationPolicy.SyntaxOnly ||
            LocalizedNameCatalogEngine.IsLocaleAvailable(canonical))
        {
            return CreateValidResult(canonical);
        }

        return CreateFailureResult(
            canonical,
            ValidationFailureKind.UnsupportedLocale,
            false,
            0,
            0);
    }

    internal static ValidationResult ValidateName(
        string? value,
        ValidationPolicy policy)
    {
        CodeNormalizationResult normalized =
            NormalizationEngine.NormalizeName(value, true, true);
        if (normalized.CanonicalValue == null)
        {
            return CreateNameSyntaxFailure(value);
        }

        string canonical = normalized.CanonicalValue;
        if (policy == ValidationPolicy.SyntaxOnly)
        {
            return CreateValidResult(canonical);
        }

        CountryLookupResult current = CountryLookupEngine.FindNameText(value);
        if (current.Status == LookupStatus.Ambiguous)
        {
            return CreateFailureResult(
                canonical,
                ValidationFailureKind.Ambiguous,
                false,
                0,
                0);
        }

        if (current.Status == LookupStatus.Found)
        {
            if (IsOnlyDeprecatedCurrentName(value, canonical))
            {
                return CreateFailureResult(
                    canonical,
                    ValidationFailureKind.Deprecated,
                    false,
                    0,
                    0);
            }

            return CreateValidResult(canonical);
        }

        HistoricalLookupResult historical = HistoricalLookupEngine.FindName(value);
        if (historical.Status == LookupStatus.Ambiguous)
        {
            return CreateFailureResult(
                canonical,
                ValidationFailureKind.Ambiguous,
                false,
                0,
                0);
        }

        if (historical.Status == LookupStatus.Found)
        {
            if (policy == ValidationPolicy.AnyKnownIso ||
                policy == ValidationPolicy.AllowHistorical)
            {
                return CreateValidResult(canonical);
            }

            return CreateFailureResult(
                canonical,
                ValidationFailureKind.FormerlyAssigned,
                false,
                0,
                0);
        }

        return CreateFailureResult(
            canonical,
            ValidationFailureKind.Unknown,
            false,
            0,
            0);
    }

    internal static ValidationResult ValidateCountryState(
        CodeNormalizationResult normalized,
        ValidationPolicy policy)
    {
        string? canonical = normalized.CanonicalValue;
        if (canonical == null)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidShape,
                false,
                0,
                0);
        }

        if (policy == ValidationPolicy.SyntaxOnly ||
            IsCountryStateAccepted(normalized, policy))
        {
            return CreateValidResult(canonical);
        }

        return CreateFailureResult(
            canonical,
            GetCountryStateFailure(normalized.AssignmentStatus),
            false,
            0,
            0);
    }

    internal static bool IsCountryStateAccepted(
        CodeNormalizationResult normalized,
        ValidationPolicy policy)
    {
        CodeAssignmentStatus status = normalized.AssignmentStatus;
        if (policy == ValidationPolicy.AssignedOnly)
        {
            return status == CodeAssignmentStatus.Assigned;
        }

        if (policy == ValidationPolicy.AnyKnownIso)
        {
            return status != CodeAssignmentStatus.Unassigned;
        }

        if (policy == ValidationPolicy.AllowHistorical)
        {
            return status == CodeAssignmentStatus.Assigned ||
                   status == CodeAssignmentStatus.FormerlyAssigned ||
                   normalized.ReplacementCandidates.Length > 0;
        }

        if (policy == ValidationPolicy.AllowUserAssigned)
        {
            return status == CodeAssignmentStatus.Assigned ||
                   status == CodeAssignmentStatus.UserAssigned;
        }

        return false;
    }

    internal static ValidationFailureKind GetCountryStateFailure(
        CodeAssignmentStatus status)
    {
        if (status == CodeAssignmentStatus.FormerlyAssigned)
        {
            return ValidationFailureKind.FormerlyAssigned;
        }

        if (status == CodeAssignmentStatus.UserAssigned)
        {
            return ValidationFailureKind.UserAssignedDisallowed;
        }

        if (status == CodeAssignmentStatus.ExceptionallyReserved ||
            status == CodeAssignmentStatus.TransitionallyReserved ||
            status == CodeAssignmentStatus.IndeterminatelyReserved)
        {
            return ValidationFailureKind.Reserved;
        }

        return ValidationFailureKind.Unknown;
    }

    internal static ValidationResult CreateFixedCodeSyntaxFailure(
        string? value,
        int expectedLength,
        bool numeric)
    {
        if (value == null || value.Length == 0)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.NullOrEmpty,
                false,
                0,
                0);
        }

        if (value.Length != expectedLength)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidLength,
                false,
                0,
                0);
        }

        int index = 0;
        while (index < value.Length)
        {
            bool valid = AsciiCodeUtility.IsAsciiLetter(value[index]);
            if (numeric)
            {
                valid = AsciiCodeUtility.IsAsciiDigit(value[index]);
            }

            if (!valid)
            {
                return CreateFailureResult(
                    null,
                    ValidationFailureKind.InvalidCharacter,
                    true,
                    index,
                    1);
            }

            index += 1;
        }

        return CreateFailureResult(
            null,
            ValidationFailureKind.InvalidShape,
            false,
            0,
            0);
    }

    internal static ValidationResult CreateAnyCodeSyntaxFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.NullOrEmpty,
                false,
                0,
                0);
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiAlphanumeric(value[index]))
            {
                return CreateFailureResult(
                    null,
                    ValidationFailureKind.InvalidCharacter,
                    true,
                    index,
                    1);
            }

            index += 1;
        }

        if (value.Length < CountryCodeConstants.Alpha2Length ||
            value.Length > CountryCodeConstants.Alpha4Length)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidLength,
                false,
                0,
                0);
        }

        return CreateFailureResult(
            null,
            ValidationFailureKind.InvalidShape,
            false,
            0,
            0);
    }

    internal static ValidationResult CreateSubdivisionSyntaxFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.NullOrEmpty,
                false,
                0,
                0);
        }

        int minimumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMinimumSuffixLength;
        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidLength,
                false,
                0,
                0);
        }

        int index = 0;
        while (index < CountryCodeConstants.SubdivisionCountryCodeLength)
        {
            if (!AsciiCodeUtility.IsAsciiLetter(value[index]))
            {
                return CreateFailureResult(
                    null,
                    ValidationFailureKind.InvalidCharacter,
                    true,
                    index,
                    1);
            }

            index += 1;
        }

        if (value[CountryCodeConstants.SubdivisionCountryCodeLength] !=
            CountryCodeConstants.SubdivisionSeparator)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidShape,
                true,
                CountryCodeConstants.SubdivisionCountryCodeLength,
                1);
        }

        index = CountryCodeConstants.SubdivisionCountryCodeLength + 1;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiAlphanumeric(value[index]))
            {
                return CreateFailureResult(
                    null,
                    ValidationFailureKind.InvalidCharacter,
                    true,
                    index,
                    1);
            }

            index += 1;
        }

        return CreateFailureResult(
            null,
            ValidationFailureKind.InvalidShape,
            false,
            0,
            0);
    }

    internal static ValidationResult CreateLocaleSyntaxFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.NullOrEmpty,
                false,
                0,
                0);
        }

        if (value.Length > 255)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidLength,
                false,
                0,
                0);
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiAlphanumeric(value[index]) &&
                value[index] != '-')
            {
                return CreateFailureResult(
                    null,
                    ValidationFailureKind.InvalidCharacter,
                    true,
                    index,
                    1);
            }

            index += 1;
        }

        return CreateFailureResult(
            null,
            ValidationFailureKind.InvalidShape,
            false,
            0,
            0);
    }

    internal static ValidationResult CreateNameSyntaxFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.NullOrEmpty,
                false,
                0,
                0);
        }

        int invalidPosition = FindInvalidUtf16Position(value);
        if (invalidPosition >= 0)
        {
            return CreateFailureResult(
                null,
                ValidationFailureKind.InvalidCharacter,
                true,
                invalidPosition,
                1);
        }

        return CreateFailureResult(
            null,
            ValidationFailureKind.InvalidShape,
            false,
            0,
            0);
    }

    internal static int FindInvalidUtf16Position(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length ||
                    !char.IsLowSurrogate(value[index + 1]))
                {
                    return index;
                }

                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static bool IsOnlyDeprecatedCurrentName(
        string? value,
        string canonical)
    {
        if (value == null)
        {
            return false;
        }

        GeneratedCountryNameRow[] rows =
            NameIndex.FindCurrentCountryNamesBySearchKey(canonical);
        bool foundDeprecated = false;
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedCountryNameRow row = rows[index];
            if (global::System.String.Equals(
                    row.DisplayName,
                    value,
                    global::System.StringComparison.OrdinalIgnoreCase))
            {
                if (row.NameKind != GeneratedCountryNameKind.Deprecated)
                {
                    return false;
                }

                foundDeprecated = true;
            }

            index += 1;
        }

        return foundDeprecated;
    }

    internal static ValidationResult CreateValidResult(string normalizedValue)
    {
        return new ValidationResult(
            true,
            normalizedValue,
            new ValidationIssue[0]);
    }

    internal static ValidationResult CreateFailureResult(
        string? normalizedValue,
        ValidationFailureKind kind,
        bool hasPosition,
        int position,
        int length)
    {
        ValidationIssue issue = new ValidationIssue(
            kind,
            hasPosition,
            position,
            length,
            GetMessage(kind));
        return new ValidationResult(
            false,
            normalizedValue,
            new ValidationIssue[] { issue });
    }

    internal static string GetMessage(ValidationFailureKind kind)
    {
        switch (kind)
        {
            case ValidationFailureKind.NullOrEmpty:
                return "The input is null or empty.";
            case ValidationFailureKind.InvalidLength:
                return "The input has an invalid length.";
            case ValidationFailureKind.InvalidCharacter:
                return "The input contains an invalid character.";
            case ValidationFailureKind.InvalidShape:
                return "The input has an invalid shape.";
            case ValidationFailureKind.Unknown:
                return "The input is not known to the catalog.";
            case ValidationFailureKind.Reserved:
                return "The input is reserved by ISO.";
            case ValidationFailureKind.FormerlyAssigned:
                return "The input was formerly assigned.";
            case ValidationFailureKind.UserAssignedDisallowed:
                return "The user-assigned input is disallowed by the policy.";
            case ValidationFailureKind.UnknownCountryPrefix:
                return "The subdivision country prefix is unknown.";
            case ValidationFailureKind.Deprecated:
                return "The input is deprecated.";
            case ValidationFailureKind.Ambiguous:
                return "The input identifies multiple candidates.";
            case ValidationFailureKind.UnsupportedLocale:
                return "The locale is unsupported by the catalog.";
            default:
                return "The input is invalid.";
        }
    }
}
