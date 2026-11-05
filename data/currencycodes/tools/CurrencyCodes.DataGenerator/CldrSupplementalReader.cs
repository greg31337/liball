using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CurrencyCodes.DataGenerator;

internal sealed class CldrSupplementalReader
{
    internal CldrFractionData ReadFractionData(string path)
    {
        XDocument document = LoadDocument(path);

        XElement root = document.Root ?? throw new InvalidDataException("CLDR supplemental-data XML has no root element.");
        RequireElementName(root, "supplementalData");
        XElement currencyData = GetSingleChild(root, "currencyData");
        XElement fractions = GetSingleChild(currencyData, "fractions");

        List<FractionInput> inputs = new List<FractionInput>();
        foreach (XNode node in fractions.Nodes())
        {
            if (node is XElement element)
            {
                RequireElementName(element, "info");
                inputs.Add(ReadInput(element));
            }
            else if (node is XText text && string.IsNullOrWhiteSpace(text.Value))
            {
            }
            else if (node is XComment)
            {
            }
            else
            {
                throw Invalid(node, "Unexpected content in CLDR fractions.");
            }
        }

        if (inputs.Count == 0)
        {
            throw Invalid(fractions, "CLDR fractions must contain at least one info element.");
        }

        ValidateUniqueCodes(inputs);
        FractionInput defaultInput = GetDefaultInput(inputs, fractions);
        if (!defaultInput.HasDigits || !defaultInput.HasRounding)
        {
            throw Invalid(defaultInput.Source, "The CLDR DEFAULT rule must define digits and rounding.");
        }

        CldrFractionRule defaultRule = ResolveDefaultRule(defaultInput);
        List<CldrFractionRule> explicitRules = new List<CldrFractionRule>(inputs.Count - 1);
        foreach (FractionInput input in inputs)
        {
            if (input.CurrencyCode != "DEFAULT")
            {
                explicitRules.Add(ResolveExplicitRule(input, defaultRule));
            }
        }

        return new CldrFractionData(defaultRule, explicitRules.AsReadOnly());
    }

    internal CldrCurrencyUsageData ReadCurrencyUsageData(string path)
    {
        XDocument document = LoadDocument(path);
        XElement root = document.Root ?? throw new InvalidDataException("CLDR supplemental-data XML has no root element.");
        RequireElementName(root, "supplementalData");
        XElement currencyData = GetSingleChild(root, "currencyData");

        List<CldrCurrencyUsage> usages = new List<CldrCurrencyUsage>();
        HashSet<string> territoryCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement element in currencyData.Elements())
        {
            if (element.Name == "region")
            {
                string territoryCode = ReadTerritoryCode(element);
                if (!territoryCodes.Add(territoryCode))
                {
                    throw Invalid(element, $"Duplicate CLDR currency region '{territoryCode}'.");
                }

                ReadTerritoryUsages(element, territoryCode, usages);
            }
            else if (element.Name != "fractions")
            {
                throw Invalid(element, $"Unexpected element '{element.Name}' in CLDR currencyData.");
            }
        }

        if (territoryCodes.Count == 0)
        {
            throw Invalid(currencyData, "CLDR currencyData must contain at least one region.");
        }

