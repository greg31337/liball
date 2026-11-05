using System;
using System.Globalization;
using System.Text;

namespace fake;

internal class PhoneMaskValue
{
    internal string Compact = string.Empty;
    internal string Variables = string.Empty;

    internal PhoneMaskValue()
    {
    }
}

internal class PhoneNumberParts
{
    internal string AreaCode = string.Empty;
    internal string Subscriber = string.Empty;
    internal string Variables = string.Empty;

    internal PhoneNumberParts()
    {
    }
}

internal static class PhoneNumberGenerator
{
    internal static string Formatted(
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        SafetyMode safetyMode,
        int maximumOutputSize)
    {
        PhoneFormatRule rule = Rule(metadata, category);
        PhoneSafetyMetadata safety = PhoneSafetyResolver.Resolve(
            metadata,
            category,
            safetyMode);
        string mask = Mask(rule, safety.SafeRange, format);
        PhoneNumberParts parts = Parts(random, rule, safety.SafeRange);
        string result = Render(mask, parts.Variables);
        if (format == PhoneNumberFormat.E164)
        {
            ValidateE164(result);
        }
        Guard.OutputSize(result.Length, maximumOutputSize, "phoneNumber");
        return result;
    }

    internal static string WithExtension(
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        int extensionLength,
        SafetyMode safetyMode,
        int maximumOutputSize)
    {
        if (format == PhoneNumberFormat.E164)
        {
            throw new ArgumentException(
                "E.164 output cannot contain an extension suffix.",
                "format");
        }
        PhoneFormatRule rule = Rule(metadata, category);
        if (!rule.AllowsExtension)
        {
            throw new ArgumentException(
                "The requested phone category does not allow extensions.",
                "category");
        }
        PhoneSafetyMetadata safety = PhoneSafetyResolver.Resolve(
            metadata,
            category,
            safetyMode);
        string mask = Mask(rule, safety.SafeRange, format);
        PhoneNumberParts parts = Parts(random, rule, safety.SafeRange);
        string number = Render(mask, parts.Variables);
        string extension = Extension(
            random,
            metadata,
            extensionLength,
            maximumOutputSize);
        string result = number + metadata.ExtensionPrefix + extension;
        Guard.OutputSize(result.Length, maximumOutputSize, "phoneNumber");
        return result;
    }

    internal static string WithRandomExtension(
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        SafetyMode safetyMode,
        int maximumOutputSize)
    {
        if (format == PhoneNumberFormat.E164)
        {
            throw new ArgumentException(
                "E.164 output cannot contain an extension suffix.",
                "format");
        }
        PhoneFormatRule rule = Rule(metadata, category);
        if (!rule.AllowsExtension)
        {
            throw new ArgumentException(
                "The requested phone category does not allow extensions.",
                "category");
        }
        PhoneSafetyMetadata safety = PhoneSafetyResolver.Resolve(
            metadata,
            category,
            safetyMode);
        string mask = Mask(rule, safety.SafeRange, format);
        PhoneNumberParts parts = Parts(random, rule, safety.SafeRange);
        string number = Render(mask, parts.Variables);
        int length = random.IntFromMinimumAndMaximum(
            metadata.MinimumExtensionDigits,
            metadata.MaximumExtensionDigits + 1);
        string extension = Extension(
            random,
            metadata,
            length,
            maximumOutputSize);
        string result = number + metadata.ExtensionPrefix + extension;
        Guard.OutputSize(result.Length, maximumOutputSize, "phoneNumber");
        return result;
    }

    internal static string Extension(
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        int length,
        int maximumOutputSize)
    {
        if (length < metadata.MinimumExtensionDigits ||
            length > metadata.MaximumExtensionDigits)
        {
            throw new ArgumentOutOfRangeException(
                "length",
                length,
                "length must be within the resolved locale extension bounds.");
        }
        return NumericStringGenerator.Digits(random, length, maximumOutputSize);
    }

    internal static string AreaCode(
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        SafetyMode safetyMode,
        int maximumOutputSize)
    {
        PhoneFormatRule rule = Rule(metadata, category);
        PhoneSafetyMetadata safety = PhoneSafetyResolver.Resolve(
            metadata,
            category,
            safetyMode);
        PhoneMaskValue area;
        if (safety.SafeRange == null)
        {
            area = Area(random, rule);
        }
        else
        {
            area = GenerateMask(random, safety.SafeRange.AreaCodeMask);
        }
        Guard.OutputSize(area.Compact.Length, maximumOutputSize, "areaCode");
        return area.Compact;
    }

