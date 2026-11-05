# Universal TFTP Client/Server Development Plan

## Goal and scope

Build a portable .NET 10 library that provides low-level packet access and high-level client/server transfers for Trivial File Transfer Protocol (TFTP). The normative protocol scope is RFC 1350, RFC 2347, RFC 2348, and RFC 2349, with the applicable operational corrections from RFC 1123 and verified RFC 1350 errata. Production code under `src/` will use only built-in .NET APIs and will follow `syntax.md`.

The finished library will:

- Act as both a client and a concurrent server for RRQ downloads and WRQ uploads.
- Interoperate with classic RFC 1350 peers that do not understand options and modern peers that negotiate `blksize`, `timeout`, and `tsize`.
- Expose packet encoding/decoding, transfer configuration, stream and file operations, server storage integration, progress events, results, and lifecycle control.
- Support host names, IPv4, and IPv6 while retaining RFC 1350 transfer-ID (UDP port) behavior.
- Use bounded memory, strict packet validation, cancellation, retry limits, access control, and safe directory-backed storage.
- Keep expected failures in result objects where they are frequent, and use exceptions for failed operations as required by `syntax.md`.

### Deliberate exclusions

- RFC 7440 `windowsize`, RFC 2090 multicast TFTP, and nonstandard `blksize2`, `utimeout`, and `rollover` options are outside the requested RFC baseline. The option codec and negotiation internals will remain extensible, but these options will not be advertised or accepted.
- RFC 1350 `mail` mode will be recognized on the wire but rejected, following RFC 1123's recommendation that it not be supported. `octet` and `netascii` are supported completely.
- Authentication, encryption, remote rename/delete, directory listing, resume, and an executable/daemon/CLI are not TFTP features in the selected RFCs and are out of scope.
- The library will not implement tftp-hpa's process account/chroot controls or filename-remapping language. It will provide equivalent library-level root confinement and storage policy controls.
- There are no documentation/release tasks, syntax-policy test tasks, or performance/benchmark tasks in this plan.

## Research baseline

### Standards findings

- RFC 1350 defines opcodes RRQ (1), WRQ (2), DATA (3), ACK (4), and ERROR (5); network-byte-order integer fields; zero-terminated request/error strings; 512-byte default data blocks; block numbers beginning at 1; ACK 0 for a basic WRQ; server-selected transfer ports; and ERROR 5 for an unknown transfer ID.
- Transfers are lockstep. Only the last outbound packet needs to be retained. A short DATA block ends a transfer, including an empty DATA block when content length is an exact multiple of the active block size.
- The receiver must acknowledge the final DATA block. Final-ACK dallying is needed so a repeated final DATA packet receives another ACK.
- ERROR packets are neither acknowledged nor retransmitted. Errors 0 through 7 cover undefined, file not found, access violation, storage exhausted, illegal operation, unknown TID, file exists, and no such user.
- RFC 2347 adds option/value pairs to RRQ and WRQ and opcode 6 OACK. Option names are case-insensitive, each option may appear only once, request packets are at most 512 octets, unrecognized options are omitted rather than rejected, and an option-unaware server can fall back to DATA 1 or ACK 0.
- An RRQ OACK is confirmed by ACK 0; a WRQ OACK is confirmed by DATA 1. An unrequested, malformed, duplicate, or unacceptable OACK option causes ERROR 8 and termination.
- RFC 2348 defines `blksize` values from 8 through 65464 data octets. A server may lower the requested value but may not raise it. End-of-transfer detection uses the negotiated size.
- RFC 2349 defines `timeout` from 1 through 255 seconds and requires the server to echo the exact requested value. It also defines nonnegative decimal `tsize`: RRQ sends zero and receives the transfer size, while WRQ sends the size and receives the same value.
- RFC 1123 requires the Sorcerer's Apprentice fix, adaptive retransmission timeout behavior with at least exponential backoff, configurable server pathname access control, and silent rejection of broadcast requests.
- Verified RFC 1350 errata correct the textual datagram-length boundary for terminal DATA and clarify packet diagrams/text. Tests will follow the corrected interpretation.

### Existing implementation findings

