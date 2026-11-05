# Universal JMAP Core and Mail development plan

## Scope and completion target

Build a transport- and storage-neutral .NET library that can be used on both sides of RFC 8620 JMAP Core and RFC 8621 JMAP Mail. The library must expose exact wire data models, a low-level batch API, convenient typed client operations, and a host-neutral server dispatcher backed by caller-supplied services. Completion means every mandatory Core/Mail object, method, error, state transition, reference form, binary endpoint, and EventSource behavior in those RFCs is represented, validated, encoded/decoded, dispatched, and covered through the public API.

The implementation under `src/` will use only built-in .NET APIs, principally `System.Text.Json`, `System.Net.Http`, `System.IO`, `System.Buffers`, `System.Text`, `System.Threading`, and `System.Threading.Tasks`. `ReadOnlySpan<byte>`, `Span<byte>`, `ReadOnlyMemory<byte>`, `Memory<byte>`, pooled buffers, and streaming APIs should be used where they make parsing or binary transfer clearer without leaking lifetimes across asynchronous boundaries. External packages may be used only by `tests/`.

The current implementation scope is RFC 8620 plus RFC 8621. RFC 8887 WebSocket transport, RFC 9007 MDNs, RFC 9219 S/MIME verification, RFC 9404 Blob management, RFC 9425 Quotas, RFC 9610 Contacts, RFC 9661 Sieve, RFC 9670 Sharing, and RFC 9749 Web Push VAPID are separate extensions. The Core design must preserve unknown capabilities, methods, properties, data types, and error codes so these can be added later without breaking the base API, but this plan does not implement those extensions.

## Research basis and conclusions

- Normative sources: [RFC 8620](specs/rfc/rfc8620.txt), its [inline verified errata](specs/rfc/rfc8620-inline-errata.html) and [full errata report](specs/rfc/rfc8620-errata.html), [RFC 8621](specs/rfc/rfc8621.txt), its [errata report](specs/rfc/rfc8621-errata.html), and the downloaded [IANA JMAP registry](specs/iana/jmap.xml) and [keyword registry](specs/iana/imap-jmap-keywords.xml).
- Supporting JSON, URI-template, date, message, and MIME specifications and all provenance are indexed in [specs/SOURCES.md](specs/SOURCES.md).
- Stalwart's [jmap-client](specs/reference/jmap-client/README.md) demonstrates the value of a session-caching client, typed convenience calls, batch builders, result-reference helpers, streaming upload/download, and EventSource parsing alongside a raw protocol layer.
- Linagora's [jmap-client-ts](specs/reference/jmap-client-ts/README.md) reinforces keeping the transport pluggable and offering automatic primary-account selection, while its narrower method/type surface is another checklist for methods a complete implementation must not omit.
- Apache James's [annotated implementation specifications](specs/reference/apache-james/) are a useful negative checklist: partial implementations commonly omit tree mailbox queries, real threads, `:all` header selection, `Email/queryChanges`, `Email/copy`, `Email/parse`, stored submission state, and push. Each of those is explicitly included below.
- Stalwart's split between `jmap-proto` parsing/reference/error components and server handlers supports keeping codecs, validation, reference resolution, dispatch, and backend services independent. The downloaded [reference resolver sources](specs/reference/stalwart/) also highlight that result-reference paths and patch paths need their own strict implementations rather than ad hoc JSON traversal.
- Reference implementations are informative only. Where their behavior differs or is incomplete, the RFC and applicable verified errata govern.

## Syntax-policy design decisions

- Every public type lives under `src/Api` in its required category. Public field-only DTO classes go in `Data`; operation-only interfaces go in `Contracts`; string and numeric constants go in `Static`; enums go in `Enums`; direct `System.Exception` subclasses go in `Exceptions`; and the concrete codec, builder, HTTP transport, client, EventSource reader, and server dispatcher go in `Classes`.
- DTOs use public fields, not properties, records, structs, inheritance, or generic user-defined types. Dynamic protocol maps use built-in `Dictionary<string, ...>` and unknown JSON uses `JsonElement` clones. Recursive filter/body models use ordinary class references.
- Because nullable value types are forbidden, an optional scalar uses a value field plus a `Has...` boolean; a protocol `number|null` uses value, `Has...`, and `Is...Null` fields when omission and explicit null differ. Optional references use nullable reference fields.
- `System.Text.Json` source-generation attributes and custom attributes are unavailable. Internal codecs will explicitly read and write protocol property names, tuple-shaped invocations, dynamic `header:*` properties, omission/default rules, and unknown members.
- Public APIs are non-generic and have unique instance method names. Asynchronous operations return built-in `Task`/`Task<T>` without requiring `await` syntax in `src/`; synchronous span-based codec operations remain separate and uniquely named.
- Public enums are used only for closed library classifications and always include an unknown value where the wire registry can grow. Extensible wire tokens such as capability URIs, mailbox roles, keywords, method names, properties, and error codes remain strings, with known values supplied by static classes.
- Server hosting, authentication credential acquisition, persistence, indexing, MIME storage, and SMTP delivery are injected contracts. The library owns RFC parsing, validation, sequencing, reference resolution, limits, state/error envelopes, HTTP endpoint semantics, and mapping between wire calls and those contracts.

## Complete feature inventory

### Core values, JSON, and extensibility

- JMAP `Id` validation (1–255 ASCII `A-Z`, `a-z`, `0-9`, `-`, `_`), creation IDs and `#creationId` references, safe signed `Int`, non-negative `UnsignedInt`, normalized RFC 3339 `Date`, UTC `UTCDate`, UTF-8, I-JSON duplicate/member/number rules, and exact distinction between missing, null, false, zero, and empty collections.
- Forward-compatible capture and round-trip of unknown capability objects, request/response members, method arguments, object properties, vendor method names, data-type names, and error fields.
- Case-sensitive protocol names; stable ordered invocation arrays; deterministic JSON output for tests without changing semantically unordered map behavior.
- JSON Pointer escaping (`~0`, `~1`), patch-object rules (no array interiors, no overlapping ancestor/descendant paths, null means reset/delete as specified), and RFC 8620 result-reference path grammar including wildcards and array/object traversal.

### Session discovery and capabilities

