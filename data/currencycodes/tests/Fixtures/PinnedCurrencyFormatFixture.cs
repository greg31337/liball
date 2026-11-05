using System.Globalization;
using System.Xml.Linq;

namespace currencycodes.Tests;

internal class ExpectedCurrencyFormatMetadata
{
    internal string CurrencyCode = string.Empty;
    internal bool HasIsoMinorUnit;
    internal int IsoMinorUnitDigits;
    internal decimal IsoMinorUnitFactor;
    internal int StandardFormatDigits;
    internal int StandardRoundingIncrement;
    internal decimal StandardFormatFactor;
    internal int CashDigits;
    internal int CashRoundingIncrement;
    internal decimal CashFactor;
    internal bool CldrRuleIsExplicit;
    internal bool CashDigitsIsExplicit;
    internal bool CashRoundingIsExplicit;
}

internal static class PinnedCurrencyFormatFixture
{
    internal static Dictionary<string, ExpectedCurrencyFormatMetadata> Load()
    {
        string repositoryRoot = FindRepositoryRoot();
        Dictionary<string, IsoMinorUnitFact> isoFacts = LoadIsoFacts(repositoryRoot);
        XDocument cldr = XDocument.Load(
            Path.Combine(repositoryRoot, "specs", "cldr-48.2-supplementalData.xml"));
        CldrRuleSet cldrRules = LoadCldrRules(cldr);
        HashSet<string> codes = new HashSet<string>(isoFacts.Keys, StringComparer.Ordinal);

        foreach (XElement currency in cldr.Descendants("currency"))
        {
            XAttribute? codeAttribute = currency.Attribute("iso4217");
            if (codeAttribute != null)
            {
                codes.Add(codeAttribute.Value);
            }
        }

        Dictionary<string, ExpectedCurrencyFormatMetadata> result =
            new Dictionary<string, ExpectedCurrencyFormatMetadata>(StringComparer.Ordinal);
        foreach (string code in codes)
        {
            IsoMinorUnitFact isoFact = isoFacts.TryGetValue(code, out IsoMinorUnitFact? foundFact)
                ? foundFact
                : new IsoMinorUnitFact(false, 0);
            ResolvedCldrRule rule = cldrRules.ExplicitRules.TryGetValue(
                code,
                out ResolvedCldrRule? explicitRule)
                    ? explicitRule
                    : cldrRules.DefaultRule;
            result.Add(
                code,
                new ExpectedCurrencyFormatMetadata
                {
                    CurrencyCode = code,
                    HasIsoMinorUnit = isoFact.HasMinorUnit,
                    IsoMinorUnitDigits = isoFact.MinorUnitDigits,
                    IsoMinorUnitFactor = isoFact.HasMinorUnit
                        ? CreateFactor(isoFact.MinorUnitDigits)
                        : 0m,
                    StandardFormatDigits = rule.Digits,
                    StandardRoundingIncrement = rule.Rounding,
                    StandardFormatFactor = CreateFactor(rule.Digits),
                    CashDigits = rule.CashDigits,
                    CashRoundingIncrement = rule.CashRounding,
                    CashFactor = CreateFactor(rule.CashDigits),
                    CldrRuleIsExplicit = explicitRule != null,
                    CashDigitsIsExplicit = explicitRule != null && explicitRule.CashDigitsIsExplicit,
                    CashRoundingIsExplicit = explicitRule != null && explicitRule.CashRoundingIsExplicit
                });
        }

        return result;
    }

    private static Dictionary<string, IsoMinorUnitFact> LoadIsoFacts(string repositoryRoot)
    {
        Dictionary<string, IsoMinorUnitFact> active = LoadIsoSource(
            Path.Combine(repositoryRoot, "specs", "iso-4217-list-one.xml"),
            "CcyNtry");
        Dictionary<string, IsoMinorUnitFact> historical = LoadIsoSource(
            Path.Combine(repositoryRoot, "specs", "iso-4217-list-three.xml"),
            "HstrcCcyNtry");

        foreach (KeyValuePair<string, IsoMinorUnitFact> item in historical)
        {
            if (!active.ContainsKey(item.Key))
            {
                active.Add(item.Key, item.Value);
            }
        }

        return active;
    }

    private static Dictionary<string, IsoMinorUnitFact> LoadIsoSource(
        string path,
        string entryName)
    {
        XDocument document = XDocument.Load(path);
        Dictionary<string, IsoMinorUnitFact> result =
            new Dictionary<string, IsoMinorUnitFact>(StringComparer.Ordinal);

        foreach (XElement entry in document.Descendants(entryName))
        {
            XElement? codeElement = entry.Element("Ccy");
            if (codeElement == null)
            {
                continue;
            }

            XElement? minorUnitElement = entry.Element("CcyMnrUnts");
            int minorUnitDigits = 0;
            bool hasMinorUnit = minorUnitElement != null
                && int.TryParse(
                    minorUnitElement.Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out minorUnitDigits);
            IsoMinorUnitFact candidate = new IsoMinorUnitFact(
                hasMinorUnit,
                hasMinorUnit ? minorUnitDigits : 0);
            if (result.TryGetValue(codeElement.Value, out IsoMinorUnitFact? existing))
            {
                if (existing.HasMinorUnit != candidate.HasMinorUnit
                    || existing.MinorUnitDigits != candidate.MinorUnitDigits)
                {
                    throw new InvalidDataException(
                        "Conflicting ISO minor-unit facts for " + codeElement.Value + ".");
                }
            }
            else
            {
                result.Add(codeElement.Value, candidate);
            }
        }

        return result;
    }

