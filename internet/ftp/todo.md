# Universal FTP and FTPS client/server development plan

## Outcome and scope

Build a managed, cross-platform .NET 10 library that can act as both an FTP/FTPS client and an embeddable FTP/FTPS server. The implementation will cover the base protocol from RFC 959, feature negotiation from RFC 2389, machine-readable metadata and restart behavior from RFC 3659, FTP over TLS from RFC 4217, and the complementary standards needed for modern interoperability: RFC 1123, RFC 1579, RFC 2228, RFC 2428, RFC 2577, RFC 2640, RFC 5797, and RFC 7151.

The product is an FTP and FTPS library. SFTP, SCP, HTTP proxy tunnelling, SOCKS proxies, GSSAPI/Kerberos, and vendor-specific mainframe dataset semantics are outside scope. Unknown and vendor commands remain reachable through raw command and extension-handler APIs. PRET, MODE Z, CLNT, MFMT, checksum commands, SITE CHMOD/UMASK, and historic X-prefixed aliases are compatibility features, not claims of standards conformance.

The source implementation uses only the .NET base class libraries. Tests may use external packages and external server/client processes. The public API is synchronous because syntax.md prohibits await; long-running calls accept options containing CancellationToken and timeouts where applicable. Server concurrency and recoverable fallback flows use internal Task/state-machine boundaries that capture faults without source-level try/catch. Span and ReadOnlySpan are used internally for bounded parsing, tokenization, numeric conversion, CRLF scanning, and transfer framing.

## Source and design constraints

- All public types live below src/Api and are organized by the category directories required by syntax.md.
- Public data classes expose fields rather than properties, contain at least one public instance field, do not implement interfaces, and contain no public methods.
- Public operation surfaces are interfaces. Each public implementation class has one constructor and exposes only the instance methods declared by its implemented interface.
- Interface and implementation method names are globally unique within each implemented interface set; no method overloads are planned.
- Public static classes contain constants or readonly fields only and no methods.
- Every custom exception derives directly from System.Exception.
- Internal implementation types live outside src/Api, are non-public, and use no properties, authored generic types, LINQ, patterns, named arguments, async/await, or try/catch/finally.
- Expected failures in server extension points use result data objects rather than exceptions. Unexpected faults are isolated at Task continuation boundaries and terminate only the affected operation/session.
- Source dependencies are limited to built-in System namespaces, including System.IO, System.Net, System.Net.Sockets, System.Net.Security, System.Security.Authentication, System.Security.Cryptography, System.Security.Cryptography.X509Certificates, System.Text, System.Threading, and System.Threading.Tasks.

## Standards and reference basis

The downloaded source set is indexed in specs/README.md. The normative core is:

- RFC 959 and RFC 1123: FTP model, command/reply sequences, representations, file structures, transfer modes, active/passive data paths, restart, and server-to-server transfer.
- RFC 2389: FEAT and OPTS, multiline feature replies, unknown-feature tolerance, and option negotiation.
- RFC 3659: MDTM, SIZE, REST STREAM, TVFS, MLST/MLSD, timestamp syntax, fact syntax, and OPTS MLST.
- RFC 4217 with RFC 2228: AUTH TLS, PBSZ 0, PROT C/P, CCC, explicit TLS upgrade, protected data channels, TLS roles, and feature advertisement.
- RFC 2428 and RFC 1579: EPRT/EPSV, IPv4/IPv6, NAT-safe passive operation, EPSV ALL, and fallbacks.
- RFC 2640: UTF-8 pathname/reply handling, UTF8 feature advertisement, and LANG.
- RFC 2577: bounce-attack, port-theft, brute-force, resource exhaustion, and privacy mitigations.
- RFC 5797 plus the current IANA registry: complete command inventory and mandatory/optional/historic status.
- RFC 7151: HOST virtual-host selection before authentication.
- RFC 854: Telnet command handling and synchronization on the clear FTP control channel.

## Existing-library research summary

| Reference | Features carried into this plan | Deliberate differences |
| --- | --- | --- |
| libcurl | Passive default, EPSV/EPRT fallback, PASV-address ignore policy, active port ranges, resume, create missing directories, PRET, explicit/implicit FTPS, CCC, timeouts, custom commands | No URL-transfer multiprotocol layer or proxy subsystem |
| Apache Commons Net FTPClient/FTPSClient | Low-level reply access, raw commands, active/passive and FXP flows, pending-transfer completion, restart offsets, all listing forms, MLST/MLSD, UTF-8 detection, keepalive, TLS configuration, AUTH/PBSZ/PROT/CCC | No Java-style overload families; distinct C# method names and option data objects |
| FluentFTP | Auto connection negotiation, reconnect, recursive directory operations, atomic upload patterns, existence policies, progress, verification, checksum variants, permission extensions, server quirks, FXP, path sanitization | No async API, proxies, monitoring service, PowerShell layer, or platform-specific dataset API |
| Python ftplib/FTP_TLS | Small low-level surface, raw sendcmd, binary/text streams, transfercmd behavior, MLSD, passive toggle, explicit TLS upgrade and protected data channel | Broader metadata, server, validation, recursive transfer, and policy surfaces |
| Apache FtpServer, pyftpdlib, Fubar FTP Server | Embeddable server lifecycle, virtual filesystem, authentication/authorization, anonymous users, passive port/NAT configuration, FTPS, quotas/limits, connection events, and custom commands | No dependency injection framework requirement and no external source dependency |

## Architecture and implementation order

1. Protocol primitives: bounded line reader, Telnet filter, command codec, reply codec, endpoint codecs, timestamp and MLSx fact codecs.
2. Transfer primitives: representation conversion, structure/mode framing, restart markers, clear/TLS data streams, and active/passive connection coordination.
3. Client session: connect, greeting, HOST, AUTH TLS/implicit TLS, authentication, FEAT/OPTS, raw commands, state validation, and data-command completion.
4. Client operations: metadata, listings, filesystem commands, transfers, resume, verification, recursion, synchronization, and FXP.
5. Server host/session: listeners, TLS endpoints, session state machine, command dispatch, authentication/authorization, filesystem mapping, data connections, replies, and limits.
6. Compatibility extensions: aliases, PRET, MODE Z, CLNT, MFMT, checksum commands, and selected SITE commands.
7. Contract, unit, integration, interoperability, fault, security, and public-API coverage tests.

## Complete feature inventory

### Control channel and protocol framing

- TCP control listeners/connectors for IPv4, IPv6, dual-stack endpoints, DNS hostnames, configurable local bind endpoints, connect/accept/read/write/idle timeouts, and cancellation.
- Initial 120/220 greetings, delayed readiness, 421 closure, graceful QUIT, abrupt disconnect detection, and session reinitialization.
- CRLF command/reply framing with partial reads, multiple lines per read, maximum line length, maximum multiline reply size, ASCII command verbs, negotiated UTF-8 arguments, and rejection of embedded CR/LF/NUL.
- RFC 854 handling on clear control channels: escaped IAC, Telnet commands/options, IP, DM, Synch/urgent abort behavior, and tolerance of harmless negotiation. TLS control channels treat bytes as TLS application data and do not perform Telnet urgent-data processing.
- Case-insensitive four-letter and registered command verbs, whitespace rules, empty versus absent arguments, pathname preservation, numeric bounds, and raw unknown-command preservation.
- Command sequencing/state checks for connection, host selection, authentication, rename pairs, restart state, active/passive state, TLS state, transfer-in-progress state, and REIN reset.
- Client command/reply transcript events with PASS/ACCT/ADAT and certificate data redacted by default.