- Direct authenticated GET of a supplied Session URL and username-domain discovery through `_jmap._tcp` DNS SRV followed by `https://host[:port]/.well-known/jmap`; redirect handling with credential/authority safety and a bounded redirect count.
- Full Session model: `capabilities`, `accounts`, `primaryAccounts`, `username`, `apiUrl`, `downloadUrl`, `uploadUrl`, `eventSourceUrl`, and `state`; cache refresh when API `sessionState` changes; correct cache-control behavior on the server.
- Full Account model: name, personal/read-only flags, per-account capabilities, capability-based primary-account selection, and enforcement of account capability/read-only restrictions.
- Core capability limits: upload/request sizes, concurrent uploads/requests, calls per request, objects per get/set, and supported collation algorithms.
- Mail account capability limits and permissions: maximum mailboxes per email, mailbox depth, UTF-8 mailbox-name size, attachment total, query sort properties, and top-level mailbox creation permission.
- Submission capability: maximum delayed send and arbitrary SMTP extension-to-arguments map. Vacation response empty capability. Unknown and vendor capability preservation.
- RFC 6570 level-1 expansion of the account/blob/type/name download template, account upload template, and types/closeafter/ping EventSource template with correct percent encoding.

### Requests, responses, batching, references, and errors

- `Invocation` three-element JSON array, request `using`/ordered `methodCalls`/optional `createdIds`, response ordered `methodResponses`/optional `createdIds`/`sessionState`, capability opt-in, sequential call execution, multiple responses with the originating call ID, and `Core/echo` arbitrary JSON echo.
- Low-level construction of arbitrary standard or vendor calls; unique call-ID generation; explicit call IDs; capability collection; method response lookup; typed and raw response access; created-ID carry-over for proxies or split requests.
- Back-references expressed as `#argumentName` plus `resultOf`, response `name`, and `path`; resolution only against previous responses; type/shape validation; invalid-reference method error; creation-ID substitution inside create/update data.
- Request-level problem details for `unknownCapability`, `notJSON`, `notRequest`, and `limit`, including HTTP status, detail, limit name, media type, and unknown fields.
- General method errors: `serverUnavailable`, `serverFail`, `serverPartialFail`, `unknownMethod`, `invalidArguments`, `invalidResultReference`, `forbidden`, `accountNotFound`, `accountNotSupportedByMethod`, and `accountReadOnly`; unknown errors treated like server failure while preserving their token/data.
- Standard method errors: `requestTooLarge`, `cannotCalculateChanges`, `stateMismatch`, `anchorNotFound`, `unsupportedFilter`, `unsupportedSort`, `tooManyChanges`, source-account errors, and `alreadyExists` with `existingId`.
- Per-record SetError support for `forbidden`, `overQuota`, `tooLarge`, `rateLimit`, `notFound`, `invalidPatch`, `willDestroy`, `invalidProperties` with property list, and `singleton`, plus all Mail-specific SetErrors listed below.
- Client exception mapping that retains raw response/problem/error data, call ID, method name, HTTP status, and retry-relevant classification; server method failures remain in HTTP-success JMAP responses except request-level failures.
- Accept-Language parsing and server-selected localization for genuinely user-visible strings, with Content-Language on responses; protocol tokens and diagnostic descriptions remain stable/non-localized.

### Standard method families

- `/get`: account, optional/null IDs, optional/null property selection, all-object behavior, default properties, ordered list, not-found IDs, state, object-count limits, and `requestTooLarge`.
- `/changes`: since-state, optional maximum, old/new state, created/updated/destroyed partitions, `hasMoreChanges`, repeated paging until current, deduplication semantics, and `cannotCalculateChanges`.
- `/set`: conditional state, create/update/destroy, atomic method-level failure, per-record success/failure, old/new state, creation ID mapping, patch application, references, maximum combined actions, state mismatch, and singleton handling.
- `/copy`: source/target accounts and conditional states, create map, optional destruction of originals with its own source state, partial per-record results, duplicate/already-existing handling, source and destination capability checks, and implicit follow-up destruction response semantics.
- `/query`: typed recursive AND/OR/NOT filters, comparators, collation, position (including negative), anchor and offset, limit, total calculation, stable ordering, query state, change-calculation flag, server-applied limit, unsupported filter/sort, and missing anchor.
- `/queryChanges`: same filter/sort identity, since-query-state, maximum changes, `upToId`, total calculation, old/new query states, removals and indexed additions, move representation, paging/too-many changes, and cannot-calculate behavior including verified errata.

### Binary data

- Streaming upload POST to the expanded per-account URL with arbitrary media type; raw request body; size/concurrency/account enforcement; response `accountId`, `blobId`, `type`, and `size`; temporary blob lifetime delegated to storage.
- Streaming download GET with expanded account/blob/type/name variables, filename/media-type safety, content disposition and immutable private caching, standard HTTP status handling, access checks, and opaque binary content without base64 conversion.
- Unreferenced-blob behavior: uploader-only access, a separate per-user quota for shared accounts, oldest-first eviction when upload quota is exceeded, at least one-hour retention unless quota forces deletion, expiry refresh on deduplicated re-upload, and no deletion during the method call that removes the last reference.
- `Blob/copy` across accounts with source/target IDs, copied/notCopied ID maps, source/destination validation, and standard create SetErrors plus `notFound`.

### Push and state changes

- StateChange object with push event ID and account-to-data-type-state maps; states for `Mailbox`, `Thread`, `Email`, `EmailDelivery`, `Identity`, `EmailSubmission`, and `VacationResponse`; `EmailDelivery` changes only for newly delivered email.
- PushSubscription object fields `id`, `deviceClientId`, `url`, `keys`, `verificationCode`, `expires`, and `types`; credential ownership and expiry; maximum expiry; immutable URL/keys; secure destruction; `/get` omission/forbidden access to private URL/keys; `/set` restrictions and errors.
- Immediate high-entropy PushVerification POST and race-safe verification update; no state pushes before verification; HTTPS destination policy; credential revocation cleanup; StateChange coalescing and type filtering; `application/json`, TTL, optional Urgency/Topic headers; 429 backoff; optional RFC 8291 P-256/HKDF/AES-128-GCM encryption when keys are supplied.
- EventSource URL options (`types`, `closeafter`, `ping`), `text/event-stream` parsing and generation, `state` events, event IDs, comments/pings, multi-line data, reconnect and last-event ID behavior, close-after modes, cancellation, fragmented UTF-8 input, and malformed-event recovery.
- Generic server state-change publication contract so backends can notify EventSource and external push delivery without coupling storage to HTTP.

