# Universal HTTP cookies development plan

## Goal and scope

Build a transport-neutral C# library that can parse and serialize `Set-Cookie` response fields and `Cookie` request fields, normalize and validate cookies, store them in a concurrency-safe jar, enforce origin and policy rules, select and order cookies for requests, and import/export jar state. RFC 6265 is the required baseline. Current 6265bis behavior is offered through an explicit modern compliance mode because draft revision 22 was in the RFC Editor queue, but was not yet a published RFC, when this plan was written.

Production code under `src/` will use only built-in .NET APIs. In particular, host conversion can use `System.Globalization.IdnMapping`, URIs can use `System.Uri`, time can use `System.DateTimeOffset`, and persistence can use built-in text/JSON APIs. Span-based internal scanners are preferred where they simplify bounded parsing without allocating substrings. External packages and upstream vectors may be used only by `tests/`.

The library is an HTTP state engine, not an HTTP client. It accepts `Uri`, header strings, and explicit request context rather than deriving from `HttpMessageHandler`, which would violate the allowed inheritance subset. It will not implement obsolete RFC 2109, RFC 2965, `Set-Cookie2`, or `Cookie2`; unfamiliar legacy attributes remain observable as extensions. It will not bundle or update a public-suffix database in the production assembly; applications supply one through a hook.

## Standards and research basis

The downloaded sources and their provenance are indexed in [`specs/SOURCES.md`](specs/SOURCES.md).

- RFC 6265 supplies the normative baseline: liberal user-agent parsing, cookie-date parsing, canonical hosts, domain/path matching, storage identity and replacement, expiry, request filtering, ordering, and minimum capacity expectations.
- RFC 6265 errata and 6265bis revision 22 resolve grammar ambiguities and add explicit octet/attribute limits, a recommended 400-day lifetime cap, SameSite, prefix enforcement, secure-cookie overlay protection, current public-suffix handling, non-HTTP API rules, and modern retrieval behavior.
- RFC 9110 supplies safe-method classification and HTTP field/date context; RFC 9113/9114 cover Cookie field splitting in HTTP/2 and HTTP/3; RFC 3986 supplies URI host/path semantics; RFC 6761 and Secure Contexts inform explicit localhost/trust policy; the IDNA and Punycode RFCs guide canonical host handling.
- tough-cookie demonstrates useful separation between low-level parsing/matching, a high-level jar, a pluggable store, persistence, compatibility parsing, SameSite context, prefix modes, and potentially trustworthy localhost handling.
- Python `http.cookiejar` demonstrates independent acceptance/return policy, domain allow/block controls, session clearing, persistence hooks, and Netscape cookie-file compatibility. Its RFC 2109/2965 behavior is intentionally outside this library's production scope.
- Go's `net/http` and `net/http/cookiejar` demonstrate a compact transport-neutral contract, injectable public-suffix logic, stable ordering, Max-Age precedence, host/IP handling, replacement semantics, and concurrency safety.
- curl's cookie engine demonstrates robust malformed-input handling, Netscape-file interoperability including `#HttpOnly_`, public-suffix rejection, prefix enforcement, localhost secure treatment, secure-cookie overlay protection, expiry caps, and session-cookie controls.

## Compliance profiles

- `Rfc6265`: implement RFC 6265 exactly where it gives a user-agent algorithm. Parse SameSite, prefixes, Partitioned, and unknown attributes for observability, but do not apply post-6265 enforcement unless separately enabled.
- `Rfc6265Bis`: implement the revision-22 algorithms, including modern size limits, 400-day cap, SameSite default handling, `SameSite=None` requiring Secure, case-insensitive prefix enforcement, secure-overlay protection, and potentially trustworthy secure origins.
- `Compatible`: use the modern safety model while accepting common deployed deviations such as an optionally quoted cookie value, tolerant whitespace, a leading dot on Domain, and noncanonical but parseable cookie dates. Every recovery produces an issue code; control characters and header injection are never recovered.
- Modern extensions are individual switches. `Partitioned`/CHIPS is opt-in because its downloaded specification is informational and expired. Unknown attributes are preserved without inventing semantics.

## Complete feature inventory

### 1. Wire-level parsing and validation

- Parse one `Set-Cookie` field value at a time; never comma-split combined values because Expires contains commas and RFC 6265 does not define a safe combined representation.
- Provide throwing and exception-less parse entry points. Expected malformed input is represented by a result containing success, parsed value, issues, source offsets, and a stable error code.
- Implement the strict cookie-producing grammar and the liberal cookie-consuming algorithm as distinct behaviors.
- Scan cookie name/value and semicolon-delimited attributes with bounded `ReadOnlySpan<char>` helpers where appropriate.
- Handle empty values, RFC 6265 rejection of empty names, the modern consuming algorithm's carefully constrained empty-name form, optional whitespace, equals signs after the first name/value delimiter, and quoted values only according to the selected profile.
- Reject NUL, C0 controls other than permitted whitespace, DEL, CR/LF injection, invalid token characters in strict names, invalid cookie octets in strict values, and name-plus-value lengths over the configured limit.
- Recognize Expires, Max-Age, Domain, Path, Secure, HttpOnly, SameSite, and Partitioned case-insensitively; preserve original unknown extension attributes and their ordering for inspection or compatible round trips.
- Apply RFC duplicate-attribute behavior deterministically: the last recognized Domain, Path, Max-Age, SameSite, and Expires value participates in storage; repeated flags collapse to true; all raw occurrences can remain in diagnostics.
- Ignore a malformed recognized attribute when the RFC algorithm says to ignore the attribute without rejecting the whole cookie.
- Enforce per-attribute limits, including the modern 1024-octet attribute-value limit, without unbounded allocation.
- Parse `Cookie` request fields as ordered name/value pairs, preserving duplicate names and empty values rather than collapsing them into a dictionary.
- Support multiple `Cookie` field values by parsing each field and concatenating pairs in field order, while reporting malformed segments independently.
- Return diagnostics for ignored segments, unknown attributes, duplicate recognized attributes, compatibility recoveries, truncation/limit failures, and lossy imports.