### Reply generation and parsing

- All three-digit reply codes, first-digit categories (1xx preliminary, 2xx completion, 3xx intermediate, 4xx transient failure, 5xx permanent failure), second-digit functional groups, and preservation of unknown codes.
- Single-line and RFC 959 multiline replies, including matching terminating code, arbitrary intervening lines, blank text, mixed line endings in lenient client mode, strict server output, and all commands including QUIT being allowed to return multiline replies.
- Sequences containing preliminary 125/150 plus terminal 226/250 replies, ABOR sequences such as 426 then 226, asynchronous 421, and replies arriving while a data socket closes.
- Typed parsers for 211 FEAT, 213 SIZE/MDTM, 215 SYST, 227 PASV, 229 EPSV, 257 PWD/MKD, 350 REST/RNFR, 522 network-protocol rejection, and MLST facts.
- Reply-policy helpers for accepted code sets, transient/permanent classification, authentication failures, protocol malformation, and exception-less inspection before callers choose to throw.

### Complete standardized command inventory

- RFC 959 mandatory access/parameter/service commands: ABOR, ACCT, ALLO, APPE, CWD, DELE, HELP, LIST, MODE, NLST, NOOP, PASS, PASV, PORT, QUIT, REIN, REST, RETR, RNFR, RNTO, SITE, STAT, STOR, STRU, TYPE, USER.
- RFC 959 optional commands: CDUP, MKD, PWD, RMD, SMNT, STOU, SYST.
- RFC 2389 commands: FEAT and OPTS, including empty feature sets, unknown feature lines, repeated features, feature parameters, and option-specific success/failure.
- RFC 2428 commands: EPRT and EPSV, including IPv4/IPv6 protocol numbers, arbitrary valid delimiters, EPSV with protocol argument, EPSV ALL, 522 replies, and standards-directed fallback rules.
- RFC 2640 command/feature behavior: LANG, UTF8 feature advertisement, UTF-8 default/negotiation policy, language tags, and OPTS UTF8 ON/OFF as a deployed compatibility form.
- RFC 3659 commands/features: MDTM, SIZE, MLST, MLSD, amended REST STREAM, TVFS, and OPTS MLST fact selection.
- RFC 2228/RFC 4217 commands: AUTH TLS and TLS-C synonym acceptance, PBSZ, PROT, and CCC. ADAT, MIC, CONF, and ENC are recognized by codecs and can be supplied by a custom security extension handler; the built-in TLS mechanism does not emit them because RFC 4217 does not use them.
- RFC 7151 command: HOST using DNS names and bracketed address literals, sent before USER and reset by REIN.
- IANA-registered ALGS is recognized and available through the raw client/extension-handler paths, but a normal endpoint server returns 502 because ALGS reports FTP64 application-layer-gateway state rather than FTP server state.
- Historic compatibility aliases: XCUP, XCWD, XMKD, XPWD, XRMD map to their RFC 959 successors when server compatibility aliases are enabled. Obsolete LPRT/LPSV are recognized and rejected with an explanatory response rather than implemented.
- Every standardized, historic, and unknown command is available through low-level client raw-command and server extension-handler paths, without weakening command-injection checks.

### Representation, structure, and transfer modes

- TYPE A ASCII, TYPE E EBCDIC, TYPE I Image/binary, and TYPE L local byte size, with FORMAT N non-print, T Telnet, and C ASA carriage control where applicable.
- Network ASCII CRLF conversion with correct CR NUL/CR LF handling across buffer boundaries and no conversion in Image mode.
- Table-driven EBCDIC conversion with an explicitly selected built-in mapping and deterministic handling of unmappable characters.
- Local-byte packing/unpacking for legal byte widths with bit-boundary state carried across buffers; Image and TYPE L 8 remain zero-conversion paths.
- STRU F file, STRU R record, and STRU P page structures.
- MODE S stream, MODE B block, and MODE C compressed transfer modes; EOF/EOR/restart-marker descriptors, page headers, byte counts, run-length encoding, and escape handling.
- MODE Z Deflate as an opt-in deployed extension using System.IO.Compression, advertised only when enabled and never confused with RFC 959 MODE C.
- Valid representation/structure/mode combination checks and explicit 504 rejection of locally disabled combinations.

### Active, passive, and server-to-server data connections

- Passive client default with EPSV first, PASV fallback only on IPv4, configurable EPSV-only/EPSV-ALL/PASV-only modes, and correct control-peer reuse for EPSV.
- Secure PASV handling: ignore the advertised address by default, optionally honor it, reject mismatched/private/public or unparsable endpoints according to policy, and never connect to prohibited targets.
- Active client operation with EPRT first and PORT fallback on IPv4, configurable bind address and port range, advertised external address, accept timeout, and data-peer verification.
- Server PASV/EPSV listeners with configured passive port ranges, NAT masquerade address mapping, dual-stack behavior, one-shot listener lifecycle, EPSV ALL enforcement, and resource cleanup.
- Server PORT/EPRT outbound connections with RFC 2577 anti-bounce checks: same control peer by default, privileged/blocked port rejection, address-family validation, DNS-free numeric parsing, and explicit opt-in FXP policy.
- Data connection setup before or after transfer commands as permitted, 125 versus 150 replies, connection timeout/error mapping, half-close behavior, final reply only after transfer/flush/TLS shutdown, and ABOR cleanup.
- RFC 959 third-party/FXP orchestration using PASV/PORT or EPSV/EPRT between two servers, clear data by default, explicit policy gates, and rejection of protected server-to-server data paths excluded by RFC 4217.
- FTPS data-channel TLS with the FTP client always acting as TLS client and the server acting as TLS server regardless of active/passive TCP initiator.

### Directory listings and metadata

- MLST single-object and MLSD directory parsing/generation as the preferred machine-readable path.
- Standard facts: type (file, dir, cdir, pdir), size, modify, create, perm, lang, media-type, unique, and charset; case-insensitive fact names; unknown OS/vendor facts preserved.
- Exact UTC timestamp parsing/formatting for YYYYMMDDHHMMSS with optional fractions, validation without accidental local-time conversion, and clear representation of unavailable fields without nullable value types.
- OPTS MLST selection and FEAT asterisk defaults, per-session fact selection, pathname separation after the required single space, and raw-line retention for diagnostics.
- LIST and STAT listing generation and parsing for common Unix, DOS/Windows, EPLF, VMS, and OS/400 formats with automatic parser selection and explicit raw fallback.
- NLST name-only listing with no assumptions that names are safe paths or that NLST can determine type.
- Hidden entries, symlinks, ownership, group, permissions, link targets, cdir/pdir filtering, recursive listing, and cycle detection.
- Metadata operations using MLST first and safe fallbacks to SIZE, MDTM, CWD/PWD probes, LIST, and NLST according to feature availability and caller policy.

### Client connection, negotiation, and session behavior

- Plain FTP, required explicit FTPS, opportunistic explicit FTPS only when explicitly selected, and implicit FTPS on configurable ports (default 990).
- Explicitly requested connection-profile probing that tests implicit TLS, explicit TLS, and plain FTP without sending credentials; it reports viable profiles and never permits a plain fallback when TLS is required.
- DNS and dual-stack connection attempts, local endpoint binding, greeting collection, HOST before authentication, USER/PASS/ACCT exchanges, anonymous credentials, and reauthentication after REIN.
- FEAT immediately after connection or TLS upgrade as appropriate, normalized feature storage with original text, unknown feature preservation, and refresh after state changes.
- Adaptive selection of UTF-8, MLSD/MLST, EPSV/EPRT, SIZE/MDTM, REST STREAM, HOST, PRET, checksum mechanisms, and MODE Z.
- Configurable server quirks for strict versus lenient replies, PASV address use, UTF-8 advertisement errors, MLSD failure fallback, broken SIZE/MDTM, and commands required before AUTH.
- Raw command execution, SITE execution, command batches with ordered replies, server HELP/STAT/SYST retrieval, idle and during-transfer NOOP keepalive, reconnect with session setting restoration, and safe refusal to replay non-idempotent transfers automatically.

