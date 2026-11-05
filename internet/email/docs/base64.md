# Base64 encoding and decoding

## Encoding

`EmailBase64Encoder` incrementally encodes arbitrary binary chunks without
retaining spans. It buffers no more than the two bytes of an incomplete input
quantum. Each `Feed()` returns complete four-character output quantums, and
`Complete()` emits any padded final quantum.

The constructor selects a line length from 4 through 76 characters, inclusive,
in multiples of four. Lines are separated with CRLF. The encoder adds a line
break only when another output quantum follows, so encoded output never gains a
trailing CRLF. `new EmailBase64().EncodeWithInput()` provides one-shot encoding and uses the
76-character MIME line length by default.

`GetInputByteCount()` and `GetOutputByteCount()` report cumulative input and
emitted output, including CRLF bytes. `GetEncodedSize()` predicts the exact
encoded size with line breaks and checks arithmetic overflow.

## Decoding

`EmailBase64Decoder` incrementally decodes MIME Base64 across arbitrary input
chunks without retaining spans. Each `Feed()` returns only bytes completed by
that call; `Complete()` returns bytes recovered from an unpadded final quantum.
`GetDecodedByteCount()` counts all emitted chunks, and `GetIssues()` returns a
snapshot of ordered diagnostics.

## Strict and tolerant modes

Both modes accept the Base64 alphabet, `=`, space, horizontal tab, CR, and LF.
Chunk boundaries have no semantic effect, including boundaries inside padding
or CRLF. Vertical tab and form feed are non-canonical whitespace.

Strict mode reports malformed bytes, padding, unused padding bits, and truncated
final quantums as errors. Tolerant mode reports the same recoveries as warnings,
including non-canonical whitespace. Both preserve deterministic usable bytes:
invalid characters and excess padding are ignored, missing terminal padding is
inferred when at least two alphabet characters are available, and non-zero
unused bits do not alter the recoverable high-order bytes. Data after a terminal
padded quantum is ignored and reported.

Every issue carries the zero-based source byte offset and one-based line and
column captured before the offending byte. End-of-input issues point to the
position immediately after the final supplied byte. Descriptions distinguish
the defect from the recovery actually applied.

## Bounds and results

The decoder requires an explicit non-negative decoded-byte maximum. It checks a
whole quantum before emission. If that quantum would cross the limit, a fatal
`DecodedByteLimitExceeded` issue is recorded, the quantum is not emitted, and
remaining input is consumed only for source-position accounting. Previously
returned bytes remain the deterministic prefix.

`new EmailBase64().Decode()` is the one-shot facade and returns an
`EmailBase64DecodeResult`. A result is successful when it contains no error or
fatal issues; tolerant recoveries can therefore succeed with warnings.
Returned byte arrays and issue collections are defensive snapshots.

`GetMaximumDecodedSize()` gives a conservative overflow-safe bound from encoded
byte count, including a partial final quantum. `GetDecodedSizeFromQuantumCount()`
gives the exact canonical size when the number of complete quantums and terminal
padding characters are already known.
