using System;
namespace token;

/// <summary>Detects RFC 9052 tagged and contextually identified untagged COSE messages.</summary>
internal static class CoseMessage
{
    /// <summary>Reads one tagged COSE message using default limits.</summary>
    /// <remarks>Untagged messages require an expected kind because their structures are not all distinguishable.</remarks>
    /// <param name="cbor">The encoded COSE message.</param>
    /// <returns>The detected message kind and decoded array, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseMessageResult Read(byte[] cbor)
    {
        return Read(cbor, CoseMessageKind.Unknown, new TokenPolicy());
    }

    /// <summary>Reads one tagged COSE message using explicit limits.</summary>
    /// <remarks>Untagged messages require an expected kind because their structures are not all distinguishable.</remarks>
    /// <param name="cbor">The encoded COSE message.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The detected message kind and decoded array, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseMessageResult Read(byte[] cbor, TokenPolicy policy)
    {
        return Read(cbor, CoseMessageKind.Unknown, policy);
    }

    /// <summary>Reads one tagged or untagged COSE message using an expected kind and default limits.</summary>
    /// <param name="cbor">The encoded COSE message.</param>
    /// <param name="expectedKind">The message kind supplied by application context, or <see cref="CoseMessageKind.Unknown"/> to require a tag.</param>
    /// <returns>The detected message kind and decoded array, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>expectedKind</c> is not defined.</exception>
    internal static CoseMessageResult Read(
        byte[] cbor,
        CoseMessageKind expectedKind)
    {
        return Read(cbor, expectedKind, new TokenPolicy());
    }

    /// <summary>Reads one tagged or untagged COSE message using an expected kind and explicit limits.</summary>
    /// <param name="cbor">The encoded COSE message.</param>
    /// <param name="expectedKind">The message kind supplied by application context, or <see cref="CoseMessageKind.Unknown"/> to require a tag.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The detected message kind and decoded array, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>expectedKind</c> is not defined.</exception>
    internal static CoseMessageResult Read(
        byte[] cbor,
        CoseMessageKind expectedKind,
        TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (!IsDefined(expectedKind))
        {
            throw new ArgumentOutOfRangeException("expectedKind");
        }

        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return Failure(
                "invalid_policy",
                "The COSE message policy contains an invalid size or depth limit.",
                -1);
        }

        CborReader reader = CborReader.Create(
            cbor,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        if (reader.GetRemainingLength() != 0)
        {
            return Failure(
                "trailing_cose_message_data",
                "A COSE message must contain exactly one CBOR item.",
                reader.GetPosition());
        }

        CborItem? item = decoded.Item;
        bool tagged = (item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.Tag;
        ulong tag = 0;
        CoseMessageKind kind = expectedKind;
        if (tagged)
        {
            tag = item.TagValue;
            kind = KindFromTag(tag);
            if (kind == CoseMessageKind.Unknown)
            {
                return Failure(
                    "unknown_cose_message_tag",
                    "The CBOR tag does not identify an RFC 9052 COSE message.",
                    0);
            }

            if (expectedKind != CoseMessageKind.Unknown
                && expectedKind != kind)
            {
                return Failure(
                    "unexpected_cose_message_type",
                    "The tagged COSE message does not match the expected kind.",
                    0);
            }

            item = item.TaggedValue;
        }
        else if (kind == CoseMessageKind.Unknown)
        {
            return Failure(
                "untagged_cose_message_type_required",
                "An untagged COSE message requires its kind from application context.",
                0);
        }

        TokenResult structure = ValidateStructure(item, kind);
        if (!structure.Success)
        {
            return Failure(structure.Error);
        }

        CoseMessageResult result = new CoseMessageResult();
        result.Success = true;
        result.Kind = kind;
        result.Tagged = tagged;
        result.Tag = tag;
        result.Item = item;
        return result;
    }

    static TokenResult ValidateStructure(
        CborItem? item,
        CoseMessageKind kind)
    {
        if (item == null
            || item.Kind != TokenCborValueKind.Array
            || item.ArrayValue == null)
        {
            return ValidationFailure(
                "cose_message_not_array",
                "A COSE message must be encoded as a CBOR array.",
                0);
        }

        int expectedLength = ExpectedLength(kind);
        if (item.ArrayValue.Length != expectedLength)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The COSE message array has the wrong number of fields.",
                item.ArrayValue.Length);
        }