### File and directory operations

- Current-directory operations: PWD, CWD, CDUP, SMNT, path normalization, absolute/relative TVFS paths, and restoration after temporary directory traversal.
- Create directory, create parent tree, remove empty directory, recursive safe deletion, delete file, rename/move with RNFR/RNTO, existence/type probe, and collision policy.
- SIZE, MDTM, MLST metadata retrieval; optional MFMT setting; SITE CHMOD/UMASK compatibility; permissions retained when available.
- Stream-based RETR, STOR, APPE, STOU and file-path convenience operations, with caller-owned stream lifetime policy.
- REST restart for downloads and uploads, byte-offset validation, local stream seeking, server SIZE/MDTM/unique checks before resuming, and safe fallback or failure if resume invariants cannot be established.
- Stream/block/compressed-mode restart markers and client exposure of the latest restart marker.
- Transfer progress snapshots, cancellation, expected size, elapsed time, wire/logical byte counts, and observer callbacks without public delegates.
- Atomic upload through temporary remote name plus rename, unique upload name extraction, overwrite/skip/resume/append/fail collision policies, optional parent creation, and cleanup policy for failed temporary uploads.
- Verification by exact SIZE and MDTM policy, standard HASH command negotiation, MD5/XMD5/XSHA1/XSHA256/XSHA512/XCRC compatibility commands, and built-in local hashing.
- Recursive upload/download, include/exclude decisions through an interface, symlink policy, empty directories, timestamp preservation where supported, delete-extraneous synchronization, dry-run planning, per-item results, and continue/stop failure policy.
- FXP server-to-server copy, append, and unique-store flows with both final replies consumed and both session states restored.

### TLS and certificate behavior

- Explicit upgrade sequence AUTH TLS (or accepted TLS-C), 234 handling, TLS handshake, refreshed FEAT, PBSZ 0, PROT C/P, and configurable timing around USER/PASS and server-specific pre-AUTH commands.
- Implicit TLS handshake before reading/sending FTP application bytes.
- System-default TLS versions/cipher policy by default, optional SslProtocols restriction, server certificate with private key, intermediate certificate context, optional client certificates, and optional mutual TLS.
- Strict hostname validation by default, target-host/SNI handling, chain and revocation policy, certificate-validator interface, certificate diagnostics, and no accept-all default.
- Server certificate selection by implicit SNI or prior HOST for explicit TLS, with safe default-host behavior.
- PROT state retained until changed, REIN, or renegotiated AUTH; PBSZ ordering and zero value enforcement for TLS; PROT P/C only for the built-in TLS mechanism.
- Data-channel TLS handshake and shutdown, optional session-reuse requirement where supported by SslStream, and clear failure when client/server policy differs.
- CCC disabled by default on the server, explicit client opt-in, orderly TLS close before returning to clear control, prohibition on PROT changes without protected control, and audit events for downgrade.
- TLS failure never silently downgrades when required explicit or implicit FTPS was selected.

### Server hosting, storage, authentication, and policy

- One server instance can host multiple explicit/plain and implicit-TLS listeners with independent bind endpoints and shared policy providers.
- Start/stop/state operations, bounded pending accepts, maximum global/per-address/per-user sessions, idle/login/data timeouts, graceful drain, forced close deadline, and isolated session faults.
- Per-session state for selected virtual host, encoding/language, principal, current/root directory, transfer representation/mode/structure, restart marker, rename source, active/passive setup, protection level, feature options, and current transfer.
- HOST-based virtual hosting with host-specific greeting, certificate, authenticator, authorizer, filesystem root/provider, feature set, and limits.
- Authentication provider for USER/PASS/ACCT and anonymous login, optional TLS client-certificate identity, attempt limit/delay, redacted events, and uniform failure replies.
- Authorization provider for list/read/write/append/create/delete/rename/metadata/site/FXP operations with path and principal context.
- Virtual filesystem contract for metadata, listing, open-read, open-write, create/delete/rename, unique naming, allocation, free-space/quota, and structure mounts.
- Built-in local filesystem rooted beneath an explicit directory, canonical path containment, separator normalization, dot-segment handling, symlink traversal policy, case behavior inherited from the platform, atomic create/replace, and no exposure above root.
- Server implementations for every in-scope standard command, correct preconditions and reply codes, rename/restart transaction state, FEAT derived from enabled capabilities, and HELP derived from command availability.
- Extension-handler chain for custom commands, SITE subcommands, security mechanisms, and vendor replies using exception-less handled/result objects.
- Optional compatibility server commands: X aliases, PRET, CLNT, MFMT, HASH variants, MODE Z, SITE CHMOD, SITE UMASK, and RMDA; all separately enabled.
- Event sink for server lifecycle, connections, authentication, commands/replies, authorization, filesystem actions, transfer progress/completion, TLS changes, policy rejection, and faults, with secret redaction.
- Resource controls for command rate, malformed-line count, authentication attempts, passive listeners, simultaneous transfers, bytes per transfer/session, storage quota, read/write bandwidth ceilings, and listing entry count.

### Security requirements

- Reject command injection and parser confusion through CR/LF/NUL, overlong lines, invalid UTF-8, invalid Telnet escapes, numeric overflow, ambiguous endpoint syntax, and malformed multiline replies.
- Default anti-bounce and anti-port-theft peer matching, passive advertised-address suppression, blocked address/port ranges, FXP opt-in, and data-peer validation.
- Canonicalize and authorize paths after decoding but before filesystem access; block root escape through dot segments, alternate separators, device paths, symlinks/reparse points, Unicode ambiguity policy, and rename destination tricks.
- Never log PASS, ACCT, ADAT, private keys, complete certificates, or raw authorization secrets.
- Require explicit opt-in for plaintext credentials, opportunistic TLS downgrade, CCC, anonymous writes, FXP, historic aliases, and recursive destructive operations.
- Bound all protocol buffers, feature/fact counts, directory result counts, queued sessions, passive ports, transfer sizes, timeouts, and authentication retries.
- Use cryptographic random unique temporary names and fixed-time comparison for stored secret verifiers supplied by the built-in fixed-user authenticator.

## Proposed public API

The names below define shape and responsibility, not final signatures. Every method will have one unique name; related variations use request/options data instead of overloads.

### Contracts under src/Api/Contracts

