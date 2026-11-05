# Quoted-printable body encoding and decoding

## Encoding

`EmailQuotedPrintableEncoder` incrementally emits uppercase `=HH` escapes and
never splits a three-character escape across physical lines. The selected line
length is between 4 and 76 characters. Generated soft breaks use CRLF and the
trailing equals sign is included in the configured limit.

Space and tab are buffered until the following source byte determines whether
they end a line. End-of-line and end-of-input whitespace is hex encoded; other
space and tab bytes remain literal. The newline policy can preserve input
newline forms, normalize CRLF, LF, and CR to CRLF, or encode every newline byte
as data. The one-shot `new EmailQuotedPrintable().EncodeWithInput()` facade normalizes to CRLF
by default.

The optional `From ` safety mode encodes the initial `F` on dangerous source
lines. It also conservatively encodes an `F` placed at the start of a generated
soft-wrapped line, preventing mbox separator text from appearing at any
physical output line start. Prefix recognition works across input chunks.

## Decoding

`EmailQuotedPrintableDecoder` incrementally decodes MIME body
quoted-printable across arbitrary input chunks without retaining spans. Each
`Feed()` returns bytes completed by that call, while `Complete()` resolves an
incomplete final escape. `new EmailQuotedPrintable().Decode()` is the one-shot
facade.

Canonical `=HH` escapes use uppercase hexadecimal digits. Canonical soft line
breaks use `=` followed by CRLF; transport padding between the equals sign and
CRLF is accepted and removed. Hard line breaks remain in the decoded bytes.
Space and tab at the end of an encoded line, including the end of input, are
discarded as transport padding.

Malformed escapes are preserved literally and reported. Lowercase hexadecimal
escapes are decoded but reported as non-canonical. An incomplete escape at end
of input is preserved and reported. Tolerant mode also treats `=` followed by a
bare LF as a soft break; strict mode preserves that sequence. Strict-mode
recoveries are errors and tolerant-mode recoveries are warnings.

The decoder requires an explicit non-negative decoded-byte maximum and stops
before emitting the first byte that would exceed it. Issues include the source
position of the escape's equals sign. Returned results and issue collections
are defensive snapshots. `GetMaximumDecodedSize()` returns the encoded byte
count because quoted-printable decoding cannot expand its input.
