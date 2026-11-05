# Specification and research sources

Retrieved on 2026-08-27. GitHub resources are pinned to the commit shown so the research basis remains reproducible. Each reference-library snapshot is stored with its upstream license.

## Specifications

- `rfc9106.txt` and `rfc9106.pdf`: RFC 9106, *Argon2 Memory-Hard Function for Password Hashing and Proof-of-Work Applications*, from `https://www.rfc-editor.org/rfc/rfc9106`.
- `rfc9106-errata.html`: the RFC Editor's current RFC 9106 errata search result from `https://errata.rfc-editor.org/search/?rfc_number=9106`; it contains verified editorial erratum 7721.
- `rfc7693.txt`: RFC 7693, *The BLAKE2 Cryptographic Hash and Message Authentication Code (MAC)*, from `https://www.rfc-editor.org/rfc/rfc7693.txt`.
- `argon2-specs.pdf`: the Argon2 design/specification paper from the reference Argon2 repository at commit `f57e61e19229e23c4445b85494dbf7c07de721cb`.

## Reference-library snapshots

- `reference-libraries/libargon2/`: README, public C header, and canonical PHC encoder/decoder from `P-H-C/phc-winner-argon2` commit `f57e61e19229e23c4445b85494dbf7c07de721cb`. These define the reference raw/encoded/context/verification surface, version 0x10 compatibility, parameter limits, memory clearing, and PHC string grammar.
- `reference-libraries/argon2-cffi/`: API reference, parameter guidance, and profiles from `hynek/argon2-cffi` commit `ff41e1f1d74f60b8af1307aa71e772c781eea945`. These inform the high-level/low-level split, secure random salts, type-agile verification, parameter extraction, rehash checks, and RFC profiles.
- `reference-libraries/argon2-jvm/`: README and standard/advanced interfaces from `phxql/argon2-jvm` commit `501a33c0a5fd9b85e94bc71a8b3d70d1e4c8bf56`. These inform byte/text password APIs, raw hashes, KDF entry points, caller-buffer wiping, rehash checks, explicit salts, secret/associated-data support, and advanced version selection.
- `reference-libraries/konscious/`: README and primary Argon2 class from `kmaragon/Konscious.Security.Cryptography` commit `4ed95a5377e411506ca6868409b5c7d7ecaa859b`. These inform the managed-.NET KDF shape and support for iterations, memory, lanes, salt, known secret, associated data, and variable output length.

