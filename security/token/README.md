# token

`token` is a dependency-free .NET library for strict JOSE, JWT, COSE, and CWT
processing. It includes compact and JSON JOSE serializations, JWK/JWKS support,
deterministic CBOR, authenticated token creation and validation, and a universal
facade for applications that accept more than one token format.

The library targets .NET 10. Add the project reference while developing in this
repository:

```sh
dotnet add reference ../token/src/token.csproj
```

## Results and policies

Parsing, validation, and cryptographic operations return result objects. Check
`Success` before reading the value; failures provide a stable `Error.Code`, a
human-readable `Error.Message`, and an input `Error.Position` when applicable.

Cryptographic operations require an explicit algorithm allowlist:

```csharp
using token;

TokenPolicy policy = new TokenPolicy();
policy.AllowedAlgorithms = new[] { JoseAlgorithms.HmacSha256 };

TokenBytesResult decoded = Base64Url.Decode("not valid!");
if (!decoded.Success)
{
    Console.WriteLine($"{decoded.Error.Code} at {decoded.Error.Position}");
}
```

Keep allowlists as narrow as possible. Unsecured tokens, legacy RSA encryption,
compression, unauthenticated decoding, and remote key resolution are disabled by
default and should only be enabled for a protocol that explicitly requires them.

Validation APIs accept `ITokenKeyResolver`, and time-sensitive policies and remote
JWK Set caching accept `ITokenClock`. Implement these interfaces for stateful key
stores or application clocks. For simple cases, `TokenKeyResolver` and
`TokenClock` adapt callbacks while preserving the same contracts. A
`JsonWebKeySetResolver` adapts a `JsonWebKeySet` when its token metadata identifies
one compatible key. These abstractions also let tests use small recording resolvers
and manually advanced clocks without HTTP, timing, or key-store dependencies.

## Low-level codecs

Base64url is strict and unpadded by default. Standard Base64, hexadecimal, strict
UTF-8, PEM, big-endian integer, JSON, and deterministic CBOR helpers are also
available. Span overloads accept slices without first creating a caller-side copy.

```csharp
using System.Text;
using token;

byte[] framed = Encoding.UTF8.GetBytes("!payload!");
ReadOnlySpan<byte> payload = framed.AsSpan(1, 7);

TokenTextResult encoded = Base64Url.Encode(payload);
TokenBytesResult decoded = Base64Url.Decode(encoded.Text.AsSpan());

TokenTextResult hexadecimal = HexCodec.Encode(decoded.Bytes.AsSpan());
TokenBytesResult fromHex = HexCodec.Decode(hexadecimal.Text.AsSpan());

TokenTextResult text = Utf8Codec.Decode(fromHex.Bytes.AsSpan());
Console.WriteLine(text.Text); // payload
```

Use `Base64Url.Decode(text, true)` only when a compatibility profile permits
padding. `Base64Codec.Decode(text, true)` permits PEM-style ASCII whitespace;
strict decoding rejects it.

## JWK and JWKS

Keys are normalized as `TokenKey` instances. Import native or symmetric material,
serialize only the projection that should leave the process, and release parsed or
owned secret material when finished.

```csharp
using System.Security.Cryptography;
using token;

using RSA rsa = RSA.Create(2048);
TokenKeyResult imported = RsaKeyImporter.ImportJose(
    rsa,
    JoseAlgorithms.RsaSha256,
    true);
TokenKey privateKey = imported.Key;
privateKey.KeyId = "signing-2026-08";

// Public projection: no private RSA parameters are emitted.
TokenTextResult publicJwk = JsonWebKey.WritePublic(privateKey);
JwkResult parsedJwk = JsonWebKey.Read(publicJwk.Text);

JsonWebKeySet set = new JsonWebKeySet(new[] { parsedJwk.Key });
TokenTextResult jwksJson = set.Write();
JwksResult parsedSet = JsonWebKeySet.Read(jwksJson.Text);

Console.WriteLine(parsedSet.Set.Keys[0].KeyId);

parsedSet.Set.ReleaseOwnedMaterial();
parsedJwk.Key.ReleaseOwnedMaterial();
```

`JsonWebKey.WritePrivate` requires private or symmetric material.
`JsonWebKey.WriteMinimal` omits optional metadata, while `Thumbprint` and
`ThumbprintUri` implement RFC 7638 and RFC 9278 identifiers.

## JWS

This example creates and verifies an attached compact JWS with HMAC SHA-256.
Production secrets should come from protected key storage, not be generated at
application startup.

