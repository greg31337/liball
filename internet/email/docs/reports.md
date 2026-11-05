# Generic report recognition and field blocks

`EmailReportParser` recognizes a MIME entity only when its media type is
`multipart/report`. Recognition is structural and does not interpret DSN, MDN,
or ARF field semantics. The ordered roles are:

1. the first child is the human-readable part;
2. the second child is the machine-readable part;
3. the first later `message/rfc822`, `message/global`,
   `text/rfc822-headers`, or `message/global-headers` child is the optional
   original content;
4. every remaining child is retained as an additional part in input order.

The normalized `report-type` parameter is compared with the machine part's
`message` subtype. Global delivery-status and disposition-notification variants
match their non-global report types. A missing parameter may be inferred from
the machine subtype, but still produces `ReportTypeMissing`; conflicting types
produce `ReportTypeMismatch`. Malformed or incomplete structures return a
partial `EmailMultipartReport` when the root itself is recognizable. These
rules are deterministic MIME recognition only and make no delivery,
disposition, abuse, or trust claim.

## Field blocks

`EmailReportFieldBlockParser` parses an explicit byte payload or the decoded
content of a machine-readable MIME part. Blank physical lines delimit ordered
blocks. Each block retains:

- ordered `EmailHeader` fields, including duplicate and unknown extension
  names;
- each field's original casing, folding, raw value, and physical line endings;
- the exact block bytes and exact following blank-line separator bytes;
- its source position and parser issues.

An unterminated final block is accepted by supplying only the synthetic parser
separator needed to finalize its last field; `GetRawBytes()` remains the
authoritative exact input bytes. Tolerant malformed fields are retained where
possible and produce `ReportFieldBlockMalformed`; strict mode promotes that
diagnostic to an error. Field getters return clones and byte/list getters return
defensive copies.

`EmailReportParseOptions` bounds decoded report bytes, immediate report parts,
field blocks, total fields, fields per block, block bytes, and physical line
bytes. Hard-limit violations throw `EmailLimitException` before a partial
result escapes. Specialized DSN, MDN, and ARF parsers can interpret known field
names from these lossless blocks while preserving every extension field.

## Delivery status notifications

`EmailDeliveryStatusParser` specializes a delivery-status report into one
per-message block and zero or more per-recipient blocks. It exposes typed
`Reporting-MTA`, recipient, remote-MTA, diagnostic, date, action, and enhanced
status values while leaving each lossless `EmailReportFieldBlock` available.
Unknown extension fields retain their order and raw representation.

The parser diagnoses missing required `Reporting-MTA`, `Final-Recipient`,
`Action`, and `Status` fields, malformed typed values, duplicate singleton
fields, and conflicts between Action and enhanced-status class. Failed,
delayed, and successful actions require status classes 5, 4, and 2
respectively. A usable partial `EmailDeliveryStatus` remains available when its
block structure can be recovered.

`message/delivery-status` field bytes must be ASCII. The
`message/global-delivery-status` variant accepts valid UTF-8 and projects
returned `message/global-headers`; both variants also project a returned nested
original message. `GetOriginalHeaders()` and block/list accessors return
defensive copies where mutation would otherwise alter parsed state.

`new EmailDeliveryStatusWriter().WriteWithDeliveryStatus` validates the semantic model, canonicalizes
physical newlines to CRLF, enforces `MaxReportBytes`, and retains extension
fields. `CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadable`
wraps those fields with the generic report builder and
its safe original-content policy. Internationalized models require SMTPUTF8
composition and produce `message/global-delivery-status` plus
`message/global-headers` or `message/global` original content as selected.

## Message disposition notifications

`EmailDispositionNotificationParser` interprets the single field block of a
`message/disposition-notification` or
`message/global-disposition-notification` report. The typed view includes the
reporting user agent, gateway, original/final recipients, original Message-ID,
Disposition action and sending modes, disposition type, and modifiers. It
also retains repeated Error fields and the legacy Failure and Warning fields,
as well as ordered unknown extension fields.

Required Final-Recipient and Disposition fields, singleton duplication,
typed-value syntax, the Disposition grammar, ASCII/UTF-8 media-type agreement,
and a returned original Message-ID are validated without discarding recoverable
evidence. Standard MDNs accept displayed, deleted, dispatched, and processed;
denied and failed remain recognized for legacy interoperability.

`EmailDispositionNotificationWriter` validates and canonicalizes a lossless
MDN block and can create a safe multipart report. Internationalized MDNs
require SMTPUTF8 composition and select global machine/original media types.
When the original message has a Message-ID, generation requires a matching
Original-Message-ID field.

`ParseRequest` handles the request-side `Disposition-Notification-To`,
`Original-Recipient`, and `Disposition-Notification-Options` message fields.
Options expose their registered attribute, required/optional importance, and
word values. `ApplyRequest` safely replaces those three request headers on a
message using canonical address and option formatting. A request remains a
request only: the API does not decide whether user consent or sending an MDN
is appropriate.

## Abuse feedback reports

`EmailFeedbackReportParser` interprets the single ASCII field block in a
`message/feedback-report` part. Feedback-Type, User-Agent, and Version are
required exactly once. The typed projection includes the current registered
feedback types, envelope paths, arrival date, reporting MTA, source IP and
port, incident count, original recipients, reported domains and URIs, and
Authentication-Results assertions. Unknown feedback-type tokens and unknown
fields remain available as extensions for forward compatibility.

Authentication-failure reports additionally expose Auth-Failure,
Delivery-Result, DKIM identity metadata, and SPF-DNS evidence. They require
one Auth-Failure value and one Authentication-Results field describing one
method result. Parsed authentication fields are report-generator assertions;
the parser does not treat them as independently verified evidence.

ARF requires reported-original evidence. The parser projects either returned
headers or a nested original message and diagnoses reports that omit both.
`EmailFeedbackReportWriter` validates and canonicalizes the machine block,
retains extension fields, enforces output limits, and refuses report options
that would omit the safe original-content part.
