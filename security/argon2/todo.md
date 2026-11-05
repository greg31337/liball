# Universal Argon2 library development plan

## Goal and scope

Build a portable, fully managed .NET Argon2 password-hashing and password-based key-derivation library based on RFC 9106. The library will support Argon2id, Argon2i, and Argon2d; interoperable raw tags and PHC encoded strings; version 1.3 (`v=19`) generation; version 1.0 (`v=16`) compatibility; low-level span APIs; allocating KDF APIs; and safe high-level password APIs.

Production code under `src/` will depend only on built-in .NET APIs. It will not use native code, P/Invoke, unsafe code, or third-party packages. Span types will be used at input/output boundaries and in internal byte processing where they reduce copies. Public instances will be synchronous and thread-safe. Tests may use external packages or tools solely as independent interoperability oracles.

The implementation must follow `syntax.md`. In particular, it must use the restricted class/interface/enum subset, file-scoped namespaces, fields instead of properties, no overloads, no `try`/`catch`/`finally`, no `unchecked`, no LINQ, and the required `src/Api` category layout. Public implementation classes will expose one constructor and only methods declared by their interfaces. Each public declaration will receive the XML comments required by the policy as part of its implementation task; there is no separate documentation task.

Not planned: benchmark suites, performance regression tests, runtime calibration helpers, syntax-policy tests, source-scanning tests, documentation projects, packaging, or release work.

## Research baseline and adopted features

- RFC 9106 is the normative algorithm basis for Argon2 version 1.3. It requires Argon2id support, defines Argon2d and Argon2i, BLAKE2b and H-prime, lane/slice indexing, memory rounding, optional secret and associated data, test vectors, and the two recommended Argon2id profiles.
- RFC 7693 supplies the BLAKE2b definition and independent digest vectors required by the Argon2 core.
- `libargon2` supplies the interoperability baseline: all three variants, raw and encoded output, context inputs for secret/associated data, separate lanes and worker-thread limits, version 1.0 compatibility, input bounds, constant-time verification, secure memory clearing, encoded-length calculation, and the canonical PHC grammar/base64 rules.
- `argon2-cffi` contributes the explicit high-level/low-level split, secure random salts, Argon2id-by-default behavior, type-agile verification, decoded parameter extraction, exact-configuration rehash checks, byte and UTF-8 text inputs, and named RFC profiles.
- `argon2-jvm` contributes explicit byte/text surfaces, raw hashing and PBKDF/KDF entry points, user-supplied salts at the advanced layer, secret/associated-data handling, version selection, secure caller-buffer practices, and a minimum-cost rehash concept. This plan uses exact configuration comparison instead of treating cost tuples as totally ordered; decoded metadata remains available for application-specific upgrade rules.
- `Konscious.Security.Cryptography.Argon2` confirms the managed-.NET KDF use case and the need for variable-length output, iterations, memory size, lanes, salt, known secret/pepper, and associated data.
- The pinned materials and provenance are under `specs/`; see `specs/SOURCES.md`.

### Reference features intentionally represented differently

- Variant-specific entry points from `libargon2` and `argon2-jvm` become one generic API selected by `Argon2Variant`; this keeps all variants available without prohibited method overloads or a class per variant.
- Native context structs, error-code-to-string helpers, allocator callbacks, and native-library loading are not exposed because this is a direct managed implementation. Typed exceptions and verification result enums replace C error codes.
- Caller-buffer wipe helpers are not duplicated in the public API. The library clears buffers it owns, does not mutate buffers it does not own, and byte-oriented callers can clear their own arrays with built-in `CryptographicOperations.ZeroMemory`.
- The high-level layer always creates its own random salt. Advanced callers that need an explicit salt use the primitive/KDF plus codec layers, avoiding an easy salt-reuse trap while preserving the `argon2-cffi`/`argon2-jvm` advanced use case.
- Async wrappers are not planned because the source policy forbids `await` and a wrapper would not make the CPU- and memory-bound work asynchronous; applications can schedule synchronous calls according to their own concurrency limits.
- `DeriveBytes.GetBytes` continuation/reset behavior from the Konscious library is not copied because Argon2's output length participates in H0. Every derivation is a complete one-shot operation.
- Argon2d remains available for interoperability and side-channel-free applications, but Argon2id is the high-level default. Argon2i remains available for data-independent compatibility/KDF requirements.