```csharp
using System.Security.Cryptography;
using System.Text;
using token;

TokenKey key = SymmetricKeyImporter.ImportJose(
    RandomNumberGenerator.GetBytes(32),
    JoseAlgorithms.HmacSha256).Key;

TokenPolicy policy = new TokenPolicy();
policy.AllowedAlgorithms = new[] { JoseAlgorithms.HmacSha256 };

JoseHeader header = JoseHeader.Read("{\"alg\":\"HS256\"}").Header;
TokenTextResult signed = JsonWebSignature.Sign(
    Encoding.UTF8.GetBytes("authenticated payload"),
    header,
    key,
    policy);

JwsCompactResult verified = JsonWebSignature.Verify(
    signed.Text,
    key,
    policy);

if (!verified.Success || !verified.Token.SignatureVerified)
{
    throw new InvalidOperationException(verified.Error.Message);
}

Console.WriteLine(Encoding.UTF8.GetString(verified.Token.Payload));
key.ReleaseOwnedMaterial();
```

Detached payloads use `SignDetached` and `VerifyDetached`. RFC 7797 unencoded
payloads require a protected `b64` value and a matching `crit` declaration.

## JWE

Direct encryption uses the shared key as the content-encryption key. The
key-management and content-encryption algorithms must both be allowed.

```csharp
using System.Security.Cryptography;
using System.Text;
using token;

TokenKey key = SymmetricKeyImporter.ImportJose(
    RandomNumberGenerator.GetBytes(32),
    JoseAlgorithms.Direct).Key;

TokenPolicy policy = new TokenPolicy();
policy.AllowedAlgorithms = new[]
{
    JoseAlgorithms.Direct,
    JoseAlgorithms.Aes256Gcm
};

JoseHeader header = JoseHeader.Read(
    "{\"alg\":\"dir\",\"enc\":\"A256GCM\"}").Header;
TokenTextResult encrypted = JsonWebEncryption.Encrypt(
    Encoding.UTF8.GetBytes("confidential payload"),
    header,
    key,
    policy);

JweCompactResult decrypted = JsonWebEncryption.Decrypt(
    encrypted.Text,
    key,
    policy);

if (!decrypted.Success || !decrypted.Token.AuthenticationTagVerified)
{
    throw new InvalidOperationException(decrypted.Error.Message);
}

Console.WriteLine(Encoding.UTF8.GetString(decrypted.Token.Plaintext));
key.ReleaseOwnedMaterial();
```

Prefer authenticated decryption. Diagnostic reads expose unauthenticated content
only when `AllowUnauthenticatedDecode` is explicitly enabled and must never be
treated as validation.

## JWT

`JsonWebToken` creates compact JWTs and `JwtValidationPolicy` adds issuer,
audience, lifetime, token-type, required-claim, and replay controls. Resolver
metadata is untrusted and is only suitable for selecting a candidate key.

```csharp
using System.Security.Cryptography;
using token;

TokenKey key = SymmetricKeyImporter.ImportJose(
    RandomNumberGenerator.GetBytes(32),
    JoseAlgorithms.HmacSha256).Key;
key.KeyId = "jwt-signing-key";

TokenPolicy tokenPolicy = new TokenPolicy();
tokenPolicy.AllowedAlgorithms = new[] { JoseAlgorithms.HmacSha256 };

JwtClaims claims = JwtClaims.Read(
    "{\"iss\":\"https://issuer.example\","
    + "\"aud\":\"orders-api\",\"sub\":\"alice\","
    + "\"exp\":4102444800}").Claims;
TokenTextResult tokenResult = JsonWebToken.CreateSigned(
    claims,
    key,
    tokenPolicy);

TokenKeyResolver resolver = new TokenKeyResolver(context =>
{
    TokenKeyResult result = new TokenKeyResult();
    if (context.KeyId == key.KeyId
        && context.Algorithm == JoseAlgorithms.HmacSha256)
    {
        result.Success = true;
        result.Key = key;
    }
    else
    {
        result.Error = new TokenError(
            "key_not_found",
            "No matching key was found.",
            -1);
    }

    return result;
});

JwtValidationPolicy validationPolicy = new JwtValidationPolicy(tokenPolicy);
validationPolicy.ValidateIssuer = true;
validationPolicy.ValidIssuers = new[] { "https://issuer.example" };
validationPolicy.ValidateAudience = true;
validationPolicy.ValidAudiences = new[] { "orders-api" };
validationPolicy.RequireExpirationTime = true;

JwtValidationResult validated = JsonWebToken.Validate(
    tokenResult.Text,
    resolver,
    validationPolicy);

if (!validated.Success)
{
    throw new InvalidOperationException(validated.Error.Message);
}

Console.WriteLine(validated.Claims.Get(JwtClaimNames.Subject).StringValue);
key.ReleaseOwnedMaterial();
```

For nested confidentiality and authenticity, use
`CreateSignedThenEncrypted`; validation resolves and authenticates every layer
before returning claims.

## COSE

The high-level operation classes create and authenticate COSE messages. This
example creates a tagged `COSE_Mac0` using HMAC SHA-256. COSE policy names follow
the IANA registry names, such as `HMAC 256/256`.