    internal static PhoneRecord Record(
        GenerationContext context,
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        SafetyMode safetyMode,
        int maximumOutputSize)
    {
        return CreateRecord(
            context,
            random,
            metadata,
            category,
            format,
            safetyMode,
            false,
            0,
            maximumOutputSize);
    }

    internal static PhoneRecord RecordWithExtension(
        GenerationContext context,
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        int extensionLength,
        SafetyMode safetyMode,
        int maximumOutputSize)
    {
        return CreateRecord(
            context,
            random,
            metadata,
            category,
            format,
            safetyMode,
            true,
            extensionLength,
            maximumOutputSize);
    }

    internal static PhoneFormatRule Rule(
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category)
    {
        int index = 0;
        while (index < metadata.Formats.Length)
        {
            if (metadata.Formats[index].Category == category)
            {
                return metadata.Formats[index];
            }
            index = index + 1;
        }
        throw new ArgumentOutOfRangeException(
            "category",
            category,
            "category is not available in the resolved phone locale catalog.");
    }

    static PhoneRecord CreateRecord(
        GenerationContext context,
        FakeRandom random,
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        SafetyMode safetyMode,
        bool includeExtension,
        int extensionLength,
        int maximumOutputSize)
    {
        PhoneFormatRule rule = Rule(metadata, category);
        if (includeExtension && format == PhoneNumberFormat.E164)
        {
            throw new ArgumentException(
                "E.164 output cannot contain an extension suffix.",
                "format");
        }
        if (includeExtension && !rule.AllowsExtension)
        {
            throw new ArgumentException(
                "The requested phone category does not allow extensions.",
                "category");
        }
        PhoneSafetyMetadata safety = PhoneSafetyResolver.Resolve(
            metadata,
            category,
            safetyMode);
        string mask = Mask(rule, safety.SafeRange, format);
        PhoneNumberParts parts = Parts(random, rule, safety.SafeRange);
        string number = Render(mask, parts.Variables);
        if (format == PhoneNumberFormat.E164)
        {
            ValidateE164(number);
        }
        string extension = string.Empty;
        if (includeExtension)
        {
            extension = Extension(
                random,
                metadata,
                extensionLength,
                maximumOutputSize);
            number = number + metadata.ExtensionPrefix + extension;
        }
        Guard.OutputSize(number.Length, maximumOutputSize, "phoneRecord");

        PhoneRecordParsingMetadata parsing = ParsingMetadata(
            metadata,
            rule,
            category,
            format,
            mask);
        PhoneRecord result = new PhoneRecord();
        result.Number = number;
        result.AreaCode = parts.AreaCode;
        result.SubscriberNumber = parts.Subscriber;
        result.NationalSignificantNumber = parts.AreaCode + parts.Subscriber;
        result.Extension = extension;
        result.HasExtension = includeExtension;
        result.ParsingMetadata = parsing;
        result.SafetyMetadata = safety;
        AttachGeneralMetadata(context, result, metadata);
        ValidateRecordOutput(result, maximumOutputSize);
        return result;
    }

    static void AttachGeneralMetadata(
        GenerationContext context,
        PhoneRecord result,
        PhoneLocaleMetadata localeMetadata)
    {
        bool safeReserved = result.SafetyMetadata.UsesAuthoritativeSafeRange;
        ValidationResult validation = new ValidationResult();
        validation.Valid = true;
        validation.StructuralValidity = ValidityMode.Valid;
        validation.ChecksumValidity = ValidityMode.NotApplicable;
        validation.ConstraintValidity = ValidityMode.Valid;
        validation.CorrelationValidity = ValidityMode.Valid;
        validation.SafeReserved = safeReserved;
        validation.AllocationStatus = AllocationStatus.Unknown;
        validation.Detail = "Phone structure and record correlations are valid; allocation and reachability are unknown.";
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = result.Number;
        metadata.GeneratedValue = result.Number;
        metadata.ValueKind = "phone-number";
        metadata.Provider = "phone";
        metadata.Locale = localeMetadata.RequestedLocale;
        metadata.SourceLocale = localeMetadata.SourceLocale;
        metadata.UsedLocaleFallback = localeMetadata.UsedLocaleFallback;
        metadata.Rule = result.ParsingMetadata.FormatMask;
        metadata.Format = result.ParsingMetadata.Format.ToString();
        metadata.RepresentsAssignment = false;
        context.AttachModes(metadata);
        metadata.StructuralValidity = validation.StructuralValidity;
        metadata.ChecksumValidity = validation.ChecksumValidity;
        metadata.ConstraintValidity = validation.ConstraintValidity;
        metadata.CorrelationValidity = validation.CorrelationValidity;
        metadata.SafeReserved = safeReserved;
        metadata.AllocationStatus = validation.AllocationStatus;
        metadata.Validation = validation;
        result.Metadata = metadata;
    }

