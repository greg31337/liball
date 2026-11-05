# Universal Token Library Development Plan

## Goal

Build a dependency-free .NET library for reading, writing, signing, verifying, encrypting, decrypting, inspecting, and validating JOSE, JWT, COSE, CWT, and closely related token formats.

The implementation must use only built-in .NET APIs plus code in this repository. It must not depend on external packages. Span-oriented APIs should be used where they reduce allocations without violating the repository syntax policy.

## Research Notes

Existing libraries and standards reviewed to derive the feature set:

- Nimbus JOSE + JWT: broad JOSE support covering JWS, JWE, unsecured objects, signed/encrypted JWTs, JWS JSON serialization, JWK, JWK sets, JWK thumbprints, unencoded JWS payloads, many JWA algorithms, and processor-style validation.
  Source: https://javadoc.io/static/com.nimbusds/nimbus-jose-jwt/9.23/index.html
- panva/jose: "JSON Web Almost Everything" covering JWA, JWS, JWE, JWT, JWK, JWKS, compact/general/flattened serializations, JWT validation profiles, JWK thumbprints, RFC 7797 unencoded payloads, EdDSA, and explicit security choices around embedded keys and `none`.
  Source: https://app.unpkg.com/%40panva/jose%401.7.0/files/README.md
- Microsoft `JwtSecurityTokenHandler`: compact JWT creation, reading, validation, JWE decryption, issuer/audience/lifetime/signature validation, token replay checks, claim mapping, and configurable validation parameters.
  Source: https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler
- Microsoft `System.Security.Cryptography.Cose`: COSE Sign1 embedded/detached signing, Span-based encode/try-encode APIs, detached verification, protected/unprotected headers, and CoseKey-oriented verification.
  Source: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cose.cosesign1message
- cose-js: practical COSE examples for sign, verify, encrypt, decrypt, MAC, protected/unprotected headers, key identifiers, and RFC 8152-style message flows.
  Source: https://www.npmjs.com/package/cose-js
- Python `cwt`: comprehensive COSE/CWT examples for MAC0, MAC, Encrypt0, Encrypt, Sign1, Sign, countersignatures, HPKE, nested CWTs, user-defined claims, proof-of-possession keys, private CA flows, and CWT claims in COSE headers.
  Source: https://pypi.org/project/cwt/
- RFC 7515, 7516, 7517, 7518, 7519: core JWS, JWE, JWK, JWA, and JWT.
- RFC 7520: JOSE cookbook examples for interop tests.
- RFC 7638 and RFC 9278: JWK thumbprints and JWK thumbprint URIs.
- RFC 7797: JWS unencoded payload option.
- RFC 8037: OKP, EdDSA, X25519, and X448 for JOSE, subject to .NET built-in support.
- RFC 9864: fully specified JOSE and COSE algorithm identifiers, including the deprecation of the polymorphic `EdDSA` identifier.
- RFC 9964 and FIPS 204: ML-DSA key representations and algorithm identifiers, subject to built-in .NET support.
- RFC 8725: JWT best current practices.
- RFC 8392: CBOR Web Token.
- RFC 9052 and RFC 9053: COSE structures, process, keys, and initial algorithms.
- RFC 9338: COSE countersignatures version 2.
- RFC 9597: CWT claims in COSE headers.
- RFC 9901: Selective Disclosure JWT and key binding.
- IANA JOSE, JWT, CWT, and COSE registries for names, labels, claims, headers, algorithms, key types, and curves.

The plan intentionally learns feature breadth from these projects without using them as dependencies.

## Security Posture

- Use built-in .NET cryptographic primitives for actual cryptography.
- Do not hand-roll cryptographic algorithms when .NET does not provide them.
- Algorithms not available from built-in .NET APIs should be represented in registries and parser metadata but return a clear unsupported-algorithm result for cryptographic operations.
- Insecure legacy algorithms such as `none`, `RSA1_5`, weak HMAC keys, and unauthenticated compression must require explicit opt-in.
- Default validation should require an algorithm allowlist, reject unknown critical headers, enforce key use/key operations, enforce token size limits, validate time claims, and avoid network fetches unless the caller opts in.
- Remote key URL headers such as `jku`, `x5u`, and COSE equivalents must be data only by default. Fetching must be a separate resolver policy with host allowlists, HTTPS enforcement, size limits, cache limits, and timeout controls.
- Use constant-time comparisons for signatures, MAC tags, digests, token hashes, JTI replay identifiers, and audience/issuer equality when secrets or tokens are involved.
- Normalize ECDSA signatures between JOSE/COSE raw form and DER form using strict fixed-width checks.