```csharp
using System.Security.Cryptography;
using System.Text;
using token;

TokenKey key = SymmetricKeyImporter.ImportCose(
    RandomNumberGenerator.GetBytes(32),
    CoseAlgorithms.HmacSha256).Key;

TokenPolicy policy = new TokenPolicy();
policy.AllowedAlgorithms = new[] { "HMAC 256/256" };

CborItem algorithm = new CborItem();
algorithm.Kind = TokenCborValueKind.UnsignedInteger;
algorithm.IntegerArgument = (ulong)CoseAlgorithms.HmacSha256;

CoseHeader header = new CoseHeader();
header.SetProtected(CoseHeaderLabels.Algorithm, algorithm);

TokenBytesResult encoded = CoseMac0Operations.Create(
    Encoding.UTF8.GetBytes("authenticated CBOR payload"),
    header,
    key,
    policy);
CoseMac0Result verified = CoseMac0Operations.Verify(
    encoded.Bytes,
    key,
    policy);

if (!verified.Success || !verified.Message.TagVerified)
{
    throw new InvalidOperationException(verified.Error.Message);
}

Console.WriteLine(Encoding.UTF8.GetString(verified.Message.Payload));
key.ReleaseOwnedMaterial();
```

Equivalent operation facades are available for `COSE_Sign1`, `COSE_Sign`,
`COSE_Encrypt0`, `COSE_Encrypt`, and `COSE_Mac`. Attached, detached, external-AAD,
multi-party, and countersignature flows have dedicated APIs.

## CWT

`CborWebToken.Create` selects `COSE_Sign1`, `COSE_Mac0`, or `COSE_Encrypt0`
from the key's COSE algorithm. This example creates and validates a MAC-protected
CWT.

```csharp
using System.Security.Cryptography;
using token;

TokenKey key = SymmetricKeyImporter.ImportCose(
    RandomNumberGenerator.GetBytes(32),
    CoseAlgorithms.HmacSha256).Key;

TokenPolicy tokenPolicy = new TokenPolicy();
tokenPolicy.AllowedAlgorithms = new[] { "HMAC 256/256" };

CborItem issuer = new CborItem();
issuer.Kind = TokenCborValueKind.TextString;
issuer.Text = "https://issuer.example";

CwtClaims claims = new CwtClaims();
claims.Set(CwtClaimLabels.Issuer, issuer);

TokenBytesResult tokenResult = CborWebToken.Create(
    claims,
    key,
    tokenPolicy);

TokenKeyResolver resolver = new TokenKeyResolver(context =>
{
    TokenKeyResult result = new TokenKeyResult();
    if (context.HasCoseAlgorithm
        && context.CoseAlgorithm == CoseAlgorithms.HmacSha256)
    {
        result.Success = true;
        result.Key = key;
    }
    else
    {
        result.Error = new TokenError(
            "key_not_found",
            "No matching key was found.",
            -1);
    }

    return result;
});

CwtValidationPolicy validationPolicy = new CwtValidationPolicy(tokenPolicy);
validationPolicy.ValidateIssuer = true;
validationPolicy.ValidIssuers = new[] { "https://issuer.example" };

CwtValidationResult validated = CborWebToken.Validate(
    tokenResult.Bytes,
    resolver,
    validationPolicy);

if (!validated.Success)
{
    throw new InvalidOperationException(validated.Error.Message);
}

Console.WriteLine(validated.Claims.Get(CwtClaimLabels.Issuer).Text);
key.ReleaseOwnedMaterial();
```

Use `CwtProtectionLayer` and `CreateNested` when the protection form, external
authenticated data, recipients, signers, or layer ordering must be explicit.

## Universal inspection and validation

The universal facade identifies supported representations and normalizes
authenticated output. Inspection is header-only and unauthenticated; never use an
inspection result to authorize a request.

```csharp
using token;

TokenInspectionResult inspected = UniversalToken.Inspect(tokenText, policy);
Console.WriteLine(inspected.Kind); // Identification only; not authenticated.

TokenValidationResult validated = UniversalToken.Validate(
    tokenText,
    resolver,
    policy);

if (!validated.Success)
{
    Console.WriteLine(validated.Error.Code);
    if (validated.Diagnostics.Length > 0)
    {
        Console.WriteLine(validated.Diagnostics[0].Stage);
    }

    return;
}

Console.WriteLine(validated.Subject);
Console.WriteLine(validated.SignatureValidated);
Console.WriteLine(validated.EncryptionValidated);
```

For binary COSE and CWT inputs use `InspectBytes` and `ValidateBytes`. Validation
returns representation-neutral metadata plus authenticated `JwtClaims` or
`CwtClaims` when applicable.

## Verification

Run the complete test suite with:

```sh
dotnet test token.slnx
```
