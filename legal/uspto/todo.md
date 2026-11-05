# Universal USPTO data access library development plan

Research baseline: 2026-08-27. This plan targets .NET 10 and covers official USPTO Open Data Portal (ODP) APIs, TSDR trademark APIs, ODP bulk products, and PatentsView-derived online and bulk data. The library is read-only: it searches, retrieves, downloads, parses, caches, and mirrors public data; it does not file applications, submit PTAB papers, change assignments, or automate authenticated portal pages.

## Research conclusions

| Project or source | Capabilities worth carrying forward | Gaps this plan closes |
|---|---|---|
| `patent-dev/uspto-odp` | Complete ODP Patent Application, Bulk, Petition, PTAB, Office Action, and TSDR wrappers; grant/application/publication/PCT normalization; typed continuity, assignments, PTA, transactions, and foreign priority; ICE XML text extraction; retry and `Retry-After`; safe download-host validation. | No cache, durable mirror, manifest, checksum workflow, mid-stream range resume, generic bulk parsers, local querying, or comprehensive PatentsView table support. |
| `DunlapCoddingPC/pyUSPTO` | Shared configurable HTTP transport, page iterators, raw response retention, patent/status lookup helpers, firm portfolio retrieval, streamed documents, IFW/PTAB/petition archives, archive extraction limits, and path traversal defense. | No TSDR surface, local mirror, checksums/manifests, PatentsView-derived access, or BCL-only implementation. |
| `KennethThompson/uspto_odp` | Explicit coverage of all 53 ODP operations, including both GET and POST search/export variants, direct identifier lookups, typed endpoint models, status-code search, and document downloads. | No retry/cache/mirror/parser framework and limited high-level normalization and bulk processing. |
| `ip-tools/uspto-opendata-python` | Legacy PEDS lessons: application/publication/grant convenience queries, asynchronous export-package polling, ZIP extraction, and document identifier extraction. | PEDS/PBD are retired; only the reusable workflow concepts belong in the new ODP implementation. |
| `PatentsView/PatentSearch-API` | Patent/publication, inventor, assignee, location, attorney, classification, citation, related-application, claim, summary, and description endpoints; JSON query operators; selected fields; sort; `after` cursor pagination. | Online search is transition-sensitive after the March 2026 ODP migration, so local bulk-derived querying and capability detection are required. |
| USPTO specifications in `specs/` | ODP query grammar, OpenAPI schemas, TSDR routes and rate rules, Patent/Trademark XML resource descriptions, and assignment dataset semantics. | Published schemas contain known field-name/type mismatches; parsers must retain raw data, accept documented aliases, and fail clearly on incompatible shapes. |

Current service boundaries that affect the design:

- ODP patent, petition, PTAB, Office Action, and bulk APIs use `https://api.uspto.gov` with an `X-API-Key` header. The same key may cover those API families, but authorization/entitlement failures must remain distinguishable from missing records.
- TSDR uses `https://tsdrapi.uspto.gov` and a separate `USPTO-API-KEY` header. Its metadata and PDF/ZIP routes have different published rate ceilings and therefore need separate rate buckets.
- PatentsView search functions may be paused or moved during the 2026 ODP transition. Direct PatentSearch support must use a configurable base URI and a capability probe; ODP-hosted PatentsView products and local bulk queries are the durable path.
- The Trademark Decisions and Proceedings portal currently has no stable public API specification and relies on browser/WAF state. It must not be scraped. Its bulk products remain discoverable and downloadable through the generic ODP bulk layer.
- Legacy PEDS, PAIR Bulk Data, BDSS, and Developer Hub routes are not implementation targets. Their identifiers and dataset concepts are accepted only to ease migration to ODP.

## Implementation constraints and design rules

- Production code under `src/` uses only .NET runtime/BCL assemblies: notably `System.Net.Http`, `System.Text.Json`, `System.Xml`, `System.IO.Compression`, `System.Formats.Tar`, `System.Security.Cryptography`, and ordinary file/threading APIs. No source NuGet dependencies are permitted.
- Public API declarations follow `syntax.md` exactly: interfaces in `src/Api/Contracts`, data-field classes in `src/Api/Data`, constants in `src/Api/Static`, enums in `src/Api/Enums`, direct `System.Exception` subclasses in `src/Api/Exceptions`, and interface implementations in `src/Api/Classes`.
- Public data is represented by fields, not properties or records. Optional value-type data uses a value field plus a `Has...` boolean; identifiers remain strings so leading zeroes and prefixes cannot be lost.
- Each public implementation class has one constructor and exposes only methods required by its implemented interfaces. Method names are globally distinct across all interfaces implemented by a class; no overloads, generic methods, public static methods, or interface inheritance are planned.
- The public surface is synchronous and cancellation-aware. Network work uses `HttpClient`; internal code may return or coordinate framework tasks without `async`/`await` syntax. Public streaming methods accept `Stream` and `CancellationToken` rather than buffering large files.
- `ReadOnlySpan<char>` and `ReadOnlySpan<byte>` are used internally for identifier scanning, delimited rows, checksum hex, HTTP ranges, and concatenated USPTO XML boundaries. Public models use portable strings, arrays, lists, dictionaries, streams, and framework date/URI types.
- JSON is parsed with `System.Text.Json` using manual internal adapters so field-based models, schema aliases, inconsistent dates, missing-vs-zero state, and unmodeled raw fields work without attributes. XML uses forward-only secure readers with DTD resolution disabled.
- Test code may use external libraries, but tests may not redefine the production behavior around an external parser or HTTP client.

## Complete feature inventory, low level to high level

### L0 — Configuration, credentials, and security

- Separate ODP, TSDR, and PatentsView credentials; explicit values and an `IApiKeyProvider` abstraction; environment provider for `USPTO_API_KEY`, `USPTO_TSDR_API_KEY`, and `PATENTSVIEW_API_KEY`.
- Validation for absent, blank, or malformed base URIs and keys; optional anonymous mode only for endpoints that actually allow it; key rotation between calls through the provider.
- Correct header names and content negotiation per service; never place keys in URLs, cache keys, manifests, raw-response records, exception messages, redirects, or progress objects.
- Configurable base URIs, user agent, request/download timeouts, retry budget, retry delay/cap, rate policies, page limits, response-size limits, download chunk size, extraction limits, permitted download hosts, cache mode, cache directory, and mirror directory.
- Caller-provided `HttpClient` support for connection reuse and tests, with clear ownership/disposal behavior.
- HTTPS-by-default URI policy, redirect limits, cross-host credential stripping, DNS/host allow-list checks for USPTO document and bulk links, and rejection of userinfo, fragments, loopback/private targets unless explicitly configured for tests.
- Secret redaction and bounded error-body capture; preserve HTTP status, USPTO code, detailed error, request identifier, headers relevant to retry/cache, and whether the failure is retryable.

### L1 — HTTP, rate limiting, retries, pagination, and caching

- GET, JSON POST, form-encoded DSAPI POST, streamed response, and byte/file response primitives.
- Correct URI escaping, stable query ordering, request-body canonicalization, `Accept`, `Content-Type`, `User-Agent`, request identifiers, and response metadata.
- Cancellation and distinct connect/header/body/overall timeout handling; no retries after caller cancellation.
- Per-service rate buckets, including independent TSDR general-download and PDF/ZIP limits; queue fairly and expose observed limit headers when present.
- Retry idempotent searches and downloads on transient network failures, 408, 429, and selected 5xx responses; exponential backoff with jitter; parse both delta-seconds and HTTP-date `Retry-After`; cap server-requested waits; honor a total retry budget.
- Do not retry permanent 4xx failures, invalid payloads, caller cancellation, or unsafe redirects. Retry mid-stream downloads only through verified range resume.
- Offset/limit paging for ODP and DSAPI, `after` cursor paging for PatentsView, total/count tracking, deterministic sort enforcement, overflow/negative validation, empty-page termination, duplicate-page detection, and caller maximums.
- In-memory and file-backed GET/search cache; canonical keys without credentials; TTL; `Cache-Control`, `Expires`, `ETag`, `Last-Modified`, `If-None-Match`, and `If-Modified-Since`; atomic entries; stale/corrupt entry recovery; explicit invalidation.
- Optional raw JSON/XML retention with a configured size ceiling for schema troubleshooting; preserve unknown JSON fields without forcing every caller to hold full raw payloads.