### Mailbox and Thread

- Mailbox fields: id, UTF-8 name, nullable parent, extensible role, sort order, email/thread totals and unread totals, full `MailboxRights`, and subscription flag.
- Hierarchy validation: no cycles, depth/name limits, parent existence/rights, unique role constraints, inbox rules, top-level permission, sort order, subscription semantics, rights enforcement, and server-computed counts.
- `Mailbox/get`, `/changes` with `updatedProperties`, `/query`, `/queryChanges`, and `/set`; filters for parent/name/role/any-role/subscribed; sort by order/name; `sortAsTree` and `filterAsTree`; destroy behavior with `onDestroyRemoveEmails`; `mailboxHasChild` and `mailboxHasEmail` errors.
- Thread fields id and ordered email IDs; stable subject/reference-based identity delegated to the mail store; `Thread/get` and `/changes`; updates when membership/order changes and real multi-message threads rather than one-email placeholders.

### Email model, headers, and bodies

- Metadata fields: id, blobId, threadId, mailbox-id set, keyword set, size, receivedAt, hasAttachment, and preview; immutability/server-set rules and draft-only creation.
- Standard keywords `$draft`, `$seen`, `$flagged`, `$answered`, `$forwarded`, `$phishing`, `$junk`, `$notjunk`; lowercase validation for keywords beginning with `$`; arbitrary user keywords; mutual junk/not-junk behavior; IANA-registry compatibility; per-user versus shared semantics delegated through rights.
- Header list preserving order, case-insensitive lookup, original name/value, unfolded/decoded forms, repeated fields, and dynamic `header:NAME:asFORM[:all]` property selection.
- All parsed header forms: Raw, Text, Addresses, GroupedAddresses, MessageIds, Date, and URLs; known default forms; RFC 2047 decoding; RFC 5322/6532 addresses and groups; message IDs; dates; list URLs; `:all`; malformed-header tolerance required by RFC 8621.
- Convenience header properties: messageId, inReplyTo, references, sender, from, to, cc, bcc, replyTo, subject, and sentAt.
- Recursive EmailBodyPart: part/blob IDs, size, headers, name, media type, charset, disposition, content ID, languages, location, and subparts; bodyStructure, selected text/html bodies, attachment selection, and message/rfc822 handling.
- Body values keyed by part ID with decoded text, encoding-problem flag, truncation flag, requested body properties, selective text/HTML/all fetching, and byte truncation without corrupting UTF-8.
- Safe display guidance surfaced as data/utility behavior: never execute HTML, resolve CID/location relationships, distinguish multipart alternatives/related content, and avoid treating unsafe markup as trusted.

### Email operations and search snippets

- `Email/get` with every standard property, arbitrary header properties, body-property projection, fetch flags, body byte limit, not-found handling, and efficient metadata-only versus body-bearing reads.
- `Email/changes` with correct created/updated/destroyed classification across mailbox/keyword/body mutations.
- `Email/query` and `/queryChanges` with thread collapse and filters for mailbox membership/exclusion, dates, sizes, thread keyword quantifiers, keyword presence/absence, attachments, full text, address fields, subject, body, and one/two-element header tests.
- Email sorting by receivedAt, size, from, to, base subject, sentAt, keyword booleans, and thread keyword quantifiers; server-advertised support, collation, stable tie breaking, and collapsed-thread change behavior.
- `Email/set`: create RFC 5322/MIME drafts from header properties and either bodyStructure or simple text/html bodies; enforce exclusive forms and body-part constraints; keyword/mailbox patch updates; deletion; created-ID references; rights/limits; `blobNotFound`, `tooManyKeywords`, and `tooManyMailboxes`.
- `Email/copy`: cross-account copy with mutable mailbox/keyword/receivedAt overrides, duplicate detection, conditional states, optional original destruction, and all Core copy errors.
- `Email/import`: blob-based imports with mailbox IDs, keywords, optional received time, conditional state, per-item created/errors, MIME validity, attachment/message limits, and `blobNotFound`/mailbox errors.
- `Email/parse`: parse one or more uploaded blobs without adding them to the store, apply get-style property/body selection, and return parsed, notParsable, and notFound partitions.
- SearchSnippet fields emailId, nullable highlighted subject and preview; `SearchSnippet/get` using the same Email filter plus explicit email IDs; matching snippets with escaped highlight markers, no false matches, not-found behavior, and HTML/plain-text safety.

### Identity, submission, and vacation response

- Identity fields id, name, immutable email, reply-to, blind-copy list, text/HTML signatures, and mayDelete; `Identity/get`, `/changes`, and `/set`; `forbiddenFrom`, immutable field, default identity, and deletion restrictions.
- EmailSubmission fields id, identityId, emailId, threadId, optional envelope, sendAt, undoStatus, deliveryStatus map, DSN blob IDs, and MDN blob IDs.
- Envelope mail-from and recipient addresses with arbitrary SMTP extension parameter objects; derivation from message headers; advertised extension validation; recipient deduplication and internationalised address handling.
- Delivery status values per recipient: SMTP reply, delivered state, displayed state; extensible string values; delayed-send state transitions `pending`, `final`, `canceled`; cancellation window and immutable fields.
- `EmailSubmission/get`, `/changes`, `/query`, `/queryChanges`, and `/set`; filters identity/email/thread IDs, undo status, before/after; sorts emailId/threadId/sentAt; create/cancel/destroy; `onSuccessUpdateEmail` and `onSuccessDestroyEmail` implicit actions with response ordering and created-ID references.
- Submission errors `invalidEmail`, `tooManyRecipients`, `noRecipients`, `invalidRecipients`, `forbiddenMailFrom`, `forbiddenFrom`, `forbiddenToSend`, and `cannotUnsend`, with useful structured details where defined.
- Singleton VacationResponse fields fixed id, enabled flag, optional from/to dates, subject, text, and HTML body; `VacationResponse/get` and `/set`; date-range/default behavior, state conditions, singleton errors, and capability/account checks.

### Universal client and server layers