- Current tftp-hpa provides get/put, octet/netascii, IPv4/IPv6 selection, configurable local/transfer ports, per-packet and total timeout controls, trace output, safe server roots, read/write/create policy, option refusal, and `blksize`, `timeout`, and `tsize`. It also implements RFC 7440 and nonstandard options that are deliberately excluded above.
- tftp-hpa highlights practical interoperability limits: large blocks can cause IP fragmentation, some embedded clients fail when options are accepted, and `tsize` is commonly limited to octet mode when the converted netascii size is not known.
- TFTPy provides client/server objects, file and stream-like transfers, retry limits, packet hooks, per-session state objects, concurrent server sessions, dynamic read/upload providers, graceful/immediate stop, transfer metrics, option fallback, duplicate handling, rollover fixes, and `blksize`/`tsize`. Its current documented limitation to octet mode reinforces the need for dedicated netascii coverage here.
- Both mature implementations retain the last packet for retransmission, isolate a server session by peer address/port, account for duplicate packets, and treat exact block boundaries and block-number rollover as explicit edge cases.

### Downloaded references

The following upstream material is already stored in `specs/`:

- `rfc768.txt` — UDP.
- `rfc1123.txt` — host requirements, especially sections 4.2.2 through 4.2.4.
- `rfc1350.txt` and `rfc1350-inline-errata.html` — base TFTP protocol and verified errata.
- `rfc2347.txt`, `rfc2347-inline-errata.html`, `rfc2348.txt`, and `rfc2349.txt` — option negotiation and its verified example correction, block size, timeout, and transfer size.
- `rfc7440.txt` — comparison reference for the intentionally excluded window-size extension.
- `tftp-hpa-client-manpage.1`, `tftp-hpa-server-manpage.8`, and `tftp-hpa-CHANGES` — current reference implementation behavior and history.
- `tftpy-README.md` and `tftpy-api-documentation.html` — client/server API, state-machine, metrics, option, and limitation reference.

## Complete feature inventory

### Wire protocol and packet codec

- Encode and decode 16-bit unsigned opcodes, block numbers, and error codes in network byte order.
- Encode and decode RRQ/WRQ filename, mode, and zero or more zero-terminated option/value pairs.
- Encode and decode DATA payloads from zero bytes through the active block-size limit.
- Encode and decode fixed-size ACK packets.
- Encode and decode ERROR codes plus zero-terminated netascii messages, including unknown/private error codes as error 0 semantics.
- Encode and decode OACK option/value pairs without imposing recognized-option policy in the codec.
- Preserve option order for diagnostics while matching standardized option names case-insensitively.
- Reject truncated headers, missing terminators, empty required strings, odd option/value lists, duplicate options, embedded NULs supplied for encoding, invalid ASCII, fixed-packet trailing bytes, invalid opcodes, oversized requests, and DATA larger than the configured limit.
- Provide throwing decode and exception-less `TryDecode` paths; use `ReadOnlySpan<byte>` and `Span<byte>` at codec boundaries to avoid unnecessary packet copies.

### Transfer modes and content framing

- Octet mode transfers bytes without transformation and guarantees round-trip identity.
- Netascii sending maps local LF to CR-LF and local CR to CR-NUL.
- Netascii receiving maps CR-LF to local LF and CR-NUL to local CR, preserving conversion state across DATA boundaries.
- Correctly handle CR as the last byte of a block, the paired byte in the next block, and invalid/dangling netascii sequences.
- Recognize mode names case-insensitively; reject unsupported mode names and `mail` with ERROR 4.
- Send an empty final DATA block for zero-length files and content whose encoded length is an exact multiple of the active block size.

### Transfer identity and UDP transport

- Send initial client requests to the configured server port, default 69, from an ephemeral or configured local address.
- Lock the client to the address and server-selected transfer port of the first valid response.
- Give every server transfer its own UDP socket/port and keep the listener available for new requests.
- Validate both peer address and port for every established session.
- Reply to a datagram from an unexpected port with ERROR 5 without disturbing the valid session.
- Ignore unrelated addresses, silently ignore broadcast/multicast requests at the listener, and bound received datagram sizes.
- Resolve host names and support IPv4, IPv6, explicit address-family preference, wildcard server bindings, and port zero for test/dynamic binding.

### Reliability, sequencing, and termination

- Use lockstep DATA/ACK flow control with one outstanding block.
- Start data block numbering at 1, use ACK 0 only for WRQ/OACK handshakes, and wrap modulo 65536 after block 65535.
- Compare expected, previous, and unexpected block values with rollover-safe logic rather than ordinary numeric ordering.
- Write each DATA block at most once; re-ACK duplicate DATA without appending it again.
- Ignore duplicate ACKs without immediately retransmitting DATA, preventing Sorcerer's Apprentice packet multiplication.
- Treat packets that cannot be explained by delay/duplication as illegal operations and terminate appropriately.
- Retain exactly the last request, OACK, DATA, or ACK needed for timeout retransmission.
- Retransmit on timeout with adaptive exponential backoff, configured initial/capped intervals, retry and total-duration limits, and reset only when valid forward progress occurs.
- Apply an RFC 2349 negotiated timeout as the initial retransmission interval while preserving bounded backoff and overall limits.
- Support cancellation during name resolution, socket waits, stream operations between blocks, dallying, and server shutdown.
- Dally after final ACK for a bounded interval, re-ACK repeated final DATA, then complete cleanly.
- Surface remote ERROR immediately; do not ACK or retransmit ERROR.