| File/type | Planned operation groups |
| --- | --- |
| IFtpCommandCodec.cs | ParseCommand, FormatCommand |
| IFtpReplyCodec.cs | ReadReply, WriteReply, ClassifyReply |
| IFtpListingParser.cs | ParseMachineListing, ParseHumanListing, ParseNameListing |
| IFtpClient.cs | Connect, Reconnect, Disconnect, ReinitializeSession, AuthenticateUser, UpgradeControlTls, SetDataProtection, ClearControlTls, NegotiateFeatures, ExecuteCommand, ExecuteCommandBatch, all session/metadata/listing/filesystem/transfer/recursive/verification/FXP operations |
| IFtpServer.cs | StartServer, StopServer, GetServerState |
| IFtpFileSystem.cs | ResolveEntry, EnumerateDirectory, OpenFileRead, OpenFileWrite, CreateDirectoryEntry, DeleteFileEntry, DeleteDirectoryEntry, RenameEntry, CreateUniqueEntry, AllocateEntry, QuerySpace, MountStructure |
| IFtpAuthenticator.cs | AuthenticateIdentity |
| IFtpAuthorizer.cs | AuthorizeOperation |
| IFtpHostResolver.cs | ResolveVirtualHost |
| IFtpCertificateProvider.cs | SelectServerCertificate |
| IFtpCertificateValidator.cs | ValidateRemoteCertificate |
| IFtpExtensionHandler.cs | HandleExtensionCommand |
| IFtpTransferObserver.cs | ObserveTransferStarted, ObserveTransferProgress, ObserveTransferCompleted |
| IFtpTransferFilter.cs | IncludeTransferEntry |
| IFtpEventSink.cs | RecordEvent |

### Data under src/Api/Data

| Planned files | Purpose |
| --- | --- |
| FtpEndpoint.cs, FtpCredentials.cs, FtpConnectionProfile.cs, FtpConnectionProbeResult.cs | Network target/bind data, USER/PASS/ACCT data, and credential-free connection detection results |
| FtpTimeoutOptions.cs, FtpDataConnectionOptions.cs, FtpTlsOptions.cs | Shared connection, active/passive, NAT, TLS, certificate, and timeout settings |
| FtpClientOptions.cs, FtpServerOptions.cs, FtpListenerOptions.cs, FtpVirtualHostOptions.cs | Top-level client/server/listener/virtual-host configuration |
| FtpCommand.cs, FtpReply.cs, FtpReplyLine.cs, FtpCommandResult.cs | Low-level request/reply model and exception-less command inspection |
| FtpFeature.cs, FtpFeatureSet.cs, FtpOptionRequest.cs | FEAT and OPTS model with raw/normalized values |
| FtpEntry.cs, FtpFact.cs, FtpListing.cs, FtpTimestamp.cs, FtpPathProbeResult.cs | Machine/human listing, metadata, timestamp availability, and existence/type results |
| FtpTransferRequest.cs, FtpTransferOptions.cs, FtpTransferResult.cs, FtpTransferProgress.cs | Single-stream/file transfer inputs, controls, progress, and outcome |
| FtpDirectoryTransferRequest.cs, FtpDirectoryTransferOptions.cs, FtpDirectoryPlan.cs, FtpDirectoryItemResult.cs, FtpDirectoryTransferResult.cs | Recursive transfer and synchronization planning/results |
| FtpServerTransferRequest.cs, FtpServerTransferResult.cs | FXP source/target and paired reply results |
| FtpAuthenticationRequest.cs, FtpAuthenticationResult.cs, FtpPrincipal.cs, FtpUserRecord.cs, FtpFixedUserOptions.cs | Authentication provider boundary and salted fixed-user configuration |
| FtpAuthorizationRequest.cs, FtpAuthorizationResult.cs | Authorization provider boundary |
| FtpFileSystemRequest.cs, FtpFileSystemResult.cs, FtpFileOpenResult.cs, FtpSpaceInfo.cs | Virtual filesystem provider boundary |
| FtpVirtualHostRequest.cs, FtpVirtualHostResult.cs | HOST routing result |
| FtpCertificateRequest.cs, FtpCertificateResult.cs, FtpCertificateValidationRequest.cs, FtpCertificateValidationResult.cs | Certificate selection and validation boundaries |
| FtpExtensionContext.cs, FtpExtensionResult.cs | Server extension command boundary |
| FtpEvent.cs, FtpTransferFilterRequest.cs | Structured redacted events and recursive inclusion decisions |

All optional value data uses an accompanying HasX boolean field or a reference field rather than nullable value types.

### Enums under src/Api/Enums

FtpSecurityMode.cs, FtpDataProtectionLevel.cs, FtpDataConnectionMode.cs, FtpAddressFamily.cs, FtpTransferType.cs, FtpTextFormat.cs, FtpFileStructure.cs, FtpTransferMode.cs, FtpReplyCategory.cs, FtpReplyKind.cs, FtpCommandKind.cs, FtpFeatureSupport.cs, FtpEntryType.cs, FtpPermission.cs, FtpCollisionPolicy.cs, FtpVerificationMode.cs, FtpResumePolicy.cs, FtpSymlinkPolicy.cs, FtpDirectorySyncMode.cs, FtpFailurePolicy.cs, FtpTransferDirection.cs, FtpServerState.cs, FtpSessionState.cs, FtpAuthenticationStatus.cs, FtpAuthorizationOperation.cs, FtpFileOpenMode.cs, FtpEventKind.cs, and FtpExtensionDisposition.cs.

Each enum is consumed by at least one public data class or interface and has explicit Int32 member values.

### Static data under src/Api/Static

| File/type | Fields only |
| --- | --- |
| FtpPorts.cs | Control 21 and implicit FTPS 990 defaults |
| FtpCommandNames.cs | Standard, security, extension, and compatibility command strings |
| FtpReplyCodes.cs | Named RFC reply-code integer constants |
| FtpFeatureNames.cs | FEAT tokens such as AUTH, UTF8, MLST, REST STREAM, SIZE, MDTM, TVFS, HOST, and compatibility tokens |
| FtpFactNames.cs | Standard MLSx fact names |
| FtpDefaults.cs | bounded sizes, timeout, buffer, retry, and passive-policy defaults |

### Exceptions under src/Api/Exceptions

FtpProtocolException.cs, FtpConnectionException.cs, FtpCommandException.cs, FtpAuthenticationException.cs, FtpAuthorizationException.cs, FtpTransferException.cs, FtpTlsException.cs, and FtpConfigurationException.cs. Each directly derives from System.Exception and carries its relevant public data through constructor text/inner exception rather than properties.

### Public implementations under src/Api/Classes

| File/type | Implements |
| --- | --- |
| FtpCommandCodec.cs | IFtpCommandCodec |
| FtpReplyCodec.cs | IFtpReplyCodec |
| FtpListingParser.cs | IFtpListingParser |
| FtpClient.cs | IFtpClient |
| FtpServer.cs | IFtpServer |
| LocalFtpFileSystem.cs | IFtpFileSystem |
| AnonymousFtpAuthenticator.cs | IFtpAuthenticator |
| FixedUserFtpAuthenticator.cs | IFtpAuthenticator |
| AllowAllFtpAuthorizer.cs | IFtpAuthorizer |
| DefaultFtpHostResolver.cs | IFtpHostResolver |
| DefaultFtpCertificateProvider.cs | IFtpCertificateProvider |
| StrictFtpCertificateValidator.cs | IFtpCertificateValidator |
| NullFtpEventSink.cs | IFtpEventSink |

### Planned internal source files outside src/Api