### 2. Cookie date and lifetime handling

- Implement the RFC 6265 token-based cookie-date algorithm rather than relying on culture-sensitive `DateTime.Parse`.
- Recognize time, day, month names, two- and four-digit years, delimiter classes, the 1601 lower bound, and the specified 69/70 two-digit-year mapping to 2069/1970.
- Validate real calendar dates, leap years, day/month bounds, hour/minute/second bounds, and UTC conversion.
- Serialize Expires in invariant IMF-fixdate form using English month/day names and `GMT`.
- Represent optional value-type data with an explicit `Has...` boolean plus the value field because nullable value types are forbidden by `syntax.md`.
- Give Max-Age precedence over Expires, treat zero or negative Max-Age as immediate deletion, and distinguish session cookies from persistent cookies.
- Saturate arithmetic safely at supported `DateTimeOffset` bounds, handle very large signed decimal Max-Age values without overflow, and avoid Y2038 assumptions.
- Apply the configurable modern lifetime cap, defaulting to 400 days in the modern profile, to both Expires and Max-Age.
- Treat expiry at or before the evaluation clock as expired, purge expired entries on insertion, lookup, explicit maintenance, import, and quota enforcement.
- Support explicit removal of session cookies at the end of a user session.
- Inject a clock so all acceptance, grace-period, access-time, eviction, and expiry behavior is deterministic in tests.

### 3. Host canonicalization and domain rules

- Extract hosts from absolute HTTP/HTTPS URIs, discard ports correctly (including bracketed IPv6), remove a single terminal DNS root dot for request-host canonicalization, and reject absent or malformed hosts.
- Lowercase using ASCII rules after IDNA conversion; convert valid U-labels to A-labels with built-in `IdnMapping`; reject fake/invalid A-labels and invalid label/domain lengths.
- Distinguish DNS names, IPv4 literals, and IPv6 literals. Accept an explicitly supplied Domain for an IP only when it is exactly the same canonical IP; preserve whether Domain was supplied, apply the selected profile's host-only normalization, and never apply suffix domain matching to an IP address.
- Ignore one leading dot on a Domain attribute as RFC 6265 requires; reject an empty Domain, multiple leading dots, a trailing dot, illegal characters, and domains that do not domain-match the origin host.
- Implement exact domain matching and dot-boundary suffix matching with ASCII case-insensitivity after canonicalization.
- Set and retain the host-only flag when Domain is absent, and use exact matching for host-only cookies.
- Expose a public-suffix provider contract for `GetPublicSuffix`, `GetRegistrableDomain`, and `IsPublicSuffix`; require providers to accept canonical ASCII domains and be concurrency-safe.
- Make absence of a PSL provider an explicit policy choice: fail closed when PSL protection is required, or run in an intentionally insecure/testing mode with a result warning.
- Reject supercookies for public suffixes, including PSL wildcard and exception outcomes supplied by the provider. Preserve the RFC exception that a Domain equal to the origin host can become host-only where applicable.
- Recheck the current PSL during retrieval and import so a cookie is withheld if a later list revision makes its domain a public suffix.
- Handle single-label and special-use hosts (especially `localhost`) through explicit secure-origin and PSL policy rather than hidden hard-coding in the matcher.

### 4. Path computation and matching

- Compute the RFC default path from the origin URI path: `/` for empty, non-slash, or one-slash paths; otherwise the substring through the right-most slash excluding that slash.
- Ignore an invalid/empty Path attribute that does not begin with `/` and substitute the computed default path.
- Implement exact path matching and prefix matching with the required slash boundary, preventing `/foo` from matching `/foobar` while allowing `/foo`, `/foo/`, and `/foo/bar`.
- Use the escaped URI path representation consistently and do not URL-decode before matching.
- Treat an empty request path as `/` during retrieval.

### 5. Security and privacy attributes

