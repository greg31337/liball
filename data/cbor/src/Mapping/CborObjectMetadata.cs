using System.Collections.Generic;

namespace cbor;

internal class CborObjectMetadata
{
    internal List<CborObjectMemberMetadata> Members = new List<CborObjectMemberMetadata>();
    internal bool CanCreate;
}