        CborItem protectedBucket = item.ArrayValue[0];
        CborItem unprotectedBucket = item.ArrayValue[1];
        CborItem content = item.ArrayValue[2];
        if (protectedBucket == null
            || protectedBucket.Kind != TokenCborValueKind.ByteString
            || protectedBucket.Bytes == null)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The protected COSE header must be a byte string.",
                0);
        }

        if (unprotectedBucket == null
            || unprotectedBucket.Kind != TokenCborValueKind.Map
            || unprotectedBucket.MapValue == null)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The unprotected COSE header must be a map.",
                1);
        }

        if (content == null
            || content.Kind != TokenCborValueKind.ByteString
                && content.Kind != TokenCborValueKind.Null)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The COSE message content must be a byte string or null.",
                2);
        }

        if (content.Kind == TokenCborValueKind.ByteString
            && content.Bytes == null)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The COSE message content byte string is missing.",
                2);
        }

        if (kind == CoseMessageKind.Encrypt0)
        {
            return ValidationSuccess();
        }

        CborItem fourth = item.ArrayValue[3];
        if (kind == CoseMessageKind.Sign1
            || kind == CoseMessageKind.Mac0)
        {
            return ValidateByteString(fourth, 3);
        }

        if (kind == CoseMessageKind.Sign
            || kind == CoseMessageKind.Encrypt)
        {
            return ValidateNonemptyArray(fourth, 3);
        }

        TokenResult tagResult = ValidateByteString(fourth, 3);
        if (!tagResult.Success)
        {
            return tagResult;
        }

        return ValidateNonemptyArray(item.ArrayValue[4], 4);
    }

    static TokenResult ValidateByteString(CborItem item, int position)
    {
        if (item == null
            || item.Kind != TokenCborValueKind.ByteString
            || item.Bytes == null)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The COSE signature or MAC value must be a byte string.",
                position);
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateNonemptyArray(CborItem item, int position)
    {
        if (item == null
            || item.Kind != TokenCborValueKind.Array
            || item.ArrayValue == null
            || item.ArrayValue.Length == 0)
        {
            return ValidationFailure(
                "invalid_cose_message_structure",
                "The COSE signatures or recipients field must be a nonempty array.",
                position);
        }

        return ValidationSuccess();
    }

    static int ExpectedLength(CoseMessageKind kind)
    {
        if (kind == CoseMessageKind.Encrypt0)
        {
            return 3;
        }

        if (kind == CoseMessageKind.Mac)
        {
            return 5;
        }

        return 4;
    }

    static CoseMessageKind KindFromTag(ulong tag)
    {
        if (tag == 16)
        {
            return CoseMessageKind.Encrypt0;
        }

        if (tag == 17)
        {
            return CoseMessageKind.Mac0;
        }

        if (tag == 18)
        {
            return CoseMessageKind.Sign1;
        }

        if (tag == 96)
        {
            return CoseMessageKind.Encrypt;
        }

        if (tag == 97)
        {
            return CoseMessageKind.Mac;
        }

        if (tag == 98)
        {
            return CoseMessageKind.Sign;
        }

        return CoseMessageKind.Unknown;
    }

    static bool IsDefined(CoseMessageKind kind)
    {
        return kind == CoseMessageKind.Unknown
            || kind == CoseMessageKind.Encrypt0
            || kind == CoseMessageKind.Mac0
            || kind == CoseMessageKind.Sign1
            || kind == CoseMessageKind.Encrypt
            || kind == CoseMessageKind.Mac
            || kind == CoseMessageKind.Sign;
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(
        string code,
        string message,
        int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }

    static CoseMessageResult Failure(
        string code,
        string message,
        int position)
    {
        return Failure(new TokenError(code, message, position));
    }

    static CoseMessageResult Failure(TokenError? error)
    {
        CoseMessageResult result = new CoseMessageResult();
        result.Error = error;
        return result;
    }
}