- Store and serialize Secure and HttpOnly flags independently.
- Accept Secure cookies only from origins considered secure by the injected secure-origin policy; send them only to secure origins.
- Supply a default secure-origin policy for HTTPS and WSS, with a configurable modern exception for loopback/`localhost`, while allowing applications to replace it.
- Include an access-kind field (`Http` or `NonHttp`) in set and retrieval contexts. Reject HttpOnly cookies set by a non-HTTP API, prevent a non-HTTP API from overwriting an existing HttpOnly cookie, and exclude HttpOnly cookies from non-HTTP retrieval.
- Prevent a cookie set over an insecure origin from overlaying an existing Secure cookie with the same name when their domains and paths overlap under the 6265bis algorithm.
- Parse SameSite values Strict, Lax, None, and Default/unknown; preserve the difference between absent/invalid Default and an explicit value.
- Determine same-site versus cross-site from explicit request context and a schemeful-site calculator based on scheme plus registrable domain. The caller supplies browsing-context facts the HTTP library cannot infer.
- Enforce Strict only for same-site retrieval and creation contexts.
- Enforce Lax for same-site requests and eligible cross-site top-level safe-method navigations; use RFC 9110 safe methods (GET, HEAD, OPTIONS, TRACE).
- Support modern Default enforcement and optional short-lived Lax-allowing-unsafe behavior using the injected clock and configurable grace interval.
- Require Secure for `SameSite=None` in modern mode.
- Carry top-level-navigation, reload, redirect-chain/cross-site, access kind, request method, and user-activation facts in request context so SameSite decisions are explicit and testable.
- Enforce `__Secure-` and `__Host-` rules according to profile and prefix policy. `__Secure-` requires Secure and a secure origin; `__Host-` additionally requires an explicitly supplied `Path=/` and no Domain.
- Offer prefix modes that silently reject, reject with an error result, or deliberately disable enforcement; never silently accept a malformed prefixed cookie in an enforcing mode.
- Match prefixes case-insensitively in the modern profile as required by 6265bis revision 22, while retaining the baseline/compatibility behavior as an explicit policy distinction.
- Parse and serialize `Partitioned`; when enabled, require Secure, require a top-level site/partition key at creation, include that key in storage identity, and send only within the same partition.
- Preserve unrecognized security/privacy attributes such as `Priority` as extensions without claiming enforcement.

### 6. Serialization

- Validate before serialization and throw a dedicated serialization exception for programmer-supplied invalid models; also expose validation issues through non-throwing parse/import paths.
- Serialize one Set-Cookie value with deterministic attribute spelling and ordering, invariant Expires formatting, decimal Max-Age, optional Domain/Path, flags, SameSite, Partitioned, and safe extension attributes.
- Never emit CR, LF, NUL, invalid name/value octets, invalid extension delimiters, or a folded/combined Set-Cookie field.
- Preserve an allowed quoted value only when requested and valid; otherwise emit the canonical unquoted RFC representation.
- Serialize a Cookie request value as ordered `name=value` pairs joined with `; `, retaining duplicate names and omitting Set-Cookie attributes.
- Serialize selected stored cookies in RFC order: descending path length, then ascending creation time, then a stable sequence number for equal timestamps.
- Support an optional configured request-header limit by returning multiple Cookie field values split only between complete pairs. Report a single pair that exceeds the limit rather than truncating it.
- Define round-trip expectations explicitly: semantic round trip for recognized fields, best-effort raw preservation for safe unknown extensions, and diagnostics for any normalization.

### 7. Storage model and cookie jar

- Store the complete RFC state: name, value, canonical domain, path, expiry time, creation time, last-access time, persistent, host-only, Secure, HttpOnly, SameSite, optional partition key, quoted-value hint, safe extensions, and stable sequence number.
- Define cookie identity as name + canonical domain + path + partition key. Ports and schemes are not part of identity.
- Replace an existing identity atomically while preserving its original creation time and stable sequence number; refresh value, attributes, expiry, and last-access time.
- Treat an already-expired accepted cookie as a deletion of the matching identity, whether or not the identity currently exists.
- Provide a synchronous, pluggable store contract with exact lookup, candidate query, atomic upsert/delete, filtered removal, complete snapshot, and snapshot replacement operations.
- Require store implementations and public implementation services to be safe for concurrent callers. The in-memory store uses `lock`, never returns mutable internal instances, and applies defensive copies at API boundaries.
- Index memory storage so domain/partition candidate lookup does not require exposing or mutating the entire collection; correctness takes precedence over a particular index shape.
- Ingest a parsed SetCookie model or one/many raw Set-Cookie field values. Batch ingestion returns an ordered result per field and does not roll back valid independent fields when another field is malformed.
- Return explicit Accepted, Replaced, Deleted, Ignored, and Rejected outcomes with stable reason codes.
- Apply acceptance in RFC order: syntax/limits, origin and canonical host, expiry, Domain/PSL, Path, Secure/HttpOnly, overlay defense, SameSite, prefixes, partitioning, replacement/deletion, then quota eviction.
- Select cookies by partition, host-only/domain match, current PSL, path match, expiry, Secure, HttpOnly access kind, SameSite, and application policy.
- Update selected cookies' last-access time atomically and return defensive copies.
- Allow an injected policy to add domain allow/block rules and application-specific acceptance/return decisions without weakening mandatory structural and header-injection checks.
- Expose query/removal controls for all cookies, an exact identity, domain, domain-and-subdomains, path, name, partition, expired cookies, and session cookies.
- Provide count/snapshot operations for management UIs without exposing internal store objects.
- Enforce configurable limits for name+value, each attribute, one input line, per-domain count, total count, maximum returned header size, and lifetime.
- Use at least RFC-compatible defaults: 4096 octets per name+value, 50 cookies per domain, and 3000 total. Permit safer/higher application settings but reject nonsensical negative limits.
- Evict deterministically in the modern RFC order: expired first; then oldest-accessed non-Secure cookies in an overfull domain; then oldest-accessed cookies in an overfull domain; then oldest-accessed cookies globally, with stable sequence as a tie-breaker.
- Keep parsing, policy, storage, and selection transport-neutral so redirects and response processing can call the jar explicitly for every HTTP exchange.