## Complete feature inventory

### Algorithm and interoperability

- Argon2id as the default and recommended password/KDF variant, plus complete Argon2i and Argon2d support.
- RFC 9106/version 1.3 (`0x13`, PHC `v=19`) computation for every variant.
- Legacy version 1.0 (`0x10`, PHC `v=16`) computation and verification for existing hashes, including the version-specific overwrite-versus-XOR rule on later passes.
- BLAKE2b with 64-bit little-endian words, RFC 7693 initialization, compression, final block handling, and 1-to-64-byte digest output.
- Argon2 variable-length hash H-prime for every legal tag length representable by a .NET span/array, covering both the at-most-64-byte and iterative longer-output branches.
- Initial digest H0 over lanes, tag length, memory, passes, version, variant, password, salt, optional secret, and optional associated data with the exact LE32 length prefixes/order.
- 1024-byte memory blocks, four synchronization slices per pass, two initial blocks per lane, the BlaMka permutation, row/column rounds, and final XOR of each lane's last block.
- Correct data-dependent addressing for Argon2d, data-independent address-block generation for Argon2i, and hybrid first-half-of-first-pass addressing for Argon2id.
- Exact J1/J2 reference-lane and reference-index mapping, including first-pass/current-lane/cross-lane exclusions and segment boundaries.
- Deterministic output independent of the number of worker threads; lanes remain a hash parameter while maximum workers are an execution-only control.
- Portable modular 64-bit arithmetic implemented without `unchecked`, because the project enables overflow checking and the restricted syntax forbids checked/unchecked constructs.

### Parameters, validation, and resource controls

- Signed public count/size fields, using `long` where the RFC's unsigned 32-bit domain exceeds `int`, followed by explicit range checks before conversion to algorithm words or allocation sizes.
- Time cost/pass count from 1 through `2^32-1` at the format/model level, subject to representable execution limits.
- Memory cost in KiB from `8 * lanes` through `2^32-1` at the format/model level, with overflow-safe byte/block calculations and clear platform/allocation failure behavior.
- Degree of parallelism from 1 through `2^24-1` and validation that memory provides at least two blocks per lane per slice.
- RFC memory normalization: actual block count is the requested memory rounded down to a multiple of `4 * lanes`; the originally requested value remains in H0 and PHC metadata as specified.
- Tag/output length of at least 4 bytes and up to the destination/managed-array limit; clear rejection of values that are RFC-representable but not addressable by the current managed API.
- Salt length of at least 8 bytes for `libargon2`/PHC interoperability, while defaulting to the RFC-recommended 16 bytes.
- Empty passwords allowed; empty optional secret and associated data allowed; null is represented only through nullable reference fields in high-level data models and normalized to empty input.
- Validation of enum values, destination/expected-tag lengths, maximum workers, policy coherence, integer conversion, multiplication/addition overflow, and memory chunk geometry before expensive work begins.
- Separate maximum worker count so callers can cap CPU concurrency without changing the derived result.
- Verification policy limits for untrusted encoded hashes: encoded text length, memory, passes, lanes, salt length, tag length, allowed variants, and allowed versions.
- Resource-policy evaluation before allocating the Argon2 workspace or beginning the expensive hash, preventing encoded-hash denial-of-service inputs.

### Low-level raw hashing and KDF

- A non-allocating primitive that accepts password/message, salt, optional secret/pepper, optional associated data, algorithm parameters, execution options, and a caller-owned destination span.
- Raw-tag verification using identical contextual inputs and a fixed-time comparison.
- An allocating KDF convenience API returning exactly the requested key length.
- A destination-based KDF path through the primitive for callers that manage key buffers themselves.
- Explicit caller-provided salt at low and KDF levels for deterministic protocols and interoperability; no implicit salt in deterministic APIs.
- Variable output suitable for password hashes and derived encryption/MAC keys, without the 1024-byte limitation present in the researched managed library.
- No stateful `DeriveBytes` stream semantics: each call derives one independent tag whose requested length is part of H0, matching Argon2 rather than pretending output can be continued with `GetBytes` calls.