### Option negotiation

- Permit a client to request any combination of `blksize`, `timeout`, and `tsize` once each, subject to the 512-octet RRQ/WRQ limit.
- Allow the server to enable/disable each standardized option independently and omit unsupported/disabled options.
- Fall back to default block size and local timeout when an option-aware request receives DATA 1 or ACK 0 from an option-unaware server.
- Optionally retry a request once without options when a legacy server rejects a request containing options.
- Validate an OACK as a subset of the request and reject unrequested, duplicate, malformed, empty, or unacceptable values with ERROR 8.
- Negotiate `blksize` over 8..65464, allowing a server-selected value no larger than the request and applying a configurable server maximum.
- Negotiate `timeout` over 1..255 seconds with an exact echo.
- Negotiate `tsize` as a nonnegative 64-bit count, reject overflow/non-decimal values, use zero in RRQ, and require an exact echo in WRQ.
- Report which options were accepted and their effective values in progress events and transfer results.
- For netascii, advertise `tsize` only when the exact encoded/on-wire octet count is known; safely omit it otherwise.

### Client behavior

- Download to a caller-owned writable stream and upload from a caller-owned readable stream without closing caller-owned streams.
- Provide file-path convenience operations with deterministic ownership/disposal and safe replacement behavior for downloads.
- Execute the complete RRQ state machine with no options, partial/full OACK, option fallback, DATA 1 first response, ERROR, retry, cancellation, and final dallying.
- Execute the complete WRQ state machine with no options, partial/full OACK, option fallback, ACK 0 first response, DATA/ACK loop, ERROR, retry, and cancellation.
- Derive upload `tsize` from a seekable stream when requested, accept an explicitly supplied size for non-seekable streams, and reject contradictory size information before network I/O.
- Validate request filenames, endpoint, stream capabilities, mode, option ranges, and time limits before sending.
- Return byte counts, encoded byte counts where different, negotiated values, elapsed time, retransmission/duplicate counts, final endpoint, and completion state.

### Server behavior

- Bind one or more IPv4/IPv6 endpoints and accept RRQ/WRQ concurrently up to a configured session limit.
- Key active transfers by client address/port and request identity; route a duplicate initial request to the existing session and resend its last response rather than opening a second transfer.
- Reject malformed/unexpected initial packets with ERROR 4 when safe, silently discard broadcast/multicast requests, and bound request/error text.
- RRQ: authorize/open content, determine known transfer size, negotiate options, send OACK or DATA 1, send all blocks, process ACKs, retransmit, and complete after final ACK.
- WRQ: authorize/open a pending destination, enforce announced/runtime size limits, negotiate options, send OACK or ACK 0, receive blocks, ACK and dally, then commit atomically.
- Translate expected storage outcomes to the appropriate ERROR code and message; isolate unexpected session faults from the listener and expose them through transfer events.
- Enforce configurable read/write/create/overwrite policy, maximum filename/request/transfer sizes, maximum sessions, retry bounds, and idle/total session limits.
- Expose current running state, bound endpoints, accepted/completed/failed transfer counts, and active transfer count.
- Stop gracefully by rejecting new work and draining active transfers, or stop immediately by cancelling all sessions.

### Storage safety and observability

- Define a storage contract that can serve generated streams as well as filesystem content and can return expected failures without relying on exception control flow.
- Provide directory-backed storage with canonical root confinement, rejection of absolute paths and `..`, handling of both slash styles, and symlink/reparse-point escape prevention.
- Support separate read, write, create, and overwrite permissions.
- Write uploads to unique temporary files in the destination directory and atomically publish only after the final DATA is acknowledged; remove/abandon pending data on failure or cancellation.
- Never expose partial downloads when using the client file convenience API; replace the destination only after a successful transfer.
- Emit optional observer events for lifecycle/state changes, negotiated options, block progress, retransmission, duplicate, remote/local error, and completion without exposing mutable internal state.
- Keep observer absence cheap and define observer exceptions as caller faults that terminate the calling operation.

## State-machine plan

### Shared sender (client WRQ and server RRQ)

