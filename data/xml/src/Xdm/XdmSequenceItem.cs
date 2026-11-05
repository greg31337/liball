namespace xml;

internal class XdmSequenceItem
{
    internal XdmItemKind Kind;
    internal XmlNode? Node;
    internal XdmAtomicValue? AtomicValue;
    internal XdmFunctionItem? Function;
    internal XdmMapValue? Map;
    internal XdmArrayValue? Array;

    internal XdmSequenceItem(XdmItemKind kind, XmlNode? node, XdmAtomicValue? atomicValue, XdmFunctionItem? function, XdmMapValue? map, XdmArrayValue? array)
    {
        Kind = kind;
        Node = node;
        AtomicValue = atomicValue;
        Function = function;
        Map = map;
        Array = array;
    }
}