### PHC encoded hash strings

- Canonical format `$argon2{id|i|d}$v=<version>$m=<KiB>,t=<passes>,p=<lanes>$<salt>$<tag>`.
- Canonical unpadded PHC base64 with the standard `A-Z`, `a-z`, `0-9`, `+`, `/` alphabet and validation of unused trailing bits.
- Canonical minimal unsigned decimal fields with no sign, whitespace, empty digits, overflow, or leading zeroes other than the single digit `0` where structurally legal.
- Strict variant token, field names/order, separators, exact field count, ASCII content, no padding, and no trailing data.
- Decoding of the historical version-omitted form as version 1.0, matching `libargon2`; canonical encoding always emits an explicit supported version.
- Encoding, strict throwing decode, exception-less decode result, decoded metadata/salt/tag extraction, and exact encoded-length calculation.
- Round-trip preservation of variant, version, requested costs, salt bytes, and tag bytes.
- Secret/pepper and associated data deliberately remain external context because PHC strings do not encode them.

### High-level password hashing and verification

- Secure random per-password salts generated with `RandomNumberGenerator`, 16 bytes by default.
- Argon2id version 1.3 and the RFC 9106 second recommended profile (64 MiB, 3 passes, 4 lanes, 16-byte salt, 32-byte tag) as the default high-level configuration.
- Constants for both RFC 9106 recommended profiles, including the 2 GiB/1-pass first option and 64 MiB/3-pass second option.
- Constructor-time option snapshotting and validation so mutable public data fields cannot alter an existing hasher and invalid costs are not repeatedly rediscovered during password operations.
- Byte-password APIs as the preferred secret-handling surface, avoiding forced immutable string copies.
- UTF-8 text convenience APIs with explicit behavior: no Unicode normalization, and any temporary encoded byte buffers are cleared; caller-owned strings cannot be wiped.
- Basic password hashing/verification and contextual counterparts accepting a secret/pepper and associated data without retaining those values in the hasher instance.
- Type- and version-agile verification driven by the encoded hash, rather than the hasher's generation variant, while still applying the configured verification policy.
- Exception-less verification results distinguishing match, mismatch, malformed/unsupported hash, and policy rejection; unexpected runtime failures still propagate.
- A successful verification result also reports whether the hash needs rehashing, avoiding a second parse.
- Exact rehash checks across variant, version, memory, passes, lanes, salt length, and tag length against the hasher's generation configuration.
- Retrieval of an independent options snapshot for inspection without exposing mutable internal state.

### Security, memory, concurrency, and portability

- Fixed-time raw-tag and decoded-tag comparison with `CryptographicOperations.FixedTimeEquals`.
- Dedicated managed workspace chunks so RFC profiles are not forced into one oversized CLR array and large allocations stay within managed array length limits where process memory permits.
- Bounded parallel lane execution synchronized after every slice; sequential execution remains available through `MaximumThreads = 1` and produces identical output.
- Clearing of password-derived temporary buffers, H0, address blocks, intermediate hashes, final-block temporaries, and the entire memory matrix by default.
- Disposable internal workspace/secret-buffer classes used with permitted `using` statements so clearing occurs on normal completion and exceptions without forbidden `try`/`finally` syntax.
- Caller-owned input and destination buffers are never silently modified; callers retain responsibility for clearing their password/key arrays after use.
- No pooling of uncleared cryptographic workspace. Any use of built-in pooling must clear the complete rented region before return and on rent before use.
- Thread-safe, reusable, immutable-after-construction public implementation objects; all per-call mutable state stays local.
- Managed, safe, architecture-neutral implementation for the project's .NET target, with explicit little-endian reads/writes rather than host-endian assumptions.
- Exceptions for invalid caller parameters and malformed encoded hashes; mismatch and other routine verification outcomes use result values.

