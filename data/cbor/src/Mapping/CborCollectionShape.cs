using System;

namespace cbor;

internal class CborCollectionShape
{
    internal bool IsArray;
    internal bool IsList;
    internal bool IsDictionary;
    internal Type? ElementType;
    internal Type? KeyType;
    internal Type? ValueType;
    internal Type? ConcreteType;
}
