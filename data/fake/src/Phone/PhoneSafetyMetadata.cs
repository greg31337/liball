namespace fake;

internal static class PhoneSafetyResolver
{
    internal static PhoneSafetyMetadata Resolve(
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        SafetyMode requestedMode)
    {
        PhoneFormatRule rule = PhoneNumberGenerator.Rule(metadata, category);
        PhoneSafetyMetadata result = Base(metadata, category, requestedMode);
        switch (requestedMode)
        {
            case SafetyMode.Reserved:
                result.SafeRange = AuthoritativeRange(metadata, rule, category);
                if (result.SafeRange != null)
                {
                    result.AppliedMode = SafetyMode.Reserved;
                    result.UsesAuthoritativeSafeRange = true;
                    return result;
                }
                throw FakeException.Create(
                    "Reserved phone generation requires an authoritative safe-fiction range for the resolved region and category. Select SafetyMode.SyntheticPlausible or SafetyMode.UnrestrictedShape explicitly.");
            case SafetyMode.SyntheticPlausible:
            case SafetyMode.UnrestrictedShape:
                result.AppliedMode = requestedMode;
                return result;
            default:
                throw new System.ArgumentOutOfRangeException(
                    "requestedMode",
                    requestedMode,
                    "requestedMode is not a supported phone safety mode.");
        }
    }

    internal static void ValidateOutput(
        PhoneSafetyMetadata metadata,
        int maximumOutputSize)
    {
        Guard.OutputSize(metadata.RegionCode.Length, maximumOutputSize, "phoneSafetyMetadata");
        Guard.OutputSize(metadata.FallbackReason.Length, maximumOutputSize, "phoneSafetyMetadata");
        if (metadata.SafeRange != null)
        {
            Guard.OutputSize(metadata.SafeRange.AreaCodeMask.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.Exchange.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.FirstSubscriber.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.LastSubscriber.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.NationalMask.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.InternationalMask.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.E164Mask.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.Authority.Length, maximumOutputSize, "phoneSafetyMetadata");
            Guard.OutputSize(metadata.SafeRange.SourceVersion.Length, maximumOutputSize, "phoneSafetyMetadata");
        }
    }

    static PhoneSafetyMetadata Base(
        PhoneLocaleMetadata metadata,
        PhoneNumberCategory category,
        SafetyMode requestedMode)
    {
        PhoneSafetyMetadata result = new PhoneSafetyMetadata();
        result.RequestedMode = requestedMode;
        result.AppliedMode = requestedMode;
        result.Category = category;
        result.RegionCode = metadata.RegionCode;
        result.UsesAuthoritativeSafeRange = false;
        result.UsedFallback = false;
        result.FallbackReason = string.Empty;
        result.SafeRange = null;
        return result;
    }

    static PhoneSafeRange? AuthoritativeRange(
        PhoneLocaleMetadata metadata,
        PhoneFormatRule rule,
        PhoneNumberCategory category)
    {
        int index = 0;
        while (index < metadata.SafeRanges.Length)
        {
            PhoneSafeRange range = metadata.SafeRanges[index];
            if (range.IsAuthoritative &&
                (range.Category == category ||
                    range.Category == PhoneNumberCategory.General &&
                    !rule.DistinguishableByNumber))
            {
                return range;
            }
            index = index + 1;
        }
        return null;
    }
}
