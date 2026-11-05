using System.Collections.Generic;

namespace xml;

internal class XdmSequenceBuilder
{
    internal List<XdmItemKind> ItemKinds;
    internal List<int> ItemIndexes;
    internal List<XmlNode> Nodes;
    internal List<XdmAtomicValue> AtomicValues;
    internal List<XdmFunctionItem> Functions;
    internal List<XdmMapValue> Maps;
    internal List<XdmArrayValue> Arrays;

    internal XdmSequenceBuilder()
    {
        ItemKinds = new List<XdmItemKind>();
        ItemIndexes = new List<int>();
        Nodes = new List<XmlNode>();
        AtomicValues = new List<XdmAtomicValue>();
        Functions = new List<XdmFunctionItem>();
        Maps = new List<XdmMapValue>();
        Arrays = new List<XdmArrayValue>();
    }

    internal void AddXdmNode(XmlNode node)
    {
        Guard.NotNull(node, "node");
        if (!new XdmSequenceOperations().IsXdmSequenceNodeKind(node.Kind))
        {
            throw new System.ArgumentException("The XML node kind cannot be stored as an XDM node.", "node");
        }

        ItemKinds.Add(XdmItemKind.Node);
        ItemIndexes.Add(Nodes.Count);
        Nodes.Add(node);
    }

    internal void AddXdmAtomicValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        ItemKinds.Add(XdmItemKind.Atomic);
        ItemIndexes.Add(AtomicValues.Count);
        AtomicValues.Add(value);
    }

    internal void AddXdmFunction(XdmFunctionItem function)
    {
        Guard.NotNull(function, "function");
        ItemKinds.Add(XdmItemKind.Function);
        ItemIndexes.Add(Functions.Count);
        Functions.Add(function);
    }

    internal void AddXdmMap(XdmMapValue map)
    {
        Guard.NotNull(map, "map");
        ItemKinds.Add(XdmItemKind.Map);
        ItemIndexes.Add(Maps.Count);
        Maps.Add(map);
    }

    internal void AddXdmArray(XdmArrayValue array)
    {
        Guard.NotNull(array, "array");
        ItemKinds.Add(XdmItemKind.Array);
        ItemIndexes.Add(Arrays.Count);
        Arrays.Add(array);
    }

    internal void AppendXdmSequence(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Kind == XdmItemKind.Node && item.Node != null)
            {
                AddXdmNode(item.Node);
            }
            else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
            {
                AddXdmAtomicValue(item.AtomicValue);
            }
            else if (item.Kind == XdmItemKind.Function && item.Function != null)
            {
                AddXdmFunction(item.Function);
            }
            else if (item.Kind == XdmItemKind.Map && item.Map != null)
            {
                AddXdmMap(item.Map);
            }
            else if (item.Kind == XdmItemKind.Array && item.Array != null)
            {
                AddXdmArray(item.Array);
            }
            else
            {
                throw new System.ArgumentException("The XDM sequence item is incomplete.", "sequence");
            }
        }
    }

    internal XdmSequence BuildImmutableXdmSequence()
    {
        XdmSequence sequence = new XdmSequence();
        sequence.ItemKinds = ItemKinds.ToArray();
        sequence.ItemIndexes = ItemIndexes.ToArray();
        sequence.Nodes = Nodes.ToArray();
        sequence.AtomicValues = AtomicValues.ToArray();
        sequence.Functions = Functions.ToArray();
        sequence.Maps = Maps.ToArray();
        sequence.Arrays = Arrays.ToArray();
        sequence.Count = ItemKinds.Count;
        sequence.IsImmutable = true;
        return sequence;
    }
}