### 8. Persistence and interoperability

- Provide a versioned, lossless jar snapshot model and codec covering all stored fields, stable order, optional partition keys, extensions, and configuration-independent timestamps.
- Encode timestamps and integer values culture-invariantly; validate snapshot version, required fields, uniqueness, canonical domains, valid paths, expiry ranges, and size/count limits on import.
- Re-run mandatory security, public-suffix, expiry, and quota validation when importing untrusted snapshots; never restore mutable instances directly into a live store.
- Support replace and merge import modes with deterministic identity conflict handling and detailed per-entry outcomes.
- Provide a Netscape/Mozilla/curl cookie-file codec for tab-separated domain, include-subdomains, path, Secure, epoch expiry, name, and value fields.
- Recognize and emit curl's `#HttpOnly_` domain prefix, comments, session expiry conventions, and options to include/exclude session and expired cookies.
- Treat Netscape format as deliberately lossy for SameSite, Partitioned, creation/access time, and unknown extensions; report each loss and never merge partitioned cookies ambiguously.
- Keep filesystem I/O outside the codec API: callers read/write strings or streams using built-in .NET facilities and can choose their own atomicity and permissions policy.

### 9. Failure behavior and observability

- Use result objects for frequent input/policy failures and dedicated exceptions for invalid programmer models, store failures, and corrupt snapshot operations that use throwing entry points.
- Give every rejection/ignore/recovery a stable enum reason rather than requiring callers to parse messages.
- Include safe source offsets and short fragments in parse issues without reflecting control characters unsafely.
- Distinguish malformed syntax, unsupported scheme, invalid host/IDNA, domain mismatch, public suffix, invalid path, insecure Secure cookie, HttpOnly API violation, SameSite violation, prefix violation, missing partition context, expired deletion, quota eviction, and policy rejection.
- Do not log or globally publish cookie values; all diagnostics are returned to the caller, who controls handling of sensitive data.

## Proposed public API

Names are provisional, but category, responsibility, and dependency direction are fixed. Methods listed below are intentionally coarse and use unique names; there will be no overloads, properties, generic declarations, delegates, nullable value types, public static methods, or implementation-only public members.

### Data model (`src/Api/Data`)

| Type/file | Purpose |
|---|---|
| `CookiePair.cs` | Ordered Cookie-header name/value pair and quoted-value hint. |
| `CookieAttribute.cs` | Safe raw extension attribute name/value/presence. |
| `SetCookie.cs` | Parsed or producer-supplied Set-Cookie representation with explicit `HasExpires`/`HasMaxAge` fields. |
| `StoredCookie.cs` | Canonical cookie-jar state including timestamps, flags, sequence, and optional partition key. |
| `CookieIdentity.cs` | Name/domain/path/partition lookup key. |
| `CookieParseOptions.cs` | Compliance mode, limits, unknown-attribute and recovery choices. |
| `CookieSerializationOptions.cs` | Canonical/compatible output and header-splitting choices. |
| `CookieLimits.cs` | All size, count, lifetime, and header limits. |
| `CookieJarOptions.cs` | Compliance, SameSite, prefix, PSL, partition, localhost, and eviction policy. |
| `CookieSetContext.cs` | Origin URI, access kind, site relation/top-level site, and set-time facts. |
| `CookieRequestContext.cs` | Request URI/method, access kind, site relation, navigation, redirect, reload, activation, and partition facts. |
| `CookieStoreQuery.cs` | Candidate and management query criteria. |
| `CookieRemovalQuery.cs` | Exact/domain/path/name/partition/session/expired removal criteria. |
| `CookieIssue.cs` | Stable issue code, severity, offset, and safe fragment. |
| `SetCookieParseResult.cs` | Exception-less Set-Cookie parse result. |
| `CookieHeaderParseResult.cs` | Exception-less ordered Cookie-header parse result. |
| `CookieSetDecision.cs` | Policy acceptance/rejection and normalized stored candidate. |
| `CookieSetResult.cs` | One jar mutation outcome. |
| `CookieBatchSetResult.cs` | Ordered outcomes for independent header values. |
| `CookieMatchResult.cs` | Send/exclude decision and reason. |
| `CookieSelectionResult.cs` | Ordered stored cookies, pairs, and one/many field values. |
| `CookieJarSnapshot.cs` | Versioned lossless store snapshot. |
| `CookieSnapshotParseResult.cs` | Exception-less snapshot decode result. |
| `CookieImportResult.cs` | Merge/replace counts and per-entry issues. |
| `NetscapeCookieFileOptions.cs` | Lossy-file inclusion/import policy. |
| `NetscapeCookieFileParseResult.cs` | Parsed entries and loss/malformed-line issues. |

