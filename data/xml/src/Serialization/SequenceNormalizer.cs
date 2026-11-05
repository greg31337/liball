using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace xml;

internal class SerializationSequenceItem
{
    internal XmlNode? Node;
    internal string? Text;
    internal bool IsText;

    internal SerializationSequenceItem(XmlNode? node, string? text, bool isText)
    {
        Node = node;
        Text = text;
        IsText = isText;
    }
}

internal class SequenceNormalizer
{
    internal TreeMutation Mutation;
    internal List<XdmArrayValue> ActiveArrays;

    internal SequenceNormalizer()
    {
        Mutation = new TreeMutation();
        ActiveArrays = new List<XdmArrayValue>();
    }

    internal XmlDocument NormalizeXdmSerializationSequence(XdmSequence sequence, string? itemSeparator)
    {
        Guard.NotNull(sequence, "sequence");
        ActiveArrays.Clear();
        List<SerializationSequenceItem> first = new List<SerializationSequenceItem>();
        FlattenXdmSerializationSequence(sequence, first);
        if (first.Count == 0)
        {
            first.Add(new SerializationSequenceItem(null, "", true));
        }

        List<SerializationSequenceItem> separated = ApplyXmlSerializationItemSeparator(first, itemSeparator);
        return BuildNormalizedXmlSerializationDocument(separated);
    }

    internal XmlDocument NormalizeXmlSerializationDocument(XmlDocument document, string? itemSeparator)
    {
        Guard.NotNull(document, "document");
        XmlNode? node = document.DocumentNode;
        if (node == null)
        {
            return NormalizeXdmSerializationSequence(new XdmSequenceOperations().CreateEmptyXdmSequence(), itemSeparator);
        }

        List<SerializationSequenceItem> items = new List<SerializationSequenceItem>();
        items.Add(new SerializationSequenceItem(node, null, false));
        return BuildNormalizedXmlSerializationDocument(ApplyXmlSerializationItemSeparator(items, itemSeparator));
    }

    internal XmlDocument NormalizeXmlSerializationNode(XmlNode node, string? itemSeparator)
    {
        Guard.NotNull(node, "node");
        List<SerializationSequenceItem> items = new List<SerializationSequenceItem>();
        items.Add(new SerializationSequenceItem(node, null, false));
        return BuildNormalizedXmlSerializationDocument(ApplyXmlSerializationItemSeparator(items, itemSeparator));
    }

    internal void FlattenXdmSerializationSequence(XdmSequence sequence, List<SerializationSequenceItem> target)
    {
        ValidateXdmSerializationSequenceStorage(sequence);
        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        int position = 0;
        while (position < sequence.Count)
        {
            XdmItemKind kind = kinds[position];
            int index = indexes[position];
            if (kind == XdmItemKind.Node)
            {
                XmlNode[] nodes = sequence.Nodes == null ? [] : sequence.Nodes;
                if (index < 0 || index >= nodes.Length || nodes[index] == null)
                {
                    throw CreateXmlSequenceNormalizationError("The XDM sequence contains an invalid node index.");
                }

                target.Add(new SerializationSequenceItem(nodes[index], null, false));
            }
            else if (kind == XdmItemKind.Atomic)
            {
                XdmAtomicValue[] atomicValues = sequence.AtomicValues == null ? [] : sequence.AtomicValues;
                if (index < 0 || index >= atomicValues.Length || atomicValues[index] == null)
                {
                    throw CreateXmlSequenceNormalizationError("The XDM sequence contains an invalid atomic-value index.");
                }

                target.Add(new SerializationSequenceItem(null, CastXdmAtomicToSerializationString(atomicValues[index]), true));
            }
            else if (kind == XdmItemKind.Array)
            {
                XdmArrayValue[] arrays = sequence.Arrays == null ? [] : sequence.Arrays;
                if (index < 0 || index >= arrays.Length || arrays[index] == null)
                {
                    throw CreateXmlSequenceNormalizationError("The XDM sequence contains an invalid array index.");
                }

                FlattenXdmSerializationArray(arrays[index], target);
            }
            else if (kind == XdmItemKind.Function || kind == XdmItemKind.Map)
            {
                throw CreateXmlSequenceNormalizationError("A function or map item cannot be sequence-normalized for this output method.");
            }
            else
            {
                throw CreateXmlSequenceNormalizationError("The XDM sequence contains an unknown item kind.");
            }

            position = position + 1;
        }
    }

