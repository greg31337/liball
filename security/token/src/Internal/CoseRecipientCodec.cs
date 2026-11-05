using System;
using System.Collections.Generic;

namespace token;

/// <summary>Reads and writes standalone COSE recipient structures.</summary>
internal static class CoseRecipientCodec
{
    internal static CoseRecipientResult Read(
        byte[] cbor,
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

        TokenResult policyResult = CoseEncrypt.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
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
            return FailureWithMessage(
                "trailing_cose_recipient_data",
                "A COSE recipient must contain exactly one CBOR item.");
        }

        return CoseEncrypt.ReadRecipient(
            (decoded.Item ?? throw new InvalidOperationException("A required value is null.")),
            policy,
            0);
    }

    internal static TokenBytesResult Write(
        CoseRecipient recipient,
        TokenPolicy policy)
    {
        if (recipient == null)
        {
            throw new ArgumentNullException("recipient");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseEncrypt.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        HashSet<CoseRecipient> ancestors =
            new HashSet<CoseRecipient>();
        TokenResult validation = CoseEncrypt.ValidateRecipient(
            recipient,
            ancestors);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        CoseRecipientEncoding encoding = CoseEncrypt.EncodeRecipient(
            recipient,
            policy);
        if (!encoding.Success)
        {
            return BytesFailure(encoding.Error);
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(
            (encoding.Item ?? throw new InvalidOperationException("A required value is null.")));
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        CoseEncrypt.CommitRecipient(recipient, encoding);
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    static CoseRecipientResult FailureWithMessage(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseRecipientResult Failure(TokenError? error)
    {
        CoseRecipientResult result = new CoseRecipientResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