Every data-holding class has public instance fields, no public methods, and is used by a contract or another data class.

### Operation contracts (`src/Api/Contracts`)

| Interface/file | Planned operation names |
|---|---|
| `ICookieParser.cs` | `ParseSetCookie`, `TryParseSetCookie`, `ParseCookieFields`, `TryParseCookieFields`. |
| `ICookieSerializer.cs` | `SerializeSetCookie`, `SerializeCookiePairs`, `SerializeCookieFields`. |
| `ICookieMatcher.cs` | `CanonicalizeHost`, `DomainMatches`, `ComputeDefaultPath`, `PathMatches`, `ComputeSchemefulSite`, `EvaluateCookieMatch`, `IsCookieExpired`. |
| `ICookiePolicy.cs` | `EvaluateCookieAcceptance`, `EvaluateCookieReturn`. |
| `ICookieStore.cs` | `FindStoredCookie`, `QueryStoredCookies`, `ApplyStoredCookie`, `RemoveStoredCookies`, `CaptureStoreSnapshot`, `ReplaceStoreSnapshot`. |
| `ICookieJar.cs` | `StoreSetCookieHeader`, `StoreSetCookieHeaders`, `StoreSetCookieModel`, `SelectRequestCookies`, `CreateRequestCookieFields`, `QueryJarCookies`, `RemoveJarCookies`, `PurgeExpiredCookies`, `ExportJarSnapshot`, `ImportJarSnapshot`. |
| `IPublicSuffixList.cs` | `GetPublicSuffix`, `GetRegistrableDomain`, `IsPublicSuffix`. |
| `ISecureOriginPolicy.cs` | `IsSecureOrigin`. |
| `ICookieClock.cs` | `GetUtcNow`. |
| `ICookieSnapshotCodec.cs` | `SerializeJarSnapshot`, `ParseJarSnapshot`, `TryParseJarSnapshot`. |
| `INetscapeCookieFileCodec.cs` | `SerializeNetscapeCookieFile`, `ParseNetscapeCookieFile`, `TryParseNetscapeCookieFile`. |

### Static data (`src/Api/Static`)

| Type/file | Purpose |
|---|---|
| `CookieDefaults.cs` | Permitted public constants/static readonly values for sizes, counts, lifetime, grace interval, and snapshot version; no methods. |
| `CookieAttributeNames.cs` | Canonical attribute and prefix strings; no methods. |

### Enums (`src/Api/Enums`)

| Type/file | Values represented |
|---|---|
| `CookieComplianceMode.cs` | RFC 6265, 6265bis, compatible. |
| `CookieSameSiteMode.cs` | Default, Lax, Strict, None. |
| `CookieSameSiteContext.cs` | Same-site, cross-site. |
| `CookieAccessKind.cs` | HTTP, non-HTTP. |
| `CookiePrefixPolicy.cs` | Silent reject, error reject, disabled. |
| `CookieParseSeverity.cs` | Information, warning, error. |
| `CookieErrorCode.cs` | Stable parse, policy, matching, persistence, and limit outcomes. |
| `CookieSetStatus.cs` | Accepted, replaced, deleted, ignored, rejected. |
| `CookieMatchStatus.cs` | Included, excluded. |
| `CookieImportMode.cs` | Merge, replace. |

### Exceptions (`src/Api/Exceptions`)

Each exception derives directly from `System.Exception`, has at most one constructor, and is used only by a throwing operation.

- `CookieParseException.cs`
- `CookieSerializationException.cs`
- `CookieStoreException.cs`
- `CookiePolicyException.cs`
- `CookieSnapshotException.cs`

### Public implementation classes (`src/Api/Classes`)

Each class has exactly one public constructor, implements its matching interface(s), and exposes no instance member beyond interface methods.

| Type/file | Implements |
|---|---|
| `CookieParser.cs` | `ICookieParser` |
| `CookieSerializer.cs` | `ICookieSerializer` |
| `CookieMatcher.cs` | `ICookieMatcher` |
| `DefaultCookiePolicy.cs` | `ICookiePolicy` |
| `MemoryCookieStore.cs` | `ICookieStore` |
| `CookieJar.cs` | `ICookieJar` |
| `DefaultSecureOriginPolicy.cs` | `ISecureOriginPolicy` |
| `SystemCookieClock.cs` | `ICookieClock` |
| `CookieSnapshotCodec.cs` | `ICookieSnapshotCodec` |
| `NetscapeCookieFileCodec.cs` | `INetscapeCookieFileCodec` |

No built-in `IPublicSuffixList` implementation is planned because a useful one requires externally maintained data. Tests will provide deterministic fake and PSL-snapshot-backed implementations under `tests/`.

## Planned internal source files

All types outside `src/Api` are non-public. The files still obey the restricted C# subset in `syntax.md`.