    private static CldrRuleSet LoadCldrRules(XDocument document)
    {
        XElement? fractions = document.Descendants("fractions").SingleOrDefault();
        if (fractions == null)
        {
            throw new InvalidDataException("The pinned CLDR data has no fractions element.");
        }

        XElement? defaultElement = fractions.Elements("info").SingleOrDefault(
            element => (string?)element.Attribute("iso4217") == "DEFAULT");
        if (defaultElement == null)
        {
            throw new InvalidDataException("The pinned CLDR data has no default fraction rule.");
        }

        int defaultDigits = ReadRequiredInteger(defaultElement, "digits");
        int defaultRounding = ReadRequiredInteger(defaultElement, "rounding");
        ResolvedCldrRule defaultRule = new ResolvedCldrRule(
            defaultDigits,
            defaultRounding,
            ReadOptionalInteger(defaultElement, "cashDigits", defaultDigits),
            ReadOptionalInteger(defaultElement, "cashRounding", defaultRounding),
            false,
            false);
        Dictionary<string, ResolvedCldrRule> explicitRules =
            new Dictionary<string, ResolvedCldrRule>(StringComparer.Ordinal);

        foreach (XElement element in fractions.Elements("info"))
        {
            string code = ReadRequiredText(element, "iso4217");
            if (code == "DEFAULT")
            {
                continue;
            }

            int digits = ReadOptionalInteger(element, "digits", defaultRule.Digits);
            int rounding = ReadOptionalInteger(element, "rounding", defaultRule.Rounding);
            XAttribute? cashDigits = element.Attribute("cashDigits");
            XAttribute? cashRounding = element.Attribute("cashRounding");
            explicitRules.Add(
                code,
                new ResolvedCldrRule(
                    digits,
                    rounding,
                    cashDigits == null ? digits : ParseInteger(cashDigits.Value),
                    cashRounding == null ? rounding : ParseInteger(cashRounding.Value),
                    cashDigits != null,
                    cashRounding != null));
        }

        return new CldrRuleSet(defaultRule, explicitRules);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "currencycodes.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the currencycodes repository root.");
    }

    private static string ReadRequiredText(XElement element, string attributeName)
    {
        XAttribute? attribute = element.Attribute(attributeName);
        if (attribute == null)
        {
            throw new InvalidDataException("Missing CLDR attribute " + attributeName + ".");
        }

        return attribute.Value;
    }

    private static int ReadRequiredInteger(XElement element, string attributeName)
    {
        return ParseInteger(ReadRequiredText(element, attributeName));
    }

    private static int ReadOptionalInteger(
        XElement element,
        string attributeName,
        int fallback)
    {
        XAttribute? attribute = element.Attribute(attributeName);
        return attribute == null ? fallback : ParseInteger(attribute.Value);
    }

    private static int ParseInteger(string text)
    {
        return int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static decimal CreateFactor(int digits)
    {
        decimal factor = 1m;
        for (int index = 0; index < digits; index++)
        {
            factor *= 10m;
        }

        return factor;
    }

    private class IsoMinorUnitFact
    {
        internal bool HasMinorUnit;
        internal int MinorUnitDigits;

        internal IsoMinorUnitFact(bool hasMinorUnit, int minorUnitDigits)
        {
            HasMinorUnit = hasMinorUnit;
            MinorUnitDigits = minorUnitDigits;
        }
    }

    private class ResolvedCldrRule
    {
        internal int Digits;
        internal int Rounding;
        internal int CashDigits;
        internal int CashRounding;
        internal bool CashDigitsIsExplicit;
        internal bool CashRoundingIsExplicit;

        internal ResolvedCldrRule(
            int digits,
            int rounding,
            int cashDigits,
            int cashRounding,
            bool cashDigitsIsExplicit,
            bool cashRoundingIsExplicit)
        {
            Digits = digits;
            Rounding = rounding;
            CashDigits = cashDigits;
            CashRounding = cashRounding;
            CashDigitsIsExplicit = cashDigitsIsExplicit;
            CashRoundingIsExplicit = cashRoundingIsExplicit;
        }
    }

    private class CldrRuleSet
    {
        internal ResolvedCldrRule DefaultRule;
        internal Dictionary<string, ResolvedCldrRule> ExplicitRules;

        internal CldrRuleSet(
            ResolvedCldrRule defaultRule,
            Dictionary<string, ResolvedCldrRule> explicitRules)
        {
            DefaultRule = defaultRule;
            ExplicitRules = explicitRules;
        }
    }
}
