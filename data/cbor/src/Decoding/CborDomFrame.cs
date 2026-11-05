using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CborDomFrame
{
    internal CborTokenKind Kind;
    internal CborValue? Value;
    internal CborValue? MapKey;
    internal List<byte>? ByteStringBytes;
    internal StringBuilder? TextStringBuilder;
    internal long SourceEnd;
}