### L2 — Identifiers and query construction

- Patent application normalization from compact, slash/comma, `US`-prefixed, and display forms while preserving series and leading zeroes.
- Publication normalization for `USYYYYNNNNNN` plus A-kind codes, including A1/A2/A9 corrections and canonical compact/display forms.
- Grant normalization for utility, design (`D`), plant (`PP`), reissue (`RE`), statutory invention registration (`H`), and historical-prefix forms where the ODP can resolve them; preserve B/E/S kind codes.
- PCT normalization for 15-character ODP, 17-character display, and legacy 12-character forms, including national-stage resolution.
- Ambiguous bare 7/8-digit handling that returns candidates or performs exact ODP resolution rather than guessing between application and grant.
- Offline parse results with kind, canonical number, display number, country, year/series, sequence, kind code, validity, ambiguity, and explanatory failure; exception-less `Try...` results for frequent invalid input.
- Online identifier resolution from grant/publication/PCT to application number with exact-field queries, zero/multiple-match handling, and cached resolution.
- Trademark serial, registration, and USPTO-reference normalization; accept punctuation and `sn`/`rn`/`ref` prefixes but retain identifiers as strings.
- ODP simplified-query escaping and structured filters, ranges, sort order, selected fields, facets, GET/POST encoding, and maximum request-size enforcement.
- Office Action Solr/Lucene criteria, correct application-number form, field discovery, form encoding, and safe literal escaping.
- PatentsView JSON query tree supporting equality, comparison, text, arrays, conjunction, disjunction, negation, field selection, sort, size, and cursor values without anonymous objects.

### L3 — Patent File Wrapper and patent application APIs

- Search patent applications with free query or structured search via GET and POST; fields, filters, ranges, facets, sort, offset/limit, automatic all-page retrieval, raw response, and CSV/JSON search export.
- Retrieve by normalized application identifier or resolve grant/publication/PCT first; retain the full wrapper and provenance.
- Complete application metadata: application/publication/grant/PCT identifiers, title, type, filing/publication/grant dates, status and status code, entity status, confirmation/customer numbers, art unit/technology center, examiner, docket data, applicants, inventors, assignees, correspondence, classifications, and file metadata.
- File-wrapper transaction history with event date, code, description, sequence/order, source, and last-modified metadata.
- Document listing with document identifiers, mail-room/submission dates, document codes/descriptions, page counts, file size, MIME type, and all available PDF/XML/ZIP download choices.
- Stream or save one document; filename/content-type verification; safe redirect; progress reporting; atomic completion; overwrite policy; content-length validation.
- Associated publication/grant documents and XML locations; fetch and parse full text.
- Parent/child domestic continuity, relationship type, status, filing dates, priority claims, continuity graph building, deduplication, and cycle protection.
- Foreign priority claims with country, application number, filing date, and priority sequence.
- Assignments with reel/frame, conveyance, execution/recordation/received/mailed dates, assignors, assignees and addresses, correspondent, properties, document location, and field-name compatibility aliases.
- Attorney/agent and correspondence data, registration numbers, active indicators, names, addresses, and telecommunications.
- Patent-term adjustment total plus A/B/C delay, overlap, applicant delay, history entries, and absent-value state.
- Status-code catalog retrieval/search and mapping from application status codes to human-readable descriptions.
- High-level file-wrapper dossier that can hydrate selected sections, report partial failures per section, and avoid refetching shared metadata.
- Portfolio search by attorney, customer number, applicant, inventor, assignee, status, art unit, and date range with bounded hydration.

### L4 — Petitions and PTAB

- Final petition-decision GET/POST search with fields, filters, ranges, facets, sort, pagination, direct record lookup, optional document inclusion, and CSV/JSON export.
- Petition document metadata and secure streamed download.
- PTAB trial proceeding GET/POST search and export; direct trial-number lookup; trial type/status, filing/institution/termination dates, parties, counsel, patent/application identifiers, and facets.
- PTAB trial decision GET/POST search and export; direct document lookup and list-by-trial; outcomes, issue types, statutes/rules, OCR text, document metadata, and downloads.
- PTAB trial document GET/POST search and export; direct document lookup and list-by-trial; filing party, category/type/title, MIME type, size, OCR text, and downloads.
- PTAB appeal-decision GET/POST search and export; direct document lookup and list-by-appeal; appeal number, application/patent data, appellant/requestor, examiner, technology center, outcome, and document metadata.
- PTAB interference-decision GET/POST search and export; direct lookup and list-by-interference; senior/junior/additional parties, decision/outcome, statutes/rules, OCR, and documents.
- Compatibility for known PTAB schema/live-response differences such as decision bag naming, `appellantData`, real-party, technology-center, third-party requester, document type, download URI, list-vs-string, and numeric document number.
- High-level PTAB dossier combining a proceeding with its decisions and documents, preserving partial failures and avoiding duplicate document downloads.

### L5 — Office Action and citation data

- Discover searchable fields and dataset last-update metadata for Office Action text, citations, rejections, and enriched citations.
- Search and page Office Action text with body/section text, summary, 101/102/103/112 rejection passages and form paragraphs, withdrawal/terminal-disclaimer data, application and examiner/art-unit metadata.
- Search and page Office Action citations with action type, legal section, raw and parsed reference identifiers, paragraph, applicant/examiner citation flags, and document linkage.
- Search and page rejection records with rejected claims, legal section, rejection-type flags, quality indicators, form/header anomalies, precedential-case indicators, and allowed-claim indicator.
- Search and page enriched citations with normalized publication/kind/country, NPL flag, related claims, citation category, passage locations, examiner/applicant flags, quality summary, and art-unit metadata.
- Normalize cited patent/publication identifiers where possible while retaining verbatim references and non-patent literature.
- Join Office Action data to file-wrapper transaction/document records and expose an application prosecution timeline without treating derived linkage as official USPTO record data.
- Access the Office Action Weekly Archive bulk product (`OACT`) through the generic catalog/mirror/parser pipeline.

### L6 — Trademark and TSDR

- TSDR case status by serial, registration, or reference identifier in parsed JSON/XML form; status, mark text/design metadata, filing/registration dates, owners, attorneys, correspondent, prosecution events, goods/services, classes, and international/Madrid data when present.
- Case-status content and downloads in HTML, PDF, and ZIP when supported; strict media-type validation.
- Case document list/info, individual PDF/ZIP content/download, page media, all-document content/download, server bundle, and caller-selected multi-case PDF/XML/ZIP bundle.
- Last-update information and multi-case status transactions.
- TSDR-specific date tolerance and XML namespace handling; preserve original strings where dates are inconsistent.
- Enforce published TSDR request classes and rate limits; clear errors for unsupported content negotiation and excessive bundle size.
- Generic ODP bulk discovery, download, mirror, and parsing for trademark application/status daily XML, trademark assignment XML, TTAB XML, and other trademark products.
- No browser automation for Trademark Decisions and Proceedings until USPTO publishes a stable API; capability metadata identifies this as unavailable while bulk products remain usable.

### L7 — Bulk catalog, transfer, checksums, and decompression

