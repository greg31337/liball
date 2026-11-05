using System;

namespace fake;

internal static class SimIdentifierGenerator
{
    internal const string E118Version = "09/2025";
    internal const string E212Version = "06/2024";
    internal const int IccidMaximumLength = 19;
    internal const int ImsiMaximumLength = 15;
    internal const int MobileCountryCodeLength = 3;
    internal const int MaximumMobileSubscriptionLength = 10;

    const string TelecommunicationIndustryIdentifier = "89";
    const string PrivateNetworkMobileCountryCode = "999";
    const string TestMobileNetworkCodeTwoDigits = "99";
    const string TestMobileNetworkCodeThreeDigits = "999";

    internal static string GenerateIccid(
        IRandomSource random,
        int maximumCount,
        int maximumOutputSize)
    {
        ValidateGeneration(
            IccidMaximumLength,
            IccidMaximumLength - 1,
            maximumCount,
            maximumOutputSize);
        char[] destination = new char[IccidMaximumLength];
        destination[0] = TelecommunicationIndustryIdentifier[0];
        destination[1] = TelecommunicationIndustryIdentifier[1];
        FillDigits(
            random,
            new Span<char>(destination).Slice(
                TelecommunicationIndustryIdentifier.Length,
                IccidMaximumLength - TelecommunicationIndustryIdentifier.Length - 1));
        ApplyIccidCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static IccidResult GenerateIccidResult(
        GenerationContext context,
        IRandomSource random,
        int maximumCount,
        int maximumOutputSize)
    {
        string value = GenerateIccid(random, maximumCount, maximumOutputSize);
        return BuildIccidResult(
            context,
            value.AsSpan(),
            true,
            maximumCount,
            maximumOutputSize);
    }

    internal static IccidResult InspectIccidResult(
        GenerationContext context,
        ReadOnlySpan<char> value,
        int maximumCount,
        int maximumOutputSize)
    {
        return BuildIccidResult(
            context,
            value,
            false,
            maximumCount,
            maximumOutputSize);
    }

    internal static string ComposeIccid(
        ReadOnlySpan<char> payload,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireIccidPayload(payload);
        int outputLength = payload.Length + 1;
        ValidateGeneration(
            outputLength,
            payload.Length,
            maximumCount,
            maximumOutputSize);
        char[] destination = new char[outputLength];
        payload.CopyTo(destination);
        ApplyIccidCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static bool ValidateIccid(
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (value.Length < TelecommunicationIndustryIdentifier.Length + 1 ||
            value.Length > IccidMaximumLength ||
            value[0] != TelecommunicationIndustryIdentifier[0] ||
            value[1] != TelecommunicationIndustryIdentifier[1])
        {
            return false;
        }
        return ChecksumAlgorithms.IsLuhn(value, maximumCount);
    }

    internal static string IccidPayload(
        ReadOnlySpan<char> value,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireIccid(value, maximumCount);
        int length = value.Length - 1;
        Guard.OutputSize(length, maximumOutputSize, "value");
        return new string(value.Slice(0, length));
    }

    internal static string GenerateImsi(
        IRandomSource random,
        int mobileNetworkCodeLength,
        int maximumCount,
        int maximumOutputSize)
    {
        ValidateMobileNetworkCodeLength(mobileNetworkCodeLength);
        string mobileNetworkCode = TestMobileNetworkCodeTwoDigits;
        if (mobileNetworkCodeLength == 3)
        {
            mobileNetworkCode = TestMobileNetworkCodeThreeDigits;
        }
        return GenerateImsi(
            random,
            PrivateNetworkMobileCountryCode.AsSpan(),
            mobileNetworkCode.AsSpan(),
            maximumCount,
            maximumOutputSize);
    }

    internal static ImsiResult GenerateImsiResult(
        GenerationContext context,
        IRandomSource random,
        int mobileNetworkCodeLength,
        int maximumCount,
        int maximumOutputSize)
    {
        string value = GenerateImsi(
            random,
            mobileNetworkCodeLength,
            maximumCount,
            maximumOutputSize);
        return BuildImsiResult(
            context,
            value.AsSpan(),
            mobileNetworkCodeLength,
            true,
            maximumCount,
            maximumOutputSize);
    }

    internal static ImsiResult InspectImsiResult(
        GenerationContext context,
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        int maximumCount,
        int maximumOutputSize)
    {
        return BuildImsiResult(
            context,
            value,
            mobileNetworkCodeLength,
            false,
            maximumCount,
            maximumOutputSize);
    }

    internal static string GenerateImsi(
        IRandomSource random,
        ReadOnlySpan<char> mobileCountryCode,
        ReadOnlySpan<char> mobileNetworkCode,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireMobileCountryCode(mobileCountryCode);
        RequireMobileNetworkCode(mobileNetworkCode);
        ValidateGeneration(
            ImsiMaximumLength,
            ImsiMaximumLength,
            maximumCount,
            maximumOutputSize);
        char[] destination = new char[ImsiMaximumLength];
        mobileCountryCode.CopyTo(destination);
        mobileNetworkCode.CopyTo(
            new Span<char>(destination).Slice(MobileCountryCodeLength));
        int prefixLength = mobileCountryCode.Length + mobileNetworkCode.Length;
        FillDigits(
            random,
            new Span<char>(destination).Slice(
                prefixLength,
                ImsiMaximumLength - prefixLength));
        return new string(destination);
    }

    internal static string ComposeImsi(
        ReadOnlySpan<char> mobileCountryCode,
        ReadOnlySpan<char> mobileNetworkCode,
        ReadOnlySpan<char> mobileSubscriptionIdentificationNumber,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireMobileCountryCode(mobileCountryCode);
        RequireMobileNetworkCode(mobileNetworkCode);
        RequireMobileSubscriptionIdentificationNumber(
            mobileSubscriptionIdentificationNumber,
            mobileCountryCode.Length + mobileNetworkCode.Length);
        int length = mobileCountryCode.Length +
            mobileNetworkCode.Length +
            mobileSubscriptionIdentificationNumber.Length;
        ValidateGeneration(length, length, maximumCount, maximumOutputSize);
        char[] destination = new char[length];
        mobileCountryCode.CopyTo(destination);
        mobileNetworkCode.CopyTo(
            new Span<char>(destination).Slice(mobileCountryCode.Length));
        mobileSubscriptionIdentificationNumber.CopyTo(
            new Span<char>(destination).Slice(
                mobileCountryCode.Length + mobileNetworkCode.Length));
        return new string(destination);
    }

    internal static bool ValidateImsi(
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (value.Length < MobileCountryCodeLength + 2 + 1 ||
            value.Length > ImsiMaximumLength)
        {
            return false;
        }
        Guard.Count(value.Length, maximumCount, "value");
        return Digits(value);
    }

    internal static bool ValidateImsi(
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        int maximumCount)
    {
        ValidateMobileNetworkCodeLength(mobileNetworkCodeLength);
        if (!ValidateImsi(value, maximumCount))
        {
            return false;
        }
        int subscriptionLength = value.Length -
            MobileCountryCodeLength -
            mobileNetworkCodeLength;
        return subscriptionLength >= 1 &&
            subscriptionLength <= MaximumMobileSubscriptionLength;
    }

    internal static string ImsiMobileCountryCode(
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireImsi(value, mobileNetworkCodeLength, maximumCount);
        Guard.OutputSize(
            MobileCountryCodeLength,
            maximumOutputSize,
            "value");
        return new string(value.Slice(0, MobileCountryCodeLength));
    }

    internal static string ImsiMobileNetworkCode(
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireImsi(value, mobileNetworkCodeLength, maximumCount);
        Guard.OutputSize(
            mobileNetworkCodeLength,
            maximumOutputSize,
            "value");
        return new string(value.Slice(
            MobileCountryCodeLength,
            mobileNetworkCodeLength));
    }

    internal static string ImsiMobileSubscriptionIdentificationNumber(
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        int maximumCount,
        int maximumOutputSize)
    {
        RequireImsi(value, mobileNetworkCodeLength, maximumCount);
        int start = MobileCountryCodeLength + mobileNetworkCodeLength;
        int length = value.Length - start;
        Guard.OutputSize(length, maximumOutputSize, "value");
        return new string(value.Slice(start, length));
    }

    static IccidResult BuildIccidResult(
        GenerationContext context,
        ReadOnlySpan<char> value,
        bool isSynthetic,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(value.Length, maximumCount, "value");
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        bool structural = IccidStructure(value);
        IccidResult result = new IccidResult();
        result.Value = new string(value);
        result.StructuralValidity = structural
            ? ValidityMode.Valid
            : ValidityMode.Invalid;
        result.ChecksumValidity = structural
            ? (ChecksumAlgorithms.IsLuhn(value, maximumCount)
                ? ValidityMode.Valid
                : ValidityMode.Invalid)
            : ValidityMode.Unknown;
        result.IsSynthetic = isSynthetic;
        result.RepresentsAssignment = false;
        result.AllocationStatus = AllocationStatus.Unknown;
        if (structural)
        {
            int payloadLength = value.Length - 1;
            result.Payload = new string(value.Slice(0, payloadLength));
            result.MajorIndustryIdentifier = TelecommunicationIndustryIdentifier;
            result.IssuerAndAccountIdentifier = new string(value.Slice(
                TelecommunicationIndustryIdentifier.Length,
                payloadLength - TelecommunicationIndustryIdentifier.Length));
            result.CheckDigit = new string(value.Slice(payloadLength, 1));
        }
        ValidationResult validation = new ValidationResult();
        validation.Valid = result.StructuralValidity == ValidityMode.Valid &&
            result.ChecksumValidity == ValidityMode.Valid;
        validation.StructuralValidity = result.StructuralValidity;
        validation.ChecksumValidity = result.ChecksumValidity;
        validation.ConstraintValidity = ValidityMode.NotApplicable;
        validation.CorrelationValidity = structural
            ? ValidityMode.Valid
            : ValidityMode.Unknown;
        validation.AllocationStatus = result.AllocationStatus;
        if (result.StructuralValidity == ValidityMode.Invalid)
            validation.Detail = "ICCID structure is invalid.";
        else if (result.ChecksumValidity == ValidityMode.Invalid)
            validation.Detail = "ICCID checksum is invalid.";
        else
            validation.Detail = "ICCID structure, checksum, and parsed components are valid; allocation is unknown.";
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = result.Value;
        metadata.GeneratedValue = result.Value;
        metadata.ValueKind = "iccid";
        metadata.Provider = "phone";
        metadata.Locale = context.CapturedOptions.Locale;
        metadata.SourceLocale = LocaleFieldValue.InvariantLocale;
        metadata.Rule = "ITU-T E.118 " + E118Version;
        metadata.Format = "ICCID";
        metadata.RepresentsAssignment = false;
        context.AttachModes(metadata);
        metadata.StructuralValidity = validation.StructuralValidity;
        metadata.ChecksumValidity = validation.ChecksumValidity;
        metadata.ConstraintValidity = validation.ConstraintValidity;
        metadata.CorrelationValidity = validation.CorrelationValidity;
        metadata.AllocationStatus = validation.AllocationStatus;
        metadata.Validation = validation;
        result.Metadata = metadata;
        return result;
    }

    static ImsiResult BuildImsiResult(
        GenerationContext context,
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        bool isSynthetic,
        int maximumCount,
        int maximumOutputSize)
    {
        ValidateMobileNetworkCodeLength(mobileNetworkCodeLength);
        Guard.Count(value.Length, maximumCount, "value");
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        bool structural = ValidateImsi(
            value,
            mobileNetworkCodeLength,
            maximumCount);
        ImsiResult result = new ImsiResult();
        result.Value = new string(value);
        result.MobileNetworkCodeLength = mobileNetworkCodeLength;
        result.StructuralValidity = structural
            ? ValidityMode.Valid
            : ValidityMode.Invalid;
        result.ChecksumValidity = ValidityMode.NotApplicable;
        result.IsSynthetic = isSynthetic;
        result.RepresentsAssignment = false;
        result.AllocationStatus = AllocationStatus.Unknown;
        if (structural)
        {
            int prefixLength = MobileCountryCodeLength + mobileNetworkCodeLength;
            result.MobileCountryCode = new string(value.Slice(
                0,
                MobileCountryCodeLength));
            result.MobileNetworkCode = new string(value.Slice(
                MobileCountryCodeLength,
                mobileNetworkCodeLength));
            result.MobileSubscriptionIdentificationNumber = new string(
                value.Slice(prefixLength, value.Length - prefixLength));
        }
        bool safeReserved = structural &&
            result.MobileCountryCode == PrivateNetworkMobileCountryCode &&
            (result.MobileNetworkCode == TestMobileNetworkCodeTwoDigits ||
                result.MobileNetworkCode == TestMobileNetworkCodeThreeDigits);
        ValidationResult validation = new ValidationResult();
        validation.Valid = result.StructuralValidity == ValidityMode.Valid;
        validation.StructuralValidity = result.StructuralValidity;
        validation.ChecksumValidity = ValidityMode.NotApplicable;
        validation.ConstraintValidity = ValidityMode.NotApplicable;
        validation.CorrelationValidity = structural
            ? ValidityMode.Valid
            : ValidityMode.Unknown;
        validation.SafeReserved = safeReserved;
        validation.AllocationStatus = result.AllocationStatus;
        if (result.StructuralValidity == ValidityMode.Invalid)
            validation.Detail = "IMSI structure or requested component boundary is invalid.";
        else
            validation.Detail = "IMSI structure and parsed components are valid; allocation is unknown.";
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = result.Value;
        metadata.GeneratedValue = result.Value;
        metadata.ValueKind = "imsi";
        metadata.Provider = "phone";
        metadata.Locale = context.CapturedOptions.Locale;
        metadata.SourceLocale = LocaleFieldValue.InvariantLocale;
        metadata.Rule = "ITU-T E.212 " + E212Version;
        metadata.Format = "IMSI with an explicit MNC boundary";
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
        return result;
    }

    static void ApplyIccidCheckDigit(
        Span<char> destination,
        int maximumCount)
    {
        int checkDigit = ChecksumAlgorithms.Luhn(
            destination.Slice(0, destination.Length - 1),
            maximumCount);
        destination[destination.Length - 1] = (char)('0' + checkDigit);
    }

    static void FillDigits(IRandomSource random, Span<char> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index = index + 1;
        }
    }

    static void ValidateGeneration(
        int outputLength,
        int digitCount,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(digitCount, maximumCount, "value");
        Guard.OutputSize(outputLength, maximumOutputSize, "value");
    }

    static void RequireIccidPayload(ReadOnlySpan<char> value)
    {
        if (value.Length < TelecommunicationIndustryIdentifier.Length ||
            value.Length >= IccidMaximumLength ||
            value[0] != TelecommunicationIndustryIdentifier[0] ||
            value[1] != TelecommunicationIndustryIdentifier[1] ||
            !Digits(value))
        {
            throw new ArgumentException(
                "payload must contain 2 through 18 ASCII decimal digits beginning with 89.",
                "payload");
        }
    }

    static bool IccidStructure(ReadOnlySpan<char> value)
    {
        return value.Length >= TelecommunicationIndustryIdentifier.Length + 1 &&
            value.Length <= IccidMaximumLength &&
            value[0] == TelecommunicationIndustryIdentifier[0] &&
            value[1] == TelecommunicationIndustryIdentifier[1] &&
            Digits(value);
    }

    static void RequireIccid(
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (!ValidateIccid(value, maximumCount))
        {
            throw new ArgumentException(
                "value must be an ICCID shape of at most 19 digits beginning with 89 and ending in a valid Luhn digit.",
                "value");
        }
    }

    static void RequireMobileCountryCode(ReadOnlySpan<char> value)
    {
        if (value.Length != MobileCountryCodeLength || !Digits(value))
        {
            throw new ArgumentException(
                "mobileCountryCode must contain exactly 3 ASCII decimal digits.",
                "mobileCountryCode");
        }
    }

    static void RequireMobileNetworkCode(ReadOnlySpan<char> value)
    {
        if ((value.Length != 2 && value.Length != 3) || !Digits(value))
        {
            throw new ArgumentException(
                "mobileNetworkCode must contain exactly 2 or 3 ASCII decimal digits.",
                "mobileNetworkCode");
        }
    }

    static void RequireMobileSubscriptionIdentificationNumber(
        ReadOnlySpan<char> value,
        int prefixLength)
    {
        if (value.Length < 1 ||
            value.Length > MaximumMobileSubscriptionLength ||
            prefixLength + value.Length > ImsiMaximumLength ||
            !Digits(value))
        {
            throw new ArgumentException(
                "mobileSubscriptionIdentificationNumber must contain 1 through 10 ASCII decimal digits while keeping the IMSI at no more than 15 digits.",
                "mobileSubscriptionIdentificationNumber");
        }
    }

    static void ValidateMobileNetworkCodeLength(int value)
    {
        if (value != 2 && value != 3)
        {
            throw new ArgumentOutOfRangeException(
                "mobileNetworkCodeLength",
                value,
                "mobileNetworkCodeLength must be 2 or 3.");
        }
    }

    static void RequireImsi(
        ReadOnlySpan<char> value,
        int mobileNetworkCodeLength,
        int maximumCount)
    {
        if (!ValidateImsi(value, mobileNetworkCodeLength, maximumCount))
        {
            throw new ArgumentException(
                "value must contain 6 through 15 ASCII decimal digits with at least one MSIN digit for the stated MNC length.",
                "value");
        }
    }

    static bool Digits(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
