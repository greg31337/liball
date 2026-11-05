using System;
namespace token;

/// <summary>Implements public COSE signing and RFC 9338 countersignature operations.</summary>
public class CoseSigningService : ICoseSigningOperations
{
    /// <summary>Initializes a stateless COSE signing service.</summary>
    public CoseSigningService()
    {
    }

    /// <summary>Reads COSE_Sign1 metadata while withholding an unauthenticated attached payload.</summary>
    public CoseSign1Result ReadCoseSign1(byte[] cbor, TokenPolicy policy)
    {
        CoseSign1Result result = CoseSign1.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.Payload = null;
            result.Message.PayloadAvailable = false;
        }

        return result;
    }

    /// <summary>Reads an unauthenticated COSE_Sign1 payload when diagnostics are enabled.</summary>
    public CoseSign1Result ReadCoseSign1Diagnostic(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseSign1.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return CoseSign1.Failure(policyResult.Error);
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            return CoseSign1.Failure(new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic COSE_Sign1 payload decoding requires explicit policy opt-in.",
                -1));
        }

        CoseSign1Result result = CoseSign1.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Writes a COSE_Sign1 model.</summary>
    public TokenBytesResult WriteCoseSign1(CoseSign1 message, bool tagged, TokenPolicy policy)
    {
        return CoseSign1.Write(message, tagged, policy);
    }

    /// <summary>Creates an attached COSE_Sign1 message.</summary>
    public TokenBytesResult CreateCoseSign1(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseSign1Operations.CreateWithExternalAuthenticatedData(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates a COSE_Sign1 message with a detached payload.</summary>
    public TokenBytesResult CreateDetachedCoseSign1(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseSign1Operations.CreateDetached(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies an attached COSE_Sign1 message.</summary>
    public CoseSign1Result VerifyCoseSign1(byte[] cbor, byte[] externalAuthenticatedData, TokenKey key, bool allowCompatibility, TokenPolicy policy)
    {
        CoseSign1Result result =
            CoseSign1Operations.VerifyWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                key,
                allowCompatibility,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Verifies a COSE_Sign1 message against a detached payload.</summary>
    public CoseSign1Result VerifyDetachedCoseSign1(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey key, bool allowCompatibility, TokenPolicy policy)
    {
        CoseSign1Result result = CoseSign1Operations.VerifyDetached(
            cbor,
            payload,
            externalAuthenticatedData,
            key,
            allowCompatibility,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Reads COSE_Sign metadata while withholding an unauthenticated attached payload.</summary>
    public CoseSignResult ReadCoseSign(byte[] cbor, TokenPolicy policy)
    {
        CoseSignResult result = CoseSign.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.Payload = null;
            result.Message.PayloadAvailable = false;
        }

        return result;
    }

    /// <summary>Reads an unauthenticated COSE_Sign payload when diagnostics are enabled.</summary>
    public CoseSignResult ReadCoseSignDiagnostic(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseSign.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return CoseSign.Failure(policyResult.Error);
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            return CoseSign.Failure(new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic COSE_Sign payload decoding requires explicit policy opt-in.",
                -1));
        }

        CoseSignResult result = CoseSign.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Writes a COSE_Sign model.</summary>
    public TokenBytesResult WriteCoseSign(CoseSign message, bool tagged, TokenPolicy policy)
    {
        return CoseSign.Write(message, tagged, policy);
    }

    /// <summary>Creates an attached COSE_Sign message.</summary>
    public TokenBytesResult CreateCoseSign(byte[] payload, byte[] externalAuthenticatedData, CoseHeader bodyHeader, CoseHeader[] signerHeaders, TokenKey[] keys, bool tagged, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseSignOperations.CreateWithExternalAuthenticatedData(
            payload,
            externalAuthenticatedData,
            bodyHeader,
            signerHeaders,
            keys,
            tagged,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates a COSE_Sign message with a detached payload.</summary>
    public TokenBytesResult CreateDetachedCoseSign(byte[] payload, byte[] externalAuthenticatedData, CoseHeader bodyHeader, CoseHeader[] signerHeaders, TokenKey[] keys, bool tagged, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseSignOperations.CreateDetached(
            payload,
            externalAuthenticatedData,
            bodyHeader,
            signerHeaders,
            keys,
            tagged,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies every signature in an attached COSE_Sign message.</summary>
    public CoseSignResult VerifyCoseSign(byte[] cbor, byte[] externalAuthenticatedData, TokenKey[] keys, bool allowCompatibility, TokenPolicy policy)
    {
        CoseSignResult result =
            CoseSignOperations.VerifyWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                keys,
                allowCompatibility,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Verifies every signature in a detached COSE_Sign message.</summary>
    public CoseSignResult VerifyDetachedCoseSign(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey[] keys, bool allowCompatibility, TokenPolicy policy)
    {
        CoseSignResult result = CoseSignOperations.VerifyDetached(
            cbor,
            payload,
            externalAuthenticatedData,
            keys,
            allowCompatibility,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Reads a standalone full version 2 countersignature.</summary>
    public CoseCounterSignatureResult ReadCoseCounterSignature(byte[] cbor, TokenPolicy policy)
    {
        return CoseCounterSignature.Read(cbor, policy);
    }

    /// <summary>Writes a standalone full version 2 countersignature.</summary>
    public TokenBytesResult WriteCoseCounterSignature(CoseCounterSignature counterSignature, bool tagged, TokenPolicy policy)
    {
        return CoseCounterSignature.Write(
            counterSignature,
            tagged,
            policy);
    }

    /// <summary>Reads full version 2 countersignatures from a COSE header.</summary>
    public CoseCounterSignatureCollectionResult ReadCoseCounterSignaturesFromHeader(CoseHeader header, TokenPolicy policy)
    {
        return CoseCounterSignature.ReadHeader(header, policy);
    }

    /// <summary>Stores full version 2 countersignatures in a COSE header.</summary>
    public TokenResult SetCoseCounterSignaturesOnHeader(CoseHeader header, CoseCounterSignature[] counterSignatures, TokenPolicy policy)
    {
        return CoseCounterSignature.SetHeader(
            header,
            counterSignatures,
            policy);
    }

    /// <summary>Creates a full version 2 countersignature.</summary>
    public CoseCounterSignatureResult CreateCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseCounterSignatureOperations.Create(
            target,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies a full version 2 countersignature.</summary>
    public TokenResult VerifyCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, CoseCounterSignature counterSignature, TokenKey key, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseCounterSignatureOperations.Verify(
            target,
            externalAuthenticatedData,
            counterSignature,
            key,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates an abbreviated version 2 countersignature.</summary>
    public TokenBytesResult CreateAbbreviatedCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, int algorithm, TokenKey key, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseCounterSignatureOperations.CreateAbbreviated(
            target,
            externalAuthenticatedData,
            algorithm,
            key,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies an abbreviated version 2 countersignature.</summary>
    public TokenResult VerifyAbbreviatedCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, byte[] signature, int algorithm, TokenKey key, bool allowCompatibility, TokenPolicy policy)
    {
        return CoseCounterSignatureOperations.VerifyAbbreviated(
            target,
            externalAuthenticatedData,
            signature,
            algorithm,
            key,
            allowCompatibility,
            policy);
    }

    /// <summary>Stores an abbreviated version 2 countersignature in a COSE header.</summary>
    public TokenResult SetAbbreviatedCoseCounterSignatureOnHeader(CoseHeader header, byte[] signature)
    {
        CoseCounterSignatureOperations.SetAbbreviatedHeader(
            header,
            signature);
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    /// <summary>Reads an abbreviated version 2 countersignature from a COSE header.</summary>
    public TokenBytesResult ReadAbbreviatedCoseCounterSignatureFromHeader(CoseHeader header)
    {
        return CoseCounterSignatureOperations.ReadAbbreviatedHeader(
            header);
    }
}