    internal void FlattenXdmSerializationArray(XdmArrayValue array, List<SerializationSequenceItem> target)
    {
        int activeIndex = 0;
        while (activeIndex < ActiveArrays.Count)
        {
            if (ActiveArrays[activeIndex] == array)
            {
                throw CreateXmlSequenceNormalizationError("The XDM sequence contains a recursive array.");
            }

            activeIndex = activeIndex + 1;
        }

        XdmSequence[] members = array.Members == null ? [] : array.Members;
        if (array.Count < 0 || array.Count != members.Length)
        {
            throw CreateXmlSequenceNormalizationError("An XDM array has inconsistent member storage.");
        }

        ActiveArrays.Add(array);
        int index = 0;
        while (index < members.Length)
        {
            if (members[index] == null)
            {
                RemoveActiveXdmSerializationArray(array);
                throw CreateXmlSequenceNormalizationError("An XDM array contains a missing member sequence.");
            }

            FlattenXdmSerializationSequence(members[index], target);
            index = index + 1;
        }

        RemoveActiveXdmSerializationArray(array);
    }

    internal void RemoveActiveXdmSerializationArray(XdmArrayValue array)
    {
        int index = ActiveArrays.Count - 1;
        while (index >= 0)
        {
            if (ActiveArrays[index] == array)
            {
                ActiveArrays.RemoveAt(index);
                return;
            }

            index = index - 1;
        }
    }

    internal List<SerializationSequenceItem> ApplyXmlSerializationItemSeparator(List<SerializationSequenceItem> source, string? itemSeparator)
    {
        List<SerializationSequenceItem> result = new List<SerializationSequenceItem>();
        if (itemSeparator != null)
        {
            int separatedIndex = 0;
            while (separatedIndex < source.Count)
            {
                if (separatedIndex > 0)
                {
                    result.Add(new SerializationSequenceItem(null, itemSeparator, true));
                }

                result.Add(source[separatedIndex]);
                separatedIndex = separatedIndex + 1;
            }

            return result;
        }

        int index = 0;
        while (index < source.Count)
        {
            SerializationSequenceItem item = source[index];
            if (!item.IsText)
            {
                result.Add(item);
                index = index + 1;
                continue;
            }

            StringBuilder adjacent = new StringBuilder();
            bool first = true;
            while (index < source.Count && source[index].IsText)
            {
                if (!first)
                {
                    adjacent.Append(' ');
                }

                adjacent.Append(source[index].Text == null ? "" : source[index].Text);
                first = false;
                index = index + 1;
            }

            result.Add(new SerializationSequenceItem(null, adjacent.ToString(), true));
        }

        return result;
    }

    internal XmlDocument BuildNormalizedXmlSerializationDocument(List<SerializationSequenceItem> source)
    {
        XmlDocument result = new XmlDocument();
        result.Version = XmlVersion.Xml10;
        XmlNode container = Mutation.CreateXmlTreeNodeOperation(result, XmlNodeKind.Document, null, null);
        result.DocumentNode = container;
        List<XmlNode> children = new List<XmlNode>();
        StringBuilder pendingText = new StringBuilder();
        int index = 0;
        while (index < source.Count)
        {
            SerializationSequenceItem item = source[index];
            if (item.IsText)
            {
                pendingText.Append(item.Text == null ? "" : item.Text);
            }
            else
            {
                XmlNode? node = item.Node;
                if (node == null)
                {
                    throw CreateXmlSequenceNormalizationError("The sequence contains a missing node.");
                }

                if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
                {
                    XmlNode[] documentChildren = node.Children == null ? [] : node.Children;
                    int childIndex = 0;
                    while (childIndex < documentChildren.Length)
                    {
                        AppendNormalizedXmlSerializationNode(result, children, pendingText, documentChildren[childIndex]);
                        childIndex = childIndex + 1;
                    }
                }
                else
                {
                    AppendNormalizedXmlSerializationNode(result, children, pendingText, node);
                }
            }

            index = index + 1;
        }

        AppendNormalizedXmlSerializationText(result, children, pendingText);
        container.Children = children.ToArray();
        int elementCount = 0;
        index = 0;
        while (index < container.Children.Length)
        {
            container.Children[index].Parent = container;
            if (container.Children[index].Kind == XmlNodeKind.Element)
            {
                result.DocumentElement = container.Children[index];
                elementCount = elementCount + 1;
            }

            index = index + 1;
        }

        if (elementCount != 1)
        {
            result.DocumentElement = null;
        }

        return result;
    }

