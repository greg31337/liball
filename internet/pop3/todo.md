# Universal POP3 client/server development plan

## Goal and boundaries

Build a transport-aware but storage- and identity-provider-neutral POP3 library for
.NET 10. The library will implement the complete client and server protocol
surface in RFC 1939, the extension framework in RFC 2449, STLS from RFC 2595,
POP3 SASL from RFC 5034, the registered response codes from RFC 3206, and the
UTF8/LANG extension from RFC 6856. Modern TLS defaults follow RFC 8314, RFC 8996,
and current .NET platform policy.

The public surface is synchronous and cancellation-aware because `syntax.md`
does not permit `async`/`await`. Message data remains raw Internet Message Format
octets; MIME parsing, a local message store, mailbox folders other than the POP3
maildrop, message arrival notification, and permanent read/recent flags are not
POP3 responsibilities. Host names are supplied by callers; RFC 6186 SRV lookup is
not implemented, but the standard POP3/POP3S ports and custom connected-stream
entry points make external discovery and proxying possible.

No external package will be referenced by `src/pop3.csproj`. Only built-in .NET
networking, cryptography, TLS, text, collection, stream, and threading APIs may be
used under `src/`. Test-only packages are permitted.

## Research basis

The local research corpus is indexed in [specs/README.md](specs/README.md).

| Source | Features or lessons carried into this plan |
| --- | --- |
| RFC 1939 | AUTHORIZATION/TRANSACTION/UPDATE states; USER/PASS, APOP, STAT, LIST, RETR, DELE, NOOP, RSET, QUIT, TOP, UIDL; stable transaction numbering; maildrop locking; commit only on QUIT; inactivity handling; dot-stuffed multiline data. |
| RFC 2449 | CAPA in both states, extensible capability parsing, 255-octet commands, 512-octet response/capability lines, RESP-CODES, LOGIN-DELAY, PIPELINING, EXPIRE, IMPLEMENTATION, capability refresh rules, and probing when CAPA is unavailable. |
| RFC 2595 and RFC 8314 | STLS, implicit TLS on port 995, certificate/service-identity verification, no pipelined STLS, capability invalidation across the TLS boundary, and a secure-by-default cleartext-authentication policy. |
| RFC 5034 | AUTH mechanism discovery, optional initial response, `=` empty response, `*` cancellation, strict Base64, arbitrary-length SASL challenge lines, retry after failed AUTH, PLAIN-over-TLS interoperability, and optional SASL security-layer hooks. |
| RFC 3206 and IANA registry | LOGIN-DELAY, IN-USE, SYS/TEMP, SYS/PERM, AUTH, and UTF8 response codes; AUTH-RESP-CODE capability; unknown hierarchical codes retained for future use. |
| RFC 6856 | UTF8 and UTF8=USER modes, internationalized message handling, LANG listing/selection, localized human-readable response text, and UTF8 errors for LIST/TOP/RETR. |
| MailKit | Explicit and stream-based connections, TLS modes, APOP/USER-PASS/SASL selection, capability re-evaluation, pipelined bulk operations, streaming retrieval, cancellation, timeouts, transfer progress, protocol logging, UTF8, and LANG. |
| libEtPan | A useful low-level command-oriented layer alongside the high-level client; stream connection injection, command methods, STLS, SASL callbacks, timeouts, progress, and logging. |
| Angus/Jakarta Mail POP3 | CAPA and TOP compatibility switches, pipelining, streaming/file-friendly retrieval, UID tracking, APOP and configurable auth mechanisms, STLS required/optional modes, timeouts, raw proxy-capable transport, and the important fact that deletion commits only when the mailbox session closes successfully. |

The comparison does not copy any third-party source and does not introduce their
MIME or mail-store abstractions. It identifies behavior expected from a mature
POP3 implementation.

## Complete feature inventory

### Wire format and low-level protocol

- Read and write commands as octets terminated by exact CRLF; accept command
  keywords case-insensitively while emitting canonical upper case.
- Validate keyword, argument, control-character, spacing, numeric, and state
  rules without locale-sensitive conversions or LINQ.
- Enforce the RFC 2449 255-octet command limit and 512-octet greeting,
  single-line response, and CAPA-line limits. Apply separate configurable safety
  limits to unbounded message lines and SASL exchanges without violating RFC
  5034's arbitrary mechanism-payload requirement.
- Parse `+OK`, `-ERR`, and SASL `+` continuation responses; retain human text,
  greeting timestamp, hierarchical bracketed response code, and unknown values.
- Stream multiline bodies to a caller-provided `Stream`; recognize `.\r\n`,
  remove exactly one dot from dot-stuffed lines, preserve all other octets and
  CRLFs, and never buffer an entire large message unless the byte-array
  convenience API is explicitly used.
- Generate multiline bodies with CRLF normalization where server storage uses a
  different line ending, dot-stuff every line beginning with `.`, and terminate
  exactly once. Compute advertised octet sizes using transmitted, unstuffed CRLF
  form.
- Parse and generate CAPA entries with case-insensitive tags, ordered parameters,
  duplicate/unknown entry preservation, and a current IANA capability view.
- Parse and generate LIST, UIDL, STAT, LANG, and extended response-code payloads,
  rejecting overflow, zero/negative message numbers, malformed UTF-8, and invalid
  unique IDs deterministically.
- Provide raw single-line and multiline extension-command paths plus public
  reader/writer contracts so future registered or private extensions are usable.