- Raw codec and batch builder for proxy, testing, and extension use; typed high-level client methods for every method above; session caching, primary-account resolution, configured/custom authorization, cancellation, HTTP disposal/stream ownership, and explicit retry guidance without automatic replay of unsafe writes.
- Host-neutral server entry points for Session GET, API POST, upload POST, download GET, and EventSource GET using request/response DTOs and streams, so ASP.NET, `HttpListener`, tests, or another host can adapt without a source dependency.
- Ordered dispatcher with capability/method registry, call/size/concurrency/object limits, cancellation, account and rights checks, created-ID context, back-reference resolution, implicit method responses, method-error isolation, and request-level rejection.
- Caller-provided contracts for sessions, each RFC 8621 object family, blobs, push subscriptions/publication, and time. Backends own durable storage, full-text indexing, thread assignment, MIME persistence/construction, submission delivery, ACL source of truth, and monotonic opaque state production while returning typed protocol results.
- Security and interoperability: TLS required outside explicit local/test configuration; authorization never forwarded to an untrusted redirect host; no secrets in errors; strict request sizes/depth/counts; safe URI expansion and filenames; JSON/MIME recursion limits; SSRF-resistant push URL policy hook; push key confidentiality; no localized protocol tokens; Unicode and collation correctness; proxy-safe created IDs.

## Proposed public API and planned source files

Each listed file contains only the public type category allowed for its directory. Names are provisional but define the intended granularity and ownership.

### `src/Api/Data`

- `SessionModels.cs`: `JmapSession`, `JmapAccount`, `JmapCapability`, `JmapCoreCapabilities`, `JmapMailCapabilities`, `JmapSubmissionCapabilities`, `JmapDiscoveryOptions`, and `JmapClientOptions`.
- `ProtocolModels.cs`: `JmapInvocation`, `JmapRequest`, `JmapResponse`, `JmapResultReference`, `JmapProblem`, `JmapMethodError`, `JmapSetError`, `JmapCallResult`, and `JmapRawMethodCall`.
- `StandardMethodModels.cs`: non-generic common request/result components for get, changes, set, copy, query, and queryChanges, including `JmapComparator`, `JmapFilterNode`, `JmapPatchObject`, and `JmapAddedItem`.
- `BlobModels.cs`: `BlobUploadRequest`, `BlobUploadResponse`, `BlobDownloadRequest`, `BlobDownloadResponse`, `BlobCopyRequest`, and `BlobCopyResponse`.
- `PushModels.cs`: `StateChange`, `PushSubscription`, get/set request/response DTOs, `EventSourceOptions`, and `JmapEvent`.
- `MailboxModels.cs`: `Mailbox`, `MailboxRights`, `MailboxFilter`, and all concrete Mailbox get/changes/query/queryChanges/set request and response DTOs.
- `ThreadModels.cs`: `Thread`, and concrete Thread get/changes request and response DTOs.
- `EmailModels.cs`: `Email`, `EmailAddress`, `EmailAddressGroup`, `EmailHeader`, `EmailBodyPart`, and `EmailBodyValue`.
- `EmailQueryModels.cs`: `EmailFilter`, `EmailComparator`, and concrete Email query/queryChanges request and response DTOs.
- `EmailMethodModels.cs`: concrete Email get/changes/set/copy/import/parse request and response DTOs plus `EmailImport`.
- `SearchSnippetModels.cs`: `SearchSnippet` and its get request/response DTOs.
- `IdentityModels.cs`: `Identity` and concrete Identity get/changes/set request and response DTOs.
- `EmailSubmissionModels.cs`: `EmailSubmission`, `EmailSubmissionEnvelope`, `EmailSubmissionAddress`, `EmailDeliveryStatus`, its filter/comparator, and all concrete method DTOs.
- `VacationResponseModels.cs`: `VacationResponse` and its get/set request/response DTOs.
- `ServerModels.cs`: `JmapServerOptions`, `JmapServerServices`, `JmapRequestContext`, host-neutral endpoint request/response DTOs, `JmapDownloadContent`, and `JmapStatePublication`.

### `src/Api/Contracts`

- `IJmapCodec.cs`: unique span/byte/stream encode/decode operations for Session, Request, Response, problems, and state events.
- `IJmapRequestBuilder.cs`: capability, raw call, typed call, result-reference, created-ID, build, and reset operations with unique method names.
- `IJmapTransport.cs`: asynchronous Session/API/upload/download/EventSource transport operations.
- `IJmapAuthorizationProvider.cs`: asynchronous authorization-value acquisition without prescribing Basic, Bearer, or OAuth token refresh.
- `IJmapClient.cs`: discovery, session refresh/access, raw batch execution, echo, and primary-account operations.
- `IBlobClient.cs`, `IPushClient.cs`, `IMailboxClient.cs`, `IThreadClient.cs`, `IEmailClient.cs`, `ISearchSnippetClient.cs`, `IIdentityClient.cs`, `IEmailSubmissionClient.cs`, `IVacationResponseClient.cs`: one uniquely named typed client method for every supported endpoint/method.
- `IJmapEventSourceReader.cs`: connect/read/close operations for streamed state events.
- `IJmapServer.cs`: host-neutral Session/API/upload/download/EventSource processing operations.
- `IJmapSessionService.cs`, `IJmapServiceLocator.cs`, `IJmapCoreService.cs`, `IBlobService.cs`, `IPushService.cs`, `IMailboxService.cs`, `IThreadService.cs`, `IEmailService.cs`, `ISearchSnippetService.cs`, `IIdentityService.cs`, `IEmailSubmissionService.cs`, `IVacationResponseService.cs`: discovery and backend operations consumed by the client or dispatcher.
- `IJmapStatePublisher.cs`, `IJmapClock.cs`, `IJmapPushUrlPolicy.cs`, `IJmapStringLocalizer.cs`: state fan-out, deterministic time, push destination approval, and Accept-Language-aware user-string localization extension points.

### `src/Api/Static`

- `JmapCapabilities.cs`: Core, Mail, Submission, and VacationResponse capability URIs.
- `JmapMethods.cs`: every RFC 8620/8621 method name.
- `JmapErrorCodes.cs`: request, method, SetError, and Mail-specific error strings.
- `JmapDataTypes.cs`: Core/Mail push data-type names including EmailDelivery.
- `JmapMailKeywords.cs`: standard RFC/IANA mail keywords.
- `JmapMailboxRoles.cs`: standard mailbox role strings.
- `JmapProperties.cs`: common property tokens and dynamic header-form suffix tokens.
- `JmapMediaTypes.cs`: JMAP JSON, problem JSON, octet stream, and event-stream media types.
- `JmapProtocolLimits.cs`: safe integer bounds and RFC suggested minimum limit constants.

