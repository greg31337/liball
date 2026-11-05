using System;
using System.Globalization;

namespace fake;

internal static class CompanyGenerator
{
    const string Marker = "fake.synthetic-company.v1";

    internal static string Name(GenerationContext context, FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        ValidateValues(context, profile.NameStems, "companyName");
        ValidateValues(context, profile.ActivityNouns, "companyName");
        ValidateForms(context, profile.LegalForms, false, "companyName");
        int maximumLength = MaximumLength(profile.NameStems) +
            MaximumLength(profile.ActivityNouns) +
            MaximumAbbreviationLength(profile.LegalForms) +
            (profile.LegalFormPrefix ? 0 : 2);
        Guard.OutputSize(
            maximumLength,
            context.CapturedOptions.MaximumOutputSize,
            "companyName");
        string stem = Choose(random, profile.NameStems);
        string activity = Choose(random, profile.ActivityNouns);
        CompanyLegalFormData form = ChooseForm(random, profile.LegalForms);
        string result = profile.LegalFormPrefix
            ? form.Abbreviation + stem + activity
            : stem + " " + activity + " " + form.Abbreviation;
        return Normalize(context, result, "companyName");
    }

    internal static string Suffix(GenerationContext context, FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        ValidateForms(context, profile.LegalForms, false, "companySuffix");
        return Normalize(
            context,
            ChooseForm(random, profile.LegalForms).Abbreviation,
            "companySuffix");
    }

    internal static string LegalForm(GenerationContext context, FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        ValidateForms(context, profile.LegalForms, true, "companyLegalForm");
        return Normalize(
            context,
            ChooseForm(random, profile.LegalForms).Name,
            "companyLegalForm");
    }

    internal static string Industry(GenerationContext context, FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        return ChooseValue(context, random, profile.Industries, "companyIndustry");
    }

    internal static string Department(GenerationContext context, FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        return ChooseValue(context, random, profile.Departments, "companyDepartment");
    }

    internal static string CatchDescriptor(
        GenerationContext context,
        FakeRandom random)
    {
        return ChooseValue(
            context,
            random,
            CompanyPhraseData.CatchDescriptors,
            "companyCatchDescriptor");
    }

    internal static string CatchSubject(
        GenerationContext context,
        FakeRandom random)
    {
        return ChooseValue(
            context,
            random,
            CompanyPhraseData.CatchSubjects,
            "companyCatchSubject");
    }

    internal static string CatchBenefit(
        GenerationContext context,
        FakeRandom random)
    {
        return ChooseValue(
            context,
            random,
            CompanyPhraseData.CatchBenefits,
            "companyCatchBenefit");
    }

