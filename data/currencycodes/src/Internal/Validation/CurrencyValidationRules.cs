namespace currencycodes;

internal static class CurrencyValidationRules
{
    internal static CurrencyValidationResult ValidateCurrencyAlphabeticSyntax(string? code)
    {
        NormalizationResult normalization =
            CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode(code);
        if (!normalization.Success)
        {
            return CreateMalformedAlphabeticSyntaxResult();
        }

        return CreateValidCurrencySyntaxResult(normalization.NormalizedText);
    }

    internal static CurrencyValidationResult ValidateCurrencyNumericSyntax(string? code)
    {
        if (code == null)
        {
            return CreateMalformedNumericSyntaxResult();
        }

        string trimmed = AsciiCodeText.TrimWhitespace(code);
        if (trimmed.Length != CurrencyCodeConstants.CurrencyNumericCodeLength
            || !AsciiCodeText.IsAsciiDigitText(trimmed))
        {
            return CreateMalformedNumericSyntaxResult();
        }

        return CreateValidCurrencySyntaxResult(trimmed);
    }

    internal static CurrencyValidationResult ValidateCurrencyAssignment(
        CurrencyDataStore store,
        string? code,
        CurrencySet currencySet)
    {
        CurrencyLookupResult lookup = store.FindCurrencyAlphabeticLookup(code, currencySet);
        if (lookup.Error == LookupError.MalformedInput)
        {
            return CreateMalformedAlphabeticSyntaxResult();
        }

        if (lookup.Error == LookupError.Unassigned)
        {
            return CreateUnassignedCurrencyResult(lookup.NormalizedQuery);
        }

        if (lookup.Error == LookupError.OutOfScope)
        {
            CurrencyLookupResult allLookup = store.FindCurrencyAlphabeticLookup(
                lookup.NormalizedQuery,
                CurrencySet.All);
            CurrencyInfo? outOfScopeCurrency = allLookup.Match == null
                ? null
                : CurrencySnapshotFactory.CopyCurrencyInfo(allLookup.Match);
            return CreateInvalidCurrencyStatusResult(
                lookup.NormalizedQuery,
                outOfScopeCurrency);
        }

        CurrencyInfo? match = lookup.Match;
        if (match == null)
        {
            return CreateUnassignedCurrencyResult(lookup.NormalizedQuery);
        }

        return CreateValidCurrencyAssignmentResult(
            lookup.NormalizedQuery,
            CurrencySnapshotFactory.CopyCurrencyInfo(match));
    }

    internal static CurrencyValidationResult ValidateCurrencyOnDate(
        CurrencyDataStore store,
        string? code,
        System.DateOnly date)
    {
        CurrencyLookupResult lookup = store.FindCurrencyAlphabeticLookup(code, CurrencySet.All);
        if (lookup.Error == LookupError.MalformedInput)
        {
            return CreateMalformedAlphabeticSyntaxResult();
        }

        if (lookup.Error == LookupError.Unassigned)
        {
            return CreateUnassignedCurrencyResult(lookup.NormalizedQuery);
        }

        CurrencyInfo? indeterminateCurrency = null;
        for (int index = 0; index < lookup.Matches.Length; index++)
        {
            CurrencyInfo currency = lookup.Matches[index];
            CurrencyValidationError dateError = GetCurrencyDateValidationError(currency, date);
            if (dateError == CurrencyValidationError.None)
            {
                return CreateValidCurrencyDateResult(
                    lookup.NormalizedQuery,
                    CurrencySnapshotFactory.CopyCurrencyInfo(currency));
            }

            if (dateError == CurrencyValidationError.Indeterminate
                && indeterminateCurrency == null)
            {
                indeterminateCurrency = CurrencySnapshotFactory.CopyCurrencyInfo(currency);
            }
        }

        if (indeterminateCurrency != null)
        {
            return CreateIndeterminateCurrencyDateResult(
                lookup.NormalizedQuery,
                indeterminateCurrency);
        }

        CurrencyInfo? match = lookup.Match;
        if (match == null)
        {
            return CreateUnassignedCurrencyResult(lookup.NormalizedQuery);
        }

        return CreateInvalidCurrencyDateResult(
            lookup.NormalizedQuery,
            CurrencySnapshotFactory.CopyCurrencyInfo(match));
    }