- Search all patent, trademark, and research bulk products by query, labels, category, dataset type, title, date, MIME type, and product identifier; retrieve a product with all file metadata.
- Retrieve file metadata/redirect by product identifier and exact filename; tolerate very large sizes with signed 64-bit fields.
- Stream to caller output or download to disk; deterministic sanitized filename from catalog/`Content-Disposition`; progress with known/unknown totals; cancellation; atomic `.partial` file and final rename.
- Resumable downloads using `Range` and `If-Range`; validate 206 status and `Content-Range` start/total before append; restart safely on 200/416, validator change, or incompatible partial state.
- Retry connection setup and mid-stream interruptions within policy; never append unverified bytes; persist resume metadata without credentials.
- Validate expected size and locally compute SHA-256 (plus catalog-specified algorithms when present); compare case-insensitive hex/base64 forms; report verified, unavailable, mismatch, or incomplete state.
- Detect ZIP, gzip, tar, and tar+gzip by signature and extension; use BCL readers; clearly reject unsupported compression such as bzip2 instead of guessing.
- List archive entries and extract selected/all entries with canonical-path checks, duplicate-name policy, entry-count/individual/total-size/ratio limits, no device/symlink materialization, cancellation, and atomic target files.
- Product-family presets for complete and delta Patent File Wrapper, grants/applications full text, patent/trademark assignments, Office Action archives, PTAB, trademark daily data, and PatentsView grant/pre-grant/annualized/sorted datasets without hard-coding catalog availability.

### L8 — XML, JSON, CSV/TSV, and dataset parsers

- Streaming JSON object/array/JSON-lines readers with bounded depth/token/string sizes, flexible numeric/date handling, unknown-field retention, and actionable path/offset errors.
- Secure streaming XML with external entity and network resolution disabled, bounded depth/text, BOM/encoding detection, namespaces, mixed content, CDATA, entity text, and source line information where available.
- Split concatenated USPTO weekly XML documents using buffered `ReadOnlySpan<byte>` scanning around XML declarations/DOCTYPE/root boundaries without loading multi-gigabyte files.
- RFC 4180 CSV plus tab-delimited reader with quoted separators/newlines/quotes, CRLF/LF, BOM, headers, missing/extra columns, row number, bounded fields, and streaming batches.
- ICE/ST.36 grant and pre-grant application parsing for bibliographic data, title, abstract, description headings/paragraphs, nested claim text and references, inventors/applicants/assignees, classifications, citations, related applications, government interest, and external image/resource references.
- Patent File Wrapper bulk JSON/XML parser covering metadata, transactions, documents, continuity, priority, assignment, attorney, PTA, and associated documents.
- Patent assignment XML parser and record flattening keyed by reel/frame while retaining nested assignors, assignees, correspondent, conveyance, properties, and dates.
- Office Action archive parser for action text, citations, rejections, and enriched citations.
- Trademark daily application/status, assignment, and TTAB XML parsers with raw-field fallback for schema generations not yet modeled.
- PatentsView TSV parsers for grant and pre-grant patent/publication core tables, inventors, assignees/applicants, locations, attorneys, CPC/IPC/USPC/WIPO classifications, domestic/foreign/NPL citations, related applications, government interests, claims, summaries, descriptions, and persistent entity crosswalks.
- Generic JSON/XML/delimited row access for any catalog product lacking a typed parser; expose schema/headers and values without pretending the record is a typed domain model.

### L9 — Local mirroring, incremental updates, manifests, and high-level access

- Versioned JSON `DatasetManifest` per mirror: product/file identifiers, canonical source, release/data ranges, remote size/ETag/last-modified, local relative paths, actual size/digest, download/extraction/parser state, schema version, timestamps, and failure details without credentials.
- Mirror planning that compares catalog state and local manifests, classifying files as unchanged, new, changed, missing, corrupt, resumable, or orphaned; dry-run is the default planning operation.
- Incremental sync by release/high-water mark for daily/weekly delta products and snapshot replacement detection for PatentsView; fetch only new/changed data and make manifest updates atomic.
- Crash-safe locks, temporary files, restart/recovery, idempotent sync, bounded parallelism subject to rate buckets, and verification/repair operations. No automatic deletion of old data.
- Extraction and parse state tracked separately so failed downstream work can resume without redownloading verified archives.
- Local structured query over mirrored JSON/XML/CSV/TSV using table name, selected fields, structured predicates, sort, page/cursor, and maximum scan/return limits; typed convenience queries for patents, publications, inventors, assignees, classifications, and citations.
- Direct PatentsView client for all published endpoint families and by-ID routes with `X-Api-Key`, JSON query language, 1,000-result limit, cursor pagination, capability probe, and clear transition/unavailable state.
- Provenance on every high-level result: authoritative ODP/TSDR, research-derived PatentsView, local mirror, cache, raw bulk, or client-derived join; never merge research-derived fields into official records without labeling.
- Client suite factory that wires shared `HttpClient`, credentials, retry/rate/cache components, domain clients, bulk reader, mirror, and local query while preserving one-constructor public implementation rules.
- Composite workflows: complete patent dossier, prosecution timeline, continuity family, assignment history, citation graph inputs, PTAB dossier, trademark dossier, portfolio retrieval, bulk-to-local refresh, and online/cache/local fallback policy with per-section errors.

## Proposed public API surface

Signatures are intentionally omitted. Every operation also accepts cancellation through its request data or a plain cancellation parameter, and all methods have unique names.

| Contract file | Proposed operations |
|---|---|
| `src/Api/Contracts/IApiKeyProvider.cs` | `GetApiKey` returning a service-specific credential result. |
| `src/Api/Contracts/IIdentifierNormalizer.cs` | `NormalizePatentIdentifier`, `TryNormalizePatentIdentifier`, `NormalizeTrademarkIdentifier`, `TryNormalizeTrademarkIdentifier`. |
| `src/Api/Contracts/IPatentClient.cs` | `SearchPatentApplications`, `GetAllPatentApplications`, `DownloadPatentApplicationSearch`, `ResolvePatentApplicationIdentifier`, `GetPatentApplication`, `GetPatentMetadata`, `GetPatentTransactions`, `GetPatentDocuments`, `DownloadPatentDocument`, `GetPatentAssociatedDocuments`, `GetPatentContinuity`, `GetPatentForeignPriorities`, `GetPatentAssignments`, `GetPatentAttorneys`, `GetPatentTermAdjustment`, `SearchPatentStatusCodes`, `GetPatentFullText`, `GetPatentDossier`, `GetPatentPortfolio`. |
| `src/Api/Contracts/IPetitionClient.cs` | `SearchPetitionDecisions`, `GetAllPetitionDecisions`, `DownloadPetitionDecisionSearch`, `GetPetitionDecision`, `DownloadPetitionDocument`. |
| `src/Api/Contracts/IPtabClient.cs` | Search, all-page retrieval, export, direct lookup, by-case listing, and document download operations with distinct names for proceedings, trial decisions, trial documents, appeal decisions, and interference decisions; `GetPtabDossier`. |
| `src/Api/Contracts/IOfficeActionClient.cs` | Field discovery, one-page search, all-page search for actions/citations/rejections/enriched citations, plus `GetProsecutionTimeline`. |
| `src/Api/Contracts/ITrademarkClient.cs` | `GetTrademarkStatus`, `GetTrademarkStatusContent`, `GetTrademarkDocuments`, `GetTrademarkDocumentInfo`, `DownloadTrademarkDocument`, `DownloadTrademarkPageMedia`, `DownloadTrademarkCaseBundle`, `DownloadTrademarkMultiCaseBundle`, `GetTrademarkLastUpdate`, `GetTrademarkMultiStatus`, `GetTrademarkDossier`, `GetTrademarkDecisionCapability`. |
| `src/Api/Contracts/IBulkCatalogClient.cs` | `SearchBulkProducts`, `GetAllBulkProducts`, `GetBulkProduct`, `GetBulkFileMetadata`, `GetDatasetPresets`. |
| `src/Api/Contracts/IBulkTransferClient.cs` | `DownloadBulkFile`, `ResumeBulkFile`, `VerifyBulkFile`, `ListArchiveEntries`, `ExtractBulkArchive`. |
| `src/Api/Contracts/IBulkDataReader.cs` | Generic JSON/XML/delimited reads plus distinct typed read methods for patent grant/application, file wrapper, assignment, Office Action, trademark, TTAB, and PatentsView record families. |
| `src/Api/Contracts/IDatasetMirror.cs` | `PlanDatasetMirror`, `SynchronizeDatasetMirror`, `GetDatasetManifest`, `VerifyDatasetMirror`, `RepairDatasetMirror`. |
| `src/Api/Contracts/ILocalDatasetQuery.cs` | `ListLocalDatasetTables`, `QueryLocalRows`, `QueryLocalPatents`, `QueryLocalPublications`, `QueryLocalInventors`, `QueryLocalAssignees`, `QueryLocalClassifications`, `QueryLocalCitations`. |
| `src/Api/Contracts/IPatentsViewClient.cs` | Capability probe; query and by-ID operations for patents, publications, inventors, assignees, locations, attorneys, classifications, citations, related applications, claims, summaries, and descriptions. |
| `src/Api/Contracts/IUsptoClientFactory.cs` | `CreateUsptoClients` returning a configured `UsptoClients` data object. |