        return new CldrCurrencyUsageData(usages.AsReadOnly());
    }

    internal CldrTerritoryCodeData ReadTerritoryCodeData(
        string supplementalDataPath,
        string supplementalMetadataPath)
    {
        XDocument dataDocument = LoadDocument(supplementalDataPath);
        XElement dataRoot = dataDocument.Root
            ?? throw new InvalidDataException("CLDR supplemental-data XML has no root element.");
        RequireElementName(dataRoot, "supplementalData");
        XElement codeMappings = GetSingleChild(dataRoot, "codeMappings");
        List<CldrTerritoryCode> territoryCodes = ReadTerritoryCodes(codeMappings);

        XDocument metadataDocument = LoadDocument(supplementalMetadataPath);
        XElement metadataRoot = metadataDocument.Root
            ?? throw new InvalidDataException("CLDR supplemental-metadata XML has no root element.");
        RequireElementName(metadataRoot, "supplementalData");
        XElement metadata = GetSingleChild(metadataRoot, "metadata");
        XElement aliasData = GetSingleChild(metadata, "alias");
        List<CldrTerritoryAlias> aliases = ReadTerritoryAliases(aliasData);

        return new CldrTerritoryCodeData(territoryCodes.AsReadOnly(), aliases.AsReadOnly());
    }

    internal IReadOnlyList<CldrCurrencyCodeMapping> ReadCurrencyCodeMappings(string path)
    {
        XDocument document = LoadDocument(path);
        XElement root = document.Root
            ?? throw new InvalidDataException("CLDR supplemental-data XML has no root element.");
        RequireElementName(root, "supplementalData");
        XElement codeMappings = GetSingleChild(root, "codeMappings");
        List<CldrCurrencyCodeMapping> mappings = new List<CldrCurrencyCodeMapping>();
        HashSet<string> alphabeticCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement element in codeMappings.Elements())
        {
            if (element.Name != "currencyCodes")
            {
                continue;
            }

            CldrCurrencyCodeMapping mapping = ReadCurrencyCodeMapping(element);
            if (!alphabeticCodes.Add(mapping.AlphabeticCode))
            {
                throw Invalid(element, $"Duplicate CLDR currency code mapping '{mapping.AlphabeticCode}'.");
            }

            mappings.Add(mapping);
        }

        if (mappings.Count == 0)
        {
            throw Invalid(codeMappings, "CLDR codeMappings must contain at least one currencyCodes element.");
        }

        return mappings.AsReadOnly();
    }

    private static CldrCurrencyCodeMapping ReadCurrencyCodeMapping(XElement element)
    {
        if (element.FirstNode != null)
        {
            throw Invalid(element, "CLDR currencyCodes elements must be empty.");
        }

        string? alphabeticCode = null;
        string? sourceNumericCodeText = null;
        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name == "type")
            {
                alphabeticCode = SetOnce(alphabeticCode, attribute);
            }
            else if (attribute.Name == "numeric")
            {
                sourceNumericCodeText = SetOnce(sourceNumericCodeText, attribute);
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CLDR currencyCodes.");
            }
        }

        if (alphabeticCode == null || sourceNumericCodeText == null)
        {
            throw Invalid(element, "CLDR currencyCodes requires type and numeric.");
        }

        if (!IsAsciiCurrencyCode(alphabeticCode))
        {
            throw Invalid(element, "CLDR currencyCodes type must be three uppercase ASCII letters.");
        }

        if (sourceNumericCodeText.Length > 3
            || !ContainsOnlyAsciiDigits(sourceNumericCodeText)
            || !int.TryParse(
                sourceNumericCodeText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int numericCodeValue))
        {
            throw Invalid(element, "CLDR currencyCodes numeric must be one to three ASCII digits.");
        }

        return new CldrCurrencyCodeMapping(
            alphabeticCode,
            sourceNumericCodeText,
            sourceNumericCodeText.PadLeft(3, '0'),
            numericCodeValue);
    }

    private static List<CldrTerritoryCode> ReadTerritoryCodes(XElement codeMappings)
    {
        List<CldrTerritoryCode> territoryCodes = new List<CldrTerritoryCode>();
        HashSet<string> alpha2Codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (XElement element in codeMappings.Elements())
        {
            if (element.Name != "territoryCodes")
            {
                continue;
            }

            CldrTerritoryCode territoryCode = ReadTerritoryCodeMapping(element);
            if (!alpha2Codes.Add(territoryCode.Alpha2Code))
            {
                throw Invalid(element, $"Duplicate CLDR territory alpha-2 code '{territoryCode.Alpha2Code}'.");
            }

            territoryCodes.Add(territoryCode);
        }

        if (territoryCodes.Count == 0)
        {
            throw Invalid(codeMappings, "CLDR codeMappings must contain at least one territoryCodes element.");
        }

        return territoryCodes;
    }

    private static CldrTerritoryCode ReadTerritoryCodeMapping(XElement element)
    {
        if (element.FirstNode != null)
        {
            throw Invalid(element, "CLDR territoryCodes elements must be empty.");
        }

        string? alpha2Code = null;
        string? alpha3Code = null;
        string? numericCodeText = null;
        string? fips10Code = null;
        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name == "type")
            {
                alpha2Code = SetOnce(alpha2Code, attribute);
            }
            else if (attribute.Name == "alpha3")
            {
                alpha3Code = SetOnce(alpha3Code, attribute);
            }
            else if (attribute.Name == "numeric")
            {
                numericCodeText = SetOnce(numericCodeText, attribute);
            }
            else if (attribute.Name == "fips10")
            {
                fips10Code = SetOnce(fips10Code, attribute);
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CLDR territoryCodes.");
            }
        }

        if (alpha2Code == null)
        {
            throw Invalid(element, "CLDR territoryCodes is missing type.");
        }

        if (!IsAsciiTerritoryCode(alpha2Code))
        {
            throw Invalid(element, "CLDR territoryCodes type must be two uppercase ASCII letters.");
        }

        if (alpha3Code != null && !IsAsciiCurrencyCode(alpha3Code))
        {
            throw Invalid(element, "CLDR territoryCodes alpha3 must be three uppercase ASCII letters.");
        }

        if (fips10Code != null && !IsAsciiTerritoryCode(fips10Code))
        {
            throw Invalid(element, "CLDR territoryCodes fips10 must be two uppercase ASCII letters.");
        }

        int numericCodeValue = ReadTerritoryNumericCode(element, numericCodeText, out bool hasNumericCode);
        return new CldrTerritoryCode(
            alpha2Code,
            alpha3Code,
            numericCodeText,
            hasNumericCode,
            numericCodeValue);
    }

    private static int ReadTerritoryNumericCode(
        XElement source,
        string? numericCodeText,
        out bool hasNumericCode)
    {
        hasNumericCode = numericCodeText != null;
        if (numericCodeText == null)
        {
            return 0;
        }

        if (numericCodeText.Length != 3
            || !ContainsOnlyAsciiDigits(numericCodeText)
            || !int.TryParse(
                numericCodeText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int numericCodeValue))
        {
            throw Invalid(source, "CLDR territoryCodes numeric must be exactly three ASCII digits.");
        }

        return numericCodeValue;
    }

    private static List<CldrTerritoryAlias> ReadTerritoryAliases(XElement aliasData)
    {
        List<CldrTerritoryAlias> aliases = new List<CldrTerritoryAlias>();
        HashSet<string> aliasCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement element in aliasData.Elements())
        {
            if (element.Name != "territoryAlias")
            {
                continue;
            }

            CldrTerritoryAlias alias = ReadTerritoryAlias(element);
            if (!aliasCodes.Add(alias.AliasCode))
            {
                throw Invalid(element, $"Duplicate CLDR territory alias '{alias.AliasCode}'.");
            }

            aliases.Add(alias);
        }

        if (aliases.Count == 0)
        {
            throw Invalid(aliasData, "CLDR alias data must contain at least one territoryAlias element.");
        }

        return aliases;
    }

    private static CldrTerritoryAlias ReadTerritoryAlias(XElement element)
    {
        if (element.FirstNode != null)
        {
            throw Invalid(element, "CLDR territoryAlias elements must be empty.");
        }

        string? aliasCode = null;
        string? reason = null;
        string? replacementText = null;
        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name == "type")
            {
                aliasCode = SetOnce(aliasCode, attribute);
            }
            else if (attribute.Name == "reason")
            {
                reason = SetOnce(reason, attribute);
            }
            else if (attribute.Name == "replacement")
            {
                replacementText = SetOnce(replacementText, attribute);
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CLDR territoryAlias.");
            }
        }

        if (aliasCode == null || reason == null || replacementText == null)
        {
            throw Invalid(element, "CLDR territoryAlias requires type, reason, and replacement.");
        }

        if (!IsTerritoryAliasCode(aliasCode))
        {
            throw Invalid(element, "CLDR territoryAlias type must be an alpha-2, alpha-3, or numeric-3 code.");
        }

        string[] replacementCodes = replacementText.Split(' ');
        foreach (string replacementCode in replacementCodes)
        {
            if (!IsTerritoryReplacementCode(replacementCode))
            {
                throw Invalid(
                    element,
                    "CLDR territoryAlias replacement must be one or more space-separated alpha-2 or numeric-3 codes.");
            }
        }

        return new CldrTerritoryAlias(
            aliasCode,
            reason,
            replacementText,
            Array.AsReadOnly(replacementCodes));
    }

    private static bool IsTerritoryAliasCode(string value)
    {
        return IsAsciiTerritoryCode(value)
            || IsAsciiCurrencyCode(value)
            || IsAsciiNumericCode(value);
    }

    private static bool IsTerritoryReplacementCode(string value)
    {
        return IsAsciiTerritoryCode(value) || IsAsciiNumericCode(value);
    }

    private static bool IsAsciiNumericCode(string value)
    {
        return value.Length == 3 && ContainsOnlyAsciiDigits(value);
    }

    private static XDocument LoadDocument(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A CLDR supplemental-data XML path is required.", nameof(path));
        }

        XmlReaderSettings settings = new XmlReaderSettings
        {
            CloseInput = true,
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = false,
            IgnoreProcessingInstructions = false,
            IgnoreWhitespace = false,
            XmlResolver = null
        };

        using FileStream stream = File.OpenRead(path);
        using XmlReader reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
    }

    private static string ReadTerritoryCode(XElement region)
    {
        string? territoryCode = null;
        foreach (XAttribute attribute in region.Attributes())
        {
            if (attribute.Name == "iso3166")
            {
                territoryCode = SetOnce(territoryCode, attribute);
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CLDR currency region.");
            }
        }

        if (territoryCode == null)
        {
            throw Invalid(region, "CLDR currency region is missing iso3166.");
        }

        if (!IsAsciiTerritoryCode(territoryCode))
        {
            throw Invalid(region, "CLDR currency region iso3166 must be two uppercase ASCII letters.");
        }

        return territoryCode;
    }

    private static void ReadTerritoryUsages(
        XElement region,
        string territoryCode,
        List<CldrCurrencyUsage> usages)
    {
        foreach (XNode node in region.Nodes())
        {
            if (node is XElement element)
            {
                RequireElementName(element, "currency");
                usages.Add(ReadCurrencyUsage(element, territoryCode));
            }
            else if (node is XText text && string.IsNullOrWhiteSpace(text.Value))
            {
            }
            else if (node is XComment)
            {
            }
            else
            {
                throw Invalid(node, $"Unexpected content in CLDR currency region '{territoryCode}'.");
            }
        }
    }

    private static CldrCurrencyUsage ReadCurrencyUsage(XElement element, string territoryCode)
    {
        if (element.FirstNode != null)
        {
            throw Invalid(element, "CLDR currency elements must be empty.");
        }

        string? currencyCode = null;
        string? fromText = null;
        string? toText = null;
        string? tenderText = null;
        string? startTimeZone = null;
        string? endTimeZone = null;

        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name == "iso4217")
            {
                currencyCode = SetOnce(currencyCode, attribute);
            }
            else if (attribute.Name == "from")
            {
                fromText = SetOnce(fromText, attribute);
            }
            else if (attribute.Name == "to")
            {
                toText = SetOnce(toText, attribute);
            }
            else if (attribute.Name == "tender")
            {
                tenderText = SetOnce(tenderText, attribute);
            }
            else if (attribute.Name == "tz")
            {
                startTimeZone = SetOnce(startTimeZone, attribute);
            }
            else if (attribute.Name == "to-tz")
            {
                endTimeZone = SetOnce(endTimeZone, attribute);
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CLDR currency.");
            }
        }

        if (currencyCode == null)
        {
            throw Invalid(element, "CLDR currency is missing iso4217.");
        }

        if (!IsAsciiCurrencyCode(currencyCode))
        {
            throw Invalid(element, "CLDR currency iso4217 must be three uppercase ASCII letters.");
        }

        bool tender = ReadTender(element, tenderText, out bool tenderExplicit);
        IsoPartialDate sourceStartDate = ParsePartialDate(element, "from", fromText);
        IsoPartialDate sourceEndDate = ParsePartialDate(element, "to", toText);
        bool hasStartDate = sourceStartDate.IsKnown;
        bool hasEndDate = sourceEndDate.IsKnown;
        DateOnly startDate = hasStartDate ? NormalizeStartDate(sourceStartDate) : default;
        DateOnly endDate = hasEndDate ? NormalizeEndDate(sourceEndDate) : default;

        return new CldrCurrencyUsage(
            territoryCode,
            currencyCode,
            sourceStartDate,
            sourceEndDate,
            hasStartDate,
            startDate,
            hasEndDate,
            endDate,
            tender,
            tenderExplicit,
            startTimeZone,
            endTimeZone);
    }

    private static bool ReadTender(XElement source, string? text, out bool tenderExplicit)
    {
        tenderExplicit = text != null;
        if (text == null || text == "true")
        {
            return true;
        }

        if (text == "false")
        {
            return false;
        }

        throw Invalid(source, "CLDR currency tender must be 'true' or 'false'.");
    }

    private static IsoPartialDate ParsePartialDate(XElement source, string attributeName, string? text)
    {
        if (text == null)
        {
            return new IsoPartialDate(false, 0, 0, 0, IsoDatePrecision.Unknown);
        }

        if (HasAsciiDateShape(text, 4, -1, -1)
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int year)
            && year >= 1
            && year <= 9999)
        {
            return new IsoPartialDate(true, year, 0, 0, IsoDatePrecision.Year);
        }

        if (HasAsciiDateShape(text, 7, 4, -1)
            && DateOnly.TryParseExact(
                text + "-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly monthDate))
        {
            return new IsoPartialDate(true, monthDate.Year, monthDate.Month, 0, IsoDatePrecision.Month);
        }

        if (HasAsciiDateShape(text, 10, 4, 7)
            && DateOnly.TryParseExact(
                text,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly dayDate))
        {
            return new IsoPartialDate(true, dayDate.Year, dayDate.Month, dayDate.Day, IsoDatePrecision.Day);
        }

        throw Invalid(source, $"CLDR currency {attributeName} must be a valid yyyy, yyyy-MM, or yyyy-MM-dd date.");
    }

    private static bool HasAsciiDateShape(string text, int length, int firstHyphen, int secondHyphen)
    {
        if (text.Length != length)
        {
            return false;
        }

        for (int index = 0; index < text.Length; index++)
        {
            if (index == firstHyphen || index == secondHyphen)
            {
                if (text[index] != '-')
                {
                    return false;
                }
            }
            else if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static DateOnly NormalizeStartDate(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 1 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day ? date.Day : 1;
        return new DateOnly(date.Year, month, day);
    }

    private static DateOnly NormalizeEndDate(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 12 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day
            ? date.Day
            : DateTime.DaysInMonth(date.Year, month);
        return new DateOnly(date.Year, month, day);
    }

    private static FractionInput ReadInput(XElement element)
    {
        if (element.FirstNode != null)
        {
            throw Invalid(element, "CLDR fraction info elements must be empty.");
        }

        string? currencyCode = null;
        string? digitsText = null;
        string? roundingText = null;
        string? cashDigitsText = null;
        string? cashRoundingText = null;

        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name == "iso4217")
            {
                currencyCode = SetOnce(currencyCode, attribute);
            }
            else if (attribute.Name == "digits")
            {
                digitsText = SetOnce(digitsText, attribute);
            }
            else if (attribute.Name == "rounding")
            {
                roundingText = SetOnce(roundingText, attribute);
            }
            else if (attribute.Name == "cashDigits")
            {
                cashDigitsText = SetOnce(cashDigitsText, attribute);
            }
            else if (attribute.Name == "cashRounding")
            {
                cashRoundingText = SetOnce(cashRoundingText, attribute);
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CLDR fraction info.");
            }
        }

        if (currencyCode == null)
        {
            throw Invalid(element, "CLDR fraction info is missing iso4217.");
        }

        if (currencyCode != "DEFAULT" && !IsAsciiCurrencyCode(currencyCode))
        {
            throw Invalid(element, "CLDR fraction iso4217 must be DEFAULT or three uppercase ASCII letters.");
        }

        int digits = ParseOptionalNonNegativeInteger(element, "digits", digitsText, out bool hasDigits);
        int rounding = ParseOptionalNonNegativeInteger(element, "rounding", roundingText, out bool hasRounding);
        int cashDigits = ParseOptionalNonNegativeInteger(element, "cashDigits", cashDigitsText, out bool hasCashDigits);
        int cashRounding = ParseOptionalNonNegativeInteger(element, "cashRounding", cashRoundingText, out bool hasCashRounding);

        return new FractionInput(
            element,
            currencyCode,
            hasDigits,
            digits,
            hasRounding,
            rounding,
            hasCashDigits,
            cashDigits,
            hasCashRounding,
            cashRounding);
    }

    private static string SetOnce(string? currentValue, XAttribute attribute)
    {
        if (currentValue != null)
        {
            throw Invalid(attribute, $"Duplicate CLDR attribute '{attribute.Name}'.");
        }

        if (attribute.Value.Length == 0)
        {
            throw Invalid(attribute, $"CLDR attribute '{attribute.Name}' must not be empty.");
        }

        return attribute.Value;
    }

    private static int ParseOptionalNonNegativeInteger(
        XElement source,
        string attributeName,
        string? text,
        out bool hasValue)
    {
        hasValue = text != null;
        if (text == null)
        {
            return 0;
        }

        if (!ContainsOnlyAsciiDigits(text)
            || !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            throw Invalid(source, $"CLDR fraction {attributeName} must be a non-negative ASCII integer.");
        }

        return value;
    }

    private static bool ContainsOnlyAsciiDigits(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (char character in text)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiCurrencyCode(string value)
    {
        if (value.Length != 3)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < 'A' || character > 'Z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiTerritoryCode(string value)
    {
        if (value.Length != 2)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < 'A' || character > 'Z')
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateUniqueCodes(List<FractionInput> inputs)
    {
        HashSet<string> codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (FractionInput input in inputs)
        {
            if (!codes.Add(input.CurrencyCode))
            {
                throw Invalid(input.Source, $"Duplicate CLDR fraction rule for '{input.CurrencyCode}'.");
            }
        }
    }

    private static FractionInput GetDefaultInput(List<FractionInput> inputs, XElement fractions)
    {
        foreach (FractionInput input in inputs)
        {
            if (input.CurrencyCode == "DEFAULT")
            {
                return input;
            }
        }

        throw Invalid(fractions, "CLDR fractions are missing the DEFAULT rule.");
    }

    private static CldrFractionRule ResolveDefaultRule(FractionInput input)
    {
        int cashDigits = input.HasCashDigits ? input.CashDigits : input.Digits;
        int cashRounding = input.HasCashRounding ? input.CashRounding : input.Rounding;
        return new CldrFractionRule(
            input.CurrencyCode,
            input.Digits,
            input.Rounding,
            cashDigits,
            cashRounding,
            true,
            true,
            input.HasCashDigits,
            input.HasCashRounding);
    }

    private static CldrFractionRule ResolveExplicitRule(FractionInput input, CldrFractionRule defaultRule)
    {
        int digits = input.HasDigits ? input.Digits : defaultRule.Digits;
        int rounding = input.HasRounding ? input.Rounding : defaultRule.Rounding;
        int cashDigits = input.HasCashDigits ? input.CashDigits : digits;
        int cashRounding = input.HasCashRounding ? input.CashRounding : rounding;
        return new CldrFractionRule(
            input.CurrencyCode,
            digits,
            rounding,
            cashDigits,
            cashRounding,
            input.HasDigits,
            input.HasRounding,
            input.HasCashDigits,
            input.HasCashRounding);
    }

    private static XElement GetSingleChild(XElement parent, string childName)
    {
        XElement? result = null;
        foreach (XElement element in parent.Elements())
        {
            if (element.Name == childName)
            {
                if (result != null)
                {
                    throw Invalid(element, $"Element '{parent.Name}' contains more than one '{childName}'.");
                }

                result = element;
            }
        }

        if (result == null)
        {
            throw Invalid(parent, $"Element '{parent.Name}' is missing '{childName}'.");
        }

        return result;
    }

    private static void RequireElementName(XElement element, string expectedName)
    {
        if (element.Name != expectedName)
        {
            throw Invalid(element, $"Expected '{expectedName}' but found '{element.Name}'.");
        }
    }

    private static InvalidDataException Invalid(XObject source, string message)
    {
        if (source is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            return new InvalidDataException($"{message} Line {lineInfo.LineNumber}, position {lineInfo.LinePosition}.");
        }

        return new InvalidDataException(message);
    }

    private sealed class FractionInput
    {
        internal FractionInput(
            XElement source,
            string currencyCode,
            bool hasDigits,
            int digits,
            bool hasRounding,
            int rounding,
            bool hasCashDigits,
            int cashDigits,
            bool hasCashRounding,
            int cashRounding)
        {
            Source = source;
            CurrencyCode = currencyCode;
            HasDigits = hasDigits;
            Digits = digits;
            HasRounding = hasRounding;
            Rounding = rounding;
            HasCashDigits = hasCashDigits;
            CashDigits = cashDigits;
            HasCashRounding = hasCashRounding;
            CashRounding = cashRounding;
        }

        internal XElement Source { get; }

        internal string CurrencyCode { get; }

        internal bool HasDigits { get; }

        internal int Digits { get; }

        internal bool HasRounding { get; }

        internal int Rounding { get; }

        internal bool HasCashDigits { get; }

        internal int CashDigits { get; }

        internal bool HasCashRounding { get; }

        internal int CashRounding { get; }
    }
}
