using System;

namespace fake;

internal static class IdentifierValidation
{
    internal static ValidationResult Validate(
        ReadOnlySpan<char> value,
        IdentifierKind kind,
        int maximumCount)
    {
        ValidateKind(kind);
        Guard.Count(value.Length, maximumCount, "value");
        ValidationResult result = new ValidationResult();
        result.Kind = kind;
        result.AllocationStatus = Allocation(kind);
        result.FictionalReserved = result.AllocationStatus == AllocationStatus.FictionalReserved;
        result.SafeReserved = result.FictionalReserved;
        bool structural = Structure(value, kind, maximumCount);
        result.StructuralValidity = structural ? ValidityMode.Valid : ValidityMode.Invalid;
        result.ChecksumValidity = Checksum(value, kind, maximumCount);
        result.ConstraintValidity = Constraint(value, kind);
        bool checksumValid = result.ChecksumValidity == ValidityMode.Valid ||
            result.ChecksumValidity == ValidityMode.NotApplicable;
        bool constraintValid = result.ConstraintValidity == ValidityMode.Valid ||
            result.ConstraintValidity == ValidityMode.NotApplicable;
        result.Valid = structural && checksumValid && constraintValid;
        result.Detail = Detail(result);
        return result;
    }

    internal static GenerationMetadata Metadata(
        GenerationContext context,
        string value,
        IdentifierKind kind)
    {
        ValidationResult validation = Validate(
            value.AsSpan(),
            kind,
            context.CapturedOptions.MaximumCount);
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = value;
        metadata.GeneratedValue = value;
        metadata.ValueKind = kind.ToString();
        metadata.Provider = "identifier";
        metadata.Kind = kind;
        metadata.Locale = context.CapturedOptions.Locale;
        metadata.SourceLocale = LocaleFieldValue.InvariantLocale;
        metadata.Rule = kind.ToString();
        metadata.Format = "identifier";
        context.AttachModes(metadata);
        metadata.StructuralValidity = validation.StructuralValidity;
        metadata.ChecksumValidity = validation.ChecksumValidity;
        metadata.ConstraintValidity = validation.ConstraintValidity;
        metadata.CorrelationValidity = validation.CorrelationValidity;
        metadata.FictionalReserved = validation.FictionalReserved;
        metadata.SafeReserved = validation.SafeReserved;
        metadata.AllocationStatus = validation.AllocationStatus;
        metadata.Validation = validation;
        return metadata;
    }

    static bool Structure(
        ReadOnlySpan<char> value,
        IdentifierKind kind,
        int maximumCount)
    {
        if (kind == IdentifierKind.Uuid || kind == IdentifierKind.Uuid4 || kind == IdentifierKind.Uuid7)
        {
            return UuidGenerator.Validate(value);
        }
        if (kind == IdentifierKind.Ulid)
        {
            return UlidStructure(value);
        }
        if (kind == IdentifierKind.ObjectId)
        {
            return ObjectIdGenerator.Validate(value);
        }
        if (kind == IdentifierKind.SortableTimestampId)
        {
            return SortableTimestampIdGenerator.Validate(value);
        }
        if (kind == IdentifierKind.Doi)
        {
            return PublicationIdentifierGenerator.ValidateDoi(value);
        }
        if (kind == IdentifierKind.Isbn10)
        {
            return Isbn10Structure(value);
        }
        if (kind == IdentifierKind.Isbn13)
        {
            return Isbn13Structure(value);
        }
        if (kind == IdentifierKind.Issn)
        {
            return IssnStructure(value);
        }
        if (kind == IdentifierKind.Ean8)
        {
            return value.Length == 8 && Digits(value);
        }
        if (kind == IdentifierKind.UpcA)
        {
            return value.Length == 12 && Digits(value);
        }
        if (kind == IdentifierKind.Gtin)
        {
            return Gs1TradeItemIdentifier.CompleteLength(value.Length) && Digits(value);
        }
        if (kind == IdentifierKind.Imei)
        {
            return value.Length == 15 && Digits(value);
        }
        if (kind == IdentifierKind.ImeiSv)
        {
            return ImeiGenerator.ValidateImeiSv(value, maximumCount);
        }
        if (kind == IdentifierKind.Vin)
        {
            return VinStructure(value);
        }
        return value.Length > 0 && WellFormed(value);
    }