## Planned public source files

### Data models — `src/Api/Data`

| File | Public data held |
|---|---|
| `ApiCredentialModels.cs` | `ApiCredentialRequest`, `ApiCredentialResult`, credential source and availability fields. |
| `UsptoClientOptions.cs` | Endpoints, providers/keys, `HttpClient`, retry/rate/cache/download/mirror policies, limits, and host allow lists. |
| `RequestModels.cs` | Request context, timeout/cancellation settings, raw-data preference, response metadata, and per-section error. |
| `SearchModels.cs` | Search request, filter, range, sort, field selection, facets, offset page, cursor page, and generic search response metadata. |
| `DownloadModels.cs` | Download request, progress, result, resume state, content metadata, overwrite mode, and verification state. |
| `CacheModels.cs` | Cache policy, key metadata, entry metadata, and cache result. |
| `IdentifierModels.cs` | Patent/trademark normalized identifiers, parse result, resolution candidates, and resolution result. |
| `PartyModels.cs` | Person/organization, address, applicant, inventor, assignee, assignor, correspondent, examiner, and attorney/agent data. |
| `ClassificationModels.cs` | CPC, IPC, USPC, WIPO/technology classification and government-interest data. |
| `PatentApplicationModels.cs` | Patent application summary, full wrapper, metadata, file metadata, and official/research provenance. |
| `PatentTransactionModels.cs` | Transaction event and status-code data. |
| `PatentDocumentModels.cs` | File-wrapper document, associated publication/grant metadata, and download alternatives. |
| `ContinuityPriorityModels.cs` | Parent/child continuity, foreign priority, graph node/edge, and family result. |
| `AssignmentModels.cs` | Assignment, reel/frame, conveyance, parties, properties, correspondent, and assignment history. |
| `PatentTermAdjustmentModels.cs` | PTA totals, delay components, overlap/applicant delay, and history. |
| `PatentFullTextModels.cs` | Grant/application bibliographic data, abstract, description blocks, claims, citations, and external resources. |
| `PatentDossierModels.cs` | Section selection/result, portfolio result, and composite dossier. |
| `PetitionModels.cs` | Petition decision, decision document, facets, and response. |
| `PtabProceedingModels.cs` | Trial metadata and parties. |
| `PtabDocumentModels.cs` | Trial document and document response. |
| `PtabDecisionModels.cs` | Trial decision/outcome/issues/statutes and response. |
| `PtabAppealModels.cs` | Appeal/appellant/requestor/examiner and decision response. |
| `PtabInterferenceModels.cs` | Interference parties and decision response. |
| `PtabDossierModels.cs` | Combined proceeding/decision/document result. |
| `OfficeActionModels.cs` | Office Action text/sections and dataset field metadata. |
| `OfficeActionCitationModels.cs` | Citation, rejection, enriched citation, and prosecution timeline event/result. |
| `TrademarkModels.cs` | Case, mark, owner, attorney/correspondent, status/event, goods/services/class, Madrid data, and capability. |
| `TrademarkDocumentModels.cs` | Document info, page media, bundle request/result, update info, and multi-status result. |
| `BulkCatalogModels.cs` | Product, product file, catalog facets, date ranges, and preset. |
| `ArchiveModels.cs` | Compression detection, archive entry, extraction options/result, and safety limit outcome. |
| `BulkRecordModels.cs` | Generic schema/row/batch and typed file-wrapper, assignment, Office Action, and trademark bulk records. |
| `PatentsViewModels.cs` | Query tree, cursor, patent/publication, entity/location, classification, citation, related application, text, and persistent-ID crosswalk records. |
| `MirrorModels.cs` | Manifest, manifest file, plan/action, sync/verify/repair result, local table, and incremental watermark. |
| `LocalQueryModels.cs` | Local query, predicate, field/sort/page, generic row result, and typed local query responses. |
| `UsptoClients.cs` | Contract-typed fields for the configured client suite. |

### Contracts, static data, enums, exceptions, and implementations

| Category | Planned files |
|---|---|
| Contracts | The fourteen contract files listed in the proposed API table. |
| Static | `src/Api/Static/UsptoEndpoints.cs`, `UsptoHeaders.cs`, `UsptoEnvironmentVariables.cs`, `UsptoDatasetIdentifiers.cs`, `UsptoMediaTypes.cs`, `UsptoDefaults.cs`. These contain fields only. |
| Enums | `src/Api/Enums/ApiService.cs`, `IdentifierKind.cs`, `SearchOperator.cs`, `SortDirection.cs`, `ExportFormat.cs`, `DocumentFormat.cs`, `CompressionFormat.cs`, `ChecksumAlgorithm.cs`, `CacheMode.cs`, `DownloadState.cs`, `DatasetFamily.cs`, `DataProvenance.cs`, `MirrorAction.cs`, `PtabCollection.cs`, `TrademarkIdentifierKind.cs`. |
| Exceptions | `src/Api/Exceptions/AuthenticationException.cs`, `ApiException.cs`, `RateLimitException.cs`, `NotFoundException.cs`, `ValidationException.cs`, `DownloadException.cs`, `ChecksumException.cs`, `ArchiveException.cs`, `ParseException.cs`, `CacheException.cs`, `MirrorException.cs`, `ServiceUnavailableException.cs`; each derives directly from `System.Exception`. |
| Classes | `src/Api/Classes/EnvironmentApiKeyProvider.cs`, `IdentifierNormalizer.cs`, `PatentClient.cs`, `PetitionClient.cs`, `PtabClient.cs`, `OfficeActionClient.cs`, `TrademarkClient.cs`, `BulkCatalogClient.cs`, `BulkTransferClient.cs`, `BulkDataReader.cs`, `DatasetMirror.cs`, `LocalDatasetQuery.cs`, `PatentsViewClient.cs`, `UsptoClientFactory.cs`. |

### Internal implementation files outside the public API

