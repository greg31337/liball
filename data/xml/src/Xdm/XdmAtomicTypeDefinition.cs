namespace xml;

internal class XdmAtomicTypeDefinition
{
    internal XdmAtomicType Type;
    internal XmlName Name;
    internal XdmAtomicType BaseType;
    internal XdmAtomicType PrimitiveType;
    internal bool HasBaseType;
    internal bool HasPrimitiveType;
    internal bool IsPrimitive;
    internal bool IsAbstract;

    internal XdmAtomicTypeDefinition(XdmAtomicType type, XmlName name, XdmAtomicType baseType, XdmAtomicType primitiveType, bool hasBaseType, bool hasPrimitiveType, bool isPrimitive, bool isAbstract)
    {
        Type = type;
        Name = name;
        BaseType = baseType;
        PrimitiveType = primitiveType;
        HasBaseType = hasBaseType;
        HasPrimitiveType = hasPrimitiveType;
        IsPrimitive = isPrimitive;
        IsAbstract = isAbstract;
    }
}
