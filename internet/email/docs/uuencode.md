# Legacy uuencode

`new EmailUuencode().Detect()` searches complete lines for a traditional lowercase
`begin NNN filename` header and returns its octal file mode, printable ASCII
file name, and source position. It does not recognize the separate
`begin-base64` format. Detection is intended for legacy message recovery and
does not make uuencode a normal MIME content-transfer-encoding.

`EmailUuencodeDecoder` incrementally scans optional preamble text and decodes
traditional data lines across arbitrary chunks. CRLF, LF, and CR line endings,
space or backtick zero values, stripped empty zero lines, short data lines, and
noncanonical six-bit characters have deterministic tolerant recovery.
Recoveries produce warnings with source positions. A missing header is an
error, and crossing the explicit decoded-byte maximum is fatal.

`new EmailUuencode().Decode()` is the one-shot facade. Decode results retain the
suggested metadata but never create a file or interpret the name as a path.
Decoded byte arrays and issue lists are defensive snapshots.

Canonical generation is available only through the explicit
`new EmailUuencode().Encode()` API. It writes a caller-supplied mode and printable
ASCII name, encodes no more than 45 input bytes per data line, uses backticks
for zero values, and emits CRLF plus the zero and `end` lines. No automatic
transfer-encoding selection calls this legacy generator.
