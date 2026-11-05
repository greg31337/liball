using System.Numerics;

namespace cbor;

internal class CborBigNumberTags : ICborTagHandler
{
    internal CborBigNumberTags()
    {
    }

    public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
    {
        if (tagNumber == CborTagNumbers.PositiveBignum
            || tagNumber == CborTagNumbers.NegativeBignum)
        {
            if (content.Kind != CborValueKind.ByteString
                || content.ByteStringValue == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BIGNUM_TYPE",
                    "A bignum tag requires a byte string."
                );
            }
            return CborBuiltInTags.Valid(null);
        }
        if (tagNumber == CborTagNumbers.DecimalFraction
            || tagNumber == CborTagNumbers.Bigfloat)
        {
            if (!IsIntegerPair(content))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BIG_NUMBER_PAIR_TYPE",
                    "A decimal fraction or bigfloat requires a two-integer array."
                );
            }
            return CborBuiltInTags.Valid(null);
        }
        return CborBuiltInTags.Unhandled();
    }

    public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
    {
        CborTagResult validation = ValidateTag(tagNumber, content);
        if (!validation.Handled || !validation.IsValid)
        {
            return validation;
        }
        if (tagNumber == CborTagNumbers.PositiveBignum
            || tagNumber == CborTagNumbers.NegativeBignum)
        {
            byte[] bytes = content.ByteStringValue ?? [];
            BigInteger magnitude = new BigInteger(bytes, true, true);
            if (tagNumber == CborTagNumbers.NegativeBignum)
            {
                magnitude = -BigInteger.One - magnitude;
            }
            return CborBuiltInTags.Valid(magnitude);
        }
        if (content.ArrayItems == null)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_BIG_NUMBER_PAIR_TYPE",
                "The exponent and mantissa array is missing."
            );
        }
        CborTagResult mantissa = ReadInteger(content.ArrayItems[1]);
        if (!mantissa.IsValid || mantissa.NativeValue == null)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_BIG_NUMBER_PAIR_TYPE",
                "The mantissa is not an integer or bignum."
            );
        }
        BigInteger[] pair =
        [
            content.ArrayItems[0].IntegerValue,
            (BigInteger)mantissa.NativeValue
        ];
        return CborBuiltInTags.Valid(pair);
    }

    public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
    {
        if (tagNumber == CborTagNumbers.PositiveBignum
            || tagNumber == CborTagNumbers.NegativeBignum)
        {
            if (!CborBuiltInTags.IsRuntimeType(
                    nativeValue,
                    "System.Numerics.BigInteger"))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BIGNUM_NATIVE_TYPE",
                    "Bignum tags require a BigInteger native value."
                );
            }
            BigInteger integer = (BigInteger)nativeValue;
            BigInteger magnitude = integer;
            if (tagNumber == CborTagNumbers.PositiveBignum)
            {
                if (integer < BigInteger.Zero)
                {
                    return CborBuiltInTags.Invalid(
                        "CBOR_POSITIVE_BIGNUM_SIGN",
                        "A positive bignum cannot encode a negative integer."
                    );
                }
            }
            else
            {
                if (integer >= BigInteger.Zero)
                {
                    return CborBuiltInTags.Invalid(
                        "CBOR_NEGATIVE_BIGNUM_SIGN",
                        "A negative bignum must encode a negative integer."
                    );
                }
                magnitude = -BigInteger.One - integer;
            }
            CborValue bytes = new CborValue();
            bytes.Kind = CborValueKind.ByteString;
            bytes.ByteStringValue = magnitude.ToByteArray(true, true);
            return CborBuiltInTags.Encoded(bytes);
        }
        if (tagNumber == CborTagNumbers.DecimalFraction
            || tagNumber == CborTagNumbers.Bigfloat)
        {
            BigInteger[]? pair = nativeValue as BigInteger[];
            if (pair == null || pair.Length != 2)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BIG_NUMBER_PAIR_NATIVE_TYPE",
                    "Decimal fractions and bigfloats require a two-element BigInteger array."
                );
            }
            if (!IsBasicIntegerValue(pair[0]))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BIG_NUMBER_PAIR_NATIVE_TYPE",
                    "The exponent must fit the basic CBOR integer range."
                );
            }
            CborValue array = new CborValue();
            array.Kind = CborValueKind.Array;
            array.ArrayItems = [
                CreateInteger(pair[0]),
                CreateMantissa(pair[1])
            ];
            return CborBuiltInTags.Encoded(array);
        }
        return CborBuiltInTags.Unhandled();
    }

    internal bool IsBasicIntegerValue(BigInteger value)
    {
        BigInteger maximum = new BigInteger(ulong.MaxValue);
        BigInteger minimum = -BigInteger.One - maximum;
        return value >= minimum && value <= maximum;
    }

    internal CborValue CreateMantissa(BigInteger value)
    {
        if (IsBasicIntegerValue(value))
        {
            return CreateInteger(value);
        }
        bool negative = value < BigInteger.Zero;
        BigInteger magnitude = negative
            ? -BigInteger.One - value
            : value;
        CborValue bytes = new CborValue();
        bytes.Kind = CborValueKind.ByteString;
        bytes.ByteStringValue = magnitude.ToByteArray(true, true);
        CborValue tag = new CborValue();
        tag.Kind = CborValueKind.Tag;
        tag.TagNumber = negative
            ? CborTagNumbers.NegativeBignum
            : CborTagNumbers.PositiveBignum;
        tag.TaggedValue = bytes;
        return tag;
    }

    internal bool IsIntegerPair(CborValue content)
    {
        if (content.Kind != CborValueKind.Array
            || content.ArrayItems == null
            || content.ArrayItems.Count != 2)
        {
            return false;
        }
        return IsInteger(content.ArrayItems[0])
            && ReadInteger(content.ArrayItems[1]).IsValid;
    }

    internal bool IsInteger(CborValue value)
    {
        return value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger;
    }

    internal CborValue CreateInteger(BigInteger value)
    {
        CborValue integer = new CborValue();
        integer.Kind = value < BigInteger.Zero
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        integer.IntegerValue = value;
        return integer;
    }

    internal CborTagResult ReadInteger(CborValue value)
    {
        if (IsInteger(value))
        {
            return CborBuiltInTags.Valid(value.IntegerValue);
        }
        if (value.Kind != CborValueKind.Tag
            || value.TaggedValue == null
            || (value.TagNumber != CborTagNumbers.PositiveBignum
                && value.TagNumber != CborTagNumbers.NegativeBignum))
        {
            return CborBuiltInTags.Invalid(
                "CBOR_BIG_NUMBER_PAIR_TYPE",
                "The mantissa is not an integer or bignum."
            );
        }
        CborTagResult decoded = DecodeTag(value.TagNumber, value.TaggedValue);
        if (!decoded.IsValid
            || !CborBuiltInTags.IsRuntimeType(
                decoded.NativeValue,
                "System.Numerics.BigInteger"))
        {
            return CborBuiltInTags.Invalid(
                "CBOR_BIG_NUMBER_PAIR_TYPE",
                "The mantissa bignum is invalid."
            );
        }
        return decoded;
    }
}