## Feature Inventory, Low Level To High Level

### 1. Foundational Encoding And Utility Features

- UTF-8 read/write helpers with explicit invalid-input handling.
- Base64url encode/decode without padding.
- Base64url encode/decode with optional padding tolerance for compatibility.
- Standard base64 and base64 PEM block helpers.
- Hex encode/decode for diagnostics and test vectors.
- Percent-safe token segment scanning without allocating substrings.
- Compact token segment splitter for three-part JWS/JWT and five-part JWE.
- JSON string escaping and unescaping helpers where `System.Text.Json` does not expose the exact needed shape.
- Duplicate JSON object member detection for protected headers and claim sets.
- Deterministic JSON serialization for JWK thumbprints.
- CBOR reader for unsigned integers, negative integers, byte strings, text strings, arrays, maps, tags, booleans, null, undefined, simple values, and floating point values.
- CBOR writer for definite-length deterministic encoding.
- CBOR canonical/deterministic ordering for map keys.
- CBOR duplicate key detection.
- CBOR tag read/write support for COSE message tags.
- Strict integer range conversion helpers for COSE labels and CWT claim keys.
- Big-endian fixed-width integer helpers for RSA, ECDSA, EC coordinates, and COSE keys.
- ECDSA raw signature to DER conversion.
- ECDSA DER signature to raw fixed-width conversion.
- PEM import/export wrappers for RSA, ECDSA, and X.509 certificates using built-in .NET APIs.
- X.509 certificate chain field parsing for `x5c`.
- X.509 SHA-1 and SHA-256 thumbprint calculation for `x5t` and `x5t#S256`.
- Cryptographic random byte generation.
- Constant-time byte comparison.
- Length-limited buffer readers and writers.
- Allocation-aware byte array pooling policy only if it can be done safely with built-in .NET APIs.
- Error code catalog for parse, validation, crypto, policy, key, and unsupported-feature failures.

### 2. Registries And Metadata

- JOSE header parameter registry.
- JOSE algorithm registry for JWS and JWE.
- JOSE compression registry, initially `DEF`.
- JWT registered claim registry.
- JWK key type, key use, key operation, curve, and algorithm registries.
- COSE header parameter registry.
- COSE algorithm registry.
- COSE key type, key parameter, key operation, and curve registries.
- CWT registered claim registry.
- Token media type and `typ`/`cty` registry helpers.
- Supported-vs-known algorithm metadata.
- Recommended, deprecated, prohibited, and compatibility-only algorithm metadata.
- Mapping between JOSE algorithm names and COSE algorithm labels where standards define equivalence.
- Mapping between JWT claim names and CWT claim labels.

### 3. Key Features

- Unified key container for symmetric, RSA, EC2, and OKP-like public/private key material.
- JWK parse and write.
- JWK Set parse and write.
- JWK import from RSA, ECDSA, symmetric bytes, and X.509 certificates.
- JWK export to RSA, ECDSA, symmetric bytes, and X.509 certificates where key material allows it.
- JWK public/private/minimal projections.
- JWK thumbprint calculation.
- JWK thumbprint URI calculation.
- JWK key selection by `kid`, `kty`, `use`, `key_ops`, `alg`, curve, and certificate thumbprint.
- JWKS in-memory key store.
- JWKS remote fetch policy model using `HttpClient` only when explicitly enabled.
- JWKS cache control, refresh, stale-if-error, size limits, and host allowlists.
- COSE_Key parse and write.
- COSE_Key import/export from the unified key container.
- COSE key selection by `kid`, `alg`, key type, key operation, and curve.
- Key operation enforcement for sign, verify, encrypt, decrypt, wrap, unwrap, MAC create, MAC verify, derive, deriveBits, and deriveKey.
- Symmetric key minimum length checks.
- RSA key size policy checks.
- EC curve policy checks.
- Key material zeroization hooks for temporary arrays when possible.

### 4. Algorithm Features