    internal static string CatchPhrase(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            CompanyPhraseData.CatchDescriptors,
            "companyCatchPhrase");
        ValidateValues(
            context,
            CompanyPhraseData.CatchSubjects,
            "companyCatchPhrase");
        ValidateValues(
            context,
            CompanyPhraseData.CatchBenefits,
            "companyCatchPhrase");
        Guard.OutputSize(
            MaximumLength(CompanyPhraseData.CatchDescriptors) + 1 +
                MaximumLength(CompanyPhraseData.CatchSubjects) + 5 +
                MaximumLength(CompanyPhraseData.CatchBenefits),
            context.CapturedOptions.MaximumOutputSize,
            "companyCatchPhrase");
        string result = Choose(random, CompanyPhraseData.CatchDescriptors) + " " +
            Choose(random, CompanyPhraseData.CatchSubjects) + " for " +
            Choose(random, CompanyPhraseData.CatchBenefits);
        return Normalize(context, result, "companyCatchPhrase");
    }

    internal static string BuzzVerb(
        GenerationContext context,
        FakeRandom random)
    {
        return ChooseValue(
            context,
            random,
            CompanyPhraseData.BuzzVerbs,
            "companyBuzzVerb");
    }

    internal static string BuzzAdjective(
        GenerationContext context,
        FakeRandom random)
    {
        return ChooseValue(
            context,
            random,
            CompanyPhraseData.BuzzAdjectives,
            "companyBuzzAdjective");
    }

    internal static string BuzzNoun(
        GenerationContext context,
        FakeRandom random)
    {
        return ChooseValue(
            context,
            random,
            CompanyPhraseData.BuzzNouns,
            "companyBuzzNoun");
    }

    internal static string BuzzPhrase(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            CompanyPhraseData.BuzzVerbs,
            "companyBuzzPhrase");
        ValidateValues(
            context,
            CompanyPhraseData.BuzzAdjectives,
            "companyBuzzPhrase");
        ValidateValues(
            context,
            CompanyPhraseData.BuzzNouns,
            "companyBuzzPhrase");
        Guard.OutputSize(
            MaximumLength(CompanyPhraseData.BuzzVerbs) + 1 +
                MaximumLength(CompanyPhraseData.BuzzAdjectives) + 1 +
                MaximumLength(CompanyPhraseData.BuzzNouns),
            context.CapturedOptions.MaximumOutputSize,
            "companyBuzzPhrase");
        string result = Choose(random, CompanyPhraseData.BuzzVerbs) + " " +
            Choose(random, CompanyPhraseData.BuzzAdjectives) + " " +
            Choose(random, CompanyPhraseData.BuzzNouns);
        return Normalize(context, result, "companyBuzzPhrase");
    }

    internal static string Slogan(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            CompanyPhraseData.SloganPurposes,
            "companySlogan");
        ValidateValues(
            context,
            CompanyPhraseData.SloganOutcomes,
            "companySlogan");
        Guard.OutputSize(
            MaximumLength(CompanyPhraseData.SloganPurposes) + 2 +
                MaximumLength(CompanyPhraseData.SloganOutcomes) + 1,
            context.CapturedOptions.MaximumOutputSize,
            "companySlogan");
        string result = Choose(random, CompanyPhraseData.SloganPurposes) + ", " +
            Choose(random, CompanyPhraseData.SloganOutcomes) + ".";
        return Normalize(context, result, "companySlogan");
    }

    internal static string BusinessIdentifier(
        GenerationContext context,
        FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        return GenerateBusiness(context, random, profile);
    }

    internal static string TaxIdentifier(
        GenerationContext context,
        FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        return GenerateTax(context, random, profile);
    }

    internal static bool IsBusinessIdentifierValid(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "value");
        return ValidateBusiness(CompanyLocaleData.Resolve(context), value);
    }

    internal static bool IsTaxIdentifierValid(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "value");
        return ValidateTax(CompanyLocaleData.Resolve(context), value);
    }

    internal static CompanyIdentifierRecord Identifiers(
        GenerationContext context,
        FakeRandom random)
    {
        CompanyLocaleProfile profile = CompanyLocaleData.Resolve(context);
        int businessLength = BusinessLength(profile);
        int taxLength = TaxLength(profile);
        Guard.Count(
            businessLength > taxLength ? businessLength : taxLength,
            context.CapturedOptions.MaximumCount,
            "companyIdentifiers");
        Guard.OutputSize(
            profile.Locale.Length + profile.CountryCode.Length + businessLength +
                profile.BusinessFormat.Length + taxLength + profile.TaxFormat.Length +
                Marker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "companyIdentifiers");
        string business = GenerateBusiness(
            context,
            random,
            profile,
            FakeLimits.DefaultMaximumOutputSize);
        string tax = DeriveTax(
            context,
            random,
            profile,
            business,
            FakeLimits.DefaultMaximumOutputSize);
        CompanyIdentifierRecord result = new CompanyIdentifierRecord();
        result.Locale = profile.Locale;
        result.CountryCode = profile.CountryCode;
        result.BusinessIdentifier = business;
        result.BusinessIdentifierFormat = profile.BusinessFormat;
        result.TaxIdentifier = tax;
        result.TaxIdentifierFormat = profile.TaxFormat;
        result.IsBusinessIdentifierValid = ValidateBusiness(profile, business);
        result.IsTaxIdentifierValid = ValidateTax(profile, tax);
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsRegisteredEntity = false;
        result.RepresentsTaxRegistration = false;
        return result;
    }

    internal static CompanyProfile Profile(
        GenerationContext context,
        FakeRandom random)
    {
        string name = Name(context, random);
        string domain = CompanyDomain(
            name,
            context.CapturedOptions.MaximumOutputSize);
        string email = "contact@" + domain;
        Guard.OutputSize(
            email.Length,
            context.CapturedOptions.MaximumOutputSize,
            "companyEmail");
        string industry = Industry(context, random);
        string catchPhrase = CatchPhrase(context, random);
        CompanyIdentifierRecord identifiers = Identifiers(context, random);
        PhoneLocaleMetadata phoneMetadata = PhoneLocaleDataParser.Resolve(context);
        PhoneRecord phone = PhoneNumberGenerator.Record(
            context,
            random,
            phoneMetadata,
            PhoneNumberCategory.General,
            PhoneNumberFormat.National,
            context.SafetyModeValue,
            context.CapturedOptions.MaximumOutputSize);
        AddressRecord address = AddressRecordGenerator.AddressRecord(
            context,
            random);
        CompanyProfile result = new CompanyProfile();
        result.Locale = identifiers.Locale;
        result.CountryCode = identifiers.CountryCode;
        result.Name = name;
        result.Domain = domain;
        result.Email = email;
        result.Industry = industry;
        result.CatchPhrase = catchPhrase;
        result.Phone = phone;
        result.PhoneRegionCode = phone.ParsingMetadata.RegionCode;
        result.Address = address;
        result.AddressFixtureCountryCode = address.CountryCode;
        result.Identifiers = identifiers;
        result.Slogan = Slogan(context, random);
        result.DomainIsReservedExample = true;
        result.EmailIsReservedExample = true;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsRegisteredEntity = false;
        result.RepresentsReachableContact = false;
        result.RepresentsDeliverableAddress = false;
        Guard.OutputSize(
            ProfileOutputSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "companyProfile");
        return result;
    }

    static string ChooseValue(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string parameterName)
    {
        ValidateValues(context, values, parameterName);
        return Normalize(context, Choose(random, values), parameterName);
    }

    static string CompanyDomain(string name, int maximumOutputSize)
    {
        string label = TextTransforms.Slug(
            name,
            '-',
            FakeLimits.DefaultMaximumOutputSize);
        if (label.Length == 0)
        {
            label = HashedCompanyLabel(name);
        }
        string result = label + ".example.com";
        DomainNameGenerator.ValidateAsciiDomain(result, "companyDomain");
        Guard.OutputSize(result.Length, maximumOutputSize, "companyDomain");
        return result;
    }

    static string HashedCompanyLabel(string value)
    {
        ulong hash = 14695981039346656037UL;
        int index = 0;
        while (index < value.Length)
        {
            hash = hash ^ value[index];
            hash = WrappingArithmetic.Multiply(hash, 1099511628211UL);
            index = index + 1;
        }
        return "company-" + hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string parameterName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            "companyData");
        int index = 0;
        while (index < values.Length)
        {
            Normalize(context, values[index], parameterName);
            index = index + 1;
        }
    }

    static void ValidateForms(
        GenerationContext context,
        CompanyLegalFormData[] values,
        bool useName,
        string parameterName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            "companyData");
        int index = 0;
        while (index < values.Length)
        {
            Normalize(
                context,
                useName ? values[index].Name : values[index].Abbreviation,
                parameterName);
            index = index + 1;
        }
    }

    static string Normalize(
        GenerationContext context,
        string value,
        string parameterName)
    {
        return UnicodeText.Normalize(
            value,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string Choose(FakeRandom random, string[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static CompanyLegalFormData ChooseForm(
        FakeRandom random,
        CompanyLegalFormData[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static string GenerateBusiness(
        GenerationContext context,
        FakeRandom random,
        CompanyLocaleProfile profile)
    {
        return GenerateBusiness(
            context,
            random,
            profile,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string GenerateBusiness(
        GenerationContext context,
        FakeRandom random,
        CompanyLocaleProfile profile,
        int maximumOutputSize)
    {
        Guard.Count(
            BusinessLength(profile),
            context.CapturedOptions.MaximumCount,
            "businessIdentifier");
        Guard.OutputSize(
            BusinessLength(profile),
            maximumOutputSize,
            "businessIdentifier");
        if (profile.IdentifierKind == "fr")
        {
            string body = NumericStringGenerator.Digits(random, 8, maximumOutputSize);
            return body + ChecksumAlgorithms.Luhn(
                body.AsSpan(),
                context.CapturedOptions.MaximumCount).ToString(
                    CultureInfo.InvariantCulture);
        }
        return MaskFormatter.Format(
            random,
            BusinessMask(profile),
            maximumOutputSize);
    }

    static string GenerateTax(
        GenerationContext context,
        FakeRandom random,
        CompanyLocaleProfile profile)
    {
        Guard.Count(
            TaxLength(profile),
            context.CapturedOptions.MaximumCount,
            "taxIdentifier");
        Guard.OutputSize(
            TaxLength(profile),
            context.CapturedOptions.MaximumOutputSize,
            "taxIdentifier");
        if (profile.IdentifierKind == "fr")
        {
            string business = GenerateBusiness(
                context,
                random,
                profile,
                FakeLimits.DefaultMaximumOutputSize);
            return FrenchTax(business);
        }
        return MaskFormatter.Format(
            random,
            TaxMask(profile),
            context.CapturedOptions.MaximumOutputSize);
    }

    static string DeriveTax(
        GenerationContext context,
        FakeRandom random,
        CompanyLocaleProfile profile,
        string business,
        int maximumOutputSize)
    {
        if (profile.IdentifierKind == "us" || profile.IdentifierKind == "au")
        {
            return business;
        }
        if (profile.IdentifierKind == "gb")
        {
            return "GB" + business + NumericStringGenerator.Digits(random, 1, maximumOutputSize);
        }
        if (profile.IdentifierKind == "fr")
        {
            return FrenchTax(business);
        }
        if (profile.IdentifierKind == "ca")
        {
            return business.Substring(0, 9) + "RT0001";
        }
        if (profile.IdentifierKind == "jp")
        {
            return "T" + business;
        }
        if (profile.IdentifierKind == "de")
        {
            return MaskFormatter.Format(random, "DE#########", maximumOutputSize);
        }
        return profile.CountryCode + business.Substring(3);
    }

    static string FrenchTax(string siren)
    {
        int key = (12 + 3 * Mod97(siren)) % 97;
        return "FR" + key.ToString("D2", CultureInfo.InvariantCulture) + siren;
    }

    static int Mod97(string value)
    {
        int remainder = 0;
        int index = 0;
        while (index < value.Length)
        {
            remainder = (remainder * 10 + value[index] - '0') % 97;
            index = index + 1;
        }
        return remainder;
    }

    static bool ValidateBusiness(CompanyLocaleProfile profile, string value)
    {
        if (profile.IdentifierKind == "fr")
        {
            return value.Length == 9 && Digits(value, 0, value.Length) &&
                ChecksumAlgorithms.IsLuhn(
                    value.AsSpan(),
                    FakeLimits.DefaultMaximumCount);
        }
        return MatchesMask(value, BusinessMask(profile));
    }

    static bool ValidateTax(CompanyLocaleProfile profile, string value)
    {
        if (profile.IdentifierKind == "fr")
        {
            if (value.Length != 13 || value[0] != 'F' || value[1] != 'R' ||
                !Digits(value, 2, value.Length))
            {
                return false;
            }
            string siren = value.Substring(4);
            return ChecksumAlgorithms.IsLuhn(
                siren.AsSpan(),
                FakeLimits.DefaultMaximumCount) && value == FrenchTax(siren);
        }
        return MatchesMask(value, TaxMask(profile));
    }

    static bool MatchesMask(string value, string mask)
    {
        if (value.Length != mask.Length)
        {
            return false;
        }
        int index = 0;
        while (index < mask.Length)
        {
            if (mask[index] == '#')
            {
                if (value[index] < '0' || value[index] > '9')
                {
                    return false;
                }
            }
            else if (value[index] != mask[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool Digits(string value, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static string BusinessMask(CompanyLocaleProfile profile)
    {
        if (profile.IdentifierKind == "us") return "##-#######";
        if (profile.IdentifierKind == "gb") return "########";
        if (profile.IdentifierKind == "de") return "HRB ######";
        if (profile.IdentifierKind == "ca") return "#########RC0001";
        if (profile.IdentifierKind == "au") return "###########";
        if (profile.IdentifierKind == "jp") return "#############";
        return profile.CountryCode + "-##########";
    }

    static string TaxMask(CompanyLocaleProfile profile)
    {
        if (profile.IdentifierKind == "us") return "##-#######";
        if (profile.IdentifierKind == "gb") return "GB#########";
        if (profile.IdentifierKind == "de") return "DE#########";
        if (profile.IdentifierKind == "ca") return "#########RT0001";
        if (profile.IdentifierKind == "au") return "###########";
        if (profile.IdentifierKind == "jp") return "T#############";
        return profile.CountryCode + "##########";
    }

    static int BusinessLength(CompanyLocaleProfile profile)
    {
        return profile.IdentifierKind == "fr"
            ? 9
            : BusinessMask(profile).Length;
    }

    static int TaxLength(CompanyLocaleProfile profile)
    {
        return profile.IdentifierKind == "fr"
            ? 13
            : TaxMask(profile).Length;
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }

    static int MaximumAbbreviationLength(CompanyLegalFormData[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Abbreviation.Length > result)
            {
                result = values[index].Abbreviation.Length;
            }
            index = index + 1;
        }
        return result;
    }

    internal static int ProfileOutputSize(CompanyProfile value)
    {
        return value.Locale.Length + value.CountryCode.Length +
            value.Name.Length + value.Domain.Length + value.Email.Length +
            value.Industry.Length + value.CatchPhrase.Length + value.Slogan.Length +
            value.Phone.Number.Length + value.PhoneRegionCode.Length +
            value.Address.StreetAddress.Length + value.Address.Unit.Length +
            value.Address.District.Length + value.Address.City.Length +
            value.Address.County.Length + value.Address.Subdivision.Length +
            value.Address.PostalCode.Length + value.Address.Country.Length +
            value.Address.TimeZoneId.Length + value.AddressFixtureCountryCode.Length +
            value.Identifiers.BusinessIdentifier.Length +
            value.Identifiers.BusinessIdentifierFormat.Length +
            value.Identifiers.TaxIdentifier.Length +
            value.Identifiers.TaxIdentifierFormat.Length +
            value.Identifiers.TestMarker.Length + value.TestMarker.Length;
    }
}