### `src/Api/Enums`

- `JmapProblemKind.cs`, `JmapMethodErrorKind.cs`, and `JmapSetErrorKind.cs`: closed library classifications with `Unknown` values; wire values remain preserved strings.
- `JmapFilterOperator.cs`, `JmapHeaderForm.cs`, `JmapEventKind.cs`, and `JmapSubmissionUndoStatus.cs`: operational classifications used by DTOs.

### `src/Api/Exceptions`

- `JmapProtocolException.cs`, `JmapValidationException.cs`, `JmapTransportException.cs`, `JmapRequestException.cs`, `JmapMethodException.cs`, `JmapCapabilityException.cs`, and `JmapStateMismatchException.cs`: independent direct subclasses of `System.Exception`, each carrying relevant public data fields and a single constructor.

### `src/Api/Classes`

- `JmapJsonCodec.cs`: `IJmapCodec` implementation using the internal readers/writers.
- `JmapRequestBuilder.cs`: `IJmapRequestBuilder` implementation with ordered calls and reference helpers.
- `JmapHttpTransport.cs`: built-in `HttpClient` implementation of `IJmapTransport`.
- `JmapDnsServiceLocator.cs`: built-in socket implementation of `IJmapServiceLocator` for `_jmap._tcp` SRV discovery.
- `JmapClient.cs`: one class implementing all client operation interfaces and session caching/account selection.
- `JmapEventSourceReader.cs`: `IJmapEventSourceReader` streaming implementation.
- `JmapServer.cs`: `IJmapServer` implementation coordinating validation, dispatch, limits, backend services, endpoint responses, and event streaming.

### Internal implementation files outside `src/Api`

- `src/Internal/Json/JmapJsonReader.cs`, `JmapJsonWriter.cs`, `SessionJsonCodec.cs`, `ProtocolJsonCodec.cs`, and one codec per Core/Mail model family: exact manual JSON mapping and unknown-field retention.
- `src/Internal/Validation/ValueValidator.cs`, `SessionValidator.cs`, `RequestValidator.cs`, `PatchValidator.cs`, `MailboxValidator.cs`, `EmailValidator.cs`, `SubmissionValidator.cs`, and `PushValidator.cs`.
- `src/Internal/Core/UriTemplateExpander.cs`, `JsonPointer.cs`, `PatchApplicator.cs`, `ResultReferencePath.cs`, `ResultReferenceResolver.cs`, `CreationIdResolver.cs`, `MethodRegistry.cs`, `RequestDispatcher.cs`, `RequestLimitGate.cs`, and `StateAccumulator.cs`.
- `src/Internal/Client/SessionDiscovery.cs`, `SessionCache.cs`, `ResponseMapper.cs`, and typed operation adapters for blobs, push, mailboxes, threads, emails, snippets, identities, submissions, and vacation response.
- `src/Internal/Server/EndpointProcessor.cs`, `MethodContext.cs`, `StandardMethodProcessor.cs`, one processor per concrete method family, `EventSourceHub.cs`, and `ProblemFactory.cs`.
- `src/Internal/Mail/HeaderParser.cs`, `HeaderValueParser.cs`, `HeaderPropertySelector.cs`, `BodyPartMapper.cs`, `MimeMessageReader.cs`, `MimeMessageWriter.cs`, `EmailTextMatcher.cs`, and `SearchSnippetBuilder.cs`. These use only built-in .NET APIs and cover the message transformations the protocol layer must perform rather than becoming a general-purpose mail stack.
- `src/Internal/Streaming/EventSourceParser.cs`, `EventSourceWriter.cs`, `LimitedReadStream.cs`, and `Utf8Buffer.cs`.

## Implementation tasks

### Foundation and wire model

- [DONE] T001: Create the planned `src/Api` and internal directory structure and establish a single file-scoped namespace convention without adding external source dependencies.
- [DONE] T002: Implement public static capability, method, error, data-type, keyword, mailbox-role, property, media-type, and numeric-limit constants for all RFC 8620/8621 tokens.
- [DONE] T003: Implement the public enums with explicit Int32 values, `Unknown` members where needed, and usage from at least one public DTO or interface.
- [DONE] T004: Implement Session, Account, Core capability, Mail capability, Submission capability, Vacation capability, discovery-option, and client-option DTOs with field-based optional/null representation.
- [DONE] T005: Implement invocation, request, response, raw call/result, result-reference, problem-details, method-error, and SetError DTOs with unknown JSON member storage.
- [DONE] T006: Implement non-generic standard get, changes, set, copy, query, queryChanges, comparator, recursive filter, patch, and indexed-addition DTO components.
- [DONE] T007: Implement all blob, push subscription, state-change, EventSource, and host-neutral binary stream DTOs.
- [DONE] T008: Implement all Mailbox, MailboxRights, Mailbox filter, and concrete Mailbox method request/response DTOs.
- [DONE] T009: Implement Thread and concrete Thread get/changes request/response DTOs.
- [DONE] T010: Implement Email metadata, address/group, header, recursive body-part, body-value, filter, comparator, and all concrete Email method DTOs.
- [DONE] T011: Implement SearchSnippet, Identity, EmailSubmission/envelope/address/delivery-status, VacationResponse, and all of their concrete method DTOs.
- [DONE] T012: Implement server options, services bundle, request context, endpoint request/response, downloadable content, and state-publication DTOs.
- [DONE] T013: Implement all public client, service-locator, transport, codec, builder, server, backend service, state publisher, clock, push URL policy, and string-localizer interfaces with unique non-overloaded method names.
- [DONE] T014: Implement all direct `System.Exception` public exception types and preserve HTTP status, method, call ID, error token, structured data, and retry classification where applicable.

### JSON codecs and validation