- Tolerate arbitrary network fragmentation, partial reads/writes, and EOF only at
  legal boundaries; flush complete commands/responses and propagate I/O failure
  without losing protocol state.

### Session and mailbox semantics

- Implement Disconnected, AUTHORIZATION, TRANSACTION, UPDATE, and Closed state
  transitions with command legality checks on both client and server.
- Require the server greeting before client commands and support a negative
  greeting that closes the attempted session.
- Acquire an exclusive maildrop transaction after successful authentication;
  return IN-USE when locking fails and release all acquired resources on every
  normal terminal path.
- Snapshot message order and one-based message numbers at transaction start.
  Numbering never changes during the transaction, including after DELE.
- STAT reports count and total octets excluding messages marked deleted.
- LIST supports one message and multiline all-message forms, excludes deleted
  messages from the all form, and rejects missing/deleted message numbers.
- UIDL supports one message and multiline all-message forms, validates printable
  non-space unique IDs, and preserves stable IDs across sessions as a mailbox
  provider responsibility.
- RETR streams the complete raw message, including header/body separator, and
  reports an advisory octet count without trusting it for framing.
- TOP streams all headers, the blank separator, and exactly the requested number
  of body lines (or all available lines), including correct zero-line behavior.
- DELE marks messages only within the transaction; repeated/deleted/invalid
  requests fail without corrupting the mark set.
- RSET clears all deletion marks and returns the restored mailbox count/size;
  NOOP keeps the session alive without changing it.
- QUIT from AUTHORIZATION closes normally. QUIT from TRANSACTION enters UPDATE,
  atomically commits marked deletions through the provider, releases the lock,
  emits the final response, and closes.
- EOF, timeout, cancellation, protocol failure, TLS failure, explicit abort, or
  inactivity autologout rolls back deletion marks and never enters UPDATE.
- Server inactivity timeout is configurable but never below RFC 1939's ten-minute
  minimum when enabled; each complete client command resets it.

### Capabilities and extensions

- CAPA is available in AUTHORIZATION and TRANSACTION. Server capability output is
  state-, TLS-, authentication-, UTF8-, language-, and provider-aware.
- Implement and interpret TOP, USER, SASL, RESP-CODES, LOGIN-DELAY, PIPELINING,
  EXPIRE, UIDL, IMPLEMENTATION, STLS, AUTH-RESP-CODE, UTF8, and LANG.
- Preserve unknown capabilities and parameters. Allow server extension hooks to
  add non-conflicting capabilities and commands with declared legal states.
- Refresh client capabilities after connect, STLS, successful SASL security-layer
  negotiation, and authentication when values may differ. Discard all pre-TLS
  buffered plaintext and cached capabilities before reading protected data.
- When CAPA returns `-ERR`, support configurable conservative probing/fallback for
  USER, TOP, and UIDL; never assume PIPELINING, STLS, SASL, UTF8, or LANG.
- Parse LOGIN-DELAY seconds and optional USER scope, EXPIRE days/NEVER and optional
  USER scope, IMPLEMENTATION text, SASL mechanism order, and RESP-CODES behavior.
- Client pipelining is opt-in only when advertised or explicitly forced. Associate
  ordered responses with commands, handle mixed single/multiline responses, stop
  issuing dependent work after a failure, and keep the stream synchronized.
- Server accepts legal pipelines and returns responses in command order. STLS and
  active SASL exchanges are barriers and can never share a plaintext pipeline.
- Supply bulk LIST, UIDL, RETR, TOP, and DELE high-level operations that use
  pipelining where safe and otherwise degrade to sequential commands.

### Authentication and security

- USER/PASS supports the required two-step server state, does not send PASS after
  a rejected USER, redacts PASS everywhere, and can be disabled or restricted to
  protected connections.
- APOP extracts and validates the greeting timestamp, computes the RFC 1939 MD5
  digest for the client, compares through a server authentication hook, redacts
  secrets, and is disabled by secure-default automatic selection unless a caller
  explicitly permits this legacy mechanism.
- SASL mechanism hooks operate on decoded byte arrays and support client initial
  responses, empty challenges/responses, multi-step exchanges, cancellation,
  strict Base64, authentication/authorization identities, retry after failure,
  and an optional returned security `Stream` for negotiated SASL layers.
- Include a built-in SASL PLAIN client and server exchange wired to the
  authentication provider. Advertise/permit it only on an adequately protected
  stream by default, satisfying RFC 5034's TLS+PLAIN interoperability rule.
- Allow arbitrary SASL mechanisms (for example SCRAM, OAuth, GSSAPI, or EXTERNAL)
  through factories without any external dependency in `src/`.
- Automatic client authentication filters server mechanisms through caller policy,
  tries caller-supplied SASL hooks in preference order, optionally tries APOP, and
  uses USER/PASS only when enabled and safe. Credentials are never silently sent
  over cleartext.
- Authentication failures distinguish invalid credentials (AUTH), lock contention
  (IN-USE), too-recent login (LOGIN-DELAY), temporary system failure (SYS/TEMP),
  and permanent system failure (SYS/PERM) without revealing whether a user exists.
- STLS is legal only in AUTHORIZATION before UTF8/authentication. It waits for
  `+OK`, upgrades immediately, clears plaintext read buffers, verifies identity,
  and obtains a fresh CAPA response.