- src/Protocol/FtpLineReader.cs, TelnetControlFilter.cs, FtpCommandParser.cs, FtpCommandWriter.cs, FtpReplyParser.cs, FtpReplyWriter.cs, FtpSequenceValidator.cs, FtpFeatureParser.cs, FtpTimestampCodec.cs, FtpMlsxCodec.cs, FtpEndpointCodec.cs, and FtpPathCodec.cs.
- src/Transfer/AsciiTransferCodec.cs, EbcdicTransferCodec.cs, LocalByteTransferCodec.cs, RecordStructureCodec.cs, PageStructureCodec.cs, StreamModeCodec.cs, BlockModeCodec.cs, CompressedModeCodec.cs, ModeZCodec.cs, RestartMarkerCodec.cs, TransferPump.cs, TransferHash.cs, and TransferVerifier.cs.
- src/Transport/FtpSocketOperation.cs, FtpControlTransport.cs, FtpTlsTransport.cs, FtpActiveConnector.cs, FtpPassiveListener.cs, FtpDataConnectionCoordinator.cs, FtpPassivePortPool.cs, and FtpPeerPolicy.cs.
- src/Client/FtpClientSession.cs, FtpClientNegotiator.cs, FtpClientDataCommand.cs, FtpClientMetadata.cs, FtpClientListing.cs, FtpClientTransfer.cs, FtpClientDirectoryTransfer.cs, FtpClientVerifier.cs, FtpClientFxp.cs, and FtpClientRecovery.cs.
- src/Server/FtpServerHost.cs, FtpServerListener.cs, FtpServerSession.cs, FtpServerStateMachine.cs, FtpServerDispatcher.cs, FtpServerCommandSet.cs, FtpServerDataCommand.cs, FtpServerFeatureSet.cs, FtpServerLimits.cs, FtpServerFaultBoundary.cs, and src/Server/Commands/AccessCommandHandler.cs, ParameterCommandHandler.cs, DirectoryCommandHandler.cs, ListingCommandHandler.cs, TransferCommandHandler.cs, MetadataCommandHandler.cs, FeatureCommandHandler.cs, SecurityCommandHandler.cs, StatusCommandHandler.cs, and ExtensionCommandHandler.cs.
- src/FileSystem/LocalPathResolver.cs, LocalEntryReader.cs, LocalListingWriter.cs, LocalFileOperations.cs, LocalUniqueNameGenerator.cs, and LocalQuotaTracker.cs.
- src/Security/FtpInputSanitizer.cs, FtpPathSecurity.cs, FtpTlsPolicy.cs, FtpSecretRedactor.cs, FtpAuthenticationGate.cs, and FtpAuthorizationGate.cs.
- src/Compatibility/FtpHumanListingParsers.cs, FtpHistoricAliases.cs, FtpPretExtension.cs, FtpModeZExtension.cs, FtpChecksumExtensions.cs, FtpMfmtExtension.cs, and FtpSiteExtensions.cs.

## Implementation tasks

All tasks are open and are ordered by dependency. Test tasks intentionally cover the functionality exposed through every src/Api category; there are no syntax-policy, source-checking, benchmark, documentation, or release tasks.

### API and protocol foundations

- [DONE] T001: Create the src/Api category directories and internal layer directories described in the planned source layout.
- [DONE] T002: Implement all public protocol, connection, transfer, security, server-state, result, and policy enums with stable Int32 values.
- [DONE] T003: Implement FtpPorts, FtpCommandNames, FtpReplyCodes, FtpFeatureNames, FtpFactNames, and FtpDefaults as field-only static data classes.
- [DONE] T004: Implement endpoint, credentials, timeout, data-connection, TLS, listener, client, server, and virtual-host option data classes with bounded secure defaults.
- [DONE] T005: Implement command, reply, reply-line, reply classification, command-result, feature, feature-set, and option-request data classes.
- [DONE] T006: Implement entry, fact, listing, timestamp, and path-probe data classes without nullable value types.
- [DONE] T007: Implement single-transfer, directory-transfer, synchronization-plan, item-result, progress, verification, and FXP data classes.
- [DONE] T008: Implement authentication, principal, authorization, filesystem, space, host-routing, certificate, extension, filter, and event provider-boundary data classes.
- [DONE] T009: Define IFtpCommandCodec, IFtpReplyCodec, and IFtpListingParser with non-overloaded low-level operations.
- [DONE] T010: Define IFtpClient with uniquely named connection, negotiation, command, metadata, listing, file, transfer, recursive, verification, and FXP operations.
- [DONE] T011: Define IFtpServer with lifecycle/state operations and define the filesystem, authenticator, authorizer, host, certificate, extension, observer, filter, and event interfaces.
- [DONE] T012: Implement all direct System.Exception-derived FTP exception types and map protocol-created failures to the appropriate type.
- [DONE] T013: Implement an internal fault-isolation runner based on Task completion state and result envelopes so recoverable flows do not require prohibited try/catch syntax.
- [DONE] T014: Implement shared cancellation, deadline, timeout, size-limit, and resource-lifetime helpers for synchronous public operations.
- [DONE] T015: Implement input validation for configuration combinations, endpoint bounds, port ranges, TLS requirements, transfer options, and provider presence.

### Control channel, commands, replies, and negotiation

- [DONE] T016: Implement a bounded incremental CRLF line reader/writer handling fragmented and coalesced socket input without unbounded allocation.
- [DONE] T017: Implement the clear-channel RFC 854 Telnet filter for IAC escaping, option negotiation, IP/DM synchronization, and urgent ABOR signals.
- [DONE] T018: Implement strict command parsing for verbs, arguments, whitespace, negotiated encoding, numeric fields, and embedded-control-character rejection.
- [DONE] T019: Implement command formatting with canonical verbs, safe pathname/argument insertion, CRLF termination, and secret classification for redaction.
- [DONE] T020: Implement single-line and multiline reply parsing with matching terminators, size limits, unknown-code preservation, and strict/lenient client modes.
- [DONE] T021: Implement standards-conforming reply formatting, multiline generation, and first/second digit classification.
- [DONE] T022: Implement typed reply extraction for greetings, SIZE, MDTM, SYST, PASV, EPSV, PWD/MKD, REST/RNFR, network-protocol rejection, and transfer completion sequences.
- [DONE] T023: Implement preliminary/final reply coordination, asynchronous 421 handling, ABOR multi-reply handling, and pending data-command completion.
- [DONE] T024: Implement FEAT parsing/generation with repeated and unknown features, parameters, AUTH mechanisms, MLST default facts, and original-line retention.
- [DONE] T025: Implement OPTS parsing/generation for MLST, UTF8, and extension-owned options with per-session state.
- [DONE] T026: Implement RFC 3659 timestamp and SIZE codecs with overflow checks, fractional seconds, availability flags, and UTC semantics.
- [DONE] T027: Implement MLSx fact and entry codecs for all standard facts plus preserved OS/vendor facts and exact pathname separation.
- [DONE] T028: Implement PORT, PASV, EPRT, and EPSV endpoint codecs including arbitrary EPRT/EPSV delimiters, IPv4/IPv6 rules, EPSV ALL, and 522 data.
- [DONE] T029: Implement HOST, language-tag, quoted PWD/MKD pathname, ALLO, REST marker, and TYPE L argument codecs.
- [DONE] T030: Implement the command catalog and sequence validator for every RFC 959 command, FEAT/OPTS, EPRT/EPSV, LANG, RFC 3659 commands, FTP-TLS commands, HOST, ALGS, and historic aliases.
- [DONE] T031: Implement public FtpCommandCodec and FtpReplyCodec classes over the internal codecs and exception-less classification results.
- [DONE] T032: Implement connection/session state reset rules for connect, AUTH, USER/PASS/ACCT, REIN, HOST, rename, REST, data setup, transfer, ABOR, CCC, and QUIT.

### Representation, structure, mode, and transfer pipeline

