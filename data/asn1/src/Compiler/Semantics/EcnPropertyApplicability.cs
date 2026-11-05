namespace asn1;

internal static class EcnPropertyApplicability
{
    internal static bool IsAllowed(
        EcnEncodingPropertyKind property,
        EcnClassCategory category)
    {
        if (category == EcnClassCategory.Unknown)
        {
            return true;
        }

        if (property == EcnEncodingPropertyKind.Representation)
        {
            return IsPrimitiveValueCategory(category);
        }

        if (property == EcnEncodingPropertyKind.Size
            || property == EcnEncodingPropertyKind.Multiple)
        {
            return IsSizedCategory(category);
        }

        if (property == EcnEncodingPropertyKind.Range)
        {
            return category == EcnClassCategory.Integer
                || category == EcnClassCategory.Real
                || category == EcnClassCategory.Time;
        }

        if (property == EcnEncodingPropertyKind.Alphabet)
        {
            return category == EcnClassCategory.CharacterString;
        }

        if (property == EcnEncodingPropertyKind.ByteOrder
            || property == EcnEncodingPropertyKind.WordOrder)
        {
            return category == EcnClassCategory.Integer
                || category == EcnClassCategory.Real
                || category == EcnClassCategory.BitString
                || category == EcnClassCategory.OctetString
                || category == EcnClassCategory.CharacterString
                || category == EcnClassCategory.ObjectIdentifier
                || category == EcnClassCategory.Time;
        }

        if (property == EcnEncodingPropertyKind.BitOrder
            || property == EcnEncodingPropertyKind.BitReversal)
        {
            return category == EcnClassCategory.Integer
                || category == EcnClassCategory.BitString
                || category == EcnClassCategory.OctetString
                || category == EcnClassCategory.CharacterString
                || category == EcnClassCategory.EncodingStructure
                || category == EcnClassCategory.Concatenation
                || category == EcnClassCategory.Repetition;
        }

        if (property == EcnEncodingPropertyKind.Signedness)
        {
            return category == EcnClassCategory.Integer;
        }

        if (property == EcnEncodingPropertyKind.Termination)
        {
            return category == EcnClassCategory.BitString
                || category == EcnClassCategory.OctetString
                || category == EcnClassCategory.CharacterString
                || category == EcnClassCategory.Repetition;
        }

        if (property == EcnEncodingPropertyKind.OptionalEncoding
            || property == EcnEncodingPropertyKind.Presence)
        {
            return category == EcnClassCategory.Optionality;
        }

        if (property == EcnEncodingPropertyKind.Alternative)
        {
            return category == EcnClassCategory.Alternatives;
        }

        if (property == EcnEncodingPropertyKind.RepetitionEncoding
            || property == EcnEncodingPropertyKind.RepetitionSpace)
        {
            return category == EcnClassCategory.Repetition
                || category == EcnClassCategory.BitString
                || category == EcnClassCategory.OctetString
                || category == EcnClassCategory.CharacterString;
        }

        return property == EcnEncodingPropertyKind.Alignment
            || property == EcnEncodingPropertyKind.Padding
            || property == EcnEncodingPropertyKind.EncodingSpace
            || property == EcnEncodingPropertyKind.Pattern
            || property == EcnEncodingPropertyKind.Justification
            || property == EcnEncodingPropertyKind.ValuePadding
            || property == EcnEncodingPropertyKind.ValueReversal
            || property == EcnEncodingPropertyKind.TruePattern
            || property == EcnEncodingPropertyKind.FalsePattern
            || property == EcnEncodingPropertyKind.UnusedBits;
    }

    internal static bool IsPrimitiveValueCategory(EcnClassCategory category)
    {
        return category == EcnClassCategory.BitString
            || category == EcnClassCategory.Boolean
            || category == EcnClassCategory.CharacterString
            || category == EcnClassCategory.Integer
            || category == EcnClassCategory.Null
            || category == EcnClassCategory.ObjectIdentifier
            || category == EcnClassCategory.OctetString
            || category == EcnClassCategory.OpenType
            || category == EcnClassCategory.Padding
            || category == EcnClassCategory.Real
            || category == EcnClassCategory.Tag
            || category == EcnClassCategory.Time;
    }

    internal static bool IsSizedCategory(EcnClassCategory category)
    {
        return category != EcnClassCategory.Optionality
            && category != EcnClassCategory.EncodingProcedure;
    }
}