## Proposed public API and files

The signatures below are directional rather than fine-grained commitments. Final signatures must retain unique method names because overloads are prohibited.

### Enums: `src/Api/Enums`

- `Argon2Variant.cs`: `Argon2Variant` with `D = 0`, `I = 1`, and `Id = 2`.
- `Argon2Version.cs`: `Argon2Version` with `Version10 = 0x10` and `Version13 = 0x13`.
- `Argon2DecodeStatus.cs`: decode outcomes such as success, invalid format/base64/parameters, unsupported variant, and unsupported version.
- `Argon2VerificationStatus.cs`: match, mismatch, invalid hash, and rejected-by-policy outcomes.

### Data models: `src/Api/Data`

- `Argon2Parameters.cs`: variant, version, iterations, memory KiB, and degree of parallelism.
- `Argon2ExecutionOptions.cs`: maximum worker count and clear-memory choice.
- `Argon2VerificationPolicy.cs`: untrusted-hash maxima and allowed variant/version switches.
- `Argon2PasswordHasherOptions.cs`: generation parameters, salt/tag lengths, execution options, and verification policy; defaults initialize the RFC low-memory profile.
- `Argon2EncodedHash.cs`: decoded parameters, salt bytes, and tag bytes.
- `Argon2DecodeResult.cs`: decode status and nullable decoded-hash value.
- `Argon2VerificationResult.cs`: verification status and needs-rehash flag.

Each data model will have public fields, at most one constructor, no public methods, and defensive copying at implementation boundaries where state is retained.

### Static data: `src/Api/Static`

- `Argon2Limits.cs`: block/slice constants and public RFC/interoperability bounds using only const or static readonly fields.
- `Argon2Profiles.cs`: scalar constants for the RFC 9106 high-memory and low-memory profile values; no methods and no shared mutable options objects.

### Exceptions: `src/Api/Exceptions`

- `Argon2ParameterException.cs`: invalid caller configuration/input combination.
- `Argon2FormatException.cs`: malformed or unsupported PHC input for the throwing decode API.

Both exception classes derive directly from `System.Exception` and use their one permitted constructor.

### Operation contracts: `src/Api/Contracts`

- `IArgon2Primitive.cs`: `ComputeHash` into a destination span and `VerifyHash` against a raw tag, both accepting all RFC inputs, parameters, and execution options.
- `IArgon2KeyDeriver.cs`: allocating `DeriveKey` using all RFC inputs, parameters, execution options, and an explicit output length.
- `IArgon2EncodedHashCodec.cs`: uniquely named `EncodeHash`, `DecodeHash`, `DecodeHashWithoutException`, and `GetEncodedHashLength` operations.
- `IArgon2PasswordHasher.cs`: uniquely named byte/text hash and verify methods, contextual byte/text variants for secret/associated data, `NeedsRehash`, and `GetHashingOptions`.

### Public implementations: `src/Api/Classes`

- `Argon2Primitive.cs`: stateless implementation of `IArgon2Primitive` with one constructor.
- `Argon2KeyDeriver.cs`: implementation of `IArgon2KeyDeriver` with one constructor.
- `Argon2EncodedHashCodec.cs`: implementation of `IArgon2EncodedHashCodec` with one constructor.
- `Argon2PasswordHasher.cs`: validated configuration snapshot implementing `IArgon2PasswordHasher` with one options constructor.

### Internal implementation files outside `src/Api`

