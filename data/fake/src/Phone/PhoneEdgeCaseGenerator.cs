using System.Globalization;

namespace fake;

internal static class PhoneEdgeCaseGenerator
{
    const int CaseCount = 6;

    internal static PhoneEdgeCaseRecord Generate(
        GenerationContext context,
        FakeRandom random,
        PhoneEdgeCase edgeCase,
        int maximumCount,
        int maximumOutputSize)
    {
        ValidateCase(edgeCase);
        PhoneEdgeCaseRecord result = Base(edgeCase);
        switch (edgeCase)
        {
            case PhoneEdgeCase.VariableLength:
                VariableLength(random, result, maximumOutputSize);
                break;
            case PhoneEdgeCase.SharedCallingCode:
                SharedCallingCode(random, result, maximumOutputSize);
                break;
            case PhoneEdgeCase.SignificantLeadingZero:
                SignificantLeadingZero(random, result, maximumOutputSize);
                break;
            case PhoneEdgeCase.Extension:
                Extension(random, result, maximumOutputSize);
                break;
            case PhoneEdgeCase.NonGeographicService:
                NonGeographicService(random, result, maximumOutputSize);
                break;
            case PhoneEdgeCase.AmbiguousCategory:
                AmbiguousCategory(random, result, maximumOutputSize);
                break;
        }
        Guard.Count(result.RegionCodes.Length, maximumCount, "phoneEdgeCase");
        Guard.Count(result.PossibleCategories.Length, maximumCount, "phoneEdgeCase");
        AttachGeneralMetadata(context, result);
        ValidateOutput(result, maximumOutputSize);
        return result;
    }