| Area | Planned files |
|---|---|
| HTTP | `src/Internal/Http/RequestBuilder.cs`, `ResponseReader.cs`, `ErrorMapper.cs`, `RetryEngine.cs`, `RateLimiter.cs`, `RedirectPolicy.cs`, `DownloadHostPolicy.cs`, `PaginationEngine.cs`. |
| Cache | `src/Internal/Cache/CacheKeyBuilder.cs`, `MemoryResponseCache.cs`, `FileResponseCache.cs`, `ConditionalRequest.cs`. |
| Identifiers/query | `src/Internal/Identifiers/PatentIdentifierParser.cs`, `TrademarkIdentifierParser.cs`, `IdentifierResolver.cs`, `src/Internal/Query/OdpQueryWriter.cs`, `OfficeActionQueryWriter.cs`, `PatentsViewQueryWriter.cs`. |
| Serialization | `src/Internal/Json/JsonFieldReader.cs`, `OdpJsonParser.cs`, `TsdRJsonParser.cs`, `PatentsViewJsonParser.cs`, `src/Internal/Xml/SecureXmlReader.cs`, `CompoundXmlReader.cs`, `PatentXmlParser.cs`, `AssignmentXmlParser.cs`, `TrademarkXmlParser.cs`, `src/Internal/Delimited/DelimitedReader.cs`. |
| Bulk | `src/Internal/Bulk/FileDownloader.cs`, `ResumeValidator.cs`, `ChecksumVerifier.cs`, `ArchiveDetector.cs`, `SafeArchiveExtractor.cs`, `BulkParserRouter.cs`. |
| Mirror/local | `src/Internal/Mirror/ManifestStore.cs`, `MirrorPlanner.cs`, `MirrorSynchronizer.cs`, `MirrorLock.cs`, `LocalTableScanner.cs`, `LocalPredicateEvaluator.cs`. |
| Composite | `src/Internal/Composite/DossierBuilder.cs`, `ContinuityGraphBuilder.cs`, `ProsecutionTimelineBuilder.cs`, `PortfolioLoader.cs`. |

## Implementation tasks

### Foundation and transport

- [DONE] T001: Create the public enums and static field-only classes for services, identifiers, formats, provenance, dataset families, endpoint URIs, header names, environment variables, dataset identifiers, media types, and defaults.
- [DONE] T002: Create credential, client option, request, retry, rate, cache, download, extraction, mirror, and limit data models with field initializers and value-plus-presence pairs for optional value types.
- [DONE] T003: Create the direct `System.Exception` public exception classes with one constructor each and fields for safe structured failure context.
- [DONE] T004: Create shared search, filter, range, sort, field, facet, offset-page, cursor-page, and response metadata data models.
- [DONE] T005: Create download, progress, content metadata, resume, checksum, archive entry, extraction, and result data models.
- [DONE] T006: Create the public contracts and client-suite data model, ensuring every implementation method name is unique and every public data/enum type is consumed by a contract or another data model.
- [DONE] T007: Implement `EnvironmentApiKeyProvider` with explicit service-to-environment mapping, blank-value rejection, runtime key refresh, and secret-free errors.
- [DONE] T008: Implement URI joining, path-segment escaping, stable query ordering, and form/JSON request construction using only BCL APIs.
- [DONE] T009: Implement service-aware authentication and content negotiation for ODP, Office Action, TSDR, and PatentsView without placing credentials in URIs or persisted state.
- [DONE] T010: Implement secret redaction for headers, URIs, structured errors, raw response metadata, cache records, manifests, and diagnostic text.
- [DONE] T011: Implement bounded HTTP response reading for JSON/XML/text and direct streaming for binary content, preserving safe response metadata and request identifiers.
- [DONE] T012: Implement USPTO/TSDR/PatentsView error-body parsing and mapping for authentication, authorization, not found, payload too large, rate limit, service unavailable, malformed, empty, and unknown responses.
- [DONE] T013: Implement caller cancellation and connect/header/body/overall timeout behavior, ensuring cancellation and deadline failures are not retried.
- [DONE] T014: Implement fair per-service rate limiters, including separate TSDR general and PDF/ZIP buckets and observation of server limit headers.
- [DONE] T015: Implement retry classification for safe GET/search POST operations and transient network, 408, 429, and selected 5xx failures.
- [DONE] T016: Implement exponential backoff with jitter, delta/date `Retry-After`, configurable wait cap, attempt cap, total retry budget, and cancellation-aware waits.
- [DONE] T017: Implement secure redirects with a maximum hop count, relative-location resolution, HTTPS enforcement, loop detection, and credential stripping on cross-host hops.
- [DONE] T018: Implement USPTO download-host validation and configurable test host allow lists, rejecting userinfo, fragments, non-HTTP schemes, loopback/private targets, and unapproved hosts.
- [DONE] T019: Implement canonical cache-key construction from service, method, normalized URI, selected safe headers, and canonical body, excluding all credentials.
- [DONE] T020: Implement thread-safe bounded in-memory response caching with TTL, explicit invalidation, and no caching of failures or oversized/raw secret-bearing content.
- [DONE] T021: Implement atomic file-backed response caching with metadata, corruption recovery, directory locking, size/age eviction, and sanitized relative paths.
- [DONE] T022: Implement conditional cache revalidation with `ETag`, `Last-Modified`, `If-None-Match`, `If-Modified-Since`, `Cache-Control`, `Expires`, and 304 response merging.
- [DONE] T023: Implement offset/limit pagination validation and aggregation with total/count checks, empty-page termination, duplicate-page detection, deterministic sort, and caller result limits.
- [DONE] T024: Implement PatentsView `after` cursor pagination with stable multi-field sort requirements, cursor propagation, duplicate detection, and the documented 1,000-row page ceiling.
- [DONE] T025: Implement tolerant JSON field readers for strings, booleans, signed numbers, arrays, objects, missing/null state, flexible dates, raw unknown fields, and precise JSON paths.
- [DONE] T026: Implement secure XML reader settings with DTD/external resource resolution disabled, bounded depth/text, BOM/encoding handling, namespace tolerance, and useful parse locations.
- [DONE] T027: Implement RFC 4180 CSV and tab-delimited streaming reads with quoted fields/newlines, BOM, CRLF/LF, headers, row numbers, and bounded columns/field sizes.
- [DONE] T028: Implement buffered concatenated-XML boundary detection with `ReadOnlySpan<byte>` so USPTO weekly files can be split into individual documents without whole-file buffering.

### Identifier and query primitives

- [DONE] T029: Implement patent identifier character normalization that removes permitted punctuation/spacing without losing prefixes, leading zeroes, country, year, or kind code.
- [DONE] T030: Implement application-number parsing and canonical/display formatting for compact, slash/comma, and `US`-prefixed forms across application series.
- [DONE] T031: Implement pre-grant publication parsing and formatting for year/sequence and A-kind codes, including correction/republication kind codes.
- [DONE] T032: Implement grant parsing and formatting for utility, design, plant, reissue, statutory-invention-registration, and supported historical prefixes/kind codes.
- [DONE] T033: Implement current, display, and legacy PCT parsing/formatting with country/year/sequence validation.
- [DONE] T034: Implement ambiguity detection for bare numeric patent/application input and return explicit candidates instead of applying a length-only guess.
- [DONE] T035: Implement trademark serial, registration, and reference identifier parsing with `sn`/`rn`/`ref` prefixes and canonical/display forms.
- [DONE] T036: Implement exception-less identifier parse results and strict exception-producing normalization methods through `IdentifierNormalizer`.
- [DONE] T037: Implement online grant/publication/PCT-to-application resolution using exact ODP fields, candidate comparison, zero/multiple match errors, and resolution caching.
- [DONE] T038: Implement ODP simplified-query literal escaping and serialization for equality, comparison, text, filters, range filters, selected fields, facets, and sorts.
- [DONE] T039: Implement GET query and POST search-body writers with deterministic output, numeric bound validation, date preservation, and configured request-size ceilings.
- [DONE] T040: Implement Office Action application-number normalization, Solr/Lucene literal escaping, criteria composition, and form-urlencoded request writing.
- [DONE] T041: Implement the PatentsView JSON query tree and writer for equality, comparisons, text, arrays, AND/OR/NOT, fields, sorts, size, and cursor values without anonymous objects.

### Patent File Wrapper