- Support direct implicit TLS and explicit STLS required/optional modes. Modern
  defaults require TLS 1.2 or newer, normal chain/revocation/host validation, and
  do not offer a trust-all switch. Custom TLS providers can implement specialized
  certificate/client-certificate policy.
- TLS and SASL security-layer streams are layered in RFC order. Authentication
  state learned outside a successful SASL exchange is discarded when required.
- Protocol logging is injectable, ordered, and secret-safe: PASS, APOP material,
  SASL payloads, and message bodies are represented by redacted metadata unless a
  caller explicitly supplies its own lower-level stream logger.
- Connect, read, write, authentication, and server inactivity timeouts plus
  `CancellationToken` cancellation are supported. Failure closes or invalidates a
  session rather than attempting unsafe resynchronization.

### UTF-8, language, interoperability, and high-level behavior

- UTF8 mode is enabled only when advertised, only in AUTHORIZATION, and before
  STLS restrictions make the command illegal. Track whether UTF8=USER permits
  UTF-8 USER/PASS/APOP credentials independently of message UTF8 mode.
- Validate UTF-8 strictly. In non-UTF8 mode, the server converts when its provider
  can supply an ASCII-compatible form or returns `-ERR [UTF8]` for LIST/TOP/RETR.
- Recompute/list transmitted sizes for the selected representation so LIST and
  RETR agree.
- LANG with no argument lists language tag/description pairs; LANG with a tag
  selects localized response text; `LANG *` resets to the server default. Language
  selection is available in AUTHORIZATION and TRANSACTION and survives only for
  the current connection.
- Direct host connection supports ports 110 and 995, IPv4/IPv6 through .NET,
  configurable timeouts, and caller-owned connected streams for proxies, Unix
  sockets, test transports, or externally resolved RFC 6186 endpoints.
- Client state snapshots expose connection/security/authentication state, greeting,
  capabilities, selected language, UTF8 state, mailbox count, and last response
  without public properties.
- Transfer observers receive operation kind, message number, bytes transferred,
  and expected bytes where known. Cancellation leaves output ownership with the
  caller and invalidates the protocol session when framing cannot be completed.
- High-level byte-array conveniences enforce a configurable memory ceiling;
  stream APIs have no message-size buffering requirement.
- Batch methods validate all indexes and sinks before writing the first command,
  preserve caller order, and return per-message metadata/results.
- Public APIs use one-based POP3 message numbers. They do not pretend POP3 has
  folders, append, expunge-without-QUIT, received dates, new-mail notification, or
  persistent read/recent flags.

## Architecture and syntax constraints

- Every `src/**/*.cs` file will use one file-scoped namespace and only constructs
  allowed by `syntax.md`. No implementation decision may rely on existing source
  as permission to broaden that subset.
- Public types live only under `src/Api` in the mandated category folder. Public
  data uses documented fields, not properties. Public implementation classes have
  one constructor at most and expose only methods declared by their interfaces.
- Interface and implementation method names are unique; convenience variants use
  names such as `ListOneMessage` and `ListAllMessages`, never overloads.
- Public static classes contain constants/read-only fields only. All custom
  exceptions derive directly from `System.Exception`.
- Source contains no generic type or method declarations. Built-in generic
  collections are used explicitly. `Span<byte>`/`ReadOnlySpan<byte>` are used in
  internal parsing, validation, hashing, and buffer-copy paths where their lifetime
  is local; streams remain the public abstraction for unbounded data.
- Because `try`/`catch`/`finally` is unavailable, parsers prevalidate data before
  conversions that would throw, `using` statements own disposable scopes, and
  errors propagate through explicit result data or direct exceptions without
  exception-driven probing.
- Internal implementation types live outside `src/Api`, are non-public, and may
  not leak through public fields or interface signatures.

## Proposed public API and files

Signatures below are intentionally summarized rather than finalized. Every public
member will have the XML documentation required by `syntax.md` as part of its
implementation task.

### `src/Api/Enums`

- `Pop3SessionState.cs` — disconnected, authorization, transaction, update, and
  closed states; consumed by snapshots, extension context, and interfaces.
- `Pop3ResponseKind.cs` — positive, negative, and SASL continuation; consumed by
  `Pop3Response` and the wire contracts.
- `Pop3SecurityMode.cs` — plaintext, STLS-if-available, STLS-required, and implicit
  TLS; consumed by client/server option data.
- `Pop3AuthenticationMethod.cs` — automatic, USER/PASS, APOP, SASL, and SASL PLAIN;
  consumed by authentication request/result data.
- `Pop3AuthenticationStatus.cs` — success, rejected, login-delay, in-use,
  temporary-failure, and permanent-failure; consumed by auth/mailbox results.
- `Pop3SaslStepStatus.cs` — continue, success, rejected, and cancelled; consumed by
  `Pop3SaslStep` and SASL contracts.
- `Pop3TransferKind.cs` — LIST, UIDL, RETR, TOP, and extension data; consumed by
  transfer progress.
- `Pop3LogDirection.cs` — client-to-server, server-to-client, and local state;
  consumed by protocol log entries.

### `src/Api/Static`

- `Pop3Commands.cs` — canonical command-name constants.
- `Pop3Capabilities.cs` — registered capability-name constants.
- `Pop3ResponseCodes.cs` — registered response-code constants.
- `Pop3Defaults.cs` — port 110/995, 255/512 wire limits, minimum inactivity
  timeout, buffer defaults, and safe memory ceilings.