    internal static PhoneEdgeCaseRecord[] GenerateAll(
        GenerationContext context,
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(CaseCount, maximumCount, "phoneEdgeCases");
        PhoneEdgeCaseRecord[] result = new PhoneEdgeCaseRecord[CaseCount];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Generate(
                context,
                random,
                (PhoneEdgeCase)index,
                maximumCount,
                maximumOutputSize);
            index = index + 1;
        }
        return result;
    }

    static void AttachGeneralMetadata(
        GenerationContext context,
        PhoneEdgeCaseRecord result)
    {
        bool safeReserved = result.CallingCode == "+1" &&
            result.Number.Contains(" 555 01");
        ValidationResult validation = new ValidationResult();
        validation.Valid = true;
        validation.StructuralValidity = ValidityMode.Valid;
        validation.ChecksumValidity = ValidityMode.NotApplicable;
        validation.ConstraintValidity = ValidityMode.Valid;
        validation.CorrelationValidity = ValidityMode.Valid;
        validation.SafeReserved = safeReserved;
        validation.AllocationStatus = AllocationStatus.Unknown;
        validation.RequestedEdgeCase = result.Case.ToString();
        validation.Detail = "The requested phone edge structure and correlated fields are valid; allocation and reachability are unknown.";
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = result.Number;
        metadata.GeneratedValue = result.Number;
        metadata.ValueKind = "phone-edge-case";
        metadata.Provider = "phone";
        metadata.Locale = context.CapturedOptions.Locale;
        metadata.SourceLocale = LocaleFieldValue.InvariantLocale;
        metadata.Rule = result.Case.ToString();
        metadata.Format = "phone modeling edge fixture";
        metadata.RepresentsAssignment = false;
        context.AttachModes(metadata);
        metadata.StructuralValidity = validation.StructuralValidity;
        metadata.ChecksumValidity = validation.ChecksumValidity;
        metadata.ConstraintValidity = validation.ConstraintValidity;
        metadata.CorrelationValidity = validation.CorrelationValidity;
        metadata.SafeReserved = safeReserved;
        metadata.AllocationStatus = validation.AllocationStatus;
        metadata.Validation = validation;
        metadata.RequestedEdgeCase = result.Case.ToString();
        result.Metadata = metadata;
    }

    static PhoneEdgeCaseRecord Base(PhoneEdgeCase edgeCase)
    {
        PhoneEdgeCaseRecord result = new PhoneEdgeCaseRecord();
        result.Case = edgeCase;
        result.Number = string.Empty;
        result.RelatedNumber = string.Empty;
        result.CallingCode = string.Empty;
        result.RegionCodes = new string[0];
        result.PossibleCategories = new PhoneNumberCategory[0];
        result.HasVariableLength = false;
        result.CallingCodeIsShared = false;
        result.LeadingZeroIsSignificant = false;
        result.HasExtension = false;
        result.Extension = string.Empty;
        result.IsNonGeographic = false;
        result.CategoryIsAmbiguous = false;
        result.ReachabilityKnown = false;
        result.SubscriberIdentityKnown = false;
        result.GeographicLocationKnown = false;
        return result;
    }

    static void VariableLength(
        FakeRandom random,
        PhoneEdgeCaseRecord result,
        int maximumOutputSize)
    {
        string root = Digits(random, 4, maximumOutputSize);
        string extra = Digits(random, 2, maximumOutputSize);
        result.Number = "+999 12 " + root;
        result.RelatedNumber = "+999 12 " + root + extra;
        result.CallingCode = "+999";
        result.RegionCodes = new string[] { "XZ" };
        result.PossibleCategories = new PhoneNumberCategory[]
        {
            PhoneNumberCategory.General
        };
        result.HasVariableLength = true;
    }

    static void SharedCallingCode(
        FakeRandom random,
        PhoneEdgeCaseRecord result,
        int maximumOutputSize)
    {
        result.Number = SafeNanpa(random, maximumOutputSize);
        result.CallingCode = "+1";
        result.RegionCodes = new string[] { "US", "CA" };
        result.PossibleCategories = new PhoneNumberCategory[]
        {
            PhoneNumberCategory.General
        };
        result.CallingCodeIsShared = true;
    }

    static void SignificantLeadingZero(
        FakeRandom random,
        PhoneEdgeCaseRecord result,
        int maximumOutputSize)
    {
        result.Number = "+39 0" + Digits(random, 7, maximumOutputSize);
        result.CallingCode = "+39";
        result.RegionCodes = new string[] { "IT" };
        result.PossibleCategories = new PhoneNumberCategory[]
        {
            PhoneNumberCategory.Landline
        };
        result.LeadingZeroIsSignificant = true;
    }

    static void Extension(
        FakeRandom random,
        PhoneEdgeCaseRecord result,
        int maximumOutputSize)
    {
        string number = SafeNanpa(random, maximumOutputSize);
        string extension = Digits(random, 4, maximumOutputSize);
        result.Number = number + " ext. " + extension;
        result.CallingCode = "+1";
        result.RegionCodes = new string[] { "US", "CA" };
        result.PossibleCategories = new PhoneNumberCategory[]
        {
            PhoneNumberCategory.General
        };
        result.HasExtension = true;
        result.Extension = extension;
    }

    static void NonGeographicService(
        FakeRandom random,
        PhoneEdgeCaseRecord result,
        int maximumOutputSize)
    {
        result.Number = "+800 " + Digits(random, 8, maximumOutputSize);
        result.CallingCode = "+800";
        result.RegionCodes = new string[0];
        result.PossibleCategories = new PhoneNumberCategory[]
        {
            PhoneNumberCategory.TollFree
        };
        result.IsNonGeographic = true;
    }

    static void AmbiguousCategory(
        FakeRandom random,
        PhoneEdgeCaseRecord result,
        int maximumOutputSize)
    {
        result.Number = SafeNanpa(random, maximumOutputSize);
        result.CallingCode = "+1";
        result.RegionCodes = new string[] { "US", "CA" };
        result.PossibleCategories = new PhoneNumberCategory[]
        {
            PhoneNumberCategory.Landline,
            PhoneNumberCategory.Mobile
        };
        result.CallingCodeIsShared = true;
        result.CategoryIsAmbiguous = true;
    }

    static string SafeNanpa(FakeRandom random, int maximumOutputSize)
    {
        string areaCode = random.IntFromMinimumAndMaximum(2, 10).ToString(CultureInfo.InvariantCulture) +
            Digits(random, 2, maximumOutputSize);
        string lineNumber = Digits(random, 2, maximumOutputSize);
        string result = "+1 " + areaCode + " 555 01" + lineNumber;
        Guard.OutputSize(result.Length, maximumOutputSize, "phoneEdgeCase");
        return result;
    }

    static string Digits(
        FakeRandom random,
        int length,
        int maximumOutputSize)
    {
        return NumericStringGenerator.Digits(
            random,
            length,
            maximumOutputSize);
    }

    static void ValidateCase(PhoneEdgeCase edgeCase)
    {
        switch (edgeCase)
        {
            case PhoneEdgeCase.VariableLength:
            case PhoneEdgeCase.SharedCallingCode:
            case PhoneEdgeCase.SignificantLeadingZero:
            case PhoneEdgeCase.Extension:
            case PhoneEdgeCase.NonGeographicService:
            case PhoneEdgeCase.AmbiguousCategory:
                return;
            default:
                throw new System.ArgumentOutOfRangeException(
                    "edgeCase",
                    edgeCase,
                    "edgeCase is not a supported phone modeling case.");
        }
    }

    static void ValidateOutput(
        PhoneEdgeCaseRecord record,
        int maximumOutputSize)
    {
        Guard.OutputSize(record.Number.Length, maximumOutputSize, "phoneEdgeCase");
        Guard.OutputSize(record.RelatedNumber.Length, maximumOutputSize, "phoneEdgeCase");
        Guard.OutputSize(record.CallingCode.Length, maximumOutputSize, "phoneEdgeCase");
        Guard.OutputSize(record.Extension.Length, maximumOutputSize, "phoneEdgeCase");
        int index = 0;
        while (index < record.RegionCodes.Length)
        {
            Guard.OutputSize(
                record.RegionCodes[index].Length,
                maximumOutputSize,
                "phoneEdgeCase");
            index = index + 1;
        }
    }
}