- [DONE] T042: Implement patent party, address, applicant, inventor, examiner, assignee, correspondence, attorney, classification, and government-interest public data models.
- [DONE] T043: Implement patent search summary, application metadata, complete wrapper, file metadata, response, facet, and provenance data models.
- [DONE] T044: Implement patent application GET and POST search, selected fields, filters, ranges, facets, sorts, and one-page response parsing.
- [DONE] T045: Implement bounded all-page patent application search using the shared pagination engine.
- [DONE] T046: Implement CSV/JSON patent search-result export with streamed output, media/filename validation, and payload-too-large mapping.
- [DONE] T047: Implement normalized application retrieval and grant/publication/PCT convenience retrieval through identifier resolution.
- [DONE] T048: Implement complete application metadata parsing, including absent numeric/boolean fields, PCT data, applicants/inventors, entity status, classifications, and grant/publication file metadata.
- [DONE] T049: Implement transaction-history models and retrieval with event sequence, date, code, description, source, and last-modified data.
- [DONE] T050: Implement patent document and download-alternative models plus document listing with code, description, dates, pages, size, MIME type, and URI.
- [DONE] T051: Implement secure patent document streaming/file download with progress, atomic completion, overwrite policy, response validation, and typed failure results.
- [DONE] T052: Implement associated-document retrieval and mapping for grant/pre-grant XML, ZIP, and related publication metadata.
- [DONE] T053: Implement continuity and foreign-priority models and endpoint parsing with relationship/status/date/priority sequence fields.
- [DONE] T054: Implement continuity graph construction with canonical identifiers, duplicate-edge suppression, missing-node representation, and cycle detection.
- [DONE] T055: Implement assignment models and retrieval for reel/frame, conveyance, dates, assignors, assignees, addresses, correspondent, properties, document URI, and old/new geographic field aliases.
- [DONE] T056: Implement attorney/agent and correspondence retrieval with registration, active status, address, and telecommunications.
- [DONE] T057: Implement patent-term-adjustment totals, A/B/C delay, overlap, applicant delay, and history with explicit missing-value state.
- [DONE] T058: Implement status-code GET/POST search, catalog pagination, and code-to-description mapping.
- [DONE] T059: Implement ICE/ST.36 full-text models and typed parsing for grant and published-application roots, document identifiers, bibliographic data, title, abstract, description, claims, citations, classifications, related applications, government interest, and external resources.
- [DONE] T060: Implement mixed/nested claim and description text flattening in document order while retaining paragraph, heading, claim number, and claim-reference structure.
- [DONE] T061: Implement full-text URL selection from associated document metadata, secure download, document-type verification, and parsed result provenance.
- [DONE] T062: Implement selected-section patent dossier hydration with shared-request reuse, per-section error capture, official/research separation, and partial success.
- [DONE] T063: Implement bounded portfolio search/hydration by attorney, customer number, applicant, inventor, assignee, status, art unit, and date range.

### Petitions and PTAB

- [DONE] T064: Implement petition decision, document, facet, response, and export request data models.
- [DONE] T065: Implement final petition decision GET/POST search, filters/ranges/facets/sort/page, direct record retrieval, and optional document inclusion.
- [DONE] T066: Implement petition all-page retrieval, CSV/JSON export, and secure petition document download.
- [DONE] T067: Implement common PTAB trial metadata, patent owner, petitioner/respondent, counsel, document, decision, facet, and provenance models.
- [DONE] T068: Implement trial proceeding GET/POST search, all-page retrieval, CSV/JSON export, and direct trial-number lookup.
- [DONE] T069: Implement trial decision GET/POST search, all-page retrieval, CSV/JSON export, direct document lookup, and decisions-by-trial lookup.
- [DONE] T070: Implement trial document GET/POST search, all-page retrieval, CSV/JSON export, direct document lookup, and documents-by-trial lookup.
- [DONE] T071: Implement appeal decision models plus GET/POST search, all-page retrieval, export, direct document lookup, and decisions-by-appeal lookup.
- [DONE] T072: Implement interference party/decision models plus GET/POST search, all-page retrieval, export, direct document lookup, and decisions-by-interference lookup.
- [DONE] T073: Implement shared secure PTAB document download for every document URI and media type exposed by trial, appeal, and interference responses.
- [DONE] T074: Implement explicit PTAB compatibility aliases and type coercions for all known specification/live-response discrepancies while retaining unrecognized raw fields.
- [DONE] T075: Implement PTAB dossier aggregation with proceeding, decisions, documents, deduplication, bounded downloads, and per-section partial failure.

### Office Action and citations

- [DONE] T076: Implement Office Action dataset field metadata and response models, including version, status, schema URI, fields, count, and last update.
- [DONE] T077: Implement field discovery for Office Action text, citations, rejections, and enriched citations.
- [DONE] T078: Implement Office Action text/section models and parsing for body/summary and 101/102/103/112 rejection, form paragraph, withdrawal, terminal disclaimer, application, examiner, and art-unit data.
- [DONE] T079: Implement Office Action text one-page and all-page search using DSAPI form requests and Solr offset/row pagination.
- [DONE] T080: Implement Office Action citation models and one-page/all-page search with legal section, raw/parsed reference, paragraph, flags, and document linkage.
- [DONE] T081: Implement Office Action rejection models and one-page/all-page search with claims, rejection flags, quality anomalies, precedential indicators, and allowed-claim state.
- [DONE] T082: Implement enriched citation models and one-page/all-page search with normalized patent fields, NPL, claims, categories, passages, quality, and art-unit data.
- [DONE] T083: Implement cited-identifier normalization that preserves verbatim references and safely distinguishes patent publications from non-patent literature.
- [DONE] T084: Implement prosecution timeline joining Office Action records to file-wrapper transactions/documents using explicit derived-match confidence and provenance.

### Trademark/TSDR

- [DONE] T085: Implement trademark case, mark, owner, attorney/correspondent, status/event, goods/services/class, international/Madrid, and parsed/raw response models.
- [DONE] T086: Implement TSDR case-status retrieval by normalized serial, registration, or reference identifier with JSON/XML content negotiation and tolerant date handling.
- [DONE] T087: Implement TSDR case-status HTML/PDF/ZIP content and download routes with strict response media validation.
- [DONE] T088: Implement trademark document list/info parsing and individual document PDF/ZIP content/download.
- [DONE] T089: Implement trademark document page-media retrieval and media metadata validation.
- [DONE] T090: Implement all-document case content/download and server-generated case bundle operations.
- [DONE] T091: Implement selected multi-case PDF/XML/ZIP bundle operations with request count/size safeguards.
- [DONE] T092: Implement TSDR last-update and multi-case status transaction operations.
- [DONE] T093: Implement TSDR-specific rate classification, 406 content-negotiation errors, 429 policy, and bundle-size error mapping.
- [DONE] T094: Implement a capability result that reports Trademark Decisions and Proceedings as unavailable without a published API while directing generic bulk discovery to matching products.
- [DONE] T095: Implement composite trademark dossier retrieval with status, events, parties, goods/services, documents, selected content, provenance, and partial failure.

### Bulk transfer and parsing