### `src/Api/Data`

- `Pop3Command.cs` — keyword, ordered arguments, sensitivity, and multiline
  expectation fields; used by reader/writer and extension interfaces.
- `Pop3Response.cs` — response kind, text, response-code levels, greeting
  timestamp, and optional multiline metadata; used by client/wire interfaces.
- `Pop3Capability.cs` and `Pop3CapabilitySet.cs` — raw ordered capability entries
  and interpreted current capability values; returned by the client and extensions.
- `Pop3MailboxStatus.cs` — visible message count and total octets; returned by STAT
  and mailbox contracts.
- `Pop3MessageInfo.cs` — one-based number, octet size, deletion/representation
  state; used by LIST and mailbox snapshots.
- `Pop3UniqueId.cs` — message number and stable unique ID; used by UIDL methods.
- `Pop3Language.cs` — language tag and UTF-8 description; used by LANG methods and
  server options.
- `Pop3Credentials.cs` — username, password, and optional authorization identity;
  consumed by authentication APIs and built-in PLAIN.
- `Pop3ApopRequest.cs` — username, timestamp, and digest; consumed by the server
  authentication provider.
- `Pop3AuthenticationRequest.cs` and `Pop3AuthenticationResult.cs` — method policy,
  credentials, SASL hooks, authenticated identity/maildrop key, and classified
  outcome; shared by high-level client and server providers.
- `Pop3SaslStep.cs` — decoded challenge/response bytes, step state, and optional
  authenticated identity; shared by SASL contracts.
- `Pop3MailboxOpenRequest.cs` and `Pop3MailboxOpenResult.cs` — identity/maildrop,
  connection context, lock outcome, and acquired mailbox session; used by the
  mailbox provider.
- `Pop3ClientOptions.cs` — endpoint, security mode, timeout/limit, pipelining,
  compatibility, authentication, TLS provider, logging, and transfer fields.
- `Pop3ClientSnapshot.cs` — observable connection/session/capability/mailbox state.
- `Pop3ServerOptions.cs` — greeting/implementation, capability policy, TLS,
  authentication, timeout/limit, language, extension, logging, and listener-neutral
  session configuration.
- `Pop3ServerConnection.cs` — caller-supplied stream security and endpoint context
  for one accepted server connection.
- `Pop3TlsClientOptions.cs` and `Pop3TlsServerOptions.cs` — host/service identity,
  protocol/revocation policy, certificates, and stream ownership inputs.
- `Pop3TransferProgress.cs` — operation, message number, current bytes, expected
  bytes, and completion fields.
- `Pop3LogEntry.cs` — direction, redacted command/response text, byte count, and
  session state.
- `Pop3ExtensionContext.cs` and `Pop3ExtensionResult.cs` — safe server extension
  state/capability context and classified single/multiline result.
- `Pop3LanguageTextRequest.cs` — selected language and stable response-text key for
  localization hooks.

### `src/Api/Contracts`

- `IPop3WireReader.cs` — read a command, read a single response, and copy a
  multiline response with limits/cancellation.
- `IPop3WireWriter.cs` — write commands, responses, capability lists, and
  dot-stuffed multiline data.
- `IPop3Client.cs` — uniquely named connect-host/connect-stream, snapshot/CAPA/STLS,
  automatic/USER-PASS/APOP/SASL authentication, STAT, one/all LIST and UIDL,
  one/batch RETR and TOP to streams or bounded memory, one/batch/all DELE, RSET,
  NOOP, UTF8, LANG, extension, QUIT-commit, and abort operations.
- `IPop3ClientTlsProvider.cs` — convert an established client stream into a
  validated TLS stream.
- `IPop3ServerTlsProvider.cs` — convert an accepted server stream into a TLS stream.
- `IPop3SaslClient.cs` — report mechanism, begin/continue decoded exchanges, cancel,
  and apply an optional negotiated security stream.
- `IPop3SaslServerFactory.cs` — report an extension mechanism and create one
  connection-scoped server session.
- `IPop3SaslServerSession.cs` — begin/continue/cancel an exchange, return the final
  auth result, and apply an optional security stream.
- `IPop3AuthenticationProvider.cs` — classify USER/PASS, APOP, and built-in SASL
  PLAIN attempts plus login-delay policy without exposing account existence.
- `IPop3MailboxProvider.cs` — exception-less attempt to acquire one exclusive,
  connection-scoped maildrop transaction.
- `IPop3MailboxSession.cs` — snapshot metadata, stream RETR/TOP representations,
  mark/reset deletions, atomically commit UPDATE, and rollback/release.
- `IPop3ProtocolLogger.cs` — receive already-redacted protocol/state entries.
- `IPop3TransferObserver.cs` — receive transfer progress without delegates/events.
- `IPop3LanguageProvider.cs` — list/select languages and resolve stable response
  keys to localized text.
- `IPop3ServerExtension.cs` — advertise non-conflicting capabilities and execute a
  declared custom command in legal states.
- `IPop3Server.cs` — serve exactly one caller-owned accepted connection with
  cancellation and deterministic commit/rollback behavior.

### `src/Api/Exceptions`

- `Pop3ConnectionException.cs` — connection, EOF, timeout, or closed-session error.
- `Pop3ProtocolException.cs` — malformed or contradictory peer protocol data.
- `Pop3CommandException.cs` — a valid command received a negative POP3 response;
  carries the response data as fields.