- [DONE] T015: Implement bounded UTF-8/I-JSON reading over spans, memories, and streams with duplicate-member rejection, safe-integer enforcement, nesting limits, and cloned unknown `JsonElement` values.
- [DONE] T016: Implement deterministic streaming JSON writing with correct escaping, safe numbers, field omission/default behavior, explicit null behavior, and no dependency on serializer attributes.
- [DONE] T017: Implement Session and capability codecs, including arbitrary capability URI objects and unknown Session/Account members.
- [DONE] T018: Implement tuple-shaped Invocation and Request/Response codecs with ordered calls, optional created IDs, session state, arbitrary method arguments, and multiple responses per call ID.
- [DONE] T019: Implement problem, method error, and SetError codecs covering every Core/Mail field while preserving unrecognized codes and fields.
- [DONE] T020: Implement codecs for all standard method components and concrete Mailbox/Thread method models.
- [DONE] T021: Implement codecs for the complete Email/header/body/query/import/parse/snippet model and dynamic `header:*` property names.
- [DONE] T022: Implement codecs for Identity, EmailSubmission, VacationResponse, Blob, PushSubscription, StateChange, and EventSource event models.
- [DONE] T023: Implement Id, creation-ID, Int, UnsignedInt, Date, UTCDate, capability, method-name, keyword, mailbox-role, media-type, and UTF-8 size validators.
- [DONE] T024: Implement model validators that distinguish request-writable, immutable, and server-set fields and return precise invalid-property paths.
- [DONE] T025: Implement the public `JmapJsonCodec` operations over byte arrays, read-only spans/memories, and streams using the internal codecs.

### Core request mechanics

- [DONE] T026: Implement RFC 6570 level-1 template parsing/expansion for all Session endpoint variables with strict percent encoding and missing-variable errors.
- [DONE] T027: Implement JSON Pointer token parsing/escaping and separate validated patch-path traversal without array-interior updates.
- [DONE] T028: Implement PatchObject conflict detection and application, including null reset/removal semantics and rejection of overlapping paths.
- [DONE] T029: Implement RFC 8620 result-reference path parsing and evaluation over prior response arguments, including object/array selection and wildcard projection.
- [DONE] T030: Implement result-reference substitution for `#argumentName`, previous-call/name matching, type validation, and `invalidResultReference` generation.
- [DONE] T031: Implement request-scoped creation-ID accumulation, `#creationId` substitution in create/update data, carry-in/carry-out maps, and unresolved-reference SetErrors.
- [DONE] T032: Implement the public ordered request builder with capability deduplication, generated or explicit call IDs, arbitrary calls, typed calls, reference helpers, created IDs, build, and reset.
- [DONE] T033: Implement request validation for required capabilities, unique/usable call IDs, maximum calls/size, valid invocation shape, sequential semantics, and ignored unknown top-level members.
- [DONE] T034: Implement response correlation and typed/raw result mapping, including multiple responses from one call and per-call method error conversion.
- [DONE] T035: Implement `Core/echo` with lossless arbitrary JSON argument echo and standard invocation/call-ID behavior.
- [DONE] T036: Implement common `/get` request validation, property projection, ID ordering/notFound behavior, object limits, state response, and requestTooLarge mapping.
- [DONE] T037: Implement common `/changes` validation and result handling, created/updated/destroyed classification, maxChanges paging, hasMoreChanges, and cannotCalculateChanges.
- [DONE] T038: Implement common `/set` sequencing, ifInState, combined action limits, per-record result partitions, atomic method failures, state transitions, and SetError mapping.
- [DONE] T039: Implement common `/copy` source/target checks, conditional states, duplicate detection, per-record results, optional source destruction, and implicit follow-up response ordering.
- [DONE] T040: Implement recursive query filter/comparator validation, collation checks, stable ordering metadata, position/negative position, anchor/offset, limits, totals, and standard query errors.
- [DONE] T041: Implement queryChanges removal/addition/move representation, upToId behavior, total calculation, paging, tooManyChanges, and cannotCalculateChanges with RFC 8620 errata applied.
- [DONE] T042: Implement request-level RFC 7807 response generation and parsing for unknownCapability, notJSON, notRequest, and limit, including correct media type/status/limit data.
- [DONE] T043: Implement general method-error and SetError factories/classification, preserving unknown IANA/vendor errors, enforcing no state change except serverPartialFail, and localizing only user-visible strings selected from Accept-Language.

### Discovery, HTTP, and binary endpoints

- [DONE] T044: Implement direct Session retrieval and validated Session caching, including stale detection from response `sessionState` and explicit refresh.
- [DONE] T045: Implement `JmapDnsServiceLocator` using built-in sockets for `_jmap._tcp` SRV queries, DNS compression parsing, priority/weighted selection, IDNA, UDP truncation with TCP retry, timeout/cancellation, and deterministic `.well-known/jmap` fallback when no SRV record exists.
- [DONE] T046: Implement bounded redirect handling that follows permitted Session redirects without forwarding authorization to an untrusted authority.
- [DONE] T047: Implement `JmapHttpTransport` Session/API operations with built-in `HttpClient`, authorization provider integration, cancellation, content negotiation, response-size limits, and stream ownership rules.
- [DONE] T048: Implement upload URL expansion and streaming upload with arbitrary content type, content length/limits, cancellation, and typed upload/problem response handling.
- [DONE] T049: Implement download URL expansion and streaming download with safe requested filename/type, immutable private caching metadata, access/error handling, cancellation, and no base64 buffering.
- [DONE] T050: Implement server upload/download endpoint processors with account checks, configured size/concurrency/quota limits, uploader-only unreferenced access, oldest-first eviction, minimum lifetime/dedup refresh, safe content disposition, and backend blob streaming.
- [DONE] T051: Implement `Blob/copy` dispatch and client mapping with cross-account capability/access checks, copied ID mapping, and notCopied standard create SetErrors plus notFound.

### Push and EventSource

- [DONE] T052: Implement PushSubscription model validation, credential ownership/revocation, device identity, expiry bounds, immutable URL/keys, type filtering, high-entropy verification codes, secure key erasure, HTTPS and push URL policy checks.
- [DONE] T053: Implement PushSubscription/get and PushSubscription/set dispatch/client operations, including private URL/key projection rules, immediate race-safe PushVerification delivery, code confirmation, expiry update, and no notification before verification.
- [DONE] T054: Implement StateChange aggregation and verified push POST delivery for all RFC 8620/8621 state types, with EmailDelivery only for new mail, coalescing/type filters, TTL/Urgency/Topic headers, 429 backoff, and built-in RFC 8291 P-256/HKDF/AES-GCM encryption.
- [DONE] T055: Implement incremental EventSource parsing across arbitrary byte boundaries, UTF-8 fragments, comments, event/id/data fields, multi-line data, pings, malformed frames, and state JSON.
- [DONE] T056: Implement EventSource serialization and the server event hub with type filters, closeafter modes, ping interval, last-event ID, cancellation, subscriber cleanup, and bounded buffering.
- [DONE] T057: Implement `JmapEventSourceReader` and transport integration with reconnect metadata, clean close, cancellation, and surfaced protocol/transport failures.

