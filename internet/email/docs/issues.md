# Issue diagnostics

`EmailIssue` is the common diagnostic value for recoverable malformed input,
validation findings, policy decisions, and bounded partial results. Issues are
returned in encounter order through `EmailIssueList`.

## Severity

- `Information` records a noteworthy choice that does not reduce validity.
- `Warning` records tolerated or suspicious input for which usable output is
  still available.
- `Error` records an operation or data failure; a documented partial value may
  still be available.
- `Fatal` records that processing could not safely continue. An operation may
  return its deterministic prefix or throw when no bounded result is possible.

Severity is independent of the issue code. Callers should use both rather than
assuming every occurrence of one code has the same operational consequence.

## Stable code ranges

Numeric codes are never reassigned to a different meaning. New codes may be
added within the owning range:

| Range | Area |
| --- | --- |
| 1000–1999 | General operation, limits, cancellation, and foundation |
| 2000–2999 | Transfer codecs and charsets |
| 3000–3999 | Headers and structured field values |
| 4000–4999 | MIME structure, message model, and parsing |
| 5000–5999 | Writing, editing, normalization, and validation |
| 6000–6999 | Content processing, composition, search, and analysis |
| 7000–7999 | Specialized formats, files, and archives |
| 8000–8999 | DKIM, ARC, and authentication fields |
| 9000–9999 | CMS, S/MIME, OpenPGP, and cryptographic operations |

Zero is `Unknown` and is a sentinel, not an emitted library issue. Values at or
above 10000 are unassigned and must not be treated as library codes.

## Location and recovery context

An issue may carry an `EmailSourcePosition`, `EmailPath`, field name, and
recovery description. Their absence is reported by the matching `Has...()`
method, and the getter returns null.

Byte offsets are zero-based absolute offsets in the input represented by the
result. Line and column numbers are one-based. They describe source bytes, not
decoded Unicode scalar positions. Entity path segments are zero-based child
indexes for programmatic traversal; `GetDisplayText()` renders the root as
`root` and child numbers as one-based values such as `root/1/3`.

The main description states what was observed. The recovery description states
what the library did about it, such as retaining raw bytes, substituting a
value, skipping content, or ending a partial parse. It must not suggest that an
unperformed repair occurred.

