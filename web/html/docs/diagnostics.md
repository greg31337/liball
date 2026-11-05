# Diagnostics

UniversalHtml treats malformed HTML and other expected input problems as data. Operations retain bounded `HtmlDiagnostic` objects and return them with the result when safe continuation is possible. Exceptions are reserved for invalid API use, violated internal invariants, and conditions from which the operation cannot safely recover.

## Stable identity

Every diagnostic has an explicitly assigned nonzero `HtmlDiagnosticCode`. Its text identifier is `UH` followed by the numeric value padded to five digits; code `3002`, for example, is `UH03002`.

Numeric values and text identifiers are compatibility surface:

- never renumber or reuse an existing value;
- never change an existing value to mean a different condition;
- add a new value when callers may need to distinguish a new condition;
- keep aliases out of the enum;
- document removals or severity changes as compatibility changes.

Messages explain the occurrence to a person but are not stable machine-readable identifiers. Callers should branch on the code and severity, not compare message text.

Current numeric families are:

| Range | Area |
|---|---|
| `UH01001`–`UH01099` | normalized input and input limits |
| `UH01101`–`UH01199` | encoding detection and decoding |
| `UH02001`–`UH02099` | character references |
| `UH03001`–`UH03099` | tokenization |
| `UH04001`–`UH04099` | tree construction |
| `UH05001`–`UH05099` | selectors |
| `UH06001`–`UH06099` | URLs |
| `UH07001`–`UH07099` | serialization |
| `UH08001`–`UH08099` | sanitization |
| `UH09001`–`UH09199` | text and Markdown conversion |
| `UH10001`–`UH10199` | metadata and article extraction |
| `UH11001`–`UH11099` | processing pipeline |
| `UH12001`–`UH12099` | shared limits and incomplete operations |

The enum in [`HtmlDiagnostic.cs`](../src/HtmlDiagnostic.cs) is the authoritative allocation of individual values.

## Severity

- `Information` records useful context without a failure.
- `Warning` records suspicious or corrected input when useful output remains available.
- `Error` records a recoverable operation error or omitted result portion.
- `Fatal` records a condition that prevents safe continuation of the requested operation.

Severity does not determine whether a parser produced a tree. HTML defines recovery from many parse errors, so a result can contain warnings or errors and still be usable. A fatal diagnostic must be paired with result state that clearly reports incompleteness or failure.

## Source spans

Diagnostic spans are end-exclusive. Positions contain a zero-based normalized UTF-16 offset, a zero-based original UTF-16 offset, and one-based line and column values. Keeping both offsets makes CR and CRLF folding observable. Byte offsets are present only when byte decoding tracked them.

Use an explicitly unknown span when no honest source range exists. Do not invent offset zero. When a condition covers no consumed character, use a zero-length known span at the relevant insertion point.

## Collection and truncation

Diagnostic retention consumes the operation's shared diagnostic budget. Once the limit is reached, the collector:

1. stops retaining new diagnostic objects;
2. continues counting total and dropped occurrences with saturating counters;
3. reports that collection was truncated;
4. leaves parser and security decisions unchanged.

Snapshot methods return independent arrays in occurrence order. They return an empty array, never `null`, when nothing was retained. Code must not bypass the collector to evade the shared limit.

## Adding a diagnostic

Choose the narrowest existing family, allocate an unused explicit value, add a focused behavioral test, and verify the emitted span and severity. Prefer one diagnostic per actionable occurrence. Avoid emitting the same fact in multiple layers unless each layer reports a distinct consequence.

Do not expose exception text as a diagnostic message. Messages must be deterministic, must not contain secrets or unrelated input, and should quote attacker-controlled text only when bounded and necessary.