- JWS HMAC algorithms: HS256, HS384, HS512.
- JWS RSA PKCS#1 v1.5 algorithms: RS256, RS384, RS512.
- JWS RSA-PSS algorithms: PS256, PS384, PS512.
- JWS ECDSA algorithms: ES256, ES384, ES512.
- JWS secp256k1 algorithm: ES256K when built-in .NET support is available.
- JWS Ed25519 and Ed448 when built-in .NET support is available; treat the polymorphic `EdDSA` identifier as deprecated per RFC 9864.
- JWS ML-DSA-44, ML-DSA-65, and ML-DSA-87 when built-in .NET support and platform availability permit them.
- Unsecured JOSE/JWT `alg=none` parse support, with creation and validation disabled unless explicitly allowed.
- JWE direct key management: `dir`.
- JWE RSA-OAEP key management: RSA-OAEP, RSA-OAEP-256, RSA-OAEP-384, RSA-OAEP-512 when available.
- JWE RSA1_5 parse support, with decrypt disabled unless explicitly allowed.
- JWE AES Key Wrap: A128KW, A192KW, A256KW using built-in AES primitives.
- JWE AES-GCM Key Wrap: A128GCMKW, A192GCMKW, A256GCMKW.
- JWE PBES2 Key Wrap: PBES2-HS256+A128KW, PBES2-HS384+A192KW, PBES2-HS512+A256KW.
- JWE ECDH-ES direct agreement: ECDH-ES.
- JWE ECDH-ES with AES Key Wrap: ECDH-ES+A128KW, ECDH-ES+A192KW, ECDH-ES+A256KW.
- JWE content encryption with AES-GCM: A128GCM, A192GCM, A256GCM.
- JWE content encryption with AES-CBC-HMAC-SHA2: A128CBC-HS256, A192CBC-HS384, A256CBC-HS512.
- JWE `zip=DEF` compression using built-in compression APIs, with strict expansion limits.
- COSE ECDSA: ES256, ES384, ES512.
- COSE RSA signature algorithms when represented by supported standards and built-in .NET APIs.
- COSE HMAC algorithms with full and truncated tags where standards define them.
- COSE AES-MAC algorithms when built-in .NET APIs make safe implementation practical.
- COSE AES-GCM encryption algorithms.
- COSE AES-CCM encryption algorithms using built-in AES-CCM APIs where available.
- COSE AES Key Wrap algorithms.
- COSE direct encryption and direct MAC modes.
- COSE ECDH key agreement and KDF contexts.
- COSE HKDF/HKDF-like KDF context construction where required by COSE.
- COSE Ed25519 and Ed448 when built-in .NET support is available; treat the polymorphic EdDSA registration as deprecated.
- COSE ChaCha20/Poly1305 using the built-in .NET implementation where platform support is available.
- COSE HPKE known-algorithm parse support, with crypto unsupported unless built-in .NET support exists.

### 5. JOSE Object Features

- JOSE header parse/write with registered and custom parameters.
- Protected header validation.
- Unprotected header validation for JSON serializations.
- Critical header processing.
- `typ` and `cty` processing.
- `kid` processing.
- Embedded `jwk` and `x5c` processing as explicit opt-in key material.
- Remote `jku` and `x5u` headers as data by default.
- JWS compact serialization parse/write.
- JWS flattened JSON serialization parse/write.
- JWS general JSON serialization parse/write.
- JWS sign with attached payload.
- JWS verify with attached payload.
- JWS detached payload sign/verify.
- JWS unencoded payload option `b64=false`.
- JWS multiple-signature creation and verification.
- JWS per-signature protected and unprotected headers.
- JWE compact serialization parse/write.
- JWE flattened JSON serialization parse/write.
- JWE general JSON serialization parse/write.
- JWE encrypt/decrypt for one recipient.
- JWE encrypt/decrypt for multiple recipients.
- JWE Additional Authenticated Data support.
- JWE per-recipient headers.
- JWE content encryption key generation and handling.
- Nested JOSE tokens using `cty=JWT` or other registered content types.
- Generic JOSE payload mode for arbitrary bytes.
- Diagnostic decode mode that exposes headers and metadata without verifying or decrypting.

### 6. JWT Features

