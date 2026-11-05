# Message summaries and statistics

`new EmailMessageAnalyzer().AnalyzeWithMessage` (also available as
`SummarizeWithMessage`) creates a bounded
`EmailMessageSummary` without mutating the message. Top-level header statistics
include visible `To`, `Cc`, and `Bcc` mailbox counts, resent-recipient counts,
the envelope recipient count, the first usable `Date`, all usable
`Resent-Date` values, and all usable dates from `Received` fields. Address
groups contribute their member mailboxes.

MIME traversal follows the same ordering and nested-message behavior as
`new EmailTraversal().DepthFirstWithRoot`. Part counts include the root; maximum depth is
zero for a single root. Normalized media-type statistics retain the order in
which each distinct type first appears.

## Byte totals

Raw message size is present only when `EmailSourceInfo` retains an exact message
range. Raw content size is the sum of known direct MIME payload lengths; it is
not a reconstructed wire size and excludes container boundaries, headers,
preambles, and epilogues. Decoded content size is the sum of direct payloads
after content-transfer decoding.

Each payload and attachment total has a completeness flag. A total is
incomplete when required content was discarded, has no known length, uses an
unsupported transfer encoding, or cannot be measured under the selected
policy. Repeatable encoded or unknown-length content is measured by default
within `GetMaximumDecodedContentBytes()`. Non-repeatable content is never
consumed merely to produce a summary. Callers can disable all such measurement
with `SetMeasureRepeatableDecodedContent(false)`.

Attachment counts use the established `EmailAttachmentFinder` projection.
Downloadable attachment byte totals cover direct attachment payloads; a nested
message contributes its retained raw message range when available. Ambiguous
container attachments or nested messages without retained source information
make the corresponding total incomplete.

## Issues and security evidence

Issue totals include the message, headers, MIME descendants, and nested
messages, with counts available for every severity. `EmailSecuritySummary`
reports signed/encrypted structures, signature and encryption parts, report
structures, and the presence of DKIM, ARC, Authentication-Results, and
Received-SPF headers. These are structural or header-presence observations:
`ClaimsVerification()` always returns false, and no trust or authentication
decision is inferred.

Header, structured-header-byte, address, traversal depth/entity, and decoded
content limits are configured by `EmailMessageAnalysisOptions`. A hard limit
throws `EmailLimitException` instead of returning a misleading partial summary.
