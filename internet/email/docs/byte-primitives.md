# Byte primitives

`EmailByteScanner` provides allocation-free classification and search over
individual bytes and `ReadOnlySpan<byte>` values. It performs ASCII folding
only: bytes above 127 are compared exactly and are never interpreted using the
current culture.

## Classification

The scanner distinguishes ASCII letters, digits, WSP (space and horizontal
tab), CR, LF, controls, visible ASCII, legal field-name bytes, RFC 5322 `atext`,
RFC 2045 MIME token characters, and hexadecimal digits. `IsWsp` intentionally
does not include CR or LF. Dot is not `atext`; it is a separator in a dot-atom.
MIME token classification excludes controls, space, and every RFC 2045
`tspecials` byte.

ASCII upper/lower conversion changes only `A`–`Z` or `a`–`z`. Equality,
ordering, prefix, and case-insensitive delimiter search use that same rule and
are deterministic for arbitrary bytes.

## Hexadecimal and search

`GetHexValue` and `DecodeHexPair` return `-1` for malformed input. Hexadecimal
generation accepts a nibble from 0 through 15 and rejects other values. Search
methods return the first matching byte offset or `-1`; their start index may
equal the input length but cannot fall outside the span. An empty sequence is
found at the requested start index, matching normal substring semantics.

Search methods do not retain their spans. Sequence search uses direct bounded
comparison suitable for the short delimiters common in Internet message
syntax; later streaming boundary scanners handle cross-buffer state.

## Checked size arithmetic

`EmailSafeArithmetic` operates on non-negative signed 64-bit sizes. Addition
and multiplication offer `Can...` predicates and throwing forms. The throwing
forms distinguish negative API input (`ArgumentOutOfRangeException`) from an
unrepresentable result (`OverflowException`). Ceiling division avoids the
usual `value + divisor - 1` overflow, and narrowing to `Int32` is checked.

