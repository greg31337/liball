namespace fake;

internal class CompanyLegalFormData
{
    internal string Name = string.Empty;
    internal string Abbreviation = string.Empty;

    internal CompanyLegalFormData(string name, string abbreviation)
    {
        Name = name;
        Abbreviation = abbreviation;
    }
}

internal class CompanyLocaleProfile
{
    internal string Locale = string.Empty;
    internal string CountryCode = string.Empty;
    internal string IdentifierKind = string.Empty;
    internal bool LegalFormPrefix;
    internal string[] NameStems = new string[0];
    internal string[] ActivityNouns = new string[0];
    internal CompanyLegalFormData[] LegalForms = new CompanyLegalFormData[0];
    internal string[] Industries = new string[0];
    internal string[] Departments = new string[0];
    internal string BusinessFormat = string.Empty;
    internal string TaxFormat = string.Empty;
}

internal static class CompanyLocaleData
{
    internal static CompanyLocaleProfile Resolve(GenerationContext context)
    {
        LanguageTag tag = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "Locale");
        string countryCode = CountryCode(tag);
        if (countryCode == "DE")
        {
            return German(tag.Normalized, countryCode, "de");
        }
        if (countryCode == "FR")
        {
            return French(tag.Normalized, countryCode, "fr");
        }
        if (countryCode == "JP")
        {
            return Japanese(tag.Normalized, countryCode, "jp");
        }
        if (countryCode == "GB")
        {
            return British(tag.Normalized, countryCode, "gb");
        }
        if (countryCode == "CA")
        {
            return tag.Language == "fr"
                ? FrenchCanadian(tag.Normalized, countryCode, "ca")
                : Canadian(tag.Normalized, countryCode, "ca");
        }
        if (countryCode == "AU")
        {
            return Australian(tag.Normalized, countryCode, "au");
        }
        if (countryCode == "US")
        {
            return American(tag.Normalized, countryCode, "us");
        }
        if (tag.Language == "de")
        {
            return German(tag.Normalized, countryCode, "generic");
        }
        if (tag.Language == "fr")
        {
            return French(tag.Normalized, countryCode, "generic");
        }
        if (tag.Language == "ja")
        {
            return Japanese(tag.Normalized, countryCode, "generic");
        }
        return International(tag.Normalized, countryCode, "generic");
    }

    static CompanyLocaleProfile American(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            false,
            new string[] { "Cedar", "Harbor", "Lantern", "Meadow", "Northbridge", "Silver Oak" },
            new string[] { "Workshop", "Services", "Studio", "Fieldworks", "Collective", "Systems" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("Limited Liability Company", "LLC"),
                new CompanyLegalFormData("Corporation", "Corp."),
                new CompanyLegalFormData("Incorporated", "Inc."),
                new CompanyLegalFormData("Limited Partnership", "LP")
            },
            EnglishIndustries(),
            EnglishDepartments(),
            "NN-NNNNNNN",
            "NN-NNNNNNN");
    }

    static CompanyLocaleProfile British(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            false,
            new string[] { "Cedar", "Harbour", "Lantern", "Meadow", "Northbridge", "Silver Oak" },
            new string[] { "Works", "Services", "Studio", "Consulting", "Collective", "Systems" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("Limited", "Ltd"),
                new CompanyLegalFormData("Public Limited Company", "plc"),
                new CompanyLegalFormData("Limited Liability Partnership", "LLP")
            },
            EnglishIndustries(),
            EnglishDepartments(),
            "8 digits",
            "GB plus 9 digits");
    }

    static CompanyLocaleProfile Canadian(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            false,
            new string[] { "Cedar", "Harbour", "Lantern", "Maple", "Northbridge", "Silver Pine" },
            new string[] { "Works", "Services", "Studio", "Consulting", "Collective", "Systems" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("Incorporated", "Inc."),
                new CompanyLegalFormData("Limited", "Ltd."),
                new CompanyLegalFormData("Corporation", "Corp.")
            },
            EnglishIndustries(),
            EnglishDepartments(),
            "9 digits plus RC0001",
            "9 digits plus RT0001");
    }

    static CompanyLocaleProfile Australian(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            false,
            new string[] { "Banksia", "Blue Gum", "Harbour", "Lantern", "Riverbend", "Southern Ridge" },
            new string[] { "Works", "Services", "Studio", "Consulting", "Collective", "Systems" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("Proprietary Limited", "Pty Ltd"),
                new CompanyLegalFormData("Limited", "Ltd"),
                new CompanyLegalFormData("Incorporated", "Inc.")
            },
            EnglishIndustries(),
            EnglishDepartments(),
            "11 digits",
            "11 digits");
    }

    static CompanyLocaleProfile German(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            false,
            new string[] { "Beispielhain", "Hafenlicht", "Musterfeld", "Nordbrücke", "Silberwald" },
            new string[] { "Beratung", "Dienste", "Systeme", "Technik", "Werkstatt" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("Gesellschaft mit beschränkter Haftung", "GmbH"),
                new CompanyLegalFormData("Aktiengesellschaft", "AG"),
                new CompanyLegalFormData("Unternehmergesellschaft (haftungsbeschränkt)", "UG (haftungsbeschränkt)")
            },
            new string[]
            {
                "Bildungsdienstleistungen", "Forschung", "Logistik", "Softwaredienstleistungen",
                "Umwelttechnik", "Unternehmensberatung"
            },
            new string[]
            {
                "Finanzen", "Forschung", "Kundenservice", "Personal", "Technik", "Vertrieb"
            },
            identifierKind == "de" ? "HRB plus 6 digits" : "country code plus 10 digits",
            identifierKind == "de" ? "DE plus 9 digits" : "country code plus 10 digits");
    }

    static CompanyLocaleProfile French(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            false,
            new string[] { "Bois Clair", "Jardin Exemple", "Lumière du Port", "Passerelle", "Rivage" },
            new string[] { "Atelier", "Conseil", "Créations", "Services", "Solutions" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("Société à responsabilité limitée", "SARL"),
                new CompanyLegalFormData("Société par actions simplifiée", "SAS"),
                new CompanyLegalFormData("Société anonyme", "SA"),
                new CompanyLegalFormData("Société en nom collectif", "SNC")
            },
            new string[]
            {
                "conseil aux entreprises", "logistique", "recherche", "services éducatifs",
                "services logiciels", "technologies environnementales"
            },
            new string[]
            {
                "commercial", "finances", "ingénierie", "recherche", "ressources humaines", "service clientèle"
            },
            identifierKind == "fr" ? "SIREN, 9 digits with Luhn checksum" : "country code plus 10 digits",
            identifierKind == "fr" ? "FR plus 2 key digits and SIREN" : "country code plus 10 digits");
    }

    static CompanyLocaleProfile FrenchCanadian(
        string locale,
        string countryCode,
        string identifierKind)
    {
        CompanyLocaleProfile result = French(locale, countryCode, identifierKind);
        result.LegalForms = new CompanyLegalFormData[]
        {
            new CompanyLegalFormData("Société par actions", "inc."),
            new CompanyLegalFormData("Compagnie limitée", "ltée"),
            new CompanyLegalFormData("Société en nom collectif", "s.e.n.c.")
        };
        result.BusinessFormat = "9 digits plus RC0001";
        result.TaxFormat = "9 digits plus RT0001";
        return result;
    }

    static CompanyLocaleProfile Japanese(
        string locale,
        string countryCode,
        string identifierKind)
    {
        return Create(
            locale,
            countryCode,
            identifierKind,
            true,
            new string[] { "サンプル", "青葉", "港光", "北橋", "銀森" },
            new string[] { "企画", "工房", "サービス", "システム", "技研" },
            new CompanyLegalFormData[]
            {
                new CompanyLegalFormData("株式会社", "株式会社"),
                new CompanyLegalFormData("合同会社", "合同会社"),
                new CompanyLegalFormData("合名会社", "合名会社"),
                new CompanyLegalFormData("合資会社", "合資会社")
            },
            new string[]
            {
                "教育サービス", "環境技術", "企業コンサルティング", "研究", "物流", "ソフトウェアサービス"
            },
            new string[]
            {
                "営業", "技術", "研究", "財務", "人事", "顧客サポート"
            },
            identifierKind == "jp" ? "13 digits" : "country code plus 10 digits",
            identifierKind == "jp" ? "T plus 13 digits" : "country code plus 10 digits");
    }

    static CompanyLocaleProfile International(
        string locale,
        string countryCode,
        string identifierKind)
    {
        CompanyLocaleProfile result = American(locale, countryCode, identifierKind);
        result.LegalForms = new CompanyLegalFormData[]
        {
            new CompanyLegalFormData("Company", "Co."),
            new CompanyLegalFormData("Cooperative", "Co-op")
        };
        result.BusinessFormat = "country code plus 10 digits";
        result.TaxFormat = "country code plus 10 digits";
        return result;
    }

    static CompanyLocaleProfile Create(
        string locale,
        string countryCode,
        string identifierKind,
        bool legalFormPrefix,
        string[] nameStems,
        string[] activityNouns,
        CompanyLegalFormData[] legalForms,
        string[] industries,
        string[] departments,
        string businessFormat,
        string taxFormat)
    {
        CompanyLocaleProfile result = new CompanyLocaleProfile();
        result.Locale = locale;
        result.CountryCode = countryCode;
        result.IdentifierKind = identifierKind;
        result.LegalFormPrefix = legalFormPrefix;
        result.NameStems = nameStems;
        result.ActivityNouns = activityNouns;
        result.LegalForms = legalForms;
        result.Industries = industries;
        result.Departments = departments;
        result.BusinessFormat = businessFormat;
        result.TaxFormat = taxFormat;
        return result;
    }

    static string[] EnglishIndustries()
    {
        return new string[]
        {
            "business consulting", "education services", "environmental technology",
            "logistics", "research", "software services"
        };
    }

    static string[] EnglishDepartments()
    {
        return new string[]
        {
            "customer support", "engineering", "finance", "human resources", "research", "sales"
        };
    }

    static string CountryCode(LanguageTag tag)
    {
        if (tag.Region != null && tag.Region.Length == 2)
        {
            return tag.Region;
        }
        if (tag.Language == "de") return "DE";
        if (tag.Language == "fr") return "FR";
        if (tag.Language == "es") return "ES";
        if (tag.Language == "pt") return "PT";
        if (tag.Language == "it") return "IT";
        if (tag.Language == "nl") return "NL";
        if (tag.Language == "pl") return "PL";
        if (tag.Language == "cs") return "CZ";
        if (tag.Language == "sv") return "SE";
        if (tag.Language == "nb") return "NO";
        if (tag.Language == "da") return "DK";
        if (tag.Language == "fi") return "FI";
        if (tag.Language == "ar") return "EG";
        if (tag.Language == "fa") return "IR";
        if (tag.Language == "he") return "IL";
        if (tag.Language == "hi") return "IN";
        if (tag.Language == "th") return "TH";
        if (tag.Language == "tr") return "TR";
        if (tag.Language == "ru") return "RU";
        if (tag.Language == "uk") return "UA";
        if (tag.Language == "el") return "GR";
        if (tag.Language == "ja") return "JP";
        if (tag.Language == "ko") return "KR";
        if (tag.Language == "zh") return "CN";
        if (tag.Language == "id") return "ID";
        if (tag.Language == "vi") return "VN";
        return "US";
    }
}