- [DONE] T033: Implement streaming TYPE I and TYPE L 8 zero-conversion paths with caller-owned stream lifetime controls.
- [DONE] T034: Implement TYPE A network-ASCII encoding/decoding with CRLF, CR NUL, partial-buffer, and FORMAT N behavior.
- [DONE] T035: Implement FORMAT T Telnet and FORMAT C ASA carriage-control transforms for supported text representations.
- [DONE] T036: Implement table-driven TYPE E EBCDIC encoding/decoding and deterministic unmappable-character policy.
- [DONE] T037: Implement TYPE L non-eight-bit local-byte bit packing/unpacking with state retained across buffer boundaries.
- [DONE] T038: Implement STRU F file framing and EOF semantics for every applicable transfer mode.
- [DONE] T039: Implement STRU R record boundaries, EOR/EOF semantics, and record conversion for stream and block modes.
- [DONE] T040: Implement STRU P page headers, page indexes, descriptor validation, and page restart locations.
- [DONE] T041: Implement MODE S stream framing, EOF behavior, escape rules, and RFC 3659 byte-offset REST semantics.
- [DONE] T042: Implement MODE B block headers, EOR/EOF flags, restart-marker blocks, byte counts, and malformed-block rejection.
- [DONE] T043: Implement MODE C run-length compression, fill/replicate encoding, escape handling, EOF state, and malformed-data rejection.
- [DONE] T044: Implement optional MODE Z Deflate wrapping through built-in compression streams and keep it distinct from MODE C.
- [DONE] T045: Implement restart-marker generation, parsing, latest-marker reporting, and representation/structure/mode combination validation.
- [DONE] T046: Implement the transfer pump with bounded buffers, cancellation, deadlines, logical/wire byte accounting, progress snapshots, and deterministic flush/close order.
- [DONE] T047: Implement built-in local MD5, SHA-1, SHA-256, SHA-512, and CRC32 calculation and size/time/checksum transfer verification.

### Data connections and network policy

- [DONE] T048: Implement passive-client EPSV connection setup, control-peer address reuse, protocol selection, and EPSV ALL state.
- [DONE] T049: Implement passive-client PASV fallback with advertised-address ignore-by-default, configurable trust, NAT mismatch policy, and prohibited-target filtering.
- [DONE] T050: Implement active-client EPRT/PORT listener setup, bind/advertise address selection, port ranges, accept deadlines, and peer verification.
- [DONE] T051: Implement server PASV/EPSV one-shot listeners, passive port pooling, dual-stack selection, NAT masquerade mapping, and EPSV ALL enforcement.
- [DONE] T052: Implement server PORT/EPRT outbound setup with numeric endpoint parsing, same-peer enforcement, blocked ports/addresses, and explicit FXP policy.
- [DONE] T053: Implement the shared data-connection coordinator for pre-opened versus on-demand sockets, 125/150 selection, timeouts, half-close, ABOR, and cleanup.
- [DONE] T054: Implement peer-address normalization for IPv4-mapped IPv6, scope identifiers, loopback/private/link-local/multicast/unspecified checks, and address-family matching.
- [DONE] T055: Implement configurable control/data socket settings using only built-in networking APIs and validate unsupported combinations before connection.
- [DONE] T056: Implement clear data-stream send/receive and final-reply ordering for LIST/NLST/MLSD/RETR/STOR/STOU/APPE.
- [DONE] T057: Implement RFC 959 third-party data setup primitives and paired-session state tracking for server-to-server transfers.

### TLS and certificate security

- [DONE] T058: Implement SslStream client and server wrappers for implicit TLS before greeting and explicit AUTH TLS upgrade after a 234 reply.
- [DONE] T059: Implement strict client target-host, SNI, chain, revocation, name-mismatch, client-certificate, and custom IFtpCertificateValidator processing.
- [DONE] T060: Implement server certificate selection through IFtpCertificateProvider using implicit SNI or explicit HOST context, including intermediate certificates and optional mutual TLS.
- [DONE] T061: Implement AUTH TLS/TLS-C server and client state transitions, safe failure termination, post-upgrade feature refresh, and required-versus-opportunistic downgrade rules.
- [DONE] T062: Implement PBSZ zero validation and PROT C/P negotiation/order/state rules, including REIN and repeated AUTH effects.
- [DONE] T063: Implement protected data channels with the FTP client always taking the TLS-client role for both active and passive TCP data connections.
- [DONE] T064: Implement configurable FTPS data-session reuse checks where exposed by SslStream and explicit policy-mismatch failures.
- [DONE] T065: Implement CCC TLS shutdown and clear-control restoration with server opt-in, client opt-in, audit events, and prohibition of insecure PROT changes.
- [DONE] T066: Implement public default/strict certificate provider and validator classes without accept-all behavior.
- [DONE] T067: Implement TLS diagnostics and transcript redaction that reveal negotiated security facts without exposing keys, secrets, or full certificate material.

### Client session and low-level operations

- [DONE] T068: Implement FtpClient construction, validated option snapshotting, connection-state storage, and public IFtpClient dispatch.
- [DONE] T069: Implement DNS/IPv4/IPv6 control connection attempts, optional local bind, greeting handling, delayed 120 readiness, cancellation, and connection fault mapping.
- [DONE] T070: Implement plain, explicit-required, explicit-opportunistic, and implicit FTPS connection sequences plus credential-free profile probing, with no unintended downgrade.
- [DONE] T071: Implement HOST-before-USER routing and USER/PASS/ACCT or anonymous authentication sequences with exact intermediate/completion reply handling.
- [DONE] T072: Implement FEAT refresh and adaptive option negotiation for UTF-8, MLST facts, REST STREAM, EPSV/EPRT, TLS, HOST, and compatibility features.
- [DONE] T073: Implement raw safe command, raw SITE, command-batch, HELP, STAT, SYST, NOOP, QUIT, abrupt disconnect, and REIN client operations.
- [DONE] T074: Implement TYPE/FORMAT, STRU, MODE, ALLO, working-directory, CDUP, SMNT, PWD, and server-status client operations.
- [DONE] T075: Implement active/passive/automatic data strategy selection and standards-directed fallback without replaying a transfer that may have started.
- [DONE] T076: Implement reconnect with restoration of host, TLS, login, encoding, working directory, representation, mode, structure, protection, and feature options.
- [DONE] T077: Implement server quirk flags for reply leniency, PASV address policy, broken feature claims, MLSD fallback, SIZE/MDTM fallback, and commands around AUTH.

### Client listings, metadata, and filesystem operations

- [DONE] T078: Implement MLST single-entry and MLSD directory client operations with selected facts, raw facts, and fallback policy.
- [DONE] T079: Implement Unix, DOS/Windows, EPLF, VMS, and OS/400 LIST/STAT parsers with deterministic detection and raw-entry fallback.
- [DONE] T080: Implement LIST, STAT listing, NLST, hidden-entry, raw listing, and parser-selection client operations.
- [DONE] T081: Implement public FtpListingParser across machine, human, and name-only listing forms.
- [DONE] T082: Implement SIZE, MDTM, MLST metadata retrieval and safe existence/type fallback probes that restore the working directory.
- [DONE] T083: Implement CWD, CDUP, PWD, MKD, parent-tree creation, RMD, DELE, RNFR/RNTO rename/move, and collision handling.
- [DONE] T084: Implement recursive deletion with explicit enablement, root protection, symlink/cycle policy, and per-entry authorization-result reporting.
- [DONE] T085: Implement optional MFMT, SITE CHMOD, and SITE UMASK client operations gated by feature/configuration policy.

### Client transfers and high-level operations

