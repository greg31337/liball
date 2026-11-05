using System.Collections.Generic;

namespace cbor;

internal class CddlGenericInstantiation
{
    internal CddlRule GenericRule = new CddlRule();
    internal CddlRule InstantiatedRule = new CddlRule();
    internal List<CddlNode> Arguments = new List<CddlNode>();
    internal List<CddlNode> Applications = new List<CddlNode>();
}