    static ValidityMode Checksum(
        ReadOnlySpan<char> value,
        IdentifierKind kind,
        int maximumCount)
    {
        if (!ChecksumApplies(kind))
        {
            return ValidityMode.NotApplicable;
        }
        if (!ChecksumSyntax(value, kind))
        {
            return ValidityMode.Unknown;
        }
        bool valid;
        if (kind == IdentifierKind.Isbn10)
        {
            valid = StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.Isbn10, value, maximumCount);
        }
        else if (kind == IdentifierKind.Isbn13)
        {
            valid = StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.Gtin, value, maximumCount);
        }
        else if (kind == IdentifierKind.Issn)
        {
            valid = StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.Issn, value, maximumCount);
        }
        else if (kind == IdentifierKind.Ean8)
        {
            valid = StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.Ean8, value, maximumCount);
        }
        else if (kind == IdentifierKind.UpcA)
        {
            valid = StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.UpcA, value, maximumCount);
        }
        else if (kind == IdentifierKind.Gtin)
        {
            valid = StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.Gtin, value, maximumCount);
        }
        else if (kind == IdentifierKind.Imei)
        {
            valid = ChecksumAlgorithms.IsLuhn(value, maximumCount);
        }
        else
        {
            valid = VinAlgorithms.Validate(value, maximumCount);
        }
        return valid ? ValidityMode.Valid : ValidityMode.Invalid;
    }

    static ValidityMode Constraint(
        ReadOnlySpan<char> value,
        IdentifierKind kind)
    {
        if (kind == IdentifierKind.Uuid4)
        {
            if (!UuidGenerator.Validate(value))
            {
                return ValidityMode.Unknown;
            }
            return UuidGenerator.ValidateVersion4(value) ? ValidityMode.Valid : ValidityMode.Invalid;
        }
        if (kind == IdentifierKind.Uuid7)
        {
            if (!UuidGenerator.Validate(value))
            {
                return ValidityMode.Unknown;
            }
            return UuidGenerator.ValidateVersion7(value) ? ValidityMode.Valid : ValidityMode.Invalid;
        }
        if (kind == IdentifierKind.Ulid)
        {
            if (!UlidStructure(value))
            {
                return ValidityMode.Unknown;
            }
            return UlidGenerator.Validate(value) ? ValidityMode.Valid : ValidityMode.Invalid;
        }
        return ValidityMode.NotApplicable;
    }

    static bool ChecksumSyntax(
        ReadOnlySpan<char> value,
        IdentifierKind kind)
    {
        if (kind == IdentifierKind.Isbn10)
        {
            return Isbn10Structure(value);
        }
        if (kind == IdentifierKind.Isbn13)
        {
            return value.Length == 13 && Digits(value);
        }
        if (kind == IdentifierKind.Issn)
        {
            return IssnStructure(value);
        }
        if (kind == IdentifierKind.Ean8)
        {
            return value.Length == 8 && Digits(value);
        }
        if (kind == IdentifierKind.UpcA)
        {
            return value.Length == 12 && Digits(value);
        }
        if (kind == IdentifierKind.Gtin)
        {
            return Gs1TradeItemIdentifier.CompleteLength(value.Length) && Digits(value);
        }
        if (kind == IdentifierKind.Imei)
        {
            return value.Length == 15 && Digits(value);
        }
        return VinStructure(value);
    }

    static bool ChecksumApplies(IdentifierKind kind)
    {
        return kind == IdentifierKind.Isbn10 ||
            kind == IdentifierKind.Isbn13 ||
            kind == IdentifierKind.Issn ||
            kind == IdentifierKind.Ean8 ||
            kind == IdentifierKind.UpcA ||
            kind == IdentifierKind.Gtin ||
            kind == IdentifierKind.Imei ||
            kind == IdentifierKind.Vin;
    }

    static AllocationStatus Allocation(IdentifierKind kind)
    {
        if (kind == IdentifierKind.Sku ||
            kind == IdentifierKind.Serial ||
            kind == IdentifierKind.Lot ||
            kind == IdentifierKind.Order ||
            kind == IdentifierKind.Invoice ||
            kind == IdentifierKind.Tracking)
        {
            return AllocationStatus.FictionalReserved;
        }
        if (kind == IdentifierKind.Uuid ||
            kind == IdentifierKind.Uuid4 ||
            kind == IdentifierKind.Uuid7 ||
            kind == IdentifierKind.Ulid ||
            kind == IdentifierKind.ObjectId ||
            kind == IdentifierKind.SortableTimestampId)
        {
            return AllocationStatus.NotApplicable;
        }
        return AllocationStatus.Unknown;
    }

    static bool Isbn10Structure(ReadOnlySpan<char> value)
    {
        if (value.Length != 10 || !Digits(value.Slice(0, 9)))
        {
            return false;
        }
        char check = value[9];
        return check >= '0' && check <= '9' || check == 'X';
    }

    static bool Isbn13Structure(ReadOnlySpan<char> value)
    {
        if (value.Length != 13 || !Digits(value))
        {
            return false;
        }
        return value[0] == '9' && value[1] == '7' &&
            (value[2] == '8' || value[2] == '9');
    }

    static bool IssnStructure(ReadOnlySpan<char> value)
    {
        if (value.Length != 8 || !Digits(value.Slice(0, 7)))
        {
            return false;
        }
        char check = value[7];
        return check >= '0' && check <= '9' || check == 'X';
    }

    static bool VinStructure(ReadOnlySpan<char> value)
    {
        if (value.Length != VinAlgorithms.Length)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (VinAlgorithms.Alphabet.IndexOf(value[index]) < 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static bool UlidStructure(ReadOnlySpan<char> value)
    {
        if (value.Length != UlidGenerator.TextLength)
        {
            return false;
        }
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        int index = 0;
        while (index < value.Length)
        {
            char character = char.ToUpperInvariant(value[index]);
            if (alphabet.IndexOf(character) < 0)
            {
                return false;
            }
            index++;
        }
        return true;
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
            index++;
        }
        return true;
    }

    static bool WellFormed(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }
                index += 2;
                continue;
            }
            if (char.IsLowSurrogate(character))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static string Detail(ValidationResult result)
    {
        if (result.StructuralValidity == ValidityMode.Invalid)
        {
            return "Identifier structure is invalid.";
        }
        if (result.ChecksumValidity == ValidityMode.Unknown)
        {
            return "Identifier checksum could not be evaluated.";
        }
        if (result.ChecksumValidity == ValidityMode.Invalid)
        {
            return "Identifier checksum is invalid.";
        }
        if (result.ConstraintValidity == ValidityMode.Invalid)
        {
            return "Identifier kind-specific constraint is invalid.";
        }
        return "Identifier is valid for the requested structural contract.";
    }

    static void ValidateKind(IdentifierKind kind)
    {
        switch (kind)
        {
            case IdentifierKind.Uuid:
            case IdentifierKind.Uuid4:
            case IdentifierKind.Uuid7:
            case IdentifierKind.Ulid:
            case IdentifierKind.ObjectId:
            case IdentifierKind.SortableTimestampId:
            case IdentifierKind.Doi:
            case IdentifierKind.Isbn10:
            case IdentifierKind.Isbn13:
            case IdentifierKind.Issn:
            case IdentifierKind.Ean8:
            case IdentifierKind.UpcA:
            case IdentifierKind.Gtin:
            case IdentifierKind.Imei:
            case IdentifierKind.ImeiSv:
            case IdentifierKind.Vin:
            case IdentifierKind.Sku:
            case IdentifierKind.Serial:
            case IdentifierKind.Lot:
            case IdentifierKind.Order:
            case IdentifierKind.Invoice:
            case IdentifierKind.Tracking:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported identifier contract.");
        }
    }
}