| State | Accepted input | Action and next state |
| --- | --- | --- |
| Prepare | Local stream/storage result | Fill DATA 1 using active mode/block size; send; wait for ACK. |
| WaitAck | ACK for current block | If final, complete; otherwise advance modulo 65536, fill/send next DATA, and remain in WaitAck. |
| WaitAck | Duplicate ACK for previous block | Record and ignore; do not cause an immediate resend. |
| WaitAck | Timeout | Retransmit retained DATA, back off timer, or fail when limits are exhausted. |
| WaitAck | ERROR | Fail with the remote code/message. |
| WaitAck | Other valid-peer packet | Send ERROR 4 and fail unless it is an explained duplicate. |

### Shared receiver (client RRQ and server WRQ)

| State | Accepted input | Action and next state |
| --- | --- | --- |
| WaitData | DATA for expected block | Decode/write exactly once, send ACK, and advance; short DATA enters Dally. |
| WaitData | Duplicate DATA for previous block | Record and re-ACK without writing; remain in WaitData. |
| WaitData | Timeout | Retransmit retained ACK/request/OACK, back off timer, or fail at the configured limit. |
| WaitData | ERROR | Fail with the remote code/message. |
| Dally | Repeated final DATA | Re-send final ACK and restart only the bounded dally interval. |
| Dally | Dally expiry | Complete and commit output. |

### Client RRQ initialization

`Created -> Send RRQ -> WaitInitial -> (OACK -> validate -> ACK 0 -> WaitData) | (DATA 1 -> defaults -> receive) | (ERROR -> Failed)`. A timeout retransmits RRQ; an option-rejection ERROR may trigger the configured one-time optionless retry.

### Client WRQ initialization

`Created -> Send WRQ -> WaitInitial -> (OACK -> validate -> Send DATA 1) | (ACK 0 -> defaults -> Send DATA 1) | (ERROR -> Failed)`. A timeout retransmits WRQ; an option-rejection ERROR may trigger the configured one-time optionless retry.

### Server listener and session initialization

`Stopped -> Binding -> Listening -> Draining -> Stopped`, with an immediate-stop transition from Binding, Listening, or Draining to Stopped. While listening, a valid unicast RRQ/WRQ creates or reuses a peer session. RRQ sessions transition through authorization, option negotiation, and the sender machine; WRQ sessions transition through authorization, pending-write creation, option negotiation, and the receiver machine.

## Proposed public API

The API descriptions intentionally omit fine signature details. Every public type is in `src/Api`, every public member has the required XML comments as part of its implementation task, public implementation classes expose one constructor plus interface-declared methods only, and no public type has public static methods.

### `src/Api/Static`

- `TftpProtocolValues`: const/readonly protocol values such as port 69, header/request limits, default/minimum/maximum block sizes, timeout bounds, and canonical mode/option names. It contains no methods.

### `src/Api/Enums`

- `TftpOpcode`: RRQ, WRQ, DATA, ACK, ERROR, OACK.
- `TftpErrorCode`: codes 0 through 8, including option-negotiation failure.
- `TftpTransferMode`: Octet and Netascii. Mail is parsed but is not an offered API mode.
- `TftpTransferDirection`: Read and Write.
- `TftpTransferState`: Created, Resolving, Requesting, Negotiating, Sending, Receiving, Dallying, Completed, Failed, Cancelled.
- `TftpServerStopMode`: Graceful and Immediate.

### `src/Api/Data`

- `TftpOption`: option name and textual value.
- `TftpPacket`: opcode plus the applicable filename, mode, block number, payload, error, and option fields for low-level codec use.
- `TftpPacketDecodeResult`: success flag, decoded packet, consumed length, and validation failure detail for exception-less decoding.
- `TftpEndpoint`: host, port, optional local IP address, and IPv4/IPv6 preference.
- `TftpTransferOptions`: mode, requested RFC options, legacy fallback, retransmission/backoff limits, dally duration, and maximum accepted transfer size.
- `TftpReadRequest`: endpoint, remote filename, and transfer options.
- `TftpWriteRequest`: endpoint, remote filename, transfer options, and optional known source size represented by a separate presence flag and signed 64-bit value.
- `TftpTransferEvent`: immutable-by-convention snapshot fields for direction, state, peer, block, byte counts, retry counts, options, error, and message.
- `TftpTransferResult`: completed transfer metrics and effective negotiated settings.
- `TftpServerBinding`: listen address and port.
- `TftpServerOptions`: bindings, option policy/caps, concurrency/size limits, timeout/retry policy, and stop behavior.
- `TftpServerRequest`: per-request filename, direction, mode, client endpoint, requested/effective options, and announced size.
- `TftpStorageOpenResult`: success/error fields, stream, ownership, known-size metadata, and storage token needed for completion.
- `TftpStorageCommitResult`: success/error fields for commit or abort operations.
- `TftpDirectoryStorageOptions`: root path plus read/write/create/overwrite and path-safety settings.
- `TftpServerStatus`: lifecycle state, bound endpoints, active count, and cumulative transfer counts.

