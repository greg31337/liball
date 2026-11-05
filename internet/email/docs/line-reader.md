# Bounded line reader

`EmailLineReader` incrementally separates source bytes into lines without
normalizing their endings. It recognizes CRLF, bare LF, and bare CR, and reports
the original form through `EmailLineEnding`. Content bytes exclude the ending;
an unterminated final line has the `None` ending.

## Incremental contract

`Read(input, endOfInput)` returns one of three statuses:

- `Line` supplies one complete `EmailLine`;
- `NeedMoreData` means the supplied bytes were consumed but no line can yet be
  emitted;
- `EndOfInput` means no lines remain.

Every result reports how many bytes from the supplied span were consumed. A
line may consume fewer bytes than supplied, so callers pass the unconsumed
suffix again. When a CR from the previous chunk is followed by a non-LF byte,
the reader returns the CR-terminated line while consuming zero bytes from the
new span; the next call processes that same span. No input span is retained.

A CR at a chunk edge remains pending until another byte arrives or
`endOfInput` is true. A following LF forms one CRLF ending even across the
boundary. An empty non-final call does not resolve a pending CR. After final
input has been fully consumed, subsequent empty reads return `EndOfInput`;
supplying new bytes then is invalid. `Reset()` starts a new independent input.

## Bounds and ownership

`maxLineBytes` counts content bytes only, not CR or LF. A line exactly at the
bound is accepted. The next content byte throws `EmailLimitException` before it
is buffered, with `MaxLineBytes`, the configured limit, and the observed size.

Returned lines own defensive copies of their content, and each
`GetContentBytes()` call returns another copy. Empty input produces no line,
while explicit empty terminated lines are preserved.