    internal void AppendNormalizedXmlSerializationNode(XmlDocument document, List<XmlNode> target, StringBuilder pendingText, XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            pendingText.Append(node.Value == null ? "" : node.Value);
            return;
        }

        AppendNormalizedXmlSerializationText(document, target, pendingText);
        if (node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.Namespace)
        {
            throw CreateXmlSequenceNormalizationError("An attribute or namespace node cannot be a child of the normalized result document.");
        }

        if (node.OwnerDocument == null)
        {
            throw CreateXmlSequenceNormalizationError("A sequence node must belong to an XML document before it can be copied.");
        }

        XmlNode copy = Mutation.ImportXmlTreeNodeOperation(document, node, true);
        copy.Parent = null;
        target.Add(copy);
    }

    internal void AppendNormalizedXmlSerializationText(XmlDocument document, List<XmlNode> target, StringBuilder pendingText)
    {
        if (pendingText.Length == 0)
        {
            return;
        }

        XmlNode text = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Text, null, pendingText.ToString());
        target.Add(text);
        pendingText.Clear();
    }

    internal string CastXdmAtomicToSerializationString(XdmAtomicValue value)
    {
        if (value.Type < XdmAtomicType.AnyAtomicType || value.Type > XdmAtomicType.Notation)
        {
            throw CreateXmlSequenceNormalizationError("An atomic value has an unknown type annotation.");
        }

        if (value.StringValue != null)
        {
            return value.StringValue;
        }

        if (value.CanonicalValue != null)
        {
            return value.CanonicalValue;
        }

        if (value.LexicalValue != null)
        {
            return value.LexicalValue;
        }

        if (value.Type == XdmAtomicType.Boolean)
        {
            return value.BooleanValue ? "true" : "false";
        }

        if (value.Type == XdmAtomicType.Decimal)
        {
            return global::System.Xml.XmlConvert.ToString(value.DecimalValue);
        }

        if (value.Type >= XdmAtomicType.Integer && value.Type <= XdmAtomicType.PositiveInteger)
        {
            return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double)
        {
            if (value.CanonicalValue != null)
            {
                return value.CanonicalValue;
            }

            if (value.Type == XdmAtomicType.Float)
            {
                return global::System.Xml.XmlConvert.ToString((float)value.FloatingValue);
            }

            return global::System.Xml.XmlConvert.ToString(value.FloatingValue);
        }

        if (value.Type == XdmAtomicType.HexBinary && value.BinaryValue != null)
        {
            StringBuilder hexadecimal = new StringBuilder(value.BinaryValue.Length * 2);
            int index = 0;
            while (index < value.BinaryValue.Length)
            {
                hexadecimal.Append(value.BinaryValue[index].ToString("X2"));
                index = index + 1;
            }

            return hexadecimal.ToString();
        }

        if (value.Type == XdmAtomicType.Base64Binary && value.BinaryValue != null)
        {
            return Convert.ToBase64String(value.BinaryValue);
        }

        if ((value.Type == XdmAtomicType.QName || value.Type == XdmAtomicType.Notation) && value.NameValue != null)
        {
            XmlName name = value.NameValue;
            if (name.QualifiedName != null)
            {
                return name.QualifiedName;
            }

            if (name.Prefix != null && name.Prefix.Length > 0)
            {
                return name.Prefix + ":" + (name.LocalName == null ? "" : name.LocalName);
            }

            return name.LocalName == null ? "" : name.LocalName;
        }

        if (value.ExtendedValue != null)
        {
            string? converted = Convert.ToString(value.ExtendedValue, CultureInfo.InvariantCulture);
            return converted == null ? "" : converted;
        }

        throw CreateXmlSequenceNormalizationError("An atomic value does not provide a string representation.");
    }

    internal void ValidateXdmSerializationSequenceStorage(XdmSequence sequence)
    {
        if (sequence.Count < 0)
        {
            throw CreateXmlSequenceNormalizationError("An XDM sequence has a negative item count.");
        }

        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        if (kinds.Length != sequence.Count || indexes.Length != sequence.Count)
        {
            throw CreateXmlSequenceNormalizationError("An XDM sequence has inconsistent ordered item storage.");
        }
    }

    internal XmlSerializationException CreateXmlSequenceNormalizationError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.SENR0001;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