- [DONE] T086: Implement RETR-to-stream and RETR-to-file with preliminary/final reply handling, local file collision policy, and partial-file cleanup policy.
- [DONE] T087: Implement STOR-from-stream/file, APPE, and STOU with returned unique name, parent creation, and remote collision policy.
- [DONE] T088: Implement REST STREAM download resume with local length/seek checks and remote SIZE/MDTM/unique validation.
- [DONE] T089: Implement REST STREAM upload resume with remote length, local seek, APPE/STOR selection, and unsafe-resume refusal.
- [DONE] T090: Implement block/page-mode marker restart and expose accepted/sent markers through transfer results.
- [DONE] T091: Implement atomic upload through random temporary remote names, verification, rename commit, and configured cleanup after failure.
- [DONE] T092: Implement remote checksum negotiation across HASH and common vendor commands with algorithm selection and local digest comparison.
- [DONE] T093: Implement progress observer calls, cancellation, expected length, logical/wire counts, periodic NOOP keepalive, and observer-fault isolation for all transfer paths.
- [DONE] T094: Implement recursive directory download with empty directories, filter, symlink, collision, timestamp, verification, and failure policies.
- [DONE] T095: Implement recursive directory upload with parent creation, filter, symlink, collision, timestamp, verification, and failure policies.
- [DONE] T096: Implement remote/local directory comparison and dry-run synchronization plans without mutating either side.
- [DONE] T097: Implement mirror/update synchronization execution, optional delete-extraneous behavior, root guards, and per-item results.
- [DONE] T098: Implement FXP copy/append/unique flows using PASV/PORT or EPSV/EPRT, consume both servers' replies, reject protected-data incompatibility, and restore both sessions.

### Server host, providers, and lifecycle

- [DONE] T099: Implement FtpServer construction, validated immutable runtime settings, IFtpServer lifecycle dispatch, and observable server states.
- [DONE] T100: Implement multiple listener startup for plain/explicit-capable and implicit-TLS endpoints, including IPv4, IPv6, dual stack, backlog, and bind failures.
- [DONE] T101: Implement bounded accept loops and per-session Task fault boundaries so one malformed or failed session cannot stop other listeners.
- [DONE] T102: Implement graceful stop/drain and forced-close deadline behavior with listener, session, passive socket, data socket, TLS, and file cleanup.
- [DONE] T103: Implement global, address, user, pending-login, idle, command-rate, malformed-input, and simultaneous-transfer limits.
- [DONE] T104: Implement session creation, greeting, virtual-host state, command dispatch, reply serialization, idle tracking, and lifecycle events.
- [DONE] T105: Implement DefaultFtpHostResolver and HOST routing to host-specific greeting, providers, root, features, TLS certificate, and limits.
- [DONE] T106: Implement AnonymousFtpAuthenticator and FixedUserFtpAuthenticator with uniform failures, fixed-time secret verification, attempt limits, and optional account/client-certificate data.
- [DONE] T107: Implement AllowAllFtpAuthorizer and the authorization gate for every filesystem, transfer, metadata, SITE, and FXP operation.
- [DONE] T108: Implement NullFtpEventSink and structured redacted events for lifecycle, command, authentication, authorization, TLS, filesystem, transfer, policy, and fault activity.
- [DONE] T109: Implement exception-less extension-handler dispatch for unknown commands, custom security mechanisms, custom OPTS, and SITE subcommands.

### Server filesystem and command execution

- [DONE] T110: Implement LocalFtpFileSystem construction and root validation plus all IFtpFileSystem operations using built-in System.IO APIs.
- [DONE] T111: Implement canonical local-path containment across separators, dot segments, rooted/device paths, case rules, symlinks/reparse points, and rename destinations.
- [DONE] T112: Implement local metadata/listing conversion, stable unique identifiers where available, UTC times, permissions, media types, and unknown-field availability.
- [DONE] T113: Implement safe file read/write/append/create/replace, unique-name creation with cryptographic randomness, directory mutation, rename, allocation, space/quota, and mount results.
- [DONE] T114: Implement USER, PASS, ACCT, REIN, QUIT, HOST, AUTH, PBSZ, PROT, CCC, and login-sequence server handlers.
- [DONE] T115: Implement TYPE, STRU, MODE, ALLO, REST, PORT, PASV, EPRT, EPSV, and ABOR server handlers with exact state/precondition replies.
- [DONE] T116: Implement CWD, CDUP, SMNT, PWD, MKD, RMD, DELE, RNFR, and RNTO server handlers with authorization and transaction-state rules.
- [DONE] T117: Implement RETR, STOR, STOU, and APPE server handlers across representation/structure/mode combinations, restart behavior, limits, and final replies.
- [DONE] T118: Implement LIST, NLST, STAT, MLST, and MLSD server handlers with authorization, bounded results, selected facts, UTF-8, and transfer framing.
- [DONE] T119: Implement MDTM, SIZE, SYST, HELP, SITE, NOOP, FEAT, OPTS, and LANG server handlers with capability-derived responses.
- [DONE] T120: Implement server feature advertisement from actual enabled command/provider/TLS capabilities rather than a hard-coded optimistic list.
- [DONE] T121: Implement server active/passive data transfer lifecycle, peer validation, passive-port release, transfer limits, ABOR races, and control/data final-reply synchronization.
- [DONE] T122: Implement server ASCII/EBCDIC/local-byte, record/page, block/compressed, and restart-marker processing symmetrically with the client.
- [DONE] T123: Implement per-user storage quota, transfer byte ceilings, read/write bandwidth ceilings, listing caps, and clear 4xx/5xx resource-limit replies.

### Compatibility extensions

- [DONE] T124: Implement optional XCUP/XCWD/XMKD/XPWD/XRMD server aliases and client fallback while explicitly rejecting obsolete LPRT/LPSV and endpoint-inapplicable ALGS.
- [DONE] T125: Implement opt-in PRET client/server behavior from the retained Internet-Draft, including the exact upcoming transfer command and passive setup ordering.
- [DONE] T126: Implement opt-in CLNT and MFMT client/server compatibility commands with feature detection and strict argument parsing.
- [DONE] T127: Implement opt-in HASH plus MD5/XMD5/XSHA1/XSHA256/XSHA512/XCRC client/server commands and normalized digest results.
- [DONE] T128: Implement opt-in MODE Z client/server negotiation, FEAT advertisement, Deflate data wrapping, and interaction rules with TLS and restart.
- [DONE] T129: Implement opt-in SITE CHMOD, SITE UMASK, MKDIR/RMDIR compatibility forms, and RMDA with authorization and root-safety enforcement.

### Public API and unit test coverage