```text
src/
  Parsing/
    CookieSpanScanner.cs
    CookieDateParser.cs
    CookieHeaderParserCore.cs
    SetCookieParserCore.cs
    CookieCharacterRules.cs
    DecimalSecondsParser.cs
  Serialization/
    CookieHeaderSerializerCore.cs
    SetCookieSerializerCore.cs
    CookieDateFormatter.cs
    HeaderValueAccumulator.cs
  Matching/
    CookieHostCanonicalizer.cs
    CookieDomainMatcher.cs
    CookiePathMatcher.cs
    CookieSiteCalculator.cs
    CookieSameSiteEvaluator.cs
    CookieSecureOverlayMatcher.cs
  Policy/
    CookieAcceptanceAlgorithm.cs
    CookieRetrievalAlgorithm.cs
    CookiePrefixValidator.cs
    CookiePartitionValidator.cs
    CookieLimitValidator.cs
    CookieEvictionAlgorithm.cs
  Storage/
    CookieStoreKey.cs
    CookieStoreIndex.cs
    CookieCopy.cs
    CookieSequence.cs
  Persistence/
    CookieSnapshotReader.cs
    CookieSnapshotWriter.cs
    NetscapeCookieFileReader.cs
    NetscapeCookieFileWriter.cs
  Utility/
    CookieDateMath.cs
    CookieIssueFactory.cs
    CookieText.cs
```

## Testing approach and completion standard

- NUnit tests cover every public `src/Api` type, constructor, field-driven behavior, interface method, implementation method, success outcome, documented rejection reason, profile switch, and boundary branch.
- RFC algorithms receive table-driven examples at every step and boundary, including published RFC 6265 errata and changed 6265bis cases.
- Downloaded Go, Python, tough-cookie, curl, and WPT material is used to identify portable vectors and historical edge cases. Expected behavior follows the selected compliance profile, not another library blindly.
- Deterministic fake clocks, suffix lists, secure-origin policies, stores, and application policies live only under `tests/`.
- Parser robustness includes generated and randomized malformed strings, Unicode/IDNA cases, very large numeric values, embedded delimiters, and control characters. Tests assert bounded failure behavior and stable issue codes, not timing.
- Store/jar tests use coordinated concurrent operations to verify atomic replacement, snapshot consistency, no lost updates, and no mutation leakage.
- Persistence tests round-trip every stored field, reject corrupt/untrusted data, and prove the documented loss profile of Netscape import/export.
- Coverage is complete only when each public operation is tested through its interface and default class, data models/enums affect at least one behavior test, every `CookieErrorCode` reachable from the API has an assertion, and line/branch coverage reports show no unreviewed gaps in `src/Api` or its implementation paths.

## Implementation tasks