### Mailbox and Thread methods

- [DONE] T058: Implement Mailbox field and hierarchy validation for names, roles, parents, cycles, depth, inbox/top-level rules, subscription, rights, sort order, and immutable counts.
- [DONE] T059: Implement Mailbox/get and Mailbox/changes, including default/property projections, changed-property reporting, and correct state classifications.
- [DONE] T060: Implement Mailbox/query filters/sorts plus filterAsTree and sortAsTree ancestor/descendant semantics.
- [DONE] T061: Implement Mailbox/queryChanges with tree-query identity, indexed changes, and standard query-change failure behavior.
- [DONE] T062: Implement Mailbox/set create/update/destroy, role/parent/rights enforcement, onDestroyRemoveEmails, and mailboxHasChild/mailboxHasEmail errors.
- [DONE] T063: Implement Thread/get projections and Thread/changes against backend-provided real thread membership/order and states.

### Email representation and MIME mapping

- [DONE] T064: Implement ordered RFC 5322/6532 header parsing with folding, repeated fields, original raw values, line/count limits, and malformed-input tolerance required by RFC 8621.
- [DONE] T065: Implement RFC 2047 text decoding and Raw/Text dynamic header forms with known-header default-form selection and `:all` behavior.
- [DONE] T066: Implement Addresses and GroupedAddresses parsing/encoding for mailbox/group syntax, internationalized values, invalid-address tolerance, and all specified address headers.
- [DONE] T067: Implement MessageIds, Date, and URLs parsed header forms, including list headers, Content-ID/location, normalized Date/UTCDate, and repeated-value selection.
- [DONE] T068: Implement convenience header-property derivation and creation validation preventing duplicate semantic headers, forbidden Content headers, and invalid parsed creation forms.
- [DONE] T069: Implement bounded MIME reading for multipart nesting, transfer encodings, charset conversion, RFC 2231 parameters, content metadata, message/rfc822, and opaque unknown media types.
- [DONE] T070: Implement body-structure mapping, textBody/htmlBody choice, attachment classification, hasAttachment, preview, body values, encoding-problem detection, and UTF-8-safe truncation.
- [DONE] T071: Implement MIME writing for Email/set draft creation from bodyStructure or simple text/html bodies, uploaded blob parts, generated boundaries/headers, and RFC 8621 mutual-exclusion rules.
- [DONE] T072: Implement Email keyword/mailbox set validation, standard/user keyword rules, mailbox rights, account limits, immutable/server-set fields, and attachment-size enforcement.
- [DONE] T073: Implement safe text extraction and match tokenization used by Email filters and snippets, including phrases, case-insensitive Unicode matching, HTML text extraction, and header decoding.

### Email and SearchSnippet methods

- [DONE] T074: Implement Email/get property/header/body projection, fetchTextBodyValues/fetchHTMLBodyValues/fetchAllBodyValues precedence, body properties, byte limit, and metadata-only reads.
- [DONE] T075: Implement Email/changes state classification for creation, metadata/body update, mailbox/keyword update, thread impact, and destruction.
- [DONE] T076: Implement every Email/query filter, including mailbox exclusion, date/size, keyword/thread quantifiers, attachment, text/address/subject/body, and header name/value forms.
- [DONE] T077: Implement every Email comparator, base-subject/address comparison, keyword/thread boolean sort, stable tie breaking, advertised sort support, and collapseThreads.
- [DONE] T078: Implement Email/query and Email/queryChanges dispatch/client operations, including collapsed-thread state/change semantics and standard pagination/errors.
- [DONE] T079: Implement Email/set draft create, keyword/mailbox patch update, destroy, MIME/blob integration, state checks, created IDs, and all Email-specific SetErrors.
- [DONE] T080: Implement Email/copy with source/target states, mutable overrides, duplicate detection, optional source destruction, created IDs, and cross-account errors.
- [DONE] T081: Implement Email/import from uploaded blobs with MIME validation, mailbox/keyword/date assignment, state checks, partial results, and all specified errors.
- [DONE] T082: Implement Email/parse over multiple blobs with get-style property/body options and parsed/notParsable/notFound partitions without store insertion.
- [DONE] T083: Implement SearchSnippet/get using the Email filter and requested IDs, highlighted escaped subject/preview fragments, notFound behavior, and safe plain/HTML extraction.

### Identity, submission, and vacation methods

- [DONE] T084: Implement Identity model validation and Identity/get, Identity/changes, and Identity/set including immutable address, defaults, mayDelete, and forbiddenFrom.
- [DONE] T085: Implement submission envelope derivation from Sender/From and To/Cc/Bcc, address deduplication, explicit envelope validation, and arbitrary advertised SMTP extension parameters.
- [DONE] T086: Implement EmailSubmission/get and EmailSubmission/changes with all delivery/DSN/MDN/status fields and persistent backend state.
- [DONE] T087: Implement EmailSubmission/query and EmailSubmission/queryChanges with every filter, sort, pagination, state, and change rule.
- [DONE] T088: Implement EmailSubmission/set creation, delayed send, pending/final/canceled transitions, cancellation/destruction, and immutable field validation.
- [DONE] T089: Implement onSuccessUpdateEmail/onSuccessDestroyEmail as conditional implicit Email/set actions with correct created-ID references and response ordering.
- [DONE] T090: Implement every EmailSubmission SetError, recipient detail, identity/from permission check, delayed-send bound, and cannotUnsend behavior.
- [DONE] T091: Implement VacationResponse/get and VacationResponse/set singleton behavior, optional date interval/body fields, state checks, defaults, and capability/account restrictions.

### Client and server composition

