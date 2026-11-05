# Source locations and counters

Source offsets and lengths use signed 64-bit integers so large streams can be
described without narrowing. Negative values are rejected and arithmetic is
checked before state changes, preventing wraparound at `Int64.MaxValue`.

## Ranges

`EmailRange` is an immutable half-open byte range: its start is included and its
end is excluded. An empty range contains no offsets, but it can be contained at
the end of another range. Adjacent ranges do not intersect. `Slice` receives an
offset relative to the parent range and cannot escape it.

## Byte and line accounting

`EmailByteCounter` accumulates non-negative byte counts. `CanAdd` permits a
caller to check an addition, while `Add` throws `OverflowException` without
changing the counter when the sum cannot be represented.

`EmailSourceTracker` starts at byte offset 0, line 1, column 1. Columns count
source bytes, not characters. CRLF counts as one line ending even when split
across calls; bare CR and bare LF each count as one line ending. Advancing a
chunk is atomic: if its offset, line, or column would overflow, none of the
tracker's coordinates change.

A tracker created at an explicit `EmailSourcePosition` has no knowledge of the
byte before that position. This matters only if the first supplied byte is LF;
use one tracker continuously when classifying CRLF across chunks.

## Entity paths

`EmailPath` is immutable. Its programmatic child indexes are zero-based and its
display form uses one-based MIME-style part numbers. The root display value is
`root`; appending child indexes 0 and 2 produces `root/1/3`. Parent and prefix
operations make ancestry checks possible without parsing display text.