- `Pop3AuthenticationException.cs` — terminal authentication failure and classified
  result fields.
- `Pop3TlsException.cs` — required TLS unavailable or TLS policy/handshake failure.
- `Pop3MailboxException.cs` — provider snapshot, retrieval, or commit failure.
- `Pop3StateException.cs` — locally attempted operation is illegal in current state.

### `src/Api/Classes`

- `Pop3WireReader.cs` — built-in `IPop3WireReader` implementation.
- `Pop3WireWriter.cs` — built-in `IPop3WireWriter` implementation.
- `Pop3Client.cs` — built-in high-level `IPop3Client` implementation.
- `Pop3Server.cs` — built-in single-connection `IPop3Server` implementation with
  injected authentication and mailbox providers.
- `Pop3DefaultClientTlsProvider.cs` — secure .NET `SslStream` client policy.
- `Pop3DefaultServerTlsProvider.cs` — .NET `SslStream` server policy using supplied
  certificate options.
- `Pop3PlainSaslClient.cs` — RFC 4616 PLAIN client implementing `IPop3SaslClient`.

## Planned internal source files

- `src/Internal/IO/Pop3BufferedInput.cs`, `Pop3LineReader.cs`,
  `Pop3LineWriter.cs`, `Pop3MultilineReader.cs`, and `Pop3MultilineWriter.cs` —
  fragmented stream I/O, limits, CRLF handling, streaming, and dot transparency.
- `src/Internal/Parsing/Pop3CommandParser.cs`, `Pop3ResponseParser.cs`,
  `Pop3ResponseCodeParser.cs`, `Pop3CapabilityParser.cs`, `Pop3ListParser.cs`,
  `Pop3UidlParser.cs`, `Pop3LanguageParser.cs`, `Pop3NumberParser.cs`, and
  `Pop3Utf8Validator.cs` — allocation-conscious span-based validation/parsing.
- `src/Internal/Security/Pop3ApopDigest.cs`, `Pop3Base64.cs`,
  `Pop3SaslExchange.cs`, `Pop3PlainSaslServer.cs`, `Pop3TlsUpgrade.cs`, and
  `Pop3SecretRedactor.cs` — authentication and security-boundary mechanics.
- `src/Internal/Client/Pop3ClientEngine.cs`, `Pop3ClientStateMachine.cs`,
  `Pop3ClientPipeline.cs`, `Pop3CapabilityCache.cs`, and
  `Pop3ClientTransfer.cs` — connection, state, capability, pipeline, and streaming
  behavior behind `Pop3Client`.
- `src/Internal/Server/Pop3ServerEngine.cs`, `Pop3ServerStateMachine.cs`,
  `Pop3CommandDispatcher.cs`, `Pop3AuthorizationExchange.cs`,
  `Pop3MailboxTransaction.cs`, `Pop3CapabilityBuilder.cs`,
  `Pop3LanguageSession.cs`, and `Pop3ExtensionDispatcher.cs` — command dispatch,
  auth, mailbox, capability, language, and rollback/commit behavior.

## Implementation tasks