### `src/Api/Contracts`

- `ITftpPacketCodec`: get encoded length, encode to `Span<byte>`, decode from `ReadOnlySpan<byte>`, and exception-less decode into a result holder. Method names are unique rather than overloaded.
- `ITftpClient`: stream download, stream upload, file download, and file upload methods accepting request objects and `CancellationToken`.
- `ITftpServer`: blocking run, stop, and status methods. Applications can run the blocking method on their own thread/task.
- `ITftpServerStorage`: open read, open pending write, commit write, and abort write operations using result objects for expected failures.
- `ITftpTransferObserver`: one `Observe` method receiving a `TftpTransferEvent` snapshot.

### `src/Api/Exceptions`

Each exception derives directly from `System.Exception` to comply with `syntax.md`.

- `TftpProtocolException`: malformed local or peer protocol data.
- `TftpTimeoutException`: retry or total-time limit exhaustion.
- `TftpRemoteErrorException`: remote ERROR packet, with public error-code and remote-message fields.
- `TftpOptionNegotiationException`: rejected or invalid option negotiation.
- `TftpStorageException`: exceptional local storage failure not represented as an expected storage result.

### `src/Api/Classes`

- `TftpPacketCodec : ITftpPacketCodec`: public low-level codec implementation.
- `TftpClient : ITftpClient`: public client implementation for stream/file RRQ and WRQ.
- `TftpServer : ITftpServer`: public concurrent server implementation.
- `TftpDirectoryStorage : ITftpServerStorage`: safe directory-backed server storage implementation.

## Planned source files

```text
src/
  Api/
    Static/TftpProtocolValues.cs
    Enums/TftpOpcode.cs
    Enums/TftpErrorCode.cs
    Enums/TftpTransferMode.cs
    Enums/TftpTransferDirection.cs
    Enums/TftpTransferState.cs
    Enums/TftpServerStopMode.cs
    Data/TftpOption.cs
    Data/TftpPacket.cs
    Data/TftpPacketDecodeResult.cs
    Data/TftpEndpoint.cs
    Data/TftpTransferOptions.cs
    Data/TftpReadRequest.cs
    Data/TftpWriteRequest.cs
    Data/TftpTransferEvent.cs
    Data/TftpTransferResult.cs
    Data/TftpServerBinding.cs
    Data/TftpServerOptions.cs
    Data/TftpServerRequest.cs
    Data/TftpStorageOpenResult.cs
    Data/TftpStorageCommitResult.cs
    Data/TftpDirectoryStorageOptions.cs
    Data/TftpServerStatus.cs
    Contracts/ITftpPacketCodec.cs
    Contracts/ITftpClient.cs
    Contracts/ITftpServer.cs
    Contracts/ITftpServerStorage.cs
    Contracts/ITftpTransferObserver.cs
    Exceptions/TftpProtocolException.cs
    Exceptions/TftpTimeoutException.cs
    Exceptions/TftpRemoteErrorException.cs
    Exceptions/TftpOptionNegotiationException.cs
    Exceptions/TftpStorageException.cs
    Classes/TftpPacketCodec.cs
    Classes/TftpClient.cs
    Classes/TftpServer.cs
    Classes/TftpDirectoryStorage.cs
  Internal/
    Protocol/BigEndianCodec.cs
    Protocol/ZeroTerminatedAscii.cs
    Protocol/TftpPacketValidator.cs
    Protocol/TftpOptionNegotiator.cs
    Protocol/NetasciiEncoder.cs
    Protocol/NetasciiDecoder.cs
    Protocol/TftpBlockCounter.cs
    Transport/TftpEndpointResolver.cs
    Transport/UdpDatagramSocket.cs
    Transport/TftpRetransmissionTimer.cs
    State/TftpSessionContext.cs
    State/TftpSessionOutcome.cs
    State/TftpSenderStateMachine.cs
    State/TftpReceiverStateMachine.cs
    State/TftpClientReadStateMachine.cs
    State/TftpClientWriteStateMachine.cs
    State/TftpServerReadStateMachine.cs
    State/TftpServerWriteStateMachine.cs
    Server/TftpServerSession.cs
    Server/TftpSessionManager.cs
    Storage/TftpPathValidator.cs
    Storage/TftpPendingWrite.cs
```