- `src/Internal/Cryptography/ModularArithmetic.cs`: overflow-check-safe modular 64-bit addition and rotation helpers.
- `src/Internal/Cryptography/Blake2b.cs`: RFC 7693 BLAKE2b state and compression.
- `src/Internal/Cryptography/Blake2bLong.cs`: Argon2 H-prime.
- `src/Internal/Algorithm/Argon2ParameterValidator.cs`: range, geometry, policy, and conversion validation.
- `src/Internal/Algorithm/Argon2InitialHash.cs`: H0 serialization and first-block expansion.
- `src/Internal/Algorithm/Argon2Compression.cs`: BlaMka round/permutation and block compression.
- `src/Internal/Algorithm/Argon2Indexing.cs`: address blocks, J1/J2 mapping, and variant addressing decisions.
- `src/Internal/Algorithm/Argon2Memory.cs`: chunked memory matrix and block access.
- `src/Internal/Algorithm/Argon2Workspace.cs`: disposable per-call buffers and guaranteed clearing.
- `src/Internal/Algorithm/Argon2LaneFiller.cs`: segment and pass filling, including version rules.
- `src/Internal/Algorithm/Argon2Core.cs`: algorithm orchestration and final tag production.
- `src/Internal/Algorithm/Argon2Executor.cs`: bounded slice-synchronized worker execution.
- `src/Internal/Encoding/Argon2Base64.cs`: canonical unpadded PHC base64.
- `src/Internal/Encoding/Argon2PhcParser.cs`: allocation-bounded strict parser.
- `src/Internal/Encoding/Argon2PhcFormatter.cs`: canonical formatting and length calculation.
- `src/Internal/Security/SecureBuffer.cs`: disposable byte storage and zeroing.
- `src/Internal/Text/Utf8PasswordEncoding.cs`: UTF-8 conversion into clearable temporary storage.

No public type will be declared outside `src/Api`, and no non-public helper will be declared within it.

## Planned test files

- `tests/Api/Argon2StaticDataTests.cs`
- `tests/Api/Argon2DataModelTests.cs`
- `tests/Api/Argon2PrimitiveTests.cs`
- `tests/Api/Argon2KeyDeriverTests.cs`
- `tests/Api/Argon2EncodedHashCodecTests.cs`
- `tests/Api/Argon2PasswordHasherTests.cs`
- `tests/Api/Argon2VerificationPolicyTests.cs`
- `tests/Api/Argon2ExceptionTests.cs`
- `tests/Algorithm/Blake2bTests.cs`
- `tests/Algorithm/Blake2bLongTests.cs`
- `tests/Algorithm/Argon2ParameterValidationTests.cs`
- `tests/Algorithm/Argon2Rfc9106VectorTests.cs`
- `tests/Algorithm/Argon2Version10VectorTests.cs`
- `tests/Algorithm/Argon2MemoryGeometryTests.cs`
- `tests/Algorithm/Argon2ParallelExecutionTests.cs`
- `tests/Encoding/Argon2Base64Tests.cs`
- `tests/Encoding/Argon2PhcMalformedInputTests.cs`
- `tests/Security/Argon2MemoryClearingTests.cs`
- `tests/Concurrency/Argon2ThreadSafetyTests.cs`
- `tests/Interoperability/Argon2ReferenceLibraryTests.cs`
- `tests/TestData/` for pinned RFC and independently generated interoperability fixtures.

## Implementation tasks