- [DONE] T001: Create the planned `src/Api` and `src/Internal` directory/file skeleton and namespaces, with no external source dependencies.
- [DONE] T002: Implement all public enums and constant-only static classes for commands, capabilities, response codes, ports, limits, and timeouts.
- [DONE] T003: Implement the core wire data classes (`Pop3Command`, `Pop3Response`, capabilities, LIST/UIDL/STAT data) with documented public fields and validation-ready defaults.
- [DONE] T004: Implement option, snapshot, authentication, SASL, TLS, mailbox, language, logging, transfer, and extension data classes and ensure each is consumed by a public contract.
- [DONE] T005: Implement the seven direct `System.Exception` public exception types with one constructor each and useful documented context fields.
- [DONE] T006: Declare the low-level reader/writer, logging, transfer, TLS, SASL, authentication, mailbox, language, extension, client, and server contracts with unique instance method names.
- [DONE] T007: Implement buffered stream input/output that handles one-byte fragmentation, partial writes, cancellation, EOF, and configurable safety ceilings.
- [DONE] T008: Implement strict CRLF line reading/writing and RFC 2449 command/status/capability line-length enforcement.
- [DONE] T009: Implement span-based command tokenization and validation for case-insensitive 3–4 character standard keywords, extension keywords, printable arguments, spacing, and the 255-octet cap.
- [DONE] T010: Implement response parsing/generation for `+OK`, `-ERR`, greeting timestamps, arbitrary human text, and the 512-octet cap.
- [DONE] T011: Implement hierarchical bracketed response-code parsing/generation with registered-code interpretation and unknown-code preservation.
- [DONE] T012: Implement streaming multiline reading with terminator recognition, one-octet dot unstuffing, CRLF preservation, transfer observation, memory ceiling support, and legal EOF detection.
- [DONE] T013: Implement streaming multiline writing with CRLF representation conversion, dot stuffing, terminator emission, transmitted-octet accounting, and partial-write handling.
- [DONE] T014: Implement CAPA parsing/generation with ordered parameters, case-insensitive lookup, duplicate/unknown preservation, 512-octet capability lines, and state-dependent values.
- [DONE] T015: Implement strict non-throwing signed-decimal parsing and LIST, UIDL, STAT, and LANG payload parsers with range, uniqueness, and consistency checks.
- [DONE] T016: Implement the public `Pop3WireReader` and `Pop3WireWriter` classes over the internal I/O and parser components.
- [DONE] T017: Implement protocol logging dispatch that preserves ordering and redacts PASS, APOP, SASL exchanges, and message bodies before calling user hooks.
- [DONE] T018: Implement transfer progress dispatch for LIST, UIDL, RETR, TOP, and custom multiline operations without retaining caller streams.
- [DONE] T019: Implement the client state machine, legal-operation guards, last-response tracking, invalidation after framing/I/O failure, and immutable-by-copy snapshot production.
- [DONE] T020: Implement direct TCP host connection for IPv4/IPv6, standard/explicit ports, connect/read/write timeouts, cancellation, stream ownership, greeting validation, and negative-greeting failure.
- [DONE] T021: Implement caller-owned connected-stream attachment so proxy, Unix-socket, prerecorded, and externally discovered transports can use the full client.
- [DONE] T022: Implement the default client TLS provider using `SslStream`, TLS 1.2-or-newer platform policy, host/service identity checks, revocation options, and optional client certificates.
- [DONE] T023: Implement implicit TLS client connection on port 995 with no plaintext POP3 bytes consumed before the handshake.
- [DONE] T024: Implement STLS client negotiation as a pipeline barrier, including positive-response gating, plaintext-buffer discard, capability invalidation, TLS upgrade, and post-TLS CAPA refresh.
- [DONE] T025: Implement initial, explicit, and post-authentication CAPA refresh plus interpreted TOP/USER/SASL/RESP-CODES/LOGIN-DELAY/PIPELINING/EXPIRE/UIDL/IMPLEMENTATION/STLS/AUTH-RESP-CODE/UTF8/LANG values.
- [DONE] T026: Implement conservative CAPA-unavailable behavior and configurable USER/TOP/UIDL probing without assuming security-sensitive or pipelining capabilities.
- [DONE] T027: Implement strict RFC 4648 Base64 validation/encoding for SASL, including empty values, padding rules, arbitrary challenge sizes, and cancellation markers.
- [DONE] T028: Implement the generic client SASL exchange driver with optional initial response, oversized-initial fallback, multi-step challenge handling, retryable failure, cancellation, and success-without-extra-data enforcement.
- [DONE] T029: Implement `Pop3PlainSaslClient` with UTF-8 authzid/authcid/password encoding, NUL layout validation, secret clearing where possible, and protected-transport policy.
- [DONE] T030: Implement USER/PASS client authentication with USER rejection short-circuiting, PASS redaction, UTF8=USER rules, and cleartext credential policy.
- [DONE] T031: Implement APOP greeting challenge extraction and MD5 digest generation with explicit legacy opt-in, UTF8=USER handling, and no reusable secret/log exposure.
- [DONE] T032: Implement automatic client authentication selection across caller SASL hooks, optional APOP, and USER/PASS using advertised mechanisms, caller order, TLS strength, and fallback policy.
- [DONE] T033: Implement classified authentication exceptions from AUTH/LOGIN-DELAY/IN-USE/SYS/TEMP/SYS/PERM responses while retaining the complete peer response.
- [DONE] T034: Implement optional SASL security-stream activation at the exact RFC 5034 octet boundary, discard pre-layer knowledge/buffers, and refresh capabilities.
- [DONE] T035: Implement STAT and mailbox snapshot refresh, including zero-message maildrops and count/size overflow or contradiction rejection.
- [DONE] T036: Implement LIST for one and all messages, one-based API validation, deleted/missing response handling, and batch result consistency checks.
- [DONE] T037: Implement UIDL for one and all messages, unique-ID grammar checks, duplicate detection, stable string preservation, and unsupported-command behavior.
- [DONE] T038: Implement RETR-to-stream and bounded RETR-to-byte-array operations with advisory-size handling, exact raw-octet output, progress, cancellation, and output ownership rules.
- [DONE] T039: Implement TOP-to-stream and bounded TOP-to-byte-array operations for zero and positive body-line counts, preserving the header separator and server-returned representation.
- [DONE] T040: Implement single, selected-batch, and all-message DELE operations with prevalidation and per-command failure reporting.
- [DONE] T041: Implement RSET, NOOP, QUIT-commit, and abort/rollback client operations with correct state and connection teardown behavior.
- [DONE] T042: Implement LANG listing, selection, wildcard reset, UTF-8 description validation, selected-language state, and error handling.
- [DONE] T043: Implement UTF8 enablement and independent UTF8=USER credential capability tracking, including STLS ordering restrictions and UTF8 response-code retry guidance.
- [DONE] T044: Implement ordered client pipelining for safe independent commands, response correlation, mixed multiline framing, dependency barriers, and failure synchronization.
- [DONE] T045: Implement pipelined bulk LIST, UIDL, RETR, TOP, and DELE methods with complete argument/sink validation before any network write and sequential fallback.
- [DONE] T046: Implement client raw extension methods for explicitly classified single-line and multiline replies without allowing unknown commands to silently mutate tracked standard state.
- [DONE] T047: Assemble `Pop3Client` over the client engine and expose every `IPop3Client` operation with the planned single-constructor/no-overload surface.
- [DONE] T048: Implement server connection state, greeting generation with collision-resistant RFC message-ID APOP timestamp, endpoint context, timeout tracking, and deterministic close behavior.
- [DONE] T049: Implement the default server TLS provider using `SslStream`, supplied certificate/options, TLS 1.2-or-newer policy, and optional client-certificate handling.
- [DONE] T050: Implement implicit TLS server-session startup and plaintext server-session startup over caller-owned accepted streams.
- [DONE] T051: Implement the server command reader/dispatcher with case-insensitivity, syntax/length checks, state legality, unknown-command errors, ordered responses, and inactivity reset.
- [DONE] T052: Implement server CAPA construction in AUTHORIZATION and TRANSACTION with TLS/auth/provider/language/UTF8 state changes, extension merging, and registered capability argument rules.
- [DONE] T053: Implement server STLS as an AUTHORIZATION-only pipeline barrier, reject repeated/late upgrades, emit `+OK` before handshaking, discard buffered plaintext, and rebuild capabilities afterward.
- [DONE] T054: Implement USER/PASS server sequencing and authentication-provider calls with protected-transport policy, uniform errors, login-delay classification, and no account enumeration.
- [DONE] T055: Implement APOP server validation plumbing with per-connection challenge, strict lower/upper hexadecimal digest acceptance rules, provider classification, and replay-resistant challenge generation.
- [DONE] T056: Implement generic server AUTH mechanism selection, optional initial response, strict Base64 challenge loops, client `*` cancellation, failed-exchange reset, and rejection of AUTH after success.
- [DONE] T057: Implement built-in server SASL PLAIN decoding and authentication-provider calls, rejecting malformed NUL fields/UTF-8 and advertising it only under the configured protected-stream policy.
- [DONE] T058: Implement arbitrary server SASL factory/session hooks and exact activation ordering for optional SASL security streams layered beneath POP3 and inside TLS.
- [DONE] T059: Implement post-authentication maildrop acquisition with success, IN-USE, LOGIN-DELAY, SYS/TEMP, and SYS/PERM outcomes and release-before-negative-response guarantees.
- [DONE] T060: Implement the server mailbox transaction snapshot with stable one-based numbering, UID/size validation, deletion mark tracking, and visible STAT recomputation.
- [DONE] T061: Implement server STAT with deleted-message exclusion, signed count/size safety, and localized response text.
- [DONE] T062: Implement server single/all LIST with representation-aware sizes, deleted-message rules, multiline framing, and UTF8 failure codes.
- [DONE] T063: Implement server single/all UIDL with unique-ID stability/grammar enforcement, deleted-message rules, multiline framing, and capability control.
- [DONE] T064: Implement server RETR streaming from the mailbox provider through representation conversion and dot stuffing without whole-message buffering.
- [DONE] T065: Implement server TOP streaming with full headers, exactly one separator, requested body-line count, end-of-message handling, and UTF8 representation behavior.
- [DONE] T066: Implement server DELE, NOOP, and RSET with repeated/invalid/deleted-number errors and restored STAT metadata.
- [DONE] T067: Implement server QUIT in AUTHORIZATION and transactional UPDATE, including atomic provider commit, rollback on commit failure, lock release, final response, and connection close.
- [DONE] T068: Implement rollback/release on EOF, explicit abort, cancellation, timeout, malformed framing, provider failure, and TLS/SASL failure so disconnect never deletes messages.
- [DONE] T069: Implement RFC-compliant server inactivity autologout with the ten-minute minimum, no response on expiry, and no UPDATE transition.
- [DONE] T070: Implement server PIPELINING command intake and strictly ordered output while enforcing STLS, AUTH exchange, QUIT, and provider-stream barriers.
- [DONE] T071: Implement server UTF8/UTF8=USER state, strict UTF-8 validation, representation selection/conversion contract, LIST size agreement, command ordering, and `[UTF8]` failures.
- [DONE] T072: Implement server LANG list/select/reset and language-provider response lookup in both legal states without leaking user preference before authentication.
- [DONE] T073: Implement safe server extension capability/command dispatch with state declaration, standard-command collision rejection, response framing, logging, limits, and failure containment.
- [DONE] T074: Assemble `Pop3Server` over the server engine with injected authentication/mailbox providers and expose the one-connection `IPop3Server` surface.
- [DONE] T075: Create reusable test streams for fragmentation, short writes, injected EOF, cancellation, timeout, TLS-boundary injection, and deterministic duplex client/server scripts.
- [DONE] T076: Create reusable fake authentication, SASL, mailbox, TLS, language, logger, transfer, and extension providers with observable call/ownership/commit state.
- [DONE] T077: Test every public enum value, static constant, data-field default/round trip, constructor, contract implementation, and exception context exposed by `src/Api`.
- [DONE] T078: Test command parsing/writing across valid casing/spacing/arguments and every malformed/control/length boundary at 254/255/256 octets.
- [DONE] T079: Test greeting/single-response parsing and writing across positive/negative text, timestamps, all registered/unknown hierarchical codes, and 511/512/513-octet boundaries.
- [DONE] T080: Test multiline reading/writing for empty data, empty lines, leading dots, terminator lookalikes, CRLF preservation, fragmented boundaries, short writes, large streaming data, cancellation, and premature EOF.
- [DONE] T081: Test CAPA parsing/generation for every registered capability, parameter variations, duplicates, unknowns, case-insensitivity, state changes, and line limits against IANA/RFC examples.
- [DONE] T082: Test LIST, UIDL, STAT, LANG, decimal, UTF-8, and Base64 parsers with valid RFC examples, overflow, malformed syntax, duplicates, bad padding, and invalid encodings.
- [DONE] T083: Test client connect-host/connect-stream, ownership, greetings, IPv4/IPv6 loopback, timeouts, cancellation, negative greeting, EOF, and all observable snapshot transitions.
- [DONE] T084: Test implicit TLS and STLS client flows with trusted test certificates, required/optional policy, identity/revocation failure, repeated/late STLS, no pipelining, capability refresh, and injected plaintext-buffer discard.
- [DONE] T085: Test USER/PASS client authentication success/failure, USER short-circuiting, redaction, TLS policy, UTF8=USER, response-code classification, and retry behavior.
- [DONE] T086: Test APOP challenge/digest vectors, malformed/missing timestamps, legacy opt-in, UTF8 credentials, negative responses, and absence of secret material in logs/state.
- [DONE] T087: Test generic client SASL and built-in PLAIN for initial/empty/oversized responses, multi-step exchanges, malformed Base64, cancellation, retry, mechanism ordering, security stream activation, and capability reset.
- [DONE] T088: Test automatic authentication selection and downgrade resistance for changing pre/post-TLS capabilities, unsupported hooks, weak mechanisms, APOP policy, and USER/PASS fallback.
- [DONE] T089: Test client STAT/LIST/UIDL for empty and populated maildrops, one/all variants, malformed server data, deleted/missing messages, UID uniqueness, and capability absence.
- [DONE] T090: Test client RETR/TOP stream and memory variants for exact bytes, dot unstuffing, zero TOP lines, misleading sizes, memory ceilings, progress, cancellation, output ownership, and large messages.
- [DONE] T091: Test client DELE/RSET/NOOP/QUIT/abort operations, batch prevalidation, partial batch errors, commit intent, rollback intent, and illegal-state guards.
- [DONE] T092: Test client UTF8 and LANG operations, state/order restrictions, invalid language/UTF-8 data, wildcard reset, representation retry signaling, and snapshot fields.
- [DONE] T093: Test client pipelines and sequential fallback for mixed LIST/UIDL/RETR/TOP/DELE responses, barriers, ordered correlation, mid-pipeline negatives, malformed replies, and resynchronization decisions.
- [DONE] T094: Test client raw extension single/multiline paths, limits, logging/progress, unknown capability retention, and protection of tracked standard state.
- [DONE] T095: Test server greeting, command syntax/casing, every state/command legality combination, unknown commands, line limits, response casing, timeout reset, and negative error behavior.
- [DONE] T096: Test server CAPA in both states for every registered capability and argument, provider/TLS/auth/UTF8 differences, unknown extensions, collision rejection, and exact RFC multiline framing.
- [DONE] T097: Test server plaintext, implicit TLS, and STLS flows including required security, certificates, buffer injection, repeated/late upgrade, capability rebuild, and handshake failure rollback.
- [DONE] T098: Test server USER/PASS, APOP, AUTH PLAIN, and arbitrary SASL hooks across success, retries, cancellation, malformed data, security layers, uniform failures, response codes, login delays, and no credential logging.
- [DONE] T099: Test server maildrop acquisition and stable transaction snapshots for empty/populated mailboxes, concurrent IN-USE attempts, provider validation errors, and release on rejection.
- [DONE] T100: Test server STAT/LIST/UIDL for exact one/all output, deleted exclusion, stable numbering/UIDs, size overflow, malformed providers, UTF8 representations, and missing/deleted-number errors.
- [DONE] T101: Test server RETR/TOP exact raw output, CRLF conversion, dot stuffing, octet announcements, zero/short/excess TOP counts, UTF8 errors, provider streaming failures, and no whole-message buffering assumption.
- [DONE] T102: Test server DELE/RSET/NOOP/QUIT UPDATE semantics, repeated marks, restored totals, atomic commit success/failure, final response order, and provider release.
- [DONE] T103: Test rollback with no deletion for EOF, abort, cancellation, inactivity expiry, malformed command, TLS/SASL failure, logger/observer behavior, and provider retrieval errors.
- [DONE] T104: Test server pipelining for legal batches, ordered mixed replies, auth/STLS/QUIT barriers, buffered post-barrier injection rejection, and transaction consistency after negatives.
- [DONE] T105: Test server UTF8/LANG/localization and custom extensions in both legal states, privacy behavior before auth, response-code selection, limits, and extension failure isolation.
- [DONE] T106: Add end-to-end in-memory duplex tests that run `Pop3Client` against `Pop3Server` for USER/PASS, APOP, SASL PLAIN, STLS, implicit TLS, all mailbox commands, pipelining, UTF8, LANG, commit, and rollback.
- [DONE] T107: Add test-only MailKit interoperability coverage against `Pop3Server` for connect/TLS/auth/CAPA/STAT/LIST/UIDL/RETR/TOP/DELE/RSET/QUIT and dot-stuffed messages.
- [DONE] T108: Add RFC transcript conformance tests derived from RFCs 1939, 2449, 2595, 3206, 5034, and 6856 for both parser-level and end-to-end client/server behavior.
- [DONE] T109: Configure line/branch coverage collection and close every uncovered reachable branch in the `src/Api` implementations and their internal behavior through functional tests.
- [DONE] T110: Run the full test suite on the solution, inspect warnings as errors and coverage output, and resolve all functional, interoperability, resource-ownership, and public-API coverage gaps.