    static PhoneRecordParsingMetadata ParsingMetadata(
        PhoneLocaleMetadata metadata,
        PhoneFormatRule rule,
        PhoneNumberCategory category,
        PhoneNumberFormat format,
        string mask)
    {
        PhoneRecordParsingMetadata result = new PhoneRecordParsingMetadata();
        result.RegionCode = metadata.RegionCode;
        result.CallingCode = metadata.CallingCode;
        result.NationalPrefix = metadata.NationalPrefix;
        result.Category = category;
        result.Format = format;
        result.FormatMask = mask;
        result.AreaCodeMask = rule.AreaCodeMask;
        result.SubscriberMask = rule.SubscriberMask;
        result.ExtensionPrefix = metadata.ExtensionPrefix;
        result.CategoryDistinguishableByNumber = rule.DistinguishableByNumber;
        result.AllowsExtension = rule.AllowsExtension;
        return result;
    }

    static void ValidateRecordOutput(
        PhoneRecord record,
        int maximumOutputSize)
    {
        Guard.OutputSize(record.AreaCode.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.SubscriberNumber.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.NationalSignificantNumber.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.Extension.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.RegionCode.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.CallingCode.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.NationalPrefix.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.FormatMask.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.AreaCodeMask.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.SubscriberMask.Length, maximumOutputSize, "phoneRecord");
        Guard.OutputSize(record.ParsingMetadata.ExtensionPrefix.Length, maximumOutputSize, "phoneRecord");
        PhoneSafetyResolver.ValidateOutput(record.SafetyMetadata, maximumOutputSize);
    }

    static PhoneNumberParts Parts(
        FakeRandom random,
        PhoneFormatRule rule,
        PhoneSafeRange? range)
    {
        if (range != null)
        {
            return SafeParts(random, range);
        }
        PhoneMaskValue area = Area(random, rule);
        PhoneMaskValue subscriber = GenerateMask(random, rule.SubscriberMask);
        PhoneNumberParts result = new PhoneNumberParts();
        result.AreaCode = area.Compact;
        result.Subscriber = subscriber.Compact;
        result.Variables = area.Variables + subscriber.Variables;
        return result;
    }

    static PhoneNumberParts SafeParts(FakeRandom random, PhoneSafeRange range)
    {
        PhoneMaskValue area = GenerateMask(random, range.AreaCodeMask);
        int commonLength = CommonPrefix(
            range.FirstSubscriber,
            range.LastSubscriber);
        long first = long.Parse(
            range.FirstSubscriber,
            CultureInfo.InvariantCulture);
        long last = long.Parse(
            range.LastSubscriber,
            CultureInfo.InvariantCulture);
        long selected = random.LongFromMinimumAndMaximumAndMaximumInclusive(first, last, true);
        string format = new string('0', range.FirstSubscriber.Length);
        string subscriber = selected.ToString(format, CultureInfo.InvariantCulture);
        PhoneNumberParts result = new PhoneNumberParts();
        result.AreaCode = area.Compact;
        result.Subscriber = range.Exchange + subscriber;
        result.Variables = area.Variables + subscriber.Substring(commonLength);
        return result;
    }

    static int CommonPrefix(string first, string last)
    {
        int index = 0;
        while (index < first.Length && first[index] == last[index])
        {
            index = index + 1;
        }
        return index;
    }

    static PhoneMaskValue Area(FakeRandom random, PhoneFormatRule rule)
    {
        if (rule.AreaCodes.Length == 0)
        {
            return GenerateMask(random, rule.AreaCodeMask);
        }
        string selected = rule.AreaCodes[random.IntFromMaximumExclusive(rule.AreaCodes.Length)];
        return FixedMask(rule.AreaCodeMask, selected);
    }

