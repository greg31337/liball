# Local specification set

Retrieved 2026-08-25 from authoritative publishers. The RFC text files came from the RFC Editor, registry snapshots came from IANA, the OpenID specifications came from the OpenID Foundation, and FIPS 204 came from NIST.

## Core encodings and token formats

- `rfcs/rfc1951.txt`: DEFLATE compressed data format.
- `rfcs/rfc3394.txt`: AES Key Wrap.
- `rfcs/rfc4493.txt`: AES-CMAC.
- `rfcs/rfc4648.txt`: base-N encodings, including base64url.
- `rfcs/rfc5280.txt`: X.509 certificates and certification-path validation.
- `rfcs/rfc5869.txt`: HKDF.
- `rfcs/rfc7468.txt`: textual encodings such as PEM certificate and key blocks.
- `rfcs/rfc8017.txt`: RSA cryptography specifications.
- `rfcs/rfc8259.txt`: JSON.
- `rfcs/rfc8949.txt`: CBOR.
- `rfcs/rfc7515.txt` through `rfcs/rfc7519.txt`: JWS, JWE, JWK, JWA, and JWT.
- `rfcs/rfc7520.txt`: JOSE implementation examples.
- `rfcs/rfc8392.txt`: CWT.
- `rfcs/rfc9052.txt` and `rfcs/rfc9053.txt`: COSE structures and algorithms.
- `rfcs/rfc9054.txt`: COSE hash algorithms.

## JOSE, JWT, and application profiles

- `rfcs/rfc7523.txt`: OAuth JWT assertions and client authentication.
- `rfcs/rfc7638.txt` and `rfcs/rfc9278.txt`: JWK thumbprints and thumbprint URIs.
- `rfcs/rfc7797.txt`: JWS unencoded payloads.
- `rfcs/rfc7800.txt`: JWT confirmation claims and proof-of-possession keys.
- `rfcs/rfc8037.txt`: OKP and the original JOSE EdDSA registration.
- `rfcs/rfc8725.txt`: JWT best current practices.
- `rfcs/rfc8812.txt`: ES256K registrations.
- `rfcs/rfc9864.txt`: fully specified JOSE and COSE algorithms; this deprecates the polymorphic `EdDSA` registration.
- `rfcs/rfc9964.txt` and `nist/NIST.FIPS.204.pdf`: ML-DSA representations and algorithms.
- `rfcs/rfc8417.txt`: security event tokens.
- `rfcs/rfc9068.txt`: OAuth JWT access-token profile.
- `rfcs/rfc9101.txt`: JWT-secured authorization requests.
- `rfcs/rfc9449.txt`: DPoP.
- `rfcs/rfc9901.txt`: SD-JWT.
- `openid/openid-connect-core-1_0.html`: OpenID Connect Core 1.0 with errata set 2.
- `openid/oauth-v2-jarm-final.html`: JWT Secured Authorization Response Mode for OAuth 2.0.

## COSE and CWT extensions

- `rfcs/rfc8439.txt`: ChaCha20-Poly1305.
- `rfcs/rfc8747.txt`: CWT confirmation methods.
- `rfcs/rfc9180.txt`: HPKE.
- `rfcs/rfc9338.txt`: COSE countersignatures version 2.
- `rfcs/rfc9597.txt`: CWT claims in COSE headers.
- `rfcs/rfc9679.txt`: COSE key thumbprints.
- `drafts/draft-ietf-cose-hpke-25.txt`: current COSE HPKE work in progress retrieved for the planned known-algorithm and availability handling. It is not an RFC and can change.

## Live registry snapshots

- `registries/iana-jose.txt`: JOSE header, algorithm, key, and compression registries.
- `registries/iana-jwt.txt`: JWT claims and related registries.
- `registries/iana-cose.txt`: COSE header, algorithm, key, curve, and tag registries.
- `registries/iana-cwt.txt`: CWT claims and confirmation registries.

Canonical sources: <https://www.rfc-editor.org/>, <https://www.iana.org/protocols>, <https://openid.net/developers/specs/>, and <https://csrc.nist.gov/pubs/fips/204/final>.