- [DONE] T096: Implement bulk product, file, facet, date-range, preset, and response data models using signed 64-bit sizes/counts where needed.
- [DONE] T097: Implement bulk product search across query/labels/category/type/title/date/MIME/product identifier and bounded all-page retrieval.
- [DONE] T098: Implement exact product retrieval and exact product-file metadata/302 redirect retrieval with catalog/live response compatibility.
- [DONE] T099: Implement dynamic dataset-family presets for File Wrapper complete/delta, grants, applications, assignments, Office Action, PTAB, trademark, and PatentsView products without assuming each product is always present.
- [DONE] T100: Implement filename selection and sanitization from catalog and RFC 6266 `Content-Disposition`, preserving meaningful extensions and rejecting traversal/device names.
- [DONE] T101: Implement streamed bulk download to caller streams and atomic disk targets with progress, known/unknown total, cancellation, overwrite policy, and content-length validation.
- [DONE] T102: Implement persisted credential-free partial-download metadata with source URI, validators, expected total, completed length, and timestamp.
- [DONE] T103: Implement verified range resume using `Range`/`If-Range`, strict 206/`Content-Range` validation, safe 200/416 handling, validator changes, truncation, and restart policy.
- [DONE] T104: Implement mid-stream recovery by reopening at the verified byte boundary under retry/rate limits without duplicating or skipping bytes.
- [DONE] T105: Implement streaming SHA-256 and catalog-directed checksum calculation/comparison with hex/base64 normalization and explicit unavailable/incomplete/mismatch results.
- [DONE] T106: Implement archive signature/extension detection and entry listing for ZIP, gzip, tar, and tar+gzip with an explicit unsupported-format result.
- [DONE] T107: Implement safe ZIP extraction with canonical path validation, duplicate policy, entry/count/size/ratio limits, cancellation, and atomic files.
- [DONE] T108: Implement safe tar and tar+gzip extraction with canonical path validation, link/device rejection, entry/count/size limits, cancellation, and atomic files.
- [DONE] T109: Implement generic streaming JSON array/object/JSON-lines record reading with schema/header discovery and bounded batches.
- [DONE] T110: Implement generic streaming XML record reading with configurable root/record names, namespaces, raw subtree access, and bounded batches.
- [DONE] T111: Implement generic delimited bulk rows with schema headers, typed string access, missing/extra column reporting, and bounded batches.
- [DONE] T112: Implement Patent File Wrapper bulk JSON/XML parsing for metadata, transactions, documents, continuity, priorities, assignments, attorneys, PTA, and associated documents.
- [DONE] T113: Implement patent assignment XML parsing and flattening keyed by reel/frame while retaining nested transaction parties/properties and raw source values.
- [DONE] T114: Implement Office Action archive parsing for action text, citations, rejections, and enriched citation record generations.
- [DONE] T115: Implement trademark daily application/status XML parsing across documented schema generations with status codes, mark data, owners, events, and goods/services.
- [DONE] T116: Implement trademark assignment XML parsing with reel/frame, conveyance, parties, properties, correspondent, and dates.
- [DONE] T117: Implement trademark TTAB XML parsing with proceeding, parties, events, decisions, and document references plus generic raw fallback.
- [DONE] T118: Implement a bulk parser router that selects typed parsers by product identifier, filename, media/signature, and schema markers, or falls back to generic records.

### PatentsView-derived data and local access

- [DONE] T119: Implement PatentsView query/response and patent/publication core data models with explicit research provenance.
- [DONE] T120: Implement PatentsView inventor, assignee, applicant, location, attorney, government-interest, and persistent-entity crosswalk models.
- [DONE] T121: Implement PatentsView CPC/IPC/USPC/WIPO classification, citation, related-application, claim, summary, description, and drawing-description models.
- [DONE] T122: Implement the PatentsView capability probe, configurable base URI, `X-Api-Key` handling, transition/unavailable response, and retry/rate/cache integration.
- [DONE] T123: Implement PatentSearch GET/POST queries and by-ID retrieval for patents and pre-grant publications.
- [DONE] T124: Implement PatentSearch query/by-ID retrieval for inventors, assignees, locations, and patent attorneys.
- [DONE] T125: Implement PatentSearch query/by-ID retrieval for CPC class/subclass/group, IPC, USPC main/subclass, and WIPO classifications.
- [DONE] T126: Implement PatentSearch citation and related-application endpoints for US patents, US applications, foreign references, other references, and publication/patent relation text.
- [DONE] T127: Implement PatentSearch grant/pre-grant long-text endpoints for brief summary, claims, detailed description, and drawing description with documented availability behavior.
- [DONE] T128: Implement bounded all-page PatentsView cursor retrieval for every queryable endpoint while preserving selected fields and stable sort.
- [DONE] T129: Implement PatentsView grant bulk TSV parsing for patent, inventor, assignee/applicant, location, attorney, classification, citation, related application, government interest, text, and crosswalk tables.
- [DONE] T130: Implement PatentsView pre-grant bulk TSV parsing for publication, inventor, assignee/applicant, location, classification, citation, related application, text, and crosswalk tables.
- [DONE] T131: Implement PatentsView annualized and sorted dataset parsing with table/header discovery so new columns remain accessible as generic values.
- [DONE] T132: Implement local table discovery and schema inspection from manifests, extracted files, and delimited headers.
- [DONE] T133: Implement structured local predicate evaluation for equality, comparison, text, arrays, AND/OR/NOT, missing values, and date/numeric/string coercion without LINQ.
- [DONE] T134: Implement bounded local generic row query with field selection, deterministic sort, offset/cursor page, scan limit, result limit, cancellation, and provenance.
- [DONE] T135: Implement typed local queries for patents, publications, inventors, assignees, classifications, and citations over PatentsView mirrors.

### Manifests, mirrors, and composite workflows

- [DONE] T136: Implement versioned dataset manifest models and atomic JSON manifest serialization with product/file metadata, validators, digest, local paths, stage states, provenance, and credential exclusion.
- [DONE] T137: Implement cross-process mirror locking, stale-lock recovery, temporary work directories, and crash-safe atomic state transitions.
- [DONE] T138: Implement mirror planning that compares catalog and manifest state and classifies unchanged, new, changed, missing, corrupt, resumable, and orphaned files without deleting data.
- [DONE] T139: Implement incremental high-watermark selection for daily/weekly delta files and changed-snapshot detection for complete and PatentsView products.
- [DONE] T140: Implement mirror synchronization with bounded parallel transfers coordinated by shared rate/retry policies and idempotent re-entry.
- [DONE] T141: Implement separate download, checksum, extraction, and parse stages so a later-stage retry reuses verified earlier artifacts.
- [DONE] T142: Implement mirror verification for file existence, size, checksum, archive readability, extracted entries, parser state, and manifest consistency.
- [DONE] T143: Implement non-destructive mirror repair that resumes/redownloads corrupt or missing artifacts, re-extracts/reparses failed stages, and preserves superseded files.
- [DONE] T144: Implement high-level bulk-to-local refresh that discovers selected product families, plans, synchronizes, verifies, and returns a per-file/stage result.
- [DONE] T145: Implement the configured client-suite factory with shared transport, credentials, rate, retry, cache, identifier, bulk, mirror, and local-query components and correct disposal ownership.
- [DONE] T146: Implement official-vs-research provenance propagation and field-level source labels for composite results and local/cache returns.
- [DONE] T147: Implement online/cache/local fallback policy with explicit caller preference, freshness limits, capability checks, and no silent substitution of research data for official data.
- [DONE] T148: Implement composite citation graph inputs that combine normalized Office Action citations and PatentsView citations while retaining source, direction, category, NPL, and match confidence.
- [DONE] T149: Implement end-to-end partial-failure aggregation for patent, PTAB, trademark, portfolio, timeline, citation, and mirror workflows without losing successful sections.

### Complete public API test coverage

