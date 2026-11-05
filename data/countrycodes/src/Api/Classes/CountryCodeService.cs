namespace countrycodes;

/// <summary>
/// Provides the public entry point for country-code data operations.
/// </summary>
public class CountryCodeService :
    ICountryCatalog,
    ISubdivisionCatalog,
    IHistoricalCountryCatalog,
    ICountryCodeNormalizer,
    ICountryCodeValidator,
    ILocalizedNameCatalog,
    ICountrySearch,
    IGeographicRegionCatalog,
    IDataVersionProvider
{
    /// <summary>
    /// Creates a country-code service.
    /// </summary>
    public CountryCodeService()
    {
    }

    /// <summary>
    /// Executes the get countries operation.
    /// </summary>
    public Country[] GetCountries()
    {
        return CountryMapper.MapCountries(RuntimeCatalog.GetState().Countries);
    }

    /// <summary>
    /// Executes the get countries by assignment status operation.
    /// </summary>
    public Country[] GetCountriesByAssignmentStatus(CodeAssignmentStatus status)
    {
        GeneratedCountryRow[] rows = RuntimeCatalog.GetState().Countries;
        int count = 0;
        int index = 0;
        while (index < rows.Length)
        {
            if (CountryMapper.MapAssignmentStatus(rows[index].AssignmentState) == status)
            {
                count += 1;
            }

            index += 1;
        }

        GeneratedCountryRow[] matches = new GeneratedCountryRow[count];
        index = 0;
        int matchIndex = 0;
        while (index < rows.Length)
        {
            if (CountryMapper.MapAssignmentStatus(rows[index].AssignmentState) == status)
            {
                matches[matchIndex] = rows[index];
                matchIndex += 1;
            }

            index += 1;
        }

        return CountryMapper.MapCountries(matches);
    }

    /// <summary>
    /// Executes the get country flag emoji operation.
    /// </summary>
    public CountryFlagEmojiResult GetCountryFlagEmoji(string value)
    {
        return CountryFlagEmojiUtility.GetResult(value);
    }

    /// <summary>
    /// Executes the find country by alpha2 operation.
    /// </summary>
    public CountryLookupResult FindCountryByAlpha2(string value)
    {
        return CountryLookupEngine.FindAlpha2Text(value);
    }

    /// <summary>
    /// Executes the find country by alpha2 span operation.
    /// </summary>
    public CountryLookupResult FindCountryByAlpha2Span(global::System.ReadOnlySpan<char> value)
    {
        return CountryLookupEngine.FindAlpha2Characters(value);
    }

    /// <summary>
    /// Executes the find country by alpha3 operation.
    /// </summary>
    public CountryLookupResult FindCountryByAlpha3(string value)
    {
        return CountryLookupEngine.FindAlpha3Text(value);
    }

    /// <summary>
    /// Executes the find country by alpha3 span operation.
    /// </summary>
    public CountryLookupResult FindCountryByAlpha3Span(global::System.ReadOnlySpan<char> value)
    {
        return CountryLookupEngine.FindAlpha3Characters(value);
    }

    /// <summary>
    /// Executes the find country by numeric code operation.
    /// </summary>
    public CountryLookupResult FindCountryByNumericCode(string value)
    {
        return CountryLookupEngine.FindNumericText(value);
    }

    /// <summary>
    /// Executes the find country by numeric value operation.
    /// </summary>
    public CountryLookupResult FindCountryByNumericValue(int value)
    {
        return CountryLookupEngine.FindNumericValue(value);
    }

    /// <summary>
    /// Executes the find country by name operation.
    /// </summary>
    public CountryLookupResult FindCountryByName(string value)
    {
        return CountryLookupEngine.FindNameText(value);
    }

    /// <summary>
    /// Executes the find country by any identifier operation.
    /// </summary>
    public CountryLookupResult FindCountryByAnyIdentifier(string value)
    {
        return CountryLookupEngine.FindAnyText(value);
    }

    /// <summary>
    /// Executes the get required country by alpha2 operation.
    /// </summary>
    public Country GetRequiredCountryByAlpha2(string value)
    {
        return RequiredCountryLookup.GetByAlpha2(value);
    }

    /// <summary>
    /// Executes the get required country by alpha3 operation.
    /// </summary>
    public Country GetRequiredCountryByAlpha3(string value)
    {
        return RequiredCountryLookup.GetByAlpha3(value);
    }

    /// <summary>
    /// Executes the get required country by numeric code operation.
    /// </summary>
    public Country GetRequiredCountryByNumericCode(string value)
    {
        return RequiredCountryLookup.GetByNumericText(value);
    }

    /// <summary>
    /// Executes the get required country by numeric value operation.
    /// </summary>
    public Country GetRequiredCountryByNumericValue(int value)
    {
        return RequiredCountryLookup.GetByNumericValue(value);
    }

    /// <summary>
    /// Executes the get required country by name operation.
    /// </summary>
    public Country GetRequiredCountryByName(string value)
    {
        return RequiredCountryLookup.GetByName(value);
    }

    /// <summary>
    /// Executes the get required country by any identifier operation.
    /// </summary>
    public Country GetRequiredCountryByAnyIdentifier(string value)
    {
        return RequiredCountryLookup.GetByAnyIdentifier(value);
    }

    /// <summary>
    /// Executes the alpha2-to-alpha3 country-code conversion.
    /// </summary>
    public string ConvertCountryAlpha2ToAlpha3(string value)
    {
        Country country = RequiredCountryLookup.GetByAlpha2(value);
        return country.Alpha3;
    }

    /// <summary>
    /// Executes the alpha2-to-numeric country-code conversion.
    /// </summary>
    public string ConvertCountryAlpha2ToNumericCode(string value)
    {
        Country country = RequiredCountryLookup.GetByAlpha2(value);
        return country.NumericText;
    }

    /// <summary>
    /// Executes the alpha3-to-alpha2 country-code conversion.
    /// </summary>
    public string ConvertCountryAlpha3ToAlpha2(string value)
    {
        Country country = RequiredCountryLookup.GetByAlpha3(value);
        return country.Alpha2;
    }

    /// <summary>
    /// Executes the alpha3-to-numeric country-code conversion.
    /// </summary>
    public string ConvertCountryAlpha3ToNumericCode(string value)
    {
        Country country = RequiredCountryLookup.GetByAlpha3(value);
        return country.NumericText;
    }

    /// <summary>
    /// Executes the numeric-to-alpha2 country-code conversion.
    /// </summary>
    public string ConvertCountryNumericCodeToAlpha2(string value)
    {
        Country country = RequiredCountryLookup.GetByNumericText(value);
        return country.Alpha2;
    }

    /// <summary>
    /// Executes the numeric-to-alpha3 country-code conversion.
    /// </summary>
    public string ConvertCountryNumericCodeToAlpha3(string value)
    {
        Country country = RequiredCountryLookup.GetByNumericText(value);
        return country.Alpha3;
    }

    /// <summary>
    /// Executes the get subdivisions operation.
    /// </summary>
    public Subdivision[] GetSubdivisions()
    {
        return SubdivisionLookupEngine.GetAll();
    }

    /// <summary>
    /// Executes the get subdivisions by country operation.
    /// </summary>
    public Subdivision[] GetSubdivisionsByCountry(string countryAlpha2)
    {
        return SubdivisionLookupEngine.GetForCountry(countryAlpha2);
    }

    /// <summary>
    /// Executes the find subdivision by code operation.
    /// </summary>
    public SubdivisionLookupResult FindSubdivisionByCode(string value)
    {
        return SubdivisionLookupEngine.FindCodeText(value);
    }

    /// <summary>
    /// Executes the find subdivision by code span operation.
    /// </summary>
    public SubdivisionLookupResult FindSubdivisionByCodeSpan(global::System.ReadOnlySpan<char> value)
    {
        return SubdivisionLookupEngine.FindCodeCharacters(value);
    }

    /// <summary>
    /// Executes the find subdivision by country and suffix operation.
    /// </summary>
    public SubdivisionLookupResult FindSubdivisionByCountryAndSuffix(string countryAlpha2, string suffix)
    {
        return SubdivisionLookupEngine.FindCountryAndSuffix(countryAlpha2, suffix);
    }

    /// <summary>
    /// Executes the find subdivision by name operation.
    /// </summary>
    public SubdivisionLookupResult FindSubdivisionByName(string name)
    {
        return SubdivisionLookupEngine.FindNameText(name);
    }

    /// <summary>
    /// Executes the find subdivision by name in country operation.
    /// </summary>
    public SubdivisionLookupResult FindSubdivisionByNameInCountry(string countryAlpha2, string name)
    {
        return SubdivisionLookupEngine.FindNameInCountry(countryAlpha2, name);
    }

    /// <summary>
    /// Executes the find subdivision parent operation.
    /// </summary>
    public SubdivisionLookupResult FindSubdivisionParent(string code)
    {
        return SubdivisionLookupEngine.FindParent(code);
    }

    /// <summary>
    /// Executes the get subdivision children operation.
    /// </summary>
    public Subdivision[] GetSubdivisionChildren(string code)
    {
        return SubdivisionLookupEngine.GetChildren(code);
    }

    /// <summary>
    /// Executes the get subdivision ancestors operation.
    /// </summary>
    public Subdivision[] GetSubdivisionAncestors(string code)
    {
        return SubdivisionLookupEngine.GetAncestors(code);
    }

    /// <summary>
    /// Executes the get subdivision descendants operation.
    /// </summary>
    public Subdivision[] GetSubdivisionDescendants(string code)
    {
        return SubdivisionLookupEngine.GetDescendants(code);
    }

    /// <summary>
    /// Executes the resolve country for subdivision operation.
    /// </summary>
    public CountryLookupResult ResolveCountryForSubdivision(string code)
    {
        return SubdivisionLookupEngine.ResolveCountry(code);
    }

    /// <summary>
    /// Executes the get required subdivision by code operation.
    /// </summary>
    public Subdivision GetRequiredSubdivisionByCode(string code)
    {
        return SubdivisionLookupEngine.GetRequiredByCode(code);
    }

    /// <summary>
    /// Executes the get subdivisions by status operation.
    /// </summary>
    public Subdivision[] GetSubdivisionsByStatus(SubdivisionStatus status)
    {
        return SubdivisionLookupEngine.GetByStatus(status);
    }

    /// <summary>
    /// Executes the find historical country by former code operation.
    /// </summary>
    public HistoricalLookupResult FindHistoricalCountryByFormerCode(string value)
    {
        return HistoricalLookupEngine.FindFormerCode(value);
    }

    /// <summary>
    /// Executes the find historical country by name operation.
    /// </summary>
    public HistoricalLookupResult FindHistoricalCountryByName(string value)
    {
        return HistoricalLookupEngine.FindName(value);
    }

    /// <summary>
    /// Executes the find country change by alpha4 operation.
    /// </summary>
    public HistoricalLookupResult FindCountryChangeByAlpha4(string value)
    {
        return HistoricalLookupEngine.FindChangeAlpha4(value);
    }

    /// <summary>
    /// Executes the get country change history operation.
    /// </summary>
    public CountryChange[] GetCountryChangeHistory(string value)
    {
        return HistoricalLookupEngine.GetChangeHistory(value);
    }

    /// <summary>
    /// Executes the resolve historical successors operation.
    /// </summary>
    public HistoricalLookupResult ResolveHistoricalSuccessors(string value)
    {
        return HistoricalLookupEngine.ResolveSuccessors(value);
    }

    /// <summary>
    /// Executes the get historical countries operation.
    /// </summary>
    public HistoricalCountry[] GetHistoricalCountries()
    {
        return HistoricalLookupEngine.GetAll();
    }

    /// <summary>
    /// Executes the get country changes by kind operation.
    /// </summary>
    public CountryChange[] GetCountryChangesByKind(CountryChangeKind kind)
    {
        return HistoricalLookupEngine.GetChangesByKind(kind);
    }

    /// <summary>
    /// Executes the normalize country alpha2 operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryAlpha2(string value, bool permissive)
    {
        return NormalizationEngine.NormalizeAlpha2Text(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country alpha2 span operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryAlpha2Span(global::System.ReadOnlySpan<char> value, bool permissive)
    {
        return NormalizationEngine.NormalizeAlpha2Characters(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country alpha3 operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryAlpha3(string value, bool permissive)
    {
        return NormalizationEngine.NormalizeAlpha3Text(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country alpha3 span operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryAlpha3Span(global::System.ReadOnlySpan<char> value, bool permissive)
    {
        return NormalizationEngine.NormalizeAlpha3Characters(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country numeric code operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryNumericCode(string value, bool permissive)
    {
        return NormalizationEngine.NormalizeNumericText(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country numeric code span operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryNumericCodeSpan(global::System.ReadOnlySpan<char> value, bool permissive)
    {
        return NormalizationEngine.NormalizeNumericCharacters(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country numeric value operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryNumericValue(int value)
    {
        return NormalizationEngine.NormalizeNumericValue(value);
    }

    /// <summary>
    /// Executes the normalize country alpha4 operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryAlpha4(string value, bool permissive)
    {
        return NormalizationEngine.NormalizeAlpha4Text(value, permissive);
    }

    /// <summary>
    /// Executes the normalize country alpha4 span operation.
    /// </summary>
    public CodeNormalizationResult NormalizeCountryAlpha4Span(global::System.ReadOnlySpan<char> value, bool permissive)
    {
        return NormalizationEngine.NormalizeAlpha4Characters(value, permissive);
    }

    /// <summary>
    /// Executes the normalize any country identifier operation.
    /// </summary>
    public CodeNormalizationResult NormalizeAnyCountryIdentifier(string value, bool permissive)
    {
        return NormalizationEngine.NormalizeAnyText(value, permissive);
    }

    /// <summary>
    /// Executes the normalize any country identifier span operation.
    /// </summary>
    public CodeNormalizationResult NormalizeAnyCountryIdentifierSpan(global::System.ReadOnlySpan<char> value, bool permissive)
    {
        return NormalizationEngine.NormalizeAnyCharacters(value, permissive);
    }

    /// <summary>
    /// Executes the normalize subdivision code operation.
    /// </summary>
    public CodeNormalizationResult NormalizeSubdivisionCode(string value, bool permissive)
    {
        return NormalizationEngine.NormalizeSubdivisionText(value, permissive);
    }

    /// <summary>
    /// Executes the normalize subdivision code span operation.
    /// </summary>
    public CodeNormalizationResult NormalizeSubdivisionCodeSpan(global::System.ReadOnlySpan<char> value, bool permissive)
    {
        return NormalizationEngine.NormalizeSubdivisionCharacters(value, permissive);
    }

    /// <summary>
    /// Executes the normalize locale tag operation.
    /// </summary>
    public CodeNormalizationResult NormalizeLocaleTag(string value)
    {
        return NormalizationEngine.NormalizeLocale(value);
    }

    /// <summary>
    /// Executes the normalize name key operation.
    /// </summary>
    public CodeNormalizationResult NormalizeNameKey(string value, bool foldDiacritics, bool foldPunctuation)
    {
        return NormalizationEngine.NormalizeName(
            value,
            foldDiacritics,
            foldPunctuation);
    }

    /// <summary>
    /// Executes the validate country alpha2 operation.
    /// </summary>
    public ValidationResult ValidateCountryAlpha2(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateAlpha2(value, policy);
    }

    /// <summary>
    /// Executes the validate country alpha3 operation.
    /// </summary>
    public ValidationResult ValidateCountryAlpha3(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateAlpha3(value, policy);
    }

    /// <summary>
    /// Executes the validate country numeric code operation.
    /// </summary>
    public ValidationResult ValidateCountryNumericCode(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateNumeric3(value, policy);
    }

    /// <summary>
    /// Executes the validate country alpha4 operation.
    /// </summary>
    public ValidationResult ValidateCountryAlpha4(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateAlpha4(value, policy);
    }

    /// <summary>
    /// Executes the validate any country code operation.
    /// </summary>
    public ValidationResult ValidateAnyCountryCode(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateAnyCountryCode(value, policy);
    }

    /// <summary>
    /// Executes the validate subdivision code operation.
    /// </summary>
    public ValidationResult ValidateSubdivisionCode(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateSubdivision(value, policy);
    }

    /// <summary>
    /// Executes the validate locale tag operation.
    /// </summary>
    public ValidationResult ValidateLocaleTag(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateLocale(value, policy);
    }

    /// <summary>
    /// Executes the validate name input operation.
    /// </summary>
    public ValidationResult ValidateNameInput(string value, ValidationPolicy policy)
    {
        return ValidationEngine.ValidateName(value, policy);
    }

    /// <summary>
    /// Executes the get supported locales operation.
    /// </summary>
    public string[] GetSupportedLocales()
    {
        return LocalizedNameCatalogEngine.GetSupportedLocales();
    }

    /// <summary>
    /// Executes the is locale available operation.
    /// </summary>
    public bool IsLocaleAvailable(string locale)
    {
        return LocalizedNameCatalogEngine.IsLocaleAvailable(locale);
    }

    /// <summary>
    /// Executes the get localized country name operation.
    /// </summary>
    public string? GetLocalizedCountryName(string countryAlpha2, string locale, bool useFallback)
    {
        return LocalizedNameCatalogEngine.GetCountryName(
            countryAlpha2,
            locale,
            useFallback);
    }

    /// <summary>
    /// Executes the get localized subdivision name operation.
    /// </summary>
    public string? GetLocalizedSubdivisionName(string subdivisionCode, string locale, bool useFallback)
    {
        return LocalizedNameCatalogEngine.GetSubdivisionName(
            subdivisionCode,
            locale,
            useFallback);
    }

    /// <summary>
    /// Executes the get localized region name operation.
    /// </summary>
    public string? GetLocalizedRegionName(string m49Code, string locale, bool useFallback)
    {
        return LocalizedNameCatalogEngine.GetRegionName(
            m49Code,
            locale,
            useFallback);
    }

    /// <summary>
    /// Executes the get locale fallback chain operation.
    /// </summary>
    public string[] GetLocaleFallbackChain(string locale)
    {
        return LocalizedNameCatalogEngine.GetFallbackChain(locale);
    }

    /// <summary>
    /// Executes the search countries operation.
    /// </summary>
    public CountrySearchMatch[] SearchCountries(string query, SearchOptions options)
    {
        return SearchEngine.SearchCountries(query, options);
    }

    /// <summary>
    /// Executes the search subdivisions operation.
    /// </summary>
    public SubdivisionSearchMatch[] SearchSubdivisions(string query, SearchOptions options)
    {
        return SubdivisionSearchEngine.SearchGlobal(query, options);
    }

    /// <summary>
    /// Executes the search subdivisions in country operation.
    /// </summary>
    public SubdivisionSearchMatch[] SearchSubdivisionsInCountry(
        string countryAlpha2,
        string query,
        SearchOptions options)
    {
        return SubdivisionSearchEngine.SearchInCountry(
            countryAlpha2,
            query,
            options);
    }

    /// <summary>
    /// Executes the discover countries by subdivision operation.
    /// </summary>
    public CountrySearchMatch[] DiscoverCountriesBySubdivision(string query, SearchOptions options)
    {
        return SubdivisionCountryDiscoveryEngine.Discover(query, options);
    }

    /// <summary>
    /// Executes the get geographic regions operation.
    /// </summary>
    public GeographicRegion[] GetGeographicRegions()
    {
        return GeographicRegionCatalogEngine.GetAll();
    }

    /// <summary>
    /// Executes the get geographic regions by level operation.
    /// </summary>
    public GeographicRegion[] GetGeographicRegionsByLevel(GeographicRegionLevel level)
    {
        return GeographicRegionCatalogEngine.GetByLevel(level);
    }

    /// <summary>
    /// Executes the find geographic region by m49 operation.
    /// </summary>
    public GeographicRegion? FindGeographicRegionByM49(string m49Code)
    {
        return GeographicRegionCatalogEngine.FindByM49(m49Code);
    }

    /// <summary>
    /// Executes the get geographic region ancestry operation.
    /// </summary>
    public GeographicRegion[] GetGeographicRegionAncestry(string m49Code)
    {
        return GeographicRegionCatalogEngine.GetAncestry(m49Code);
    }

    /// <summary>
    /// Executes the get geographic regions for country operation.
    /// </summary>
    public GeographicRegion[] GetGeographicRegionsForCountry(string countryAlpha2)
    {
        return GeographicRegionCatalogEngine.GetForCountry(countryAlpha2);
    }

    /// <summary>
    /// Executes the get countries for geographic region operation.
    /// </summary>
    public Country[] GetCountriesForGeographicRegion(string m49Code)
    {
        return GeographicRegionCatalogEngine.GetCountries(m49Code);
    }

    /// <summary>
    /// Executes the get data version operation.
    /// </summary>
    public DataVersion GetDataVersion()
    {
        string[] sourceRevisions =
        {
            CountryDataMetadata.CldrSourceRevision,
            CountryDataMetadata.DebianIsoCodesSourceRevision,
            CountryDataMetadata.IanaLanguageSubtagRegistrySourceRevision,
            CountryDataMetadata.IetfBcp47SourceRevision,
            CountryDataMetadata.UnM49SourceRevision,
            CountryDataMetadata.UnicodeSpecificationsSourceRevision
        };
        return new DataVersion(
            CountryCodeConstants.SchemaVersion,
            CountryDataMetadata.DatasetVersion,
            CountryDataMetadata.EffectiveDate,
            CountryDataMetadata.EffectiveDate,
            CountryDataMetadata.GeneratorVersion,
            sourceRevisions,
            CountryDataMetadata.ContentChecksum);
    }
}