    static PhoneMaskValue GenerateMask(FakeRandom random, string mask)
    {
        StringBuilder compact = new StringBuilder();
        StringBuilder variables = new StringBuilder();
        int index = 0;
        while (index < mask.Length)
        {
            char character = mask[index];
            if (character == 'N')
            {
                char digit = (char)('0' + random.IntFromMinimumAndMaximum(2, 10));
                compact.Append(digit);
                variables.Append(digit);
            }
            else if (character == '#')
            {
                char digit = (char)('0' + random.IntFromMaximumExclusive(10));
                compact.Append(digit);
                variables.Append(digit);
            }
            else if (character >= '0' && character <= '9')
            {
                compact.Append(character);
            }
            index = index + 1;
        }
        PhoneMaskValue result = new PhoneMaskValue();
        result.Compact = compact.ToString();
        result.Variables = variables.ToString();
        return result;
    }

    static PhoneMaskValue FixedMask(string mask, string digits)
    {
        StringBuilder variables = new StringBuilder();
        int digitIndex = 0;
        int maskIndex = 0;
        while (maskIndex < mask.Length)
        {
            char character = mask[maskIndex];
            if (character == 'N' || character == '#' ||
                character >= '0' && character <= '9')
            {
                if (digitIndex >= digits.Length ||
                    digits[digitIndex] < '0' || digits[digitIndex] > '9')
                {
                    throw FakeException.Create("Phone area-code metadata does not match its mask.");
                }
                if (character == 'N' &&
                    (digits[digitIndex] < '2' || digits[digitIndex] > '9'))
                {
                    throw FakeException.Create("Phone area-code metadata violates an N mask position.");
                }
                if (character >= '0' && character <= '9' &&
                    character != digits[digitIndex])
                {
                    throw FakeException.Create("Phone area-code metadata violates a fixed mask digit.");
                }
                if (character == 'N' || character == '#')
                {
                    variables.Append(digits[digitIndex]);
                }
                digitIndex = digitIndex + 1;
            }
            maskIndex = maskIndex + 1;
        }
        if (digitIndex != digits.Length)
        {
            throw FakeException.Create("Phone area-code metadata has a different length than its mask.");
        }
        PhoneMaskValue result = new PhoneMaskValue();
        result.Compact = digits;
        result.Variables = variables.ToString();
        return result;
    }

    static string Mask(
        PhoneFormatRule rule,
        PhoneSafeRange? range,
        PhoneNumberFormat format)
    {
        if (range != null)
        {
            return SafeMask(range, format);
        }
        switch (format)
        {
            case PhoneNumberFormat.National:
                return rule.NationalMask;
            case PhoneNumberFormat.International:
                return rule.InternationalMask;
            case PhoneNumberFormat.E164:
                return rule.E164Mask;
            default:
                throw new ArgumentOutOfRangeException(
                    "format",
                    format,
                    "format is not a supported phone-number presentation.");
        }
    }

    static string SafeMask(PhoneSafeRange range, PhoneNumberFormat format)
    {
        switch (format)
        {
            case PhoneNumberFormat.National:
                return range.NationalMask;
            case PhoneNumberFormat.International:
                return range.InternationalMask;
            case PhoneNumberFormat.E164:
                return range.E164Mask;
            default:
                throw new ArgumentOutOfRangeException(
                    "format",
                    format,
                    "format is not a supported phone-number presentation.");
        }
    }

    static string Render(string mask, string variables)
    {
        StringBuilder result = new StringBuilder();
        int variableIndex = 0;
        int maskIndex = 0;
        while (maskIndex < mask.Length)
        {
            char character = mask[maskIndex];
            if (character == 'N' || character == '#')
            {
                if (variableIndex >= variables.Length)
                {
                    throw FakeException.Create("Phone format mask requires more generated digits than its category masks provide.");
                }
                char digit = variables[variableIndex];
                if (character == 'N' && (digit < '2' || digit > '9'))
                {
                    throw FakeException.Create("Phone format mask places an unrestricted digit in an N position.");
                }
                result.Append(digit);
                variableIndex = variableIndex + 1;
            }
            else
            {
                result.Append(character);
            }
            maskIndex = maskIndex + 1;
        }
        if (variableIndex != variables.Length)
        {
            throw FakeException.Create("Phone format mask does not consume every generated category digit.");
        }
        return result.ToString();
    }

    static void ValidateE164(string value)
    {
        if (value.Length < 2 || value[0] != '+')
        {
            throw FakeException.Create("E.164-shaped phone output must begin with plus.");
        }
        int digits = 0;
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                throw FakeException.Create("E.164-shaped phone output may contain only plus and decimal digits.");
            }
            digits = digits + 1;
            index = index + 1;
        }
        if (digits > 15 || value[1] == '0')
        {
            throw FakeException.Create("E.164-shaped phone output must contain at most 15 digits and a nonzero calling-code digit.");
        }
    }
}