- JWT compact parse as signed, encrypted, nested, or unsecured.
- JWT JSON claim set parse/write.
- Registered claims: `iss`, `sub`, `aud`, `exp`, `nbf`, `iat`, `jti`.
- Common OpenID/OAuth claims: `azp`, `nonce`, `auth_time`, `acr`, `amr`, `at_hash`, `c_hash`, `cnf`, `scope`, `client_id`.
- NumericDate conversion with clock abstraction.
- Audience validation for string and array forms.
- Issuer validation.
- Subject validation hooks.
- Expiration validation with clock skew.
- Not-before validation with clock skew.
- Issued-at validation with max age policy.
- JWT ID replay detection hook.
- Required claim policy.
- Claim type/value validation callbacks using built-in delegate types.
- Header claim replication checks for encrypted JWTs.
- Token type validation for `JWT`, `at+jwt`, `id_token`, `logout+jwt`, `secevent+jwt`, `dpop+jwt`, and custom types.
- Algorithm allowlist validation.
- Key resolver integration for issuer, audience, token type, key id, and algorithm.
- Signed JWT creation.
- Signed JWT validation.
- Encrypted JWT creation.
- Encrypted JWT validation.
- Nested signed-then-encrypted JWT creation and validation.
- Nested encrypted-then-signed JWT creation and validation when policy allows it.
- OAuth 2.0 JWT access token profile validation.
- OpenID Connect ID Token profile validation.
- DPoP proof JWT validation.
- JWT Secured Authorization Request and Response helper validation.
- Security Event Token helper validation.
- Client assertion JWT helper validation.
- Selective Disclosure JWT parse and digest verification support where it can be built on JOSE and built-in digests.

### 7. COSE Object Features

- COSE protected and unprotected header parse/write.
- COSE critical header processing.
- COSE external AAD support.
- COSE detached content support.
- COSE tagged and untagged message support.
- COSE_Sign parse/write.
- COSE_Sign create and verify with multiple signers.
- COSE_Sign1 parse/write.
- COSE_Sign1 create and verify.
- COSE_Encrypt parse/write.
- COSE_Encrypt create and decrypt with multiple recipients.
- COSE_Encrypt0 parse/write.
- COSE_Encrypt0 create and decrypt.
- COSE_Mac parse/write.
- COSE_Mac create and verify with recipients.
- COSE_Mac0 parse/write.
- COSE_Mac0 create and verify.
- COSE_recipient parse/write.
- COSE_signature parse/write.
- COSE countersignature version 2 parse/write/create/verify.
- COSE KDF context construction.
- COSE recipient key distribution for direct, key wrap, and ECDH modes.
- COSE diagnostic decode mode that exposes structure without cryptographic operations.

### 8. CWT Features

- CWT claim set parse/write using CBOR maps.
- Registered CWT claims: issuer, subject, audience, expiration, not-before, issued-at, CWT ID.
- CWT confirmation claim support.
- CWT proof-of-possession key support.
- CWT signed, MACed, encrypted, and nested forms through COSE.
- CWT validation policy mirroring JWT where concepts align.
- CWT claims in COSE headers per RFC 9597.
- JWT/CWT claim mapping helpers.
- CWT diagnostic decode mode.

### 9. Universal Token Features

- Token format detection for compact JWS/JWT, compact JWE, JWS JSON, JWE JSON, COSE bytes, CWT bytes, SD-JWT text form, and opaque unknown tokens.
- Safe header-only inspection.
- Safe claims-only inspection only when payload is not encrypted and policy allows unauthenticated decode.
- Unified validation result with token kind, claims, headers, key id, algorithm, issuer, subject, audiences, times, and diagnostics.
- Unified creation options for signed, encrypted, MACed, nested, and detached payload tokens.
- Unified key resolver for JOSE and COSE.
- Unified algorithm policy.
- Unified time provider using callback fields.
- Unified error reporting with stable error codes.
- Interop cookbook runner for JSON and CBOR test vectors.
- Deterministic test vector generation.
- Performance benchmarks using only built-in tooling if added later.

## Public API Completion Tasks