- [DONE] T001: Create the public enum files for variants, versions, decode statuses, and verification statuses with stable numeric values and usages in public data/contracts.
- [DONE] T002: Create `Argon2Limits` and `Argon2Profiles` as field-only static data classes containing RFC bounds, structural constants, and both RFC 9106 recommended profiles.
- [DONE] T003: Create the seven public data-model classes with public fields, single constructors where needed, secure defaults, and no public behavior.
- [DONE] T004: Create `Argon2ParameterException` and `Argon2FormatException`, each deriving directly from `System.Exception` with one constructor.
- [DONE] T005: Define the four public operation interfaces with unique instance method names and span-oriented low-level signatures.
- [DONE] T006: Implement bitwise modular 64-bit addition and rotation helpers that preserve cryptographic wraparound while compiling under overflow checking without `unchecked` syntax.
- [DONE] T007: Implement BLAKE2b initialization, block absorption, compression, counters, finalization, little-endian conversion, and variable digest lengths using only built-in .NET APIs.
- [DONE] T008: Implement Argon2 H-prime for short and long outputs, including output-length prefixing, iterative 64-byte chaining, final partial digest handling, and temporary-buffer clearing.
- [DONE] T009: Implement centralized validation for variants, versions, RFC numeric ranges, salt/tag sizes, memory-to-lane geometry, worker limits, span lengths, and overflow-safe managed allocation calculations.
- [DONE] T010: Implement H0 serialization in the exact RFC order, including every length prefix and optional secret/associated-data input, without concatenating all inputs into an unnecessary long-lived secret array.
- [DONE] T011: Implement expansion of H0 into the first two 1024-byte blocks of every lane with correct lane/block LE32 suffixes.
- [DONE] T012: Implement the BlaMka operation, BLAKE2-derived round, row/column permutation, block XOR, and Argon2 compression function.
- [DONE] T013: Implement chunked Argon2 memory storage with overflow-safe block addressing, lane/segment geometry, no dependence on host endianness, and support for large profiles without one oversized CLR array.
- [DONE] T014: Implement disposable workspace and secure-buffer helpers that clear complete allocated regions and cryptographic temporaries on disposal via allowed `using` flows.
- [DONE] T015: Implement RFC J1/J2 mapping for first/later passes, first/later slices, same/cross-lane references, and the nonuniform reference-area calculation.
- [DONE] T016: Implement Argon2i data-independent address-block generation and counter progression.
- [DONE] T017: Implement the Argon2d, Argon2i, and Argon2id addressing schedules, including Argon2id's data-independent first half of the first pass.
- [DONE] T018: Implement lane segment filling, previous-block wrapping, memory normalization, synchronization boundaries, and version 1.0 versus 1.3 overwrite/XOR behavior.
- [DONE] T019: Implement final-lane-block XOR, little-endian serialization, H-prime tag generation, and clearing of finalization temporaries.
- [DONE] T020: Implement bounded parallel execution across lanes with a barrier after every slice, an execution-only maximum-worker setting, deterministic exception propagation, and identical single/multi-worker results.
- [DONE] T021: Assemble the internal Argon2 core orchestration from validation through allocation, initialization, passes, finalization, and guaranteed workspace disposal.
- [DONE] T022: Implement the public `Argon2Primitive` class for destination-span hashing and fixed-time raw verification across every variant/version/context combination.
- [DONE] T023: Implement the public `Argon2KeyDeriver` allocating convenience class, enforcing one-shot Argon2 output semantics and exact requested key length.
- [DONE] T024: Implement canonical unpadded PHC base64 encoding/decoding with strict alphabet, length, leftover-bit, padding, whitespace, and destination-bound checks.
- [DONE] T025: Implement the strict allocation-bounded PHC parser for all variants, explicit versions 16/19, omitted-version legacy strings, canonical decimal fields, salts, tags, and trailing-data rejection.
- [DONE] T026: Implement canonical PHC formatting and exact encoded-length calculation using the original requested costs and explicit version field.
- [DONE] T027: Implement the public `Argon2EncodedHashCodec` throwing and exception-less decode paths, encode path, decoded metadata, and length operation.
- [DONE] T028: Implement secure random salt generation and clearable UTF-8 conversion helpers, with no Unicode normalization and no modification of caller-owned inputs.
- [DONE] T029: Implement `Argon2PasswordHasher` construction with deep-copied validated options, default RFC low-memory settings, thread-safe retained state, and independent options snapshots.
- [DONE] T030: Implement high-level byte and UTF-8 password hashing with fresh random salt, canonical encoded output, and contextual secret/associated-data variants.
- [DONE] T031: Implement high-level type/version-agile byte and UTF-8 verification with fixed-time comparison and exception-less match/mismatch/invalid/policy-rejected results.
- [DONE] T032: Implement verification-policy checks before workspace allocation, covering all configured maxima and allowed variant/version switches, and ensure the default policy accepts hashes generated by the same hasher.
- [DONE] T033: Implement exact rehash detection and the combined successful-verification `NeedsRehash` result across variant, version, all costs, salt length, and tag length.
- [DONE] T034: Add RFC 7693 BLAKE2b tests for empty, short, exact-block, multi-block, keyed-reference, and every supported digest-length boundary relevant to Argon2.
- [DONE] T035: Add H-prime tests for 4, 32, 64, 65, 96, 1024, non-multiple tail, and invalid output lengths against independent reference fixtures.
- [DONE] T036: Add exhaustive public parameter-validation tests for each minimum, maximum representable boundary, just-outside value, invalid enum, memory/lane relationship, worker constraint, output span, and overflow path without attempting impossible allocations.
- [DONE] T037: Add RFC 9106 end-to-end tests for Argon2d, Argon2i, and Argon2id including password, salt, secret, associated data, H0, selected intermediate blocks, and final tags.
- [DONE] T038: Add pinned `libargon2` version 1.0 vectors for all variants and tests proving omitted-version PHC hashes decode and verify as version 1.0.
- [DONE] T039: Add memory-geometry tests for exact and rounded-down memory values, minimum `8 * lanes`, multiple lane/slice layouts, previous-block wrapping, and preservation of requested memory in H0/PHC metadata.
- [DONE] T040: Add execution-control tests proving maximum worker counts from one through lane count never change output and slice dependencies remain deterministic.
- [DONE] T041: Add complete `IArgon2Primitive` API tests for all variants/versions, empty and binary passwords, optional empty/nonempty context, destination lengths, raw match/mismatch, caller-buffer preservation, and repeated calls.
- [DONE] T042: Add complete `IArgon2KeyDeriver` API tests for output-length boundaries, binary inputs, secret/associated data, deterministic repeated derivation, parameter changes, and equivalence with the destination primitive.
- [DONE] T043: Add PHC base64 unit tests for all byte-length remainders, round trips, canonical trailing bits, forbidden padding/whitespace/characters, and destination limits.
- [DONE] T044: Add complete codec API tests for every variant/version, exact output strings and lengths, round trips, legacy version omission, decoded metadata, throwing failures, and every exception-less decode status.
- [DONE] T045: Add table-driven and generated malformed-PHC tests covering missing/extra fields, wrong order/names, duplicates, non-ASCII, signs, leading zeros, numeric overflow, invalid base64 lengths/bits, short salt/tag, unknown variant/version, empty components, and trailing data.
- [DONE] T046: Add complete password-hasher API tests for default/profile/custom options, independent option snapshots, random salt uniqueness, canonical output, byte inputs, empty passwords, UTF-8/non-ASCII behavior, and caller-buffer preservation.
- [DONE] T047: Add password verification tests for every variant and supported version, correct/wrong passwords, byte/text parity, contextual secret/associated-data success and failure, malformed hashes, and combined needs-rehash results.
- [DONE] T048: Add verification-policy tests proving oversized encoded length/cost/salt/tag/lane inputs and disallowed variants/versions are rejected before core execution, while boundary values and the hasher's own output are accepted.
- [DONE] T049: Add rehash tests changing each generation field independently and together, including variant, version, passes, memory, lanes, salt length, and tag length.
- [DONE] T050: Add public exception tests that distinguish invalid caller parameters and throwing decode failures while confirming routine verification mismatch/malformed/policy outcomes remain result values.
- [DONE] T051: Add internal clearing tests for workspace chunks, H0, address blocks, UTF-8 temporary buffers, and failure disposal paths using the existing internals visibility, without timing assertions.
- [DONE] T052: Add concurrency tests that reuse every public implementation instance from multiple threads and verify deterministic results, unique random salts, isolated per-call context, and no retained mutable-call state.
- [DONE] T053: Add pinned cross-library interoperability fixtures generated independently by `libargon2`, `argon2-cffi`, `argon2-jvm`, and `Konscious.Security.Cryptography.Argon2`, then test raw tags, encoded hashes, both versions where supported, all variants, and nondefault costs/lengths.
- [DONE] T054: Exercise every public constructor, field-driven option, interface method, implementation method, enum outcome, normal path, boundary, and documented error outcome under `src/Api`, and close any uncovered API behavior with focused tests.
- [DONE] T055: Run the complete build and functional test suite in clean and repeated runs, resolve warnings/errors/flaky functional assertions, and confirm `src/` has no non-framework dependency while `tests/` remains the only location using external oracle packages.