- [DONE] T130: Add tests that construct every public data class, exercise all public fields/defaults, and verify every optional value uses its HasX/reference representation correctly.
- [DONE] T131: Add tests for every enum member and every static data field, including uniqueness, command strings, reply codes, feature names, fact names, ports, and secure defaults.
- [DONE] T132: Add contract tests for every IFtpCommandCodec method covering all standard, security, extension, historic, unknown, malformed, Unicode, and injection inputs.
- [DONE] T133: Add contract tests for every IFtpReplyCodec method covering every reply category, all typed replies, multiline forms, malformed forms, limits, and preliminary/final sequences.
- [DONE] T134: Add contract tests for every IFtpListingParser method covering MLSx facts and Unix, DOS, EPLF, VMS, OS/400, name-only, unknown, Unicode, and malicious listing lines.
- [DONE] T135: Add tests that each public implementation exposes and correctly executes exactly the operations of its declared public interface.
- [DONE] T136: Add tests that every custom exception is directly throwable/catchable as System.Exception and is produced by its intended API failure paths.
- [DONE] T137: Add exhaustive command catalog and state-transition tests for legal and illegal sequences before login, after login, during rename/restart/transfer, after AUTH/CCC, after REIN, and during QUIT.
- [DONE] T138: Add bounded line-reader and Telnet tests for every IAC form, fragmented CRLF, coalesced lines, urgent ABOR/Synch, TLS bypass, limits, EOF, and invalid encodings.
- [DONE] T139: Add property/fuzz-style parser tests for commands, replies, PASV/EPSV/EPRT/PORT endpoints, timestamps, facts, and listings using deterministic generated input.
- [DONE] T140: Add representation tests for TYPE A/E/I/L, FORMAT N/T/C, all boundary splits, invalid sequences, unmappable text, and non-eight-bit packing round trips.
- [DONE] T141: Add structure/mode tests for STRU F/R/P and MODE S/B/C, EOF/EOR/page/block headers, compression forms, restart markers, and malformed streams.
- [DONE] T142: Add MODE Z tests for negotiation, Deflate round trips, corruption, cancellation, TLS composition, and disabled-extension rejection.
- [DONE] T143: Add transfer-pump tests for partial reads/writes, cancellation, deadlines, progress, logical/wire counts, observer failures, flush/close order, and digest calculation.
- [DONE] T144: Add peer-policy tests for IPv4, IPv6, mapped addresses, scopes, NAT/private/public mismatches, blocked ports, anti-bounce, anti-port-theft, FXP opt-in, and PASV address trust modes.
- [DONE] T145: Add TLS unit tests with generated certificate chains for hostname/SNI, trust, revocation policy, client certificates, provider/validator results, protocol restrictions, and failure redaction.

### Client behavior test coverage

- [DONE] T146: Add scripted-control-server tests for every IFtpClient connection, greeting, HOST, authentication, FEAT/OPTS, explicit/implicit TLS, CCC, REIN, reconnect, raw command, and disconnect operation.
- [DONE] T147: Add client tests for every active/passive mode, EPSV/PASV and EPRT/PORT fallback, EPSV ALL, IPv4/IPv6, NAT replies, timeouts, peer mismatch, and setup failure.
- [DONE] T148: Add client tests for every metadata, existence, working-directory, create/delete, rename/move, permission, HELP, STAT, SYST, and NOOP operation and fallback.
- [DONE] T149: Add client tests for MLST/MLSD/LIST/NLST/STAT listing selection, feature lies, parser selection, raw fallbacks, hidden entries, symlinks, cycles, and Unicode names.
- [DONE] T150: Add client tests for RETR/STOR/APPE/STOU stream and file operations, every collision policy, unique names, parent creation, preliminary/final replies, and partial cleanup.
- [DONE] T151: Add client restart tests for download/upload REST STREAM, SIZE/MDTM/unique validation, unsafe resume rejection, marker restart, zero/large offsets, and non-seekable streams.
- [DONE] T152: Add client verification tests for SIZE/time policies, HASH and all compatibility algorithms, mismatch cleanup/retry policy, and unsupported-algorithm results.
- [DONE] T153: Add client atomic-upload tests for temporary naming, successful rename commit, failed verification, failed rename, disconnect, and each cleanup policy.
- [DONE] T154: Add client recursive download/upload tests for filters, empty directories, symlinks, cycles, collisions, timestamps, cancellation, verification, and continue/stop failure policy.
- [DONE] T155: Add client directory-plan/synchronization tests for no-op/update/mirror modes, dry run, delete-extraneous root guards, deterministic ordering, and per-item results.
- [DONE] T156: Add client FXP tests for passive/active pairings, paired replies, abort/failure on either server, state restoration, anti-bounce policy, and protected-data rejection.
- [DONE] T157: Add client fault tests for malformed replies, premature control/data closure, 421, TLS handshake faults, provider/observer faults, cancellation races, and no replay of non-idempotent commands.

### Server behavior test coverage

- [DONE] T158: Add contract tests for every IFtpServer lifecycle/state operation across start, multi-listener bind, running, graceful drain, forced stop, repeated calls, and isolated session failure.
- [DONE] T159: Add contract tests for every IFtpFileSystem operation and every LocalFtpFileSystem path on Windows-style and Unix-style names, root containment, symlinks, races, quotas, unique names, and failures.
- [DONE] T160: Add contract tests for all authenticator, authorizer, host resolver, certificate provider/validator, extension handler, filter, observer, and event-sink operations and result statuses.
- [DONE] T161: Add server command-matrix tests for every RFC 959 mandatory and optional command, exact success/failure/precondition replies, multiline allowance, aliases, and state changes.
- [DONE] T162: Add server command-matrix tests for FEAT, OPTS, EPRT, EPSV, LANG, MDTM, SIZE, MLST, MLSD, REST STREAM, TVFS, AUTH, PBSZ, PROT, CCC, and HOST.
- [DONE] T163: Add server data tests for every active/passive IPv4/IPv6 setup, NAT/passive port pool, peer validation, timeout, ABOR timing, half-close, TLS role, and final-reply sequence.
- [DONE] T164: Add server transfer tests for every representation/format/structure/mode combination, upload/download/append/unique paths, restart offsets/markers, allocation, limits, and disk/provider failures.
- [DONE] T165: Add server listing tests for LIST/NLST/STAT/MLST/MLSD generation, selected facts, permissions, timestamps, Unicode, symlinks, bounded results, and authorization filtering.
- [DONE] T166: Add server authentication/security tests for anonymous and fixed users, ACCT, attempts/delay, uniform errors, TLS client identity, HOST isolation, permissions, CCC policy, and secret redaction.
- [DONE] T167: Add server compatibility tests for X aliases, PRET, CLNT, MFMT, checksum commands, MODE Z, SITE variants, RMDA, disabled states, and extension-handler precedence.
- [DONE] T168: Add server abuse tests for command injection, traversal, Unicode ambiguity, overlong/malformed input, command floods, passive-listener exhaustion, bounce attempts, port theft, quota exhaustion, and recursive-delete root guards.

### End-to-end and interoperability coverage

- [DONE] T169: Add in-process client/server tests for plain FTP, explicit FTPS, implicit FTPS, PROT C/P, CCC, IPv4, IPv6, active, passive, every listing path, and representative transfers.
- [DONE] T170: Add full round-trip end-to-end tests for zero-byte, text, binary, large-offset sparse, record, page, compressed, resumed, atomic, recursive, verified, cancelled, and failed transfers.
- [DONE] T171: Add concurrent server integration tests for multiple listeners, users, virtual hosts, transfers, limits, graceful drain, forced close, and fault isolation without timing-dependent assertions.
- [DONE] T172: Add external-client interoperability tests against the library server using curl/libcurl behavior, Python ftplib/FTP_TLS, FluentFTP, and Apache Commons Net command sequences.
- [DONE] T173: Add external-server interoperability tests for the library client against pyftpdlib, Apache FtpServer or equivalent containers, and representative strict/quirky FTP and FTPS servers.
- [DONE] T174: Add RFC transcript tests derived from RFC 959, RFC 2389, RFC 2428, RFC 2640, RFC 3659, RFC 4217, and RFC 7151 examples plus all applicable verified errata.
- [DONE] T175: Add a public-API coverage test inventory that invokes and behaviorally asserts every public constructor, interface method, implementation method, data field category, enum member, static field, and exception path under src/Api.
- [DONE] T176: Run the complete test suite on supported .NET 10 platforms with IPv4/IPv6 and TLS capability detection, and resolve every functional or interoperability failure.