    internal static CurrencyValidationResult ValidateTerritoryAssignment(
        CurrencyDataStore store,
        string? territoryCode)
    {
        TerritoryLookupResult lookup = store.FindTerritoryAlpha2Lookup(territoryCode);
        if (lookup.Error == LookupError.MalformedInput)
        {
            return CreateMalformedTerritoryResult();
        }

        if (lookup.Error == LookupError.Unassigned)
        {
            return CreateUnassignedTerritoryResult(lookup.NormalizedQuery);
        }

        return CreateValidTerritoryAssignmentResult(lookup.NormalizedQuery);
    }

    internal static CurrencyValidationResult ValidateTerritoryCurrencyAssociation(
        CurrencyDataStore store,
        string? territoryCode,
        string? currencyCode,
        System.DateOnly date,
        bool tenderOnly)
    {
        CurrencyValidationResult territoryValidation = ValidateTerritoryAssignment(
            store,
            territoryCode);
        if (!territoryValidation.IsValid)
        {
            return territoryValidation;
        }

        CurrencyLookupResult currencyLookup = store.FindCurrencyAlphabeticLookup(
            currencyCode,
            CurrencySet.All);
        if (currencyLookup.Error == LookupError.MalformedInput)
        {
            return CreateMalformedAlphabeticSyntaxResult();
        }

        if (currencyLookup.Error == LookupError.Unassigned)
        {
            return CreateUnassignedCurrencyResult(currencyLookup.NormalizedQuery);
        }

        CurrencyInfo? match = currencyLookup.Match;
        if (match == null)
        {
            return CreateUnassignedCurrencyResult(currencyLookup.NormalizedQuery);
        }

        CurrencyInfo[] associatedCurrencies = store.GetCurrenciesForTerritoryOnDateSnapshots(
            territoryValidation.NormalizedText,
            date,
            tenderOnly);
        for (int index = 0; index < associatedCurrencies.Length; index++)
        {
            if (string.CompareOrdinal(
                associatedCurrencies[index].AlphabeticCode,
                currencyLookup.NormalizedQuery) == 0)
            {
                return CreateValidTerritoryCurrencyAssociationResult(
                    currencyLookup.NormalizedQuery,
                    CurrencySnapshotFactory.CopyCurrencyInfo(associatedCurrencies[index]));
            }
        }

        return CreateInvalidTerritoryCurrencyAssociationResult(
            territoryValidation.NormalizedText,
            currencyLookup.NormalizedQuery,
            CurrencySnapshotFactory.CopyCurrencyInfo(match));
    }

    internal static CurrencyValidationError GetCurrencyDateValidationError(
        CurrencyInfo currency,
        System.DateOnly date)
    {
        if (PartialDateRules.HasKnownPrecision(currency.IntroductionDate)
            && PartialDateRules.CompareDateToInclusiveStart(
                date,
                currency.IntroductionDate) < 0)
        {
            return CurrencyValidationError.InvalidDate;
        }

        if (PartialDateRules.HasKnownPrecision(currency.WithdrawalDate)
            && PartialDateRules.CompareDateToInclusiveEnd(
                date,
                currency.WithdrawalDate) > 0)
        {
            return CurrencyValidationError.InvalidDate;
        }

        if (!PartialDateRules.HasKnownPrecision(currency.IntroductionDate))
        {
            return CurrencyValidationError.Indeterminate;
        }

        if (currency.Status == CurrencyStatus.Active)
        {
            return CurrencyValidationError.None;
        }

        if (PartialDateRules.HasKnownPrecision(currency.WithdrawalDate))
        {
            return CurrencyValidationError.None;
        }

        return CurrencyValidationError.Indeterminate;
    }

    static CurrencyValidationResult CreateValidCurrencySyntaxResult(string normalizedText)
    {
        return new CurrencyValidationResult
        {
            IsValid = true,
            IsIndeterminate = false,
            Error = CurrencyValidationError.None,
            NormalizedText = normalizedText,
            Reason = string.Empty,
            Currency = null
        };
    }