- [DONE] T150: Build reusable deterministic HTTP handlers, fake clock, fake delay/rate hooks, temporary cache/mirror roots, progress collector, and fixture loader for all public client tests.
- [DONE] T151: Add constructor and option-validation tests for every public implementation class, including ownership/disposal of injected and internally created `HttpClient` instances.
- [DONE] T152: Add credential-provider and authentication tests for explicit/environment/rotated/missing keys, exact service headers, anonymous allowance, and secret redaction across every observable result.
- [DONE] T153: Add URI/query/form/body golden tests for reserved characters, Unicode, stable ordering, request sizes, base paths, and every ODP, Office Action, TSDR, and PatentsView route.
- [DONE] T154: Add transport tests for JSON/XML/binary success, empty/truncated/oversized bodies, media mismatch, response metadata, raw retention limits, and safe error-body capture.
- [DONE] T155: Add typed error tests for every relevant HTTP status and USPTO error shape, including entitlement 403, missing 404, payload 413, rate 429, malformed success, and service-unavailable responses.
- [DONE] T156: Add cancellation and timeout tests at queue, connect, headers, body, retry wait, download, extraction, parsing, mirror, and local-query stages.
- [DONE] T157: Add rate-limiter tests for fairness, independent services/buckets, TSDR 60-per-minute and 4-per-minute classes, observed headers, cancellation, and concurrent callers.
- [DONE] T158: Add retry tests for eligible/ineligible methods and failures, exponential jitter bounds, delta/date `Retry-After`, wait caps, retry budgets, eventual success, and exhaustion.
- [DONE] T159: Add redirect/host security tests for relative redirects, hop/loop limits, cross-host credential stripping, userinfo/fragments, HTTPS downgrade, loopback/private hosts, and approved test hosts.
- [DONE] T160: Add in-memory/file-cache tests for canonical keys, credential exclusion, TTL, validators, 304 merging, stale/corrupt/partial entries, atomic writes, eviction, invalidation, and concurrency.
- [DONE] T161: Add pagination tests for ODP offset and PatentsView cursor modes covering negative/overflow values, exact totals, empty pages, duplicates, unstable sort, caller caps, and cancellation.
- [DONE] T162: Add property-based and table-driven patent identifier tests for application, publication, utility/design/plant/reissue/SIR/historical grant, PCT, kind-code, punctuation, Unicode whitespace, leading-zero, invalid, and ambiguous inputs.
- [DONE] T163: Add property-based and table-driven trademark serial/registration/reference normalization tests and strict/exception-less behavior tests.
- [DONE] T164: Add identifier-resolution HTTP tests for exact grant/publication/PCT queries, ambiguity candidates, no match, multiple match, kind preservation, cache, and authorization/rate failures.
- [DONE] T165: Add query-language tests for every ODP filter/range/sort/facet/field form, Office Action escaped literal, and PatentsView operator/tree/cursor form.
- [DONE] T166: Add Patent Application search/retrieval fixture tests for GET/POST parity, sparse/full responses, facets/sorts/ranges/fields, all-page behavior, raw data, and CSV/JSON export.
- [DONE] T167: Add complete patent metadata fixture tests for every public field group, absent-vs-zero values, inconsistent dates/numbers, PCT, parties, classifications, entity/status, and unknown fields.
- [DONE] T168: Add transaction, document, associated-document, continuity, priority, assignment, attorney, PTA, and status-code endpoint tests for complete, sparse, alias, malformed, empty, and error fixtures.
- [DONE] T169: Add patent document/full-text download tests for MIME/filename alternatives, redirects, progress, cancellation, overwrite, atomic completion, truncation, host validation, and parser selection.
- [DONE] T170: Add ICE grant/application XML fixture tests for roots, namespaces/DOCTYPE handling, bibliographic fields, mixed abstract/description, nested claims/references, citations, related applications, classifications, resources, malformed input, and external-entity blocking.
- [DONE] T171: Add continuity graph, patent dossier, and portfolio tests for deduplication, cycles, bounded hydration, shared request reuse, cache use, partial failures, provenance, and cancellation.
- [DONE] T172: Add petition tests for GET/POST search, filters/ranges/facets/sort/page, direct lookup, document inclusion/download, all-page retrieval, CSV/JSON export, and every error path.
- [DONE] T173: Add PTAB proceeding tests for GET/POST search, all pages, direct lookup, export, parties/facets/dates, sparse/malformed data, and errors.
- [DONE] T174: Add PTAB trial decision/document tests for search, by-ID, by-trial, export, OCR/document metadata, downloads, live-schema aliases, type coercion, and errors.
- [DONE] T175: Add PTAB appeal/interference tests for search, by-ID, by-case, export, parties/outcomes/statutes/documents, every known field alias, and errors.
- [DONE] T176: Add PTAB dossier tests for aggregation, duplicate documents, bounded downloads, partial failures, provenance, cache, and cancellation.
- [DONE] T177: Add Office Action field-discovery and text-search tests for form encoding, Solr paging, body/section/rejection/form/withdrawal/terminal-disclaimer fields, dates, raw retention, and errors.
- [DONE] T178: Add Office Action citation/rejection/enriched tests for all modeled fields, identifier normalization, NPL preservation, flags/claims/passages/quality data, paging, aliases, malformed records, and errors.
- [DONE] T179: Add prosecution-timeline and citation-graph tests for deterministic ordering, linkage confidence, unmatched records, duplicate citations, provenance separation, and partial failures.
- [DONE] T180: Add TSDR case-status JSON/XML tests for every modeled case/mark/party/event/goods/class/Madrid field, identifier types, inconsistent dates, namespaces, unknown fields, and errors.
- [DONE] T181: Add TSDR content/document/page/case-bundle/multi-bundle/last-update/multi-status tests for exact routes, content negotiation, media validation, limits, downloads, rate classes, and all failures.
- [DONE] T182: Add bulk catalog tests for search/filter/facet/date/product/file parsing, huge signed sizes, presets, dynamic availability, 302 metadata, aliases, paging, and errors.
- [DONE] T183: Add bulk transfer tests for stream/file downloads, progress, filenames, overwrite, atomicity, content length, partial metadata, range/`If-Range`, valid/invalid 206, 200/416 fallbacks, validator changes, mid-stream retries, and cancellation.
- [DONE] T184: Add checksum tests for SHA-256 streaming, hex/base64 comparison, size validation, unavailable/incomplete/mismatch state, large synthetic streams, and cancellation.
- [DONE] T185: Add archive tests for signature detection and ZIP/gzip/tar/tar+gzip listing/extraction, traversal, absolute/device paths, duplicates, symlinks, entry/count/size/ratio limits, truncation, atomicity, unsupported formats, and cancellation.
- [DONE] T186: Add generic JSON/XML/CSV/TSV reader tests for arrays/lines, compound XML boundaries, BOM/encodings, namespaces, mixed content, quoted newlines, missing/extra columns, bounds, batches, raw fallback, malformed/truncated data, and cancellation.
- [DONE] T187: Add typed bulk parser tests for File Wrapper, patent assignment, Office Action, trademark application/status, trademark assignment, and trademark TTAB using representative schema generations and malformed/unknown variants.
- [DONE] T188: Add PatentsView online client tests for capability state, authentication, every endpoint family/by-ID route, query operators, selected fields, 1,000-row limit, cursor paging, long-text availability, transition failures, raw data, and provenance.
- [DONE] T189: Add PatentsView grant/pre-grant/annualized/sorted TSV parser tests covering all planned tables, persistent crosswalks, schema drift/new columns, missing values, malformed rows, and research provenance.
- [DONE] T190: Add local table/predicate/query tests for discovery, equality/comparison/text/arrays/AND/OR/NOT, coercion, fields, sort, offset/cursor, scan/result caps, typed patent/entity/classification/citation results, cancellation, and malformed files.
- [DONE] T191: Add manifest serialization and migration tests for every field/state, atomic writes, credential exclusion, relative-path safety, unknown future fields, missing/corrupt manifests, and deterministic round trips.
- [DONE] T192: Add mirror plan/sync/verify/repair tests for new/changed/unchanged/missing/corrupt/resumable/orphaned files, complete/delta watermarks, snapshot replacements, locks, crashes, stage resume, concurrency, idempotence, and non-deletion.
- [DONE] T193: Add client-suite, fallback, provenance, dossier, timeline, citation, portfolio, and bulk-to-local end-to-end tests that exercise every public method and all partial-failure combinations through deterministic fakes.
- [DONE] T194: Add opt-in live integration tests for each ODP Patent, Petition, PTAB, Office Action, Bulk, TSDR, and available PatentsView operation, skipping cleanly when its service-specific key or capability is absent and never persisting secrets or large downloads.
- [DONE] T195: Run coverage over `src/Api` behavior and close every unexercised public constructor, method, return state, validation branch, exception mapping, and data-model round trip until the exposed functional surface has complete automated coverage.