- [DONE] T001: Add the missing public data contracts under `src/Api/Data` for algorithm metadata, codec and key results, JWS and JWE serializations, JWT creation and validation policies and results, profile-specific policies and results, COSE operation inputs and results, CWT validation, and universal inspection, creation, validation, and diagnostics.
- [DONE] T002: Add public encoding service contracts and implementation classes for strict UTF-8, base64url, compatibility base64url, standard base64, hexadecimal, PEM blocks, compact-token segment scanning, and size-limited buffer operations, using distinctly named Span-oriented methods where they avoid allocations.
- [DONE] T003: Add public JSON and CBOR codec service contracts and implementation classes for duplicate-safe parsing, deterministic writing, all supported value kinds, tags, canonical map ordering, and depth and size enforcement.
- [DONE] T004: Add public cryptographic utility service contracts and implementation classes for constant-time comparison, secure random generation, fixed-width big-endian conversion, raw and DER ECDSA signature conversion, PEM key import and export, X.509 key import, certificate-chain validation, and SHA-1 and SHA-256 certificate thumbprints.
- [DONE] T005: Add public registry and mapping service contracts and implementation classes that expose current JOSE, JWT, JWK, COSE, and CWT registry metadata, supported and deprecated status, fully specified algorithm identifiers, JOSE-to-COSE algorithm mappings, and JWT-to-CWT claim mappings.
- [DONE] T006: Add a public key service contract and implementation class for symmetric, RSA, EC2, OKP, X.509, and built-in post-quantum key import where supported; JWK, JWKS, and COSE_Key parsing and writing; public, private, and minimal projections; JWK and COSE key thumbprints; key conversion; key selection; operation enforcement; and temporary key-material clearing.
- [DONE] T007: Add a public opt-in remote JWKS resolver contract and implementation class using built-in `HttpClient`, with HTTPS and host policy, response and cache limits, refresh and stale-if-error behavior, timeouts, and deterministic cache control.
- [DONE] T008: Add a public algorithm service contract and implementation class for the planned JWS, JWE, and COSE signature, MAC, key-management, key-agreement, KDF, compression, and content-encryption operations, using only built-in .NET cryptography and returning stable unsupported results when the runtime lacks an algorithm.
- [DONE] T009: Add a public JWS service contract and implementation class covering JOSE header parsing and writing, critical-header processing, compact and flattened and general JSON serializations, attached and detached payloads, RFC 7797 unencoded payloads, multiple signatures, diagnostic reads, and explicitly policy-gated unsecured tokens.
- [DONE] T010: Add a public JWE service contract and implementation class covering JOSE header processing, compact and flattened and general JSON serializations, one or many recipients, per-recipient headers, external authenticated data, content compression with expansion limits, encryption, decryption, and diagnostic reads.
- [DONE] T011: Add a public JWT service contract and implementation class covering claims parsing and deterministic writing, NumericDate conversion, signed and encrypted creation, supported nesting orders, signature and decryption validation, issuer, audience, subject, lifetime, replay, required-claim, token-type, replicated-claim, resolver, and algorithm-policy checks.
- [DONE] T012: Add public profile validator contracts and implementation classes for OAuth JWT access tokens, OpenID Connect ID Tokens, DPoP proofs, JWT-secured authorization requests, JARM responses, security event tokens, client assertions, and RFC 9901 SD-JWT parsing, disclosure digest verification, and key binding.
- [DONE] T013: Complete the public COSE foundational layer with service contracts and implementation classes for protected and unprotected headers, critical parameters, tagged and untagged detection, exact protected bytes, external AAD, detached content, Sig_structure, Enc_structure, MAC_structure, countersignature structures, KDF contexts, recipients, and COSE_Key processing.
- [DONE] T014: Add a public COSE signing service contract and implementation class for COSE_Sign and COSE_Sign1 parse, write, create, verify, multiple signers, detached payloads, diagnostic reads, and RFC 9338 countersignature version 2 parse, write, create, and verify.
- [DONE] T015: Add a public COSE encryption and MAC service contract and implementation class for COSE_Encrypt, COSE_Encrypt0, COSE_Mac, and COSE_Mac0 parse, write, create, decrypt, and verify operations, including nested recipients, direct, key-wrap, ECDH, supported HPKE modes, external AAD, detached content, and diagnostic reads.
- [DONE] T016: Add a public CWT service contract and implementation class for deterministic claim-set parsing and writing, registered-claim validation, confirmation and proof-of-possession methods, signed and MACed and encrypted and nested creation and validation through COSE, RFC 9597 claims in COSE headers, JWT and CWT mapping, and diagnostic reads.
- [DONE] T017: Add a public universal token service contract and implementation class for text and binary format detection, policy-gated header and claims inspection, unified creation, key resolution, validation, normalized metadata, and stable diagnostics across JOSE, JWT, SD-JWT, COSE, CWT, and unknown opaque inputs.
- [DONE] T018: Audit every public operation for secure defaults, algorithm and key-type confusion resistance, key-use and key-operation enforcement, critical-header handling, token and nesting limits, compression limits, predictable programmer-misuse exceptions, stable normal-failure results, and use of built-in .NET dependencies only.
- [DONE] T019: Add complete public-surface tests for every public data class, enum, constant registry, interface, implementation constructor, method, default value, null contract, result invariant, and stable error code.
- [DONE] T020: Add complete public encoding, JSON, CBOR, and cryptographic utility tests covering successful round trips, Span inputs, malformed inputs, duplicate keys, deterministic encodings, all value kinds, boundary lengths, depth and size limits, certificate cases, and cryptographic test vectors.
- [DONE] T021: Add complete public local-key and registry tests covering every supported key type and projection, JWK and JWKS and COSE_Key round trips, thumbprints and URIs, mappings and metadata, selection combinations, operation restrictions, malformed keys, weak-key policy, and unsupported runtime algorithms.
- [DONE] T022: Add complete public remote-JWKS resolver tests with a test-only HTTP handler covering opt-in behavior, HTTPS and host restrictions, timeouts, response limits, cache limits, refresh, stale-if-error, rotation, ambiguous key selection, and network failures.
- [DONE] T023: Add complete public algorithm-service tests for every planned built-in JWS, JWE, and COSE algorithm, including official vectors, Span paths, wrong keys, wrong algorithms, corrupted signatures and tags, invalid nonces and lengths, compatibility gates, and stable unsupported results.
- [DONE] T024: Add complete public JWS tests for compact and flattened and general forms, attached and detached payloads, encoded and unencoded payloads, single and multiple signatures, header placement and critical processing, unsecured policy gates, diagnostic reads, round trips, and malformed or adversarial inputs.
- [DONE] T025: Add complete public JWE tests for compact and flattened and general forms, all supported key-management and content-encryption combinations, single and multiple recipients, per-recipient headers, authenticated data, compression, nesting, diagnostic reads, round trips, authentication failures, and resource-limit failures.
- [DONE] T026: Add complete public JWT tests for claims and NumericDate handling, signed and encrypted and nested creation, all validation policies and resolver paths, issuer and audience and subject and lifetime and replay checks, required claims, token types, replicated claims, algorithm allowlists, malformed tokens, and clock boundaries.
- [DONE] T027: Add complete public profile tests for OAuth JWT access tokens, OpenID Connect ID Tokens, DPoP, JAR, JARM, security event tokens, client assertions, and SD-JWT, covering each required claim and header rule, positive examples, negative examples, disclosure and key-binding failures, replay, and profile-specific algorithm restrictions.
- [DONE] T028: Add complete public COSE tests for headers, tags, structures, keys, recipients, COSE_Sign and COSE_Sign1, countersignatures, COSE_Encrypt and COSE_Encrypt0, COSE_Mac and COSE_Mac0, every supported algorithm and distribution mode, external AAD, detached content, nesting, diagnostics, official vectors, and malformed or tampered messages.
- [DONE] T029: Add complete public CWT tests for claim encoding and validation, confirmation and proof-of-possession methods, all supported COSE protection layers, nested forms, claims in headers, JWT and CWT mapping, diagnostics, official vectors, malformed claims, time boundaries, and cryptographic failures.
- [DONE] T030: Add complete public universal-service tests for every detectable text and binary token kind, opaque inputs, safe inspection policy, creation and validation routing, normalized metadata, diagnostics, key-resolution failures, nesting and size limits, and consistency with the corresponding family-specific public service.
- [DONE] T031: Add end-to-end public API interoperability tests using RFC 7520, RFC 8392, RFC 9052, RFC 9053, RFC 9338, RFC 9597, RFC 9901, OpenID Connect, and JARM examples, plus deterministic JSON and CBOR vectors generated through the public layer.