- [DONE] T092: Implement high-level account selection that uses explicit account IDs or capability-specific primary accounts and rejects missing/read-only/unsupported accounts before unsafe calls.
- [DONE] T093: Implement `JmapClient` raw execution, echo, session operations, and all typed blob/push/mail client interfaces over request building, transport, response mapping, and session refresh.
- [DONE] T094: Implement client-side changes and queryChanges continuation helpers that page deterministically without losing state or duplicating IDs.
- [DONE] T095: Implement method and capability registry lookup for every RFC 8620/8621 method plus opaque vendor method passthrough hooks.
- [DONE] T096: Implement ordered server request dispatch with per-request created IDs, result references, method error isolation, implicit responses, cancellation, and sessionState output.
- [DONE] T097: Implement configured request byte/call/object/concurrency limit gates for API and uploads with leak-free release and RFC problem/method errors.
- [DONE] T098: Implement account capability, read-only, ACL/right, state-precondition, and backend-result enforcement consistently before and after each dispatched operation.
- [DONE] T099: Implement `JmapServer` Session/API/upload/download/EventSource interface methods and map all endpoint results to host-neutral HTTP status/header/body/stream DTOs, including Content-Language for selected localized strings.
- [DONE] T100: Implement security bounds for JSON, MIME, headers, filters, references, patches, EventSource queues, filenames, URI variables, push destinations, and error redaction.

### Complete public-API test coverage

- [DONE] T101: Create reusable RFC example fixtures, adversarial JSON/MIME fixtures, fake authorization/clock/state publisher/backends, scripted HTTP handlers, and public-API-only test helpers.
- [DONE] T102: Test every public static field and enum mapping against RFC/IANA tokens, including unknown/extensible token behavior.
- [DONE] T103: Test construction and field behavior of every public Data DTO, including optional scalar presence flags, explicit null, empty collections, recursive models, and unknown JSON.
- [DONE] T104: Test every `IJmapCodec` operation and public DTO round-trip with RFC examples, omitted defaults, invocation tuples, dynamic headers, Unicode, malformed I-JSON, safe integer bounds, and stream fragmentation.
- [DONE] T105: Test every public exception type and client mapping for request-level, method-level, per-record, transport, validation, capability, state, unknown-error, retry-relevant, Accept-Language, and Content-Language cases.
- [DONE] T106: Test the public request builder for ordering, capabilities, call IDs, arbitrary methods, typed calls, created IDs, result references, reset/reuse, and invalid construction.
- [DONE] T107: Test JSON Pointer, patch application, creation-ID substitution, and result-reference resolution transitively through public codec/builder/client/server APIs, including all escape, wildcard, shape, ordering, and failure cases.
- [DONE] T108: Test direct and domain Session discovery, SRV priority/weight/compression/truncation/fallback, redirects/authorization safety, Session validation/caching/refresh, primary accounts, capability limits, URI templates, cancellation, and unknown capabilities through public client APIs.
- [DONE] T109: Test raw and typed HTTP transport behavior for content types, status/problem bodies, disposal, cancellation, bounded reads, uploads, downloads, immutable caching, filenames, and fragmented streams.
- [DONE] T110: Test Core/echo and each standard get/changes/set/copy/query/queryChanges behavior through `IJmapClient` and `IJmapServer`, including all default, limit, state, paging, ordering, partial-result, and error paths.
- [DONE] T111: Test Blob upload/download/copy public APIs with empty/large/chunked binary data, arbitrary media types, immutable caching, unreferenced ownership/quota/eviction/lifetime/dedup rules, cross-account permissions, size/concurrency limits, not found, and cancellation.
- [DONE] T112: Test PushSubscription credential privacy/lifetime, verification race and failures, encrypted and unencrypted POST delivery, headers/429 coalescing, StateChange publication, EmailDelivery distinction, EventSource parsing/writing/reconnect/filter/ping/closeafter/cancellation, malformed frames, and slow-consumer cleanup through public APIs.
- [DONE] T113: Test every Mailbox public field and get/changes/query/queryChanges/set path, including full rights, roles, counts, hierarchy cycles/depth, tree filtering/sorting, subscriptions, destroy modes, and Mailbox-specific errors.
- [DONE] T114: Test Thread/get and Thread/changes with multi-email threads, membership/order mutations, paging, projections, not found, and state errors through public APIs.
- [DONE] T115: Test every Email metadata/header/address/group/message-id/date/URL/body-part/body-value field against plain, internationalized, malformed, nested multipart, related, alternative, attachment, embedded-message, and truncated MIME fixtures.
- [DONE] T116: Test Email/get and Email/changes for every property projection, dynamic header form and `:all`, body fetch flag combination, body property, byte boundary, notFound, and every change classification.
- [DONE] T117: Test every Email/query filter and comparator, boolean nesting, collations, anchors/positions/limits/totals, thread keyword quantifiers, full-text rules, header filters, collapseThreads, unsupported cases, and stable ordering.
- [DONE] T118: Test Email/queryChanges for additions, removals, moves, upToId, totals, collapseThreads, max changes, stale states, immutable/mutable filters, and verified errata behavior.
- [DONE] T119: Test Email/set creation through both body forms, MIME output, blob parts, header restrictions, created-ID references, keyword/mailbox patches, deletion, rights/limits, state mismatch, and every Email SetError.
- [DONE] T120: Test Email/copy, Email/import, and Email/parse across source/target accounts, overrides, conditional states, duplicate and blob failures, valid/invalid MIME, partial batches, property/body projections, and optional original destruction.
- [DONE] T121: Test SearchSnippet/get matching/highlighting for subject/body/header, phrase and Unicode cases, plain/HTML safety, requested ordering, nonmatches, and notFound.
- [DONE] T122: Test Identity get/changes/set for every field, defaults, immutable address, deletion permission, state transitions, account capability, and forbiddenFrom.
- [DONE] T123: Test EmailSubmission envelope derivation/validation, extension parameters, every field, get/changes/query/queryChanges/set, delayed/canceled/final transitions, delivery statuses, DSN/MDN IDs, implicit Email actions, and every submission error.
- [DONE] T124: Test VacationResponse get/set for the singleton ID, all optional fields, enabled/date interval behavior, defaults, state mismatch, singleton errors, and account/capability restrictions.
- [DONE] T125: Test complete multi-call client/server scenarios combining creation IDs, back-references, implicit responses, Mailbox/Email/Submission operations, session state changes, push delivery, partial per-record errors, and continued execution after method errors.
- [DONE] T126: Test vendor-extension interoperability through public APIs: unknown capability objects, arbitrary methods, extra request/response/object/error fields, unknown data types, and round-trip preservation without weakening base validation.
- [DONE] T127: Run the full test suite with line and branch coverage collection, identify every uncovered reachable member/branch in `src/Api`, and add behavior-focused tests until the exposed API layer has complete coverage.