Internal types remain non-public and outside `src/Api`. Implementations will use ordinary classes rather than structs/records, file-scoped namespaces, fields instead of properties, positional calls, block-bodied methods, no overloads, no LINQ, no `try`/`catch`/`finally`, and result-style paths for expected parsing/storage/network conditions. Socket waiting will use cancellation-aware polling so timeouts are ordinary state-machine events rather than exception-driven control flow.

## Planned test files and coverage approach

```text
tests/
  Api/ProtocolValuesTests.cs
  Api/PublicDataModelTests.cs
  Api/PublicExceptionTests.cs
  Protocol/PacketCodecRequestTests.cs
  Protocol/PacketCodecDataAckErrorTests.cs
  Protocol/PacketCodecOptionTests.cs
  Protocol/PacketCodecMalformedTests.cs
  Protocol/OptionNegotiationTests.cs
  Protocol/NetasciiTests.cs
  Protocol/BlockNumberTests.cs
  Transport/EndpointAndTidTests.cs
  Transport/RetransmissionTests.cs
  Client/ClientReadTests.cs
  Client/ClientWriteTests.cs
  Client/ClientFileTests.cs
  Server/DirectoryStorageTests.cs
  Server/ServerReadTests.cs
  Server/ServerWriteTests.cs
  Server/ServerLifecycleTests.cs
  Integration/ClientServerMatrixTests.cs
  Integration/AdverseNetworkTests.cs
  Integration/InteroperabilityTests.cs
  Support/ScriptedTftpPeer.cs
  Support/DatagramFaultProxy.cs
  Support/ManualTimeSource.cs
  Support/MemoryServerStorage.cs
  Support/RecordingTransferObserver.cs
  Support/TestDirectory.cs
```

Tests will cover every public constructor, field contract, interface method, implementation method, enum/static value, success path, expected failure family, cancellation point, and relevant boundary. The behavioral matrix includes both directions; octet/netascii; stream/file/storage APIs; IPv4/IPv6 where available; no/partial/full options; empty, short, exact-block, multi-block, and rollover content; all ERROR codes; packet loss/duplication/delay/reordering/wrong TID; legacy fallback; and graceful/immediate server stop. Coverage collection will require complete line and branch coverage of `src/Api` and direct behavior assertions rather than syntax/source-policy tests.

## Implementation tasks