    static CurrencyValidationResult CreateMalformedAlphabeticSyntaxResult()
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.MalformedInput,
            NormalizedText = string.Empty,
            Reason = "The currency alphabetic code is malformed.",
            Currency = null
        };
    }

    static CurrencyValidationResult CreateMalformedNumericSyntaxResult()
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.MalformedInput,
            NormalizedText = string.Empty,
            Reason = "The currency numeric code is malformed.",
            Currency = null
        };
    }

    static CurrencyValidationResult CreateMalformedTerritoryResult()
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.InvalidTerritory,
            NormalizedText = string.Empty,
            Reason = "The territory alpha-2 code is malformed.",
            Currency = null
        };
    }

    static CurrencyValidationResult CreateValidCurrencyAssignmentResult(
        string normalizedText,
        CurrencyInfo currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = true,
            IsIndeterminate = false,
            Error = CurrencyValidationError.None,
            NormalizedText = normalizedText,
            Reason = string.Empty,
            Currency = currency
        };
    }

    static CurrencyValidationResult CreateValidCurrencyDateResult(
        string normalizedText,
        CurrencyInfo currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = true,
            IsIndeterminate = false,
            Error = CurrencyValidationError.None,
            NormalizedText = normalizedText,
            Reason = string.Empty,
            Currency = currency
        };
    }

    static CurrencyValidationResult CreateInvalidCurrencyDateResult(
        string normalizedText,
        CurrencyInfo currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.InvalidDate,
            NormalizedText = normalizedText,
            Reason = "The currency is not valid on the requested date.",
            Currency = currency
        };
    }

    static CurrencyValidationResult CreateIndeterminateCurrencyDateResult(
        string normalizedText,
        CurrencyInfo currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = true,
            Error = CurrencyValidationError.Indeterminate,
            NormalizedText = normalizedText,
            Reason = "The available source dates cannot determine currency validity.",
            Currency = currency
        };
    }

    static CurrencyValidationResult CreateValidTerritoryAssignmentResult(string normalizedText)
    {
        return new CurrencyValidationResult
        {
            IsValid = true,
            IsIndeterminate = false,
            Error = CurrencyValidationError.None,
            NormalizedText = normalizedText,
            Reason = string.Empty,
            Currency = null
        };
    }

    static CurrencyValidationResult CreateUnassignedTerritoryResult(string normalizedText)
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.InvalidTerritory,
            NormalizedText = normalizedText,
            Reason = "Territory alpha-2 code '" + normalizedText + "' is not assigned.",
            Currency = null
        };
    }

    static CurrencyValidationResult CreateValidTerritoryCurrencyAssociationResult(
        string normalizedText,
        CurrencyInfo currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = true,
            IsIndeterminate = false,
            Error = CurrencyValidationError.None,
            NormalizedText = normalizedText,
            Reason = string.Empty,
            Currency = currency
        };
    }

    static CurrencyValidationResult CreateInvalidTerritoryCurrencyAssociationResult(
        string normalizedTerritory,
        string normalizedCurrency,
        CurrencyInfo currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.InvalidTerritory,
            NormalizedText = normalizedCurrency,
            Reason = "Currency alphabetic code '"
                + normalizedCurrency
                + "' is not associated with territory '"
                + normalizedTerritory
                + "' on the requested date.",
            Currency = currency
        };
    }

    static CurrencyValidationResult CreateUnassignedCurrencyResult(string normalizedText)
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.Unassigned,
            NormalizedText = normalizedText,
            Reason = "Currency alphabetic code '" + normalizedText + "' is not assigned.",
            Currency = null
        };
    }

    static CurrencyValidationResult CreateInvalidCurrencyStatusResult(
        string normalizedText,
        CurrencyInfo? currency)
    {
        return new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = false,
            Error = CurrencyValidationError.InvalidStatus,
            NormalizedText = normalizedText,
            Reason = "Currency alphabetic code '"
                + normalizedText
                + "' is outside the requested scope.",
            Currency = currency
        };
    }
}
