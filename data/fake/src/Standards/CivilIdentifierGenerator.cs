using System;
using System.Globalization;

namespace fake;

internal class CivilIdentifierRule
{
    internal IdentifierKind Kind;
    internal string SourceLocale = string.Empty;
    internal bool UsedFallback;
    internal string Rule = string.Empty;
    internal string Format = string.Empty;
    internal string Provenance = string.Empty;
}

internal static class CivilIdentifierGenerator
{
    const string BrazilianSource = "Brazilian federal identifier and Mercosur registration format rules reviewed 2026-08-25";
    const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    internal static bool IsCivil(IdentifierKind kind)
    {
        return kind == IdentifierKind.Passport ||
            kind == IdentifierKind.DrivingLicence ||
            kind == IdentifierKind.NationalIdentity ||
            kind == IdentifierKind.SocialIdentity ||
            kind == IdentifierKind.PersonalTax ||
            kind == IdentifierKind.BusinessRegistration ||
            kind == IdentifierKind.VehicleRegistration;
    }

    internal static string Generate(
        GenerationContext context,
        FakeRandom random,
        IdentifierKind kind)
    {
        CivilIdentifierRule rule = Resolve(context, kind);
        string value;
        if (kind == IdentifierKind.Passport)
        {
            value = Letter(random) + Letter(random) +
                NumericStringGenerator.Digits(random, 6, context.CapturedOptions.MaximumOutputSize);
        }
        else if (kind == IdentifierKind.DrivingLicence)
        {
            value = DrivingLicence(random, context.CapturedOptions.MaximumOutputSize);
        }
        else if (kind == IdentifierKind.NationalIdentity ||
            kind == IdentifierKind.PersonalTax)
        {
            value = Cpf(random, context.CapturedOptions.MaximumOutputSize);
        }
        else if (kind == IdentifierKind.SocialIdentity)
        {
            value = Pis(random, context.CapturedOptions.MaximumOutputSize);
        }
        else if (kind == IdentifierKind.BusinessRegistration)
        {
            value = Cnpj(random, context.CapturedOptions.MaximumOutputSize);
        }
        else
        {
            value = Letter(random) + Letter(random) + Letter(random) +
                NumericStringGenerator.Digits(random, 1, context.CapturedOptions.MaximumOutputSize) +
                Letter(random) +
                NumericStringGenerator.Digits(random, 2, context.CapturedOptions.MaximumOutputSize);
        }
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "civilIdentifier");
        Guard.OutputSize(value.Length, context.CapturedOptions.MaximumOutputSize, "civilIdentifier");
        if (!ValidateWithRule(value.AsSpan(), rule).Valid)
        {
            throw FakeException.Create("Generated civil identifier did not satisfy its locale rule.");
        }
        return value;
    }

    internal static ValidationResult Validate(
        GenerationContext context,
        string value,
        IdentifierKind kind)
    {
        Guard.NotNull(value, "value");
        CivilIdentifierRule rule = Resolve(context, kind);
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "value");
        return ValidateWithRule(value.AsSpan(), rule);
    }

    internal static string Mutate(
        GenerationContext context,
        string value,
        IdentifierKind kind,
        IdentifierMutationKind mutation)
    {
        Guard.NotNull(value, "value");
        CivilIdentifierRule rule = Resolve(context, kind);
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "value");
        Guard.OutputSize(value.Length, context.CapturedOptions.MaximumOutputSize, "value");
        if (!ValidateWithRule(value.AsSpan(), rule).Valid)
        {
            throw new ArgumentException("value must be valid for the resolved civil identifier rule.", "value");
        }
        if (mutation == IdentifierMutationKind.Length)
        {
            return value.Substring(0, value.Length - 1);
        }
        if (mutation == IdentifierMutationKind.Character)
        {
            char[] characters = value.ToCharArray();
            characters[0] = ' ';
            return new string(characters);
        }
        if (mutation == IdentifierMutationKind.Prefix)
        {
            char[] characters = value.ToCharArray();
            if (kind == IdentifierKind.Passport ||
                kind == IdentifierKind.VehicleRegistration)
            {
                characters[0] = '0';
            }
            else if (kind == IdentifierKind.BusinessRegistration)
            {
                characters[0] = 'a';
            }
            else
            {
                characters[0] = 'A';
            }
            return new string(characters);
        }
        if (mutation == IdentifierMutationKind.Checksum)
        {
            if (!HasChecksum(kind))
            {
                throw new ArgumentException("The resolved civil identifier rule has no checksum to mutate.", "mutation");
            }
            char[] characters = value.ToCharArray();
            int last = characters.Length - 1;
            characters[last] = characters[last] == '9' ? '0' :
                (char)(characters[last] + 1);
            return new string(characters);
        }
        throw new ArgumentOutOfRangeException(
            "mutation",
            mutation,
            "mutation is not supported for civil identifiers.");
    }

    internal static GenerationMetadata Metadata(
        GenerationContext context,
        string value,
        IdentifierKind kind)
    {
        Guard.NotNull(value, "value");
        CivilIdentifierRule rule = Resolve(context, kind);
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "value");
        ValidationResult validation = ValidateWithRule(value.AsSpan(), rule);
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = value;
        metadata.GeneratedValue = value;
        metadata.ValueKind = kind.ToString();
        metadata.Provider = "identifier";
        metadata.Kind = kind;
        metadata.Locale = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "Locale").Normalized;
        metadata.SourceLocale = rule.SourceLocale;
        metadata.UsedLocaleFallback = rule.UsedFallback;
        metadata.Rule = rule.Rule;
        metadata.Format = rule.Format;
        metadata.Provenance = rule.Provenance;
        metadata.RepresentsAssignment = false;
        context.AttachModes(metadata);
        metadata.StructuralValidity = validation.StructuralValidity;
        metadata.ChecksumValidity = validation.ChecksumValidity;
        metadata.ConstraintValidity = validation.ConstraintValidity;
        metadata.CorrelationValidity = validation.CorrelationValidity;
        metadata.FictionalReserved = false;
        metadata.SafeReserved = false;
        metadata.AllocationStatus = AllocationStatus.Unknown;
        metadata.Validation = validation;
        return metadata;
    }

    static CivilIdentifierRule Resolve(
        GenerationContext context,
        IdentifierKind kind)
    {
        if (!IsCivil(kind))
        {
            throw new ArgumentOutOfRangeException(
                "kind",
                kind,
                "kind is not a civil identifier contract.");
        }
        LanguageTag requested = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "Locale");
        string[] candidates = LocaleResolver.CandidateLocales(context, requested);
        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index] == "pt-BR")
            {
                return BrazilianRule(kind, requested.Normalized != "pt-BR");
            }
            index = index + 1;
        }
        throw FakeException.Create(
            $"Civil identifier '{kind}' is unsupported for locale '{requested.Normalized}' because no authoritative rule is available in its locale fallback chain.");
    }

    static CivilIdentifierRule BrazilianRule(
        IdentifierKind kind,
        bool usedFallback)
    {
        CivilIdentifierRule result = new CivilIdentifierRule();
        result.Kind = kind;
        result.SourceLocale = "pt-BR";
        result.UsedFallback = usedFallback;
        result.Provenance = BrazilianSource;
        if (kind == IdentifierKind.Passport)
        {
            result.Rule = "br-passport-ordinary-v1";
            result.Format = "two uppercase ASCII letters followed by six digits";
        }
        else if (kind == IdentifierKind.DrivingLicence)
        {
            result.Rule = "br-cnh-v1";
            result.Format = "eleven digits including two verification digits";
        }
        else if (kind == IdentifierKind.NationalIdentity)
        {
            result.Rule = "br-cin-cpf-v1";
            result.Format = "eleven CPF digits used as the CIN national registration number";
        }
        else if (kind == IdentifierKind.SocialIdentity)
        {
            result.Rule = "br-pis-nis-v1";
            result.Format = "eleven PIS/NIS digits including one verification digit";
        }
        else if (kind == IdentifierKind.PersonalTax)
        {
            result.Rule = "br-cpf-v1";
            result.Format = "eleven CPF digits including two verification digits";
        }
        else if (kind == IdentifierKind.BusinessRegistration)
        {
            result.Rule = "br-cnpj-alphanumeric-2026-v1";
            result.Format = "twelve uppercase ASCII letters or digits followed by two verification digits";
        }
        else
        {
            result.Rule = "br-mercosur-plate-v1";
            result.Format = "three letters, one digit, one letter, and two digits";
        }
        return result;
    }

    static ValidationResult ValidateWithRule(
        ReadOnlySpan<char> value,
        CivilIdentifierRule rule)
    {
        bool structure = Structure(value, rule.Kind);
        ValidityMode checksum = ValidityMode.NotApplicable;
        if (HasChecksum(rule.Kind))
        {
            checksum = structure
                ? (Checksum(value, rule.Kind) ? ValidityMode.Valid : ValidityMode.Invalid)
                : ValidityMode.Unknown;
        }
        ValidationResult result = new ValidationResult();
        result.Kind = rule.Kind;
        result.StructuralValidity = structure
            ? ValidityMode.Valid
            : ValidityMode.Invalid;
        result.ChecksumValidity = checksum;
        result.ConstraintValidity = ValidityMode.NotApplicable;
        result.Valid = structure &&
            (checksum == ValidityMode.Valid || checksum == ValidityMode.NotApplicable);
        result.FictionalReserved = false;
        result.AllocationStatus = AllocationStatus.Unknown;
        if (!structure)
        {
            result.Detail = "Civil identifier structure is invalid for the resolved locale rule.";
        }
        else if (checksum == ValidityMode.Invalid)
        {
            result.Detail = "Civil identifier checksum is invalid for the resolved locale rule.";
        }
        else
        {
            result.Detail = "Civil identifier is valid for the resolved locale rule; assignment is unknown.";
        }
        return result;
    }

    static bool Structure(ReadOnlySpan<char> value, IdentifierKind kind)
    {
        if (kind == IdentifierKind.Passport)
        {
            return value.Length == 8 && LettersFrom(value, 0, 2) &&
                DigitsFrom(value, 2, 8);
        }
        if (kind == IdentifierKind.VehicleRegistration)
        {
            return value.Length == 7 && LettersFrom(value, 0, 3) &&
                Digit(value[3]) && Letter(value[4]) &&
                DigitsFrom(value, 5, 7);
        }
        if (kind == IdentifierKind.BusinessRegistration)
        {
            return value.Length == 14 &&
                LettersOrDigitsFrom(value, 0, 12) &&
                DigitsFrom(value, 12, 14) &&
                !AllEqual(value);
        }
        int length = 11;
        return value.Length == length && DigitsFrom(value, 0, length) &&
            !AllEqual(value);
    }

    static bool Checksum(ReadOnlySpan<char> value, IdentifierKind kind)
    {
        if (kind == IdentifierKind.DrivingLicence)
        {
            string complete = CompleteDrivingLicence(value.Slice(0, 9).ToString());
            return value.SequenceEqual(complete.AsSpan());
        }
        if (kind == IdentifierKind.NationalIdentity ||
            kind == IdentifierKind.PersonalTax)
        {
            string complete = CompleteCpf(value.Slice(0, 9).ToString());
            return value.SequenceEqual(complete.AsSpan());
        }
        if (kind == IdentifierKind.SocialIdentity)
        {
            string complete = CompletePis(value.Slice(0, 10).ToString());
            return value.SequenceEqual(complete.AsSpan());
        }
        string cnpj = CompleteCnpj(value.Slice(0, 12).ToString());
        return value.SequenceEqual(cnpj.AsSpan());
    }

    static bool HasChecksum(IdentifierKind kind)
    {
        return kind == IdentifierKind.DrivingLicence ||
            kind == IdentifierKind.NationalIdentity ||
            kind == IdentifierKind.SocialIdentity ||
            kind == IdentifierKind.PersonalTax ||
            kind == IdentifierKind.BusinessRegistration;
    }

    static string DrivingLicence(FakeRandom random, int maximumOutputSize)
    {
        return CompleteDrivingLicence(BaseDigits(random, 9, maximumOutputSize));
    }

    static string Cpf(FakeRandom random, int maximumOutputSize)
    {
        return CompleteCpf(BaseDigits(random, 9, maximumOutputSize));
    }

    static string Pis(FakeRandom random, int maximumOutputSize)
    {
        return CompletePis(BaseDigits(random, 10, maximumOutputSize));
    }

    static string Cnpj(FakeRandom random, int maximumOutputSize)
    {
        return CompleteCnpj(BaseLettersOrDigits(random, 12, maximumOutputSize));
    }

    static string BaseDigits(FakeRandom random, int length, int maximumOutputSize)
    {
        string value = NumericStringGenerator.Digits(random, length, maximumOutputSize);
        if (AllEqual(value.AsSpan()))
        {
            char[] characters = value.ToCharArray();
            characters[0] = characters[0] == '9' ? '0' :
                (char)(characters[0] + 1);
            value = new string(characters);
        }
        return value;
    }

    static string CompleteCpf(string body)
    {
        int first = WeightedDigit(body.AsSpan(), new int[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 });
        string partial = body + first.ToString(CultureInfo.InvariantCulture);
        int second = WeightedDigit(partial.AsSpan(), new int[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 });
        return partial + second.ToString(CultureInfo.InvariantCulture);
    }

    static string CompleteCnpj(string body)
    {
        int first = CnpjWeightedDigit(body.AsSpan(), new int[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
        string partial = body + first.ToString(CultureInfo.InvariantCulture);
        int second = CnpjWeightedDigit(partial.AsSpan(), new int[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
        return partial + second.ToString(CultureInfo.InvariantCulture);
    }

    static int CnpjWeightedDigit(ReadOnlySpan<char> value, int[] weights)
    {
        int sum = 0;
        int index = 0;
        while (index < weights.Length)
        {
            sum = sum + (value[index] - 48) * weights[index];
            index = index + 1;
        }
        int remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    static int WeightedDigit(ReadOnlySpan<char> value, int[] weights)
    {
        int sum = 0;
        int index = 0;
        while (index < weights.Length)
        {
            sum = sum + (value[index] - '0') * weights[index];
            index = index + 1;
        }
        int remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    static string CompletePis(string body)
    {
        int[] weights = new int[] { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = 0;
        int index = 0;
        while (index < weights.Length)
        {
            sum = sum + (body[index] - '0') * weights[index];
            index = index + 1;
        }
        int digit = 11 - sum % 11;
        if (digit == 10 || digit == 11)
        {
            digit = 0;
        }
        return body + digit.ToString(CultureInfo.InvariantCulture);
    }

    static string CompleteDrivingLicence(string body)
    {
        int firstSum = 0;
        int secondSum = 0;
        int index = 0;
        while (index < 9)
        {
            int digit = body[index] - '0';
            firstSum = firstSum + digit * (9 - index);
            secondSum = secondSum + digit * (index + 1);
            index = index + 1;
        }
        int first = firstSum % 11;
        int adjustment = 0;
        if (first == 10)
        {
            first = 0;
            adjustment = 2;
        }
        int second = (secondSum + adjustment) % 11;
        if (second == 10)
        {
            second = 0;
        }
        return body + first.ToString(CultureInfo.InvariantCulture) +
            second.ToString(CultureInfo.InvariantCulture);
    }

    static string Letter(FakeRandom random)
    {
        return Letters[random.IntFromMaximumExclusive(Letters.Length)].ToString();
    }

    static string BaseLettersOrDigits(
        FakeRandom random,
        int length,
        int maximumOutputSize)
    {
        Guard.OutputSize(length, maximumOutputSize, "civilIdentifier");
        char[] characters = new char[length];
        int index = 0;
        while (index < characters.Length)
        {
            int selected = random.IntFromMaximumExclusive(36);
            characters[index] = selected < 10
                ? (char)('0' + selected)
                : (char)('A' + selected - 10);
            index = index + 1;
        }
        if (AllEqual(new ReadOnlySpan<char>(characters)))
        {
            characters[0] = characters[0] == 'Z' ? '0' :
                (char)(characters[0] + 1);
        }
        return new string(characters);
    }

    static bool Letter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    static bool Digit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool LettersFrom(ReadOnlySpan<char> value, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (!Letter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool DigitsFrom(ReadOnlySpan<char> value, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (!Digit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool LettersOrDigitsFrom(
        ReadOnlySpan<char> value,
        int start,
        int end)
    {
        int index = start;
        while (index < end)
        {
            if (!Letter(value[index]) && !Digit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool AllEqual(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] != value[0])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
