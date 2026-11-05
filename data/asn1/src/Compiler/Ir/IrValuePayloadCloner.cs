namespace asn1;

internal class IrValuePayloadCloner
{
    internal AsnBitStringValue? CloneBitString(AsnBitStringValue? value)
    {
        if (value == null)
        {
            return null;
        }

        return new AsnBitStringValue
        {
            Bytes = value.Bytes == null ? [] : (byte[])value.Bytes.Clone(),
            BitLength = value.BitLength
        };
    }

    internal AsnObjectIdentifierValue? CloneObjectIdentifier(
        AsnObjectIdentifierValue? value)
    {
        if (value == null)
        {
            return null;
        }

        return new AsnObjectIdentifierValue
        {
            Relative = value.Relative,
            Arcs = value.Arcs == null
                ? []
                : (System.Numerics.BigInteger[])value.Arcs.Clone()
        };
    }

    internal AsnRealValue? CloneReal(AsnRealValue? value)
    {
        if (value == null)
        {
            return null;
        }

        return new AsnRealValue
        {
            Normalized = value.Normalized,
            Kind = value.Kind,
            HasDecimalValue = value.HasDecimalValue,
            DecimalText = value.DecimalText,
            DecimalCoefficient = value.DecimalCoefficient,
            DecimalExponent = value.DecimalExponent,
            HasBinaryValue = value.HasBinaryValue,
            Mantissa = value.Mantissa,
            Radix = value.Radix,
            Exponent = value.Exponent
        };
    }

    internal AsnTimeValue? CloneTime(AsnTimeValue? value)
    {
        if (value == null)
        {
            return null;
        }

        return new AsnTimeValue
        {
            Kind = value.Kind,
            Text = value.Text,
            HasUtcOffsetMinutes = value.HasUtcOffsetMinutes,
            UtcOffsetMinutes = value.UtcOffsetMinutes,
            HasFractionalDigits = value.HasFractionalDigits,
            FractionalDigits = value.FractionalDigits
        };
    }
}