- [DONE] T001: Create the public enum files for compliance, SameSite, access kind, prefix policy, issue severity, error codes, set/match outcomes, and snapshot import mode, with explicit int32 values and required XML comments.
- [DONE] T002: Create `CookieDefaults` and `CookieAttributeNames` as method-free static data classes containing portable defaults and canonical strings.
- [DONE] T003: Create the low-level public data models `CookiePair`, `CookieAttribute`, `SetCookie`, `CookieIdentity`, and `CookieIssue`, using fields and explicit presence flags instead of properties or nullable value types.
- [DONE] T004: Create the canonical storage and context models `StoredCookie`, `CookieSetContext`, `CookieRequestContext`, `CookieStoreQuery`, and `CookieRemovalQuery`.
- [DONE] T005: Create the option models `CookieLimits`, `CookieParseOptions`, `CookieSerializationOptions`, `CookieJarOptions`, and `NetscapeCookieFileOptions`, including validation expectations for unsafe or contradictory settings.
- [DONE] T006: Create all parse, set, match, selection, snapshot, import, batch, and Netscape result data classes so expected failures never require exceptions.
- [DONE] T007: Define the eleven public operation interfaces with unique, non-overloaded instance method names and transport-neutral `Uri`/string/data-model inputs.
- [DONE] T008: Add the five direct-`System.Exception` public exception types and connect each to the corresponding throwing API contract.
- [DONE] T009: Implement ASCII/token/cookie-octet character classification and a bounded span-based scanner for name/value and attribute segments.
- [DONE] T010: Implement overflow-safe signed decimal parsing for Max-Age and all configured numeric limits, including saturation behavior for values outside `long` and `DateTimeOffset` ranges.
- [DONE] T011: Implement the RFC 6265 cookie-date tokenizer and parser with delimiter classes, two-digit year normalization, calendar validation, UTC output, and attribute-ignore behavior on failure.
- [DONE] T012: Implement invariant IMF-fixdate serialization for valid UTC Expires values without culture-dependent parsing or formatting.
- [DONE] T013: Implement the Set-Cookie consuming parser core for the RFC 6265 profile, including whitespace trimming, empty-name behavior, recognized attributes, duplicate resolution, ignored malformed attributes, and unknown extension preservation.
- [DONE] T014: Extend Set-Cookie parsing with the 6265bis limits and semantics for SameSite, prefixes, lifetime caps, modern control-octet rules, and case-insensitive recognition.
- [DONE] T015: Implement compatible parsing recoveries for safe quoted values and common whitespace/date deviations, emitting a stable issue for every recovery and never recovering header injection.
- [DONE] T016: Implement Cookie request-field parsing for one or many field values while retaining pair order, duplicate names, empty values, field order, and per-segment issues.
- [DONE] T017: Implement `CookieParser` throwing and exception-less interface methods over the parsing cores, with defensive result construction and no global logging.
- [DONE] T018: Implement producer-side validation for Set-Cookie and Cookie pairs, including strict names/values, extension safety, size limits, attribute consistency, and serialization-specific error codes.
- [DONE] T019: Implement deterministic Set-Cookie serialization for Expires, Max-Age, Domain, Path, Secure, HttpOnly, SameSite, Partitioned, quoted-value policy, and safe extensions.
- [DONE] T020: Implement ordered Cookie serialization and optional multi-field splitting only at pair boundaries, including an explicit over-limit result for an indivisible pair.
- [DONE] T021: Implement `CookieSerializer` interface methods and dedicated throwing failure behavior over the serializer cores.
- [DONE] T022: Implement URI host extraction for DNS, bracketed IPv6, IPv4, ports, absent hosts, and terminal root dots.
- [DONE] T023: Implement IDNA host canonicalization using built-in `IdnMapping`, ASCII lowercase, label/domain length validation, fake A-label rejection, and deterministic invalid-host errors.
- [DONE] T024: Implement RFC exact/dot-boundary domain matching, host-only semantics, leading/trailing dot Domain rules, IP literal policy, and origin-domain validation.
- [DONE] T025: Implement the `IPublicSuffixList` integration boundary, fail-closed/disabled configuration behavior, public-suffix set rejection, same-host exception, and retrieval-time revalidation.
- [DONE] T026: Implement RFC default-path computation and exact/prefix/slash-boundary path matching over consistently escaped URI paths.
- [DONE] T027: Implement schemeful-site calculation from scheme plus registrable domain, with explicit handling for IP, single-label, missing-PSL, and invalid origins.
- [DONE] T028: Implement `DefaultSecureOriginPolicy` for secure schemes and configurable trustworthy localhost/loopback behavior, and honor replacement policies through `ISecureOriginPolicy`.
- [DONE] T029: Implement expiry evaluation, Max-Age-over-Expires precedence, immediate deletion, session/persistent classification, 400-day modern cap, and safe date arithmetic.
- [DONE] T030: Implement SameSite creation and retrieval evaluation for Strict, Lax, None, Default, safe methods, top-level navigation, reload/redirect context, and optional Lax-allowing-unsafe grace.
- [DONE] T031: Implement Secure and HttpOnly set/return rules for HTTP versus non-HTTP access, including protection of existing HttpOnly cookies from non-HTTP replacement.
- [DONE] T032: Implement insecure-origin protection against overlaying an existing Secure cookie using name, domain, and path overlap rules.
- [DONE] T033: Implement profile-aware `__Secure-` and `__Host-` validation, including secure origin, explicit `Path=/`, absent Domain, case rules, and silent/error/disabled policy outcomes.
- [DONE] T034: Implement opt-in Partitioned parsing/storage enforcement, secure requirement, partition-key derivation/input, identity separation, and request partition matching.
- [DONE] T035: Implement `CookieMatcher` low-level methods and complete include/exclude evaluation with stable reasons for expiry, domain, PSL, path, Secure, HttpOnly, SameSite, and partition checks.
- [DONE] T036: Implement `DefaultCookiePolicy` acceptance/return pipelines, application domain allow/block controls, mandatory safety floors, and normalized `CookieSetDecision` results.
- [DONE] T037: Implement defensive deep-copy helpers for every public mutable data graph so parser, store, jar, query, selection, and snapshot boundaries do not share mutable internal state.
- [DONE] T038: Implement canonical cookie identity and deterministic stable sequence assignment, including partition keys and preservation of creation/sequence on replacement.
- [DONE] T039: Implement the indexed `MemoryCookieStore` with concurrency-safe exact lookup, candidate query, atomic upsert/delete, filtered removal, snapshot capture, and snapshot replacement.
- [DONE] T040: Implement insertion and replacement semantics, including expired deletion, creation-time preservation, last-access initialization, and no-op deletion outcomes.
- [DONE] T041: Implement RFC/6265bis quota validation and deterministic eviction for expired, non-Secure overfull-domain, overfull-domain, and global oldest-access candidates.
- [DONE] T042: Implement the high-level raw-header and parsed-model ingestion paths in `CookieJar`, with ordered batch outcomes and independent failure of malformed Set-Cookie fields.
- [DONE] T043: Implement request candidate lookup and full selection in `CookieJar`, stable RFC ordering, atomic last-access updates, defensive output, and current-PSL revalidation.
- [DONE] T044: Implement creation of one or many Cookie request field values from selection results, respecting configured field limits without truncation or reordering.
- [DONE] T045: Implement jar management operations for exact/domain/subdomain/path/name/partition queries and removals, plus explicit expired and session-cookie purges.
- [DONE] T046: Implement `SystemCookieClock` and ensure every time-dependent parser, policy, jar, eviction, and persistence path obtains time through `ICookieClock`.
- [DONE] T047: Implement a versioned lossless snapshot writer using built-in .NET APIs, invariant timestamps/numbers, deterministic entry order, all flags/extensions, and stable sequence state.
- [DONE] T048: Implement exception-less and throwing snapshot readers with schema/version checks, required-field validation, duplicate identity detection, safe limits, canonicalization, PSL/security revalidation, and merge/replace import semantics.
- [DONE] T049: Implement Netscape/Mozilla/curl cookie-file parsing for comments, tab fields, include-subdomains, `#HttpOnly_`, Secure, epoch expiry, session entries, malformed lines, and explicit lossy diagnostics.
- [DONE] T050: Implement deterministic Netscape cookie-file serialization with session/expired inclusion options and rejection/diagnostics for ambiguous partitioned or unrepresentable state.
- [DONE] T051: Validate constructor options and dependency graphs for every public implementation class, rejecting null/missing required services, negative limits, impossible quotas, and unsafe PSL configuration with stable failures.
- [DONE] T052: Add parser tests for every strict Set-Cookie grammar production, liberal consuming step, attribute, duplicate rule, empty-name/value case, whitespace boundary, control/octet rejection, length boundary, issue code, and throwing/non-throwing public method.
- [DONE] T053: Add cookie-date and lifetime tests for every month, delimiter class, the 69/70 two-digit-year mapping, invalid calendar/time value, leap year, the 1601 lower bound, huge Max-Age, precedence rule, immediate deletion, 400-day cap, UTC format, and clock boundary.
- [DONE] T054: Add Cookie request parsing/serialization tests for ordered duplicates, empty values, multiple field values, invalid segments, quoted compatibility, pair-boundary splitting, over-limit pairs, and injection rejection.
- [DONE] T055: Add host/IDNA/IP/domain tests covering Unicode/A-label round trips, invalid labels, ports, root dots, IPv4/IPv6, exact and suffix boundaries, host-only behavior, malformed Domain values, and all compatibility-profile differences.
- [DONE] T056: Add PSL integration tests using deterministic wildcard/exception fakes and the downloaded PSL snapshot for supercookie rejection, same-host exceptions, registrable-domain/site computation, no-provider modes, localhost, and list changes after storage.
- [DONE] T057: Add default-path and path-match tests for every RFC branch, escaped paths, empty/non-slash inputs, slash boundaries, invalid Path fallback, and ordering by path length.
- [DONE] T058: Add Secure/HttpOnly tests for secure schemes, localhost/loopback policy, HTTP/non-HTTP setting and retrieval, HttpOnly replacement protection, insecure Secure rejection, and secure-cookie overlay attacks.
- [DONE] T059: Add exhaustive SameSite tests for Strict/Lax/None/Default across same-site/cross-site, safe/unsafe methods, top-level/subresource navigation, reload, redirect chains, activation, grace-period ages, scheme changes, creation restrictions, and `None` without Secure.
- [DONE] T060: Add prefix and Partitioned tests for profile-specific case handling, every `__Secure-`/`__Host-` requirement, each prefix policy mode, explicit versus default Path, missing top-level site, Secure requirement, identity isolation, and cross-partition retrieval.
- [DONE] T061: Add storage identity/replacement/deletion tests for name/domain/path/partition keys, creation/sequence preservation, attribute refresh, immediate expiry, stable equal-time ordering, defensive copies, and every store interface operation.
- [DONE] T062: Add jar acceptance and request-selection tests that exercise every ordered policy step, batch partial success, domain allow/block policy, matching exclusion reason, duplicate-name output, last-access updates, and every public jar method through `ICookieJar`.
- [DONE] T063: Add limit and eviction tests at below/equal/above boundaries for input, attribute, per-domain, total, lifetime, and header sizes, verifying the complete RFC eviction priority and deterministic tie-breaking.
- [DONE] T064: Add coordinated concurrency tests for parsing service reuse, store lookup/upsert/delete, simultaneous identity replacement, selection plus expiry, last-access updates, removal, and snapshot consistency without mutation leakage or lost updates.
- [DONE] T065: Add lossless snapshot tests for every public stored field, deterministic output, merge/replace conflicts, expired/public-suffix revalidation, corrupt schema/version/types, duplicate identities, huge input, throwing/non-throwing paths, and every import issue code.
- [DONE] T066: Add Netscape cookie-file tests for curl/Mozilla-compatible examples, `#HttpOnly_`, comments, tabs, booleans, epoch/session/expired cases, malformed lines, round trips, inclusion options, and every documented lossy conversion.
- [DONE] T067: Port or adapt portable edge-case vectors identified in the downloaded Go, Python, tough-cookie, curl, and WPT references, recording the selected compliance profile for intentional behavioral differences.
- [DONE] T068: Add generated malformed-input and Unicode tests for Set-Cookie, Cookie, snapshot, and Netscape parsers, asserting stable results, no unhandled exceptions, no control-character reflection, and no partial live-store mutation.
- [DONE] T069: Complete the public API coverage matrix so every public constructor, interface operation, implementation operation, data field behavior, enum value, exception path, and reachable `CookieErrorCode` has a direct functional test, then close every reviewed line/branch coverage gap.
