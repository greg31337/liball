# Identity content-transfer encodings

`EmailTransferValidator` incrementally validates and copies the MIME `7bit`,
`8bit`, and `binary` identity content-transfer encodings. Each `Feed()` returns
an unchanged, caller-owned copy of the bytes accepted within the configured
overall limit. Chunk boundaries do not affect CRLF or line-length validation.

`7bit` permits bytes through 127, while `8bit` also permits high-bit bytes.
Both reject NUL, require CR and LF to occur only as CRLF pairs, and enforce a
configurable maximum number of bytes before each CRLF. The default transport
line maximum is 998 bytes. `binary` permits every byte sequence and ignores
line constraints. Constraint failures are errors with exact source positions,
but bytes remain unchanged in the deterministic identity output.

The overall byte limit is fatal. Copying stops before the byte that would cross
the limit while source accounting consumes the remainder of the supplied
chunk. `new EmailTransferEncoding().ValidateWithInputAndKind()` provides one-shot validation.
`new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytes()` reads and writes caller-owned streams in bounded
chunks without closing either stream.

The detailed copy overload accepts null-disabled `IEmailCancellation` and
`IEmailProgress` values. The library-provided `EmailCancellation` and
`EmailProgress` classes implement those contracts. Cancellation is checked before every stream read and
returns the copied prefix with an `OperationCancelled` issue. Progress reports
source bytes read, unchanged bytes written and produced, and the completed
operation count.
