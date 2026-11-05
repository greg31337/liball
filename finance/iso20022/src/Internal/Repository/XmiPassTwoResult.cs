using System.Collections.Generic;

namespace iso20022;

internal class XmiPassTwoResult
{
    internal List<XmiPassTwoEntity> Entities = new List<XmiPassTwoEntity>();
    internal List<XmiPassTwoReference> Relationships = new List<XmiPassTwoReference>();
    internal int AttributeCount;
    internal int ContainmentCount;
    internal int ResolvedReferenceCount;
    internal int UnresolvedReferenceCount;
    internal int ForwardReferenceCount;
    internal int BackwardReferenceCount;

    internal XmiPassTwoEntity FindEntityAtOrder(int documentOrder)
    {
        if (documentOrder < 0 || documentOrder >= Entities.Count)
        {
            throw new System.ArgumentOutOfRangeException("documentOrder");
        }

        return Entities[documentOrder];
    }
}