- [DONE] T001: Add `TftpProtocolValues` with the RFC port, packet/header limits, block-size bounds, timeout bounds, canonical modes, and canonical option names.
- [DONE] T002: Add and XML-comment all public protocol, transfer, and server enums with explicit default integer values matching their wire or lifecycle meanings.
- [DONE] T003: Add `TftpOption`, `TftpPacket`, and `TftpPacketDecodeResult` data classes with field defaults that can represent every RRQ, WRQ, DATA, ACK, ERROR, and OACK packet without inheritance or properties.
- [DONE] T004: Add endpoint, transfer-option, read-request, and write-request data classes, including explicit presence flags wherever nullable value types would otherwise be needed.
- [DONE] T005: Add transfer-event/result and server binding/options/request/status data classes, ensuring every data class is consumed by an interface or another data class.
- [DONE] T006: Add storage configuration/open/commit result data classes for expected storage outcomes, stream ownership, known sizes, and atomic-write tokens.
- [DONE] T007: Add the five public contracts with unique non-overloaded instance method names and Span-based low-level codec boundaries.
- [DONE] T008: Add the five directly `System.Exception`-derived public exception classes with their single constructors and error-detail fields.
- [DONE] T009: Add `BigEndianCodec` helpers for bounded 16-bit network-order reads/writes and `ZeroTerminatedAscii` helpers for strict bounded ASCII field processing.
- [DONE] T010: Implement request packet length calculation and RRQ/WRQ encoding, including filename/mode validation, zero terminators, ordered options, duplicate-name rejection, and the 512-octet cap.
- [DONE] T011: Implement RRQ/WRQ decoding with case-insensitive mode/option handling and rejection of missing/empty/odd/duplicate/invalid fields.
- [DONE] T012: Implement DATA and ACK encoded-length calculation, Span encoding, decoding, fixed-header validation, block numbers, and configured DATA-size enforcement.
- [DONE] T013: Implement ERROR encoding/decoding for standard and unknown codes, bounded netascii messages, required terminators, and rejection of malformed trailing data.
- [DONE] T014: Implement OACK encoding/decoding with ordered pairs, strict terminators, unique case-insensitive names, and no option-policy decisions in the codec layer.
- [DONE] T015: Complete `TftpPacketCodec` throwing and exception-less decode paths, opcode dispatch, destination-capacity checks, consumed-length reporting, and one public constructor.
- [DONE] T016: Test every public constant, enum value, packet/data default, packet type round trip, minimum buffer, maximum legal packet, and Span destination-too-small behavior.
- [DONE] T017: Add malformed-packet tests for every truncation boundary, unknown opcode, missing terminator, embedded/invalid ASCII, empty string, duplicate/odd options, illegal fixed-packet length, oversized request, and oversized DATA.
- [DONE] T018: Implement generic RFC 2347 option request validation, case-insensitive matching, subset OACK validation, server omission of unknown/disabled options, request-size accounting, and ERROR 8 outcomes.
- [DONE] T019: Implement RFC 2348 `blksize` client/server negotiation for 8..65464, configurable server reduction, client rejection of increases/out-of-range values, and default-512 fallback.
- [DONE] T020: Implement RFC 2349 `timeout` negotiation for exact 1..255-second echo and integration with the active retransmission interval.
- [DONE] T021: Implement RFC 2349 `tsize` negotiation using nonnegative signed 64-bit values, RRQ zero/response semantics, WRQ exact echo, capacity checks, and omission when encoded size is unknown.
- [DONE] T022: Test independent and combined option negotiation, option order/case, partial OACK, unknown/disabled options, legacy fallback, retry-without-options, all numeric boundaries/overflow, and every ERROR 8 path.
- [DONE] T023: Implement streaming netascii encoding with CR/LF expansion and persistent pending-output state so transformations can split safely at any DATA boundary.
- [DONE] T024: Implement streaming netascii decoding with cross-block CR state, CR-LF/CR-NUL handling, final flush validation, and no transformation in octet mode.
- [DONE] T025: Test octet identity and exhaustive netascii vectors, including empty input, all byte values, CR/LF runs, block-edge splits, one-byte block sizes in internal tests, dangling CR, invalid CR pairs, and exact encoded block multiples.
- [DONE] T026: Implement `TftpBlockCounter` with block 1 start, ACK 0 handshake distinction, modulo-65536 advance, expected/previous classification, and rollover-safe duplicate handling.
- [DONE] T027: Implement cancellation-aware endpoint resolution and UDP socket binding for hostnames, IPv4, IPv6, wildcard/dynamic ports, and explicit local-address/address-family selection.
- [DONE] T028: Implement transfer-ID locking, peer address/port validation, ERROR 5 replies from the receiving transfer socket, listener broadcast/multicast suppression, and bounded datagram reception.
- [DONE] T029: Implement the retransmission timer with initial/capped interval, exponential backoff, retry/total limits, valid-progress reset, negotiated timeout input, and deterministic internal time abstraction.
- [DONE] T030: Implement the shared sender state machine for block filling, last-packet retention, ACK validation, duplicate ACK suppression, exact-multiple empty final DATA, retransmission, rollover, ERROR, and cancellation.
- [DONE] T031: Implement the shared receiver state machine for expected/duplicate DATA, at-most-once writes, ACK retention/retransmission, final-block detection, bounded dally/re-ACK, netascii finalization, ERROR, and cancellation.
- [DONE] T032: Test shared sender/receiver transitions with a manual time source for ordinary progress, every size boundary, loss of each packet kind, duplicate storms, delayed packets, unexplained future blocks, adaptive backoff, exhausted limits, final-ACK loss, and rollover.
- [DONE] T033: Implement client RRQ initialization and transitions for RRQ retransmission, server TID adoption, OACK/ACK 0, direct DATA 1 fallback, option rejection retry, remote ERROR, and entry into the receiver machine.
- [DONE] T034: Implement client WRQ initialization and transitions for WRQ retransmission, server TID adoption, OACK/direct ACK 0 fallback, source/tsize validation, remote ERROR, and entry into the sender machine.
- [DONE] T035: Implement `TftpClient` stream operations with input validation, caller-owned stream lifetime, cancellation, observer snapshots, result metrics, and direct exception mapping.
- [DONE] T036: Implement `TftpClient` file operations using same-directory temporary downloads, atomic successful publication, safe upload opening, cleanup through `using`, and no partial destination exposure.
- [DONE] T037: Test client RRQ against scripted peers for optionless/OACK/fallback flows, octet/netascii, stream capability failures, all content boundaries, retransmission, duplicate/out-of-order DATA, wrong TID, ERROR codes, cancellation, metrics, and observer events.
- [DONE] T038: Test client WRQ against scripted peers for optionless/OACK/fallback flows, seekable/non-seekable `tsize`, octet/netascii, exact final blocks, retransmission, duplicate/out-of-order ACK, wrong TID, ERROR codes, cancellation, metrics, and observer events.
- [DONE] T039: Test client file operations for success, existing destination replacement, transfer failure/cancellation preservation, temporary-file cleanup, inaccessible paths, and caller-visible exceptions.
- [DONE] T040: Implement result-style custom storage flow for read open, pending-write open, commit, and abort, including exact size metadata and protocol error selection.
- [DONE] T041: Implement canonical path validation that rejects NUL, empty/absolute/device paths, drive roots, both-separator traversal, root-prefix confusion, and symlink/reparse-point escape.
- [DONE] T042: Implement `TftpDirectoryStorage` reads with root confinement, policy enforcement, known octet size, computed/omitted netascii size, stream ownership, and file-not-found/access outcomes.
- [DONE] T043: Implement `TftpDirectoryStorage` writes with read/write/create/overwrite policy, same-directory unique temporary files, size bounds, atomic commit, abort cleanup, and existing-target race handling within the allowed syntax.
- [DONE] T044: Test every public directory-storage option and contract method across normal reads/writes, generated error results, traversal variants, sibling-prefix paths, symlinks/reparse points where supported, create/overwrite combinations, atomicity, abort, and size metadata.
- [DONE] T045: Implement the server listener lifecycle with multiple bindings, status snapshots, cancellation-aware polling, duplicate request detection, session limits, and graceful/immediate stop transitions.
- [DONE] T046: Implement isolated per-transfer server sockets, session registry/cleanup, request identity, duplicate-initial-request response, event accounting, and containment/reporting of faulted sessions.
- [DONE] T047: Implement the server RRQ state machine for request validation, storage authorization/open, mail/unknown-mode rejection, option negotiation, OACK confirmation, DATA/ACK sending, retries, rollover, and result cleanup.
- [DONE] T048: Implement the server WRQ state machine for request validation, pending storage, announced/runtime size enforcement, option negotiation, OACK/ACK 0, DATA receive/dally, atomic commit, retry, abort, and cleanup.
- [DONE] T049: Implement server ERROR mapping for all standard conditions, ERROR 8 negotiation failures, bounded messages, non-retransmission, malformed/unexpected peer packets, and nonfatal unknown-TID handling.
- [DONE] T050: Complete `TftpServer` run/stop/status methods and `TftpDirectoryStorage`/server integration without exposing any public member not declared by their interfaces.
- [DONE] T051: Test server RRQ with raw scripted clients for no options, every supported option combination, OACK ACK 0, legacy packet forms, all content/mode boundaries, lost/duplicate/wrong packets, rollover, and all storage failures.
- [DONE] T052: Test server WRQ with raw scripted clients for no options, every supported option combination, OACK DATA 1, all content/mode boundaries, `tsize` mismatch/limits, lost/duplicate/wrong packets, rollover, commit, and abort.
- [DONE] T053: Test server lifecycle for IPv4/IPv6 and multiple/dynamic bindings, concurrent independent sessions, duplicate initial requests, configured session rejection, listener continuity after session failure, status counters, graceful drain, and immediate cancellation.
- [DONE] T054: Test broadcast/multicast suppression where the platform permits, unknown-TID ERROR 5 source/destination behavior, request/error size caps, malformed initial datagrams, and resource-limit enforcement.
- [DONE] T055: Build end-to-end in-process client/server tests covering both directions, both modes, stream and directory storage, IPv4/IPv6, no/partial/full options, empty/short/exact/multi-block/rollover sizes, and byte-for-byte results.
- [DONE] T056: Build a deterministic UDP fault proxy and end-to-end adverse-network tests that drop, duplicate, delay, and reorder RRQ/WRQ/OACK/DATA/ACK/ERROR packets without triggering Sorcerer's Apprentice amplification or duplicate writes.
- [DONE] T057: Add opt-in interoperability tests against locally available tftp-hpa and TFTPy peers for their common RFC 1350/2347/2348/2349 subset, with clear skips when those test-only tools are unavailable.
- [DONE] T058: Exercise every public exception constructor/detail field and every observer event/result/status field through real API behavior, including observer-thrown failure semantics.
- [DONE] T059: Run the full test suite with coverage collection, close every uncovered `src/Api` line/branch with behavior-focused cases, and require complete public API coverage without adding syntax-policy or source-check tests.
- [DONE] T060: Run final build and test validation on supported host address families, verify `src/tftp.csproj` has no external package references, and resolve all compiler warnings while preserving the simplified source syntax.
