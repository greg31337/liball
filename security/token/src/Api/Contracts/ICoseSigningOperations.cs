using System;
namespace token;

/// <summary>Creates, reads, writes, and verifies COSE signatures and countersignatures.</summary>
public interface ICoseSigningOperations
{
    /// <summary>Reads COSE_Sign1 metadata while withholding an unauthenticated attached payload.</summary>
    /// <param name="cbor">The tagged or contextually untagged message.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed message metadata or stable failure details.</returns>
    CoseSign1Result ReadCoseSign1(byte[] cbor, TokenPolicy policy);

    /// <summary>Reads an attached COSE_Sign1 payload when diagnostic access is enabled.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed unauthenticated message or stable failure details.</returns>
    CoseSign1Result ReadCoseSign1Diagnostic(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a COSE_Sign1 model with explicit registered-tag control.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 18.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded message or stable failure details.</returns>
    TokenBytesResult WriteCoseSign1(CoseSign1 message, bool tagged, TokenPolicy policy);

    /// <summary>Creates an attached COSE_Sign1 message.</summary>
    /// <param name="payload">The payload to sign.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The protected and unprotected headers.</param>
    /// <param name="key">The private signing key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 18.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The signed message or stable failure details.</returns>
    TokenBytesResult CreateCoseSign1(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Creates a COSE_Sign1 message with a separately transported payload.</summary>
    /// <param name="payload">The detached payload to sign.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The protected and unprotected headers.</param>
    /// <param name="key">The private signing key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 18.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The signed message with a null payload field or stable failure details.</returns>
    TokenBytesResult CreateDetachedCoseSign1(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Verifies an attached COSE_Sign1 message.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated message or stable failure details.</returns>
    CoseSign1Result VerifyCoseSign1(byte[] cbor, byte[] externalAuthenticatedData, TokenKey key, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Verifies a COSE_Sign1 message against a separately transported payload.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The detached payload.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated message with the supplied payload or stable failure details.</returns>
    CoseSign1Result VerifyDetachedCoseSign1(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey key, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Reads COSE_Sign metadata while withholding an unauthenticated attached payload.</summary>
    /// <param name="cbor">The tagged or contextually untagged message.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed message metadata or stable failure details.</returns>
    CoseSignResult ReadCoseSign(byte[] cbor, TokenPolicy policy);

    /// <summary>Reads an attached COSE_Sign payload when diagnostic access is enabled.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed unauthenticated message or stable failure details.</returns>
    CoseSignResult ReadCoseSignDiagnostic(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a COSE_Sign model with explicit registered-tag control.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 98.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded message or stable failure details.</returns>
    TokenBytesResult WriteCoseSign(CoseSign message, bool tagged, TokenPolicy policy);

    /// <summary>Creates an attached COSE_Sign message for one or more signers.</summary>
    /// <param name="payload">The shared payload to sign.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="bodyHeader">The shared message headers.</param>
    /// <param name="signerHeaders">The signer headers in signature order.</param>
    /// <param name="keys">The private signing keys in signature order.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 98.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The signed message or stable failure details.</returns>
    TokenBytesResult CreateCoseSign(byte[] payload, byte[] externalAuthenticatedData, CoseHeader bodyHeader, CoseHeader[] signerHeaders, TokenKey[] keys, bool tagged, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Creates a COSE_Sign message with a separately transported payload.</summary>
    /// <param name="payload">The detached shared payload.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="bodyHeader">The shared message headers.</param>
    /// <param name="signerHeaders">The signer headers in signature order.</param>
    /// <param name="keys">The private signing keys in signature order.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 98.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The signed message with a null payload field or stable failure details.</returns>
    TokenBytesResult CreateDetachedCoseSign(byte[] payload, byte[] externalAuthenticatedData, CoseHeader bodyHeader, CoseHeader[] signerHeaders, TokenKey[] keys, bool tagged, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Verifies every signature in an attached COSE_Sign message.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="keys">The public verification keys in signature order.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated message or stable failure details.</returns>
    CoseSignResult VerifyCoseSign(byte[] cbor, byte[] externalAuthenticatedData, TokenKey[] keys, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Verifies every signature in a detached COSE_Sign message.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The detached shared payload.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="keys">The public verification keys in signature order.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated message or stable failure details.</returns>
    CoseSignResult VerifyDetachedCoseSign(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey[] keys, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Reads a standalone full RFC 9338 version 2 countersignature.</summary>
    /// <param name="cbor">The tagged or untagged countersignature.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed countersignature or stable failure details.</returns>
    CoseCounterSignatureResult ReadCoseCounterSignature(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a standalone full RFC 9338 version 2 countersignature.</summary>
    /// <param name="counterSignature">The countersignature to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 19.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded countersignature or stable failure details.</returns>
    TokenBytesResult WriteCoseCounterSignature(CoseCounterSignature counterSignature, bool tagged, TokenPolicy policy);

    /// <summary>Reads full version 2 countersignatures from unprotected header label 11.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed countersignatures or stable failure details.</returns>
    CoseCounterSignatureCollectionResult ReadCoseCounterSignaturesFromHeader(CoseHeader header, TokenPolicy policy);

    /// <summary>Stores full version 2 countersignatures in unprotected header label 11.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <param name="counterSignatures">The countersignatures to store.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>A success result or stable failure details.</returns>
    TokenResult SetCoseCounterSignaturesOnHeader(CoseHeader header, CoseCounterSignature[] counterSignatures, TokenPolicy policy);

    /// <summary>Creates a full RFC 9338 version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The countersigner headers.</param>
    /// <param name="key">The private countersigning key.</param>
    /// <param name="tagged">Whether standalone serialization uses CBOR tag 19.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The countersignature or stable failure details.</returns>
    CoseCounterSignatureResult CreateCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Verifies a full RFC 9338 version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="counterSignature">The countersignature to verify.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>A success result only when verification succeeds.</returns>
    TokenResult VerifyCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, CoseCounterSignature counterSignature, TokenKey key, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Creates an abbreviated RFC 9338 version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="algorithm">The contextually supplied signature algorithm.</param>
    /// <param name="key">The private countersigning key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The abbreviated signature bytes or stable failure details.</returns>
    TokenBytesResult CreateAbbreviatedCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, int algorithm, TokenKey key, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Verifies an abbreviated RFC 9338 version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="signature">The abbreviated signature bytes.</param>
    /// <param name="algorithm">The contextually supplied signature algorithm.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are permitted.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>A success result only when verification succeeds.</returns>
    TokenResult VerifyAbbreviatedCoseCounterSignature(CoseCounterSignatureTarget target, byte[] externalAuthenticatedData, byte[] signature, int algorithm, TokenKey key, bool allowCompatibility, TokenPolicy policy);

    /// <summary>Stores an abbreviated version 2 countersignature in unprotected header label 12.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <param name="signature">The abbreviated signature bytes.</param>
    /// <returns>A successful result after the header is updated.</returns>
    TokenResult SetAbbreviatedCoseCounterSignatureOnHeader(CoseHeader header, byte[] signature);

    /// <summary>Reads an abbreviated version 2 countersignature from unprotected header label 12.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <returns>The signature bytes or stable failure details.</returns>
    TokenBytesResult ReadAbbreviatedCoseCounterSignatureFromHeader(CoseHeader header);
}
