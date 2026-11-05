using System;
using System.Collections.Generic;

namespace odf;

internal static class OperationExtensionModel
{
    internal static OdfNode SerializeOperationExtension(
        OdfOperation[] operations)
    {
        if (operations == null)
        {
            throw new OdfEditException(
                "An operation collection is required for extension serialization.");
        }
        OdfNode root = ExtensionElement("operation-log");
        WriteExtensionAttribute(root, "version", "1");
        int index = 0;
        while (index < operations.Length)
        {
            OdfOperation operation = operations[index];
            if (operation == null)
            {
                throw new OdfEditException(
                    "An operation log cannot contain a null operation.");
            }
            OdfNode child;
            if (operation.Kind == OdfOperationKind.Unknown
                && operation.ExtensionNode != null)
            {
                child = CloneExtensionNode(operation.ExtensionNode);
            }
            else
            {
                child = SerializeOneOperation(operation);
            }
            AppendExtensionChild(root, child);
            index = index + 1;
        }
        return root;
    }

    internal static OdfOperation[] DeserializeOperationExtension(OdfNode root)
    {
        if (!IsExtensionElement(root, "operation-log"))
        {
            throw new OdfEditException(
                "The extension root is not an SDK operation log.");
        }
        List<OdfOperation> result = new List<OdfOperation>();
        int index = 0;
        while (index < root.Children.Length)
        {
            OdfNode child = root.Children[index];
            if (IsExtensionElement(child, "operation"))
            {
                result.Add(DeserializeOneOperation(child));
            }
            else if (child.Kind == OdfNodeKind.Element)
            {
                OdfOperation unknown = new OdfOperation();
                unknown.Kind = OdfOperationKind.Unknown;
                unknown.ExtensionNode = CloneExtensionNode(child);
                result.Add(unknown);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfNode SerializeOneOperation(OdfOperation operation)
    {
        OdfNode result = ExtensionElement("operation");
        WriteExtensionAttribute(result, "id", operation.Id);
        WriteExtensionAttribute(
            result, "sequence", operation.Sequence.ToString());
        WriteExtensionAttribute(result, "kind", KindToken(operation.Kind));
        WriteExtensionAttribute(result, "part-path", operation.PartPath);
        WriteExtensionAttribute(
            result, "target-node-id", operation.TargetNodeId);
        WriteExtensionAttribute(
            result, "parent-node-id", operation.ParentNodeId);
        WriteExtensionAttribute(
            result, "child-index", operation.ChildIndex.ToString());
        WriteExtensionAttribute(
            result,
            "previous-parent-node-id",
            operation.PreviousParentNodeId);
        WriteExtensionAttribute(
            result,
            "previous-child-index",
            operation.PreviousChildIndex.ToString());
        WriteExtensionAttribute(result, "value", operation.Value);
        WriteExtensionAttribute(
            result,
            "had-previous-value",
            operation.HadPreviousValue ? "true" : "false");
        WriteExtensionAttribute(
            result, "previous-value", operation.PreviousValue);
        WriteExtensionAttribute(result, "author", operation.Author);
        WriteExtensionAttribute(result, "session-id", operation.SessionId);
        WriteExtensionAttribute(result, "date", operation.Date);
        if (operation.AttributeName != null)
        {
            OdfNode attribute = ExtensionElement("attribute-name");
            WriteExtensionAttribute(
                attribute,
                "namespace",
                operation.AttributeName.NamespaceName);
            WriteExtensionAttribute(
                attribute, "local-name", operation.AttributeName.LocalName);
            WriteExtensionAttribute(
                attribute, "prefix", operation.AttributeName.Prefix);
            AppendExtensionChild(result, attribute);
        }
        if (operation.Preconditions.Length > 0)
        {
            OdfNode conditions = ExtensionElement("preconditions");
            int index = 0;
            while (index < operation.Preconditions.Length)
            {
                AppendExtensionChild(
                    conditions,
                    SerializePrecondition(operation.Preconditions[index]));
                index = index + 1;
            }
            AppendExtensionChild(result, conditions);
        }
        if (operation.Payload != null)
        {
            OdfNode payload = ExtensionElement("payload");
            AppendExtensionChild(
                payload, CloneExtensionNode(operation.Payload));
            AppendExtensionChild(result, payload);
        }
        return result;
    }

    static OdfNode SerializePrecondition(OdfEditPrecondition condition)
    {
        if (condition == null)
        {
            throw new OdfEditException(
                "An operation cannot contain a null precondition.");
        }
        OdfNode result = ExtensionElement("precondition");
        WriteExtensionAttribute(
            result, "kind", PreconditionToken(condition.Kind));
        WriteExtensionAttribute(
            result, "target-node-id", condition.TargetNodeId);
        WriteExtensionAttribute(
            result, "parent-node-id", condition.ParentNodeId);
        WriteExtensionAttribute(
            result, "child-index", condition.ChildIndex.ToString());
        WriteExtensionAttribute(
            result,
            "expected-value-present",
            condition.ExpectedValuePresent ? "true" : "false");
        WriteExtensionAttribute(
            result, "expected-value", condition.ExpectedValue);
        if (condition.AttributeName != null)
        {
            WriteExtensionAttribute(
                result,
                "attribute-namespace",
                condition.AttributeName.NamespaceName);
            WriteExtensionAttribute(
                result,
                "attribute-local-name",
                condition.AttributeName.LocalName);
            WriteExtensionAttribute(
                result,
                "attribute-prefix",
                condition.AttributeName.Prefix);
        }
        return result;
    }

    static OdfOperation DeserializeOneOperation(OdfNode node)
    {
        OdfOperation result = new OdfOperation();
        result.Id = ReadExtensionAttribute(node, "id");
        result.Sequence = ReadExtensionLong(node, "sequence", 0);
        result.Kind = ParseKind(ReadExtensionAttribute(node, "kind"));
        result.PartPath = ReadExtensionAttribute(node, "part-path");
        result.TargetNodeId = ReadExtensionAttribute(
            node, "target-node-id");
        result.ParentNodeId = ReadExtensionAttribute(
            node, "parent-node-id");
        result.ChildIndex = ReadExtensionInteger(node, "child-index", -1);
        result.PreviousParentNodeId = ReadExtensionAttribute(
            node, "previous-parent-node-id");
        result.PreviousChildIndex = ReadExtensionInteger(
            node, "previous-child-index", -1);
        result.Value = ReadExtensionAttribute(node, "value");
        result.HadPreviousValue = ReadExtensionAttribute(
            node, "had-previous-value") == "true";
        result.PreviousValue = ReadExtensionAttribute(
            node, "previous-value");
        result.Author = ReadExtensionAttribute(node, "author");
        result.SessionId = ReadExtensionAttribute(node, "session-id");
        result.Date = ReadExtensionAttribute(node, "date");
        OdfNode? attribute = FindExtensionChild(node, "attribute-name");
        if (attribute != null)
        {
            result.AttributeName = DeserializeName(attribute);
        }
        OdfNode? conditions = FindExtensionChild(node, "preconditions");
        result.Preconditions = DeserializePreconditions(conditions);
        OdfNode? payload = FindExtensionChild(node, "payload");
        if (payload != null)
        {
            result.Payload = FirstElementChild(payload);
            if (result.Payload != null)
            {
                result.Payload = CloneExtensionNode(result.Payload);
            }
        }
        if (result.Kind == OdfOperationKind.Unknown)
        {
            result.ExtensionNode = CloneExtensionNode(node);
        }
        return result;
    }

    static OdfEditPrecondition[] DeserializePreconditions(OdfNode? container)
    {
        if (container == null)
        {
            return [];
        }
        List<OdfEditPrecondition> result =
            new List<OdfEditPrecondition>();
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            if (IsExtensionElement(child, "precondition"))
            {
                OdfEditPrecondition condition = new OdfEditPrecondition();
                condition.Kind = ParsePrecondition(
                    ReadExtensionAttribute(child, "kind"));
                condition.TargetNodeId = ReadExtensionAttribute(
                    child, "target-node-id");
                condition.ParentNodeId = ReadExtensionAttribute(
                    child, "parent-node-id");
                condition.ChildIndex = ReadExtensionInteger(
                    child, "child-index", -1);
                condition.ExpectedValuePresent = ReadExtensionAttribute(
                    child, "expected-value-present") == "true";
                condition.ExpectedValue = ReadExtensionAttribute(
                    child, "expected-value");
                string localName = ReadExtensionAttribute(
                    child, "attribute-local-name");
                if (localName.Length > 0)
                {
                    OdfQualifiedName name = new OdfQualifiedName();
                    name.NamespaceName = ReadExtensionAttribute(
                        child, "attribute-namespace");
                    name.LocalName = localName;
                    name.Prefix = ReadExtensionAttribute(
                        child, "attribute-prefix");
                    condition.AttributeName = name;
                }
                result.Add(condition);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfQualifiedName DeserializeName(OdfNode node)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = ReadExtensionAttribute(node, "namespace");
        result.LocalName = ReadExtensionAttribute(node, "local-name");
        result.Prefix = ReadExtensionAttribute(node, "prefix");
        return result;
    }

    static string KindToken(OdfOperationKind kind)
    {
        if (kind == OdfOperationKind.InsertNode)
        {
            return "insert-node";
        }
        if (kind == OdfOperationKind.RemoveNode)
        {
            return "remove-node";
        }
        if (kind == OdfOperationKind.MoveNode)
        {
            return "move-node";
        }
        if (kind == OdfOperationKind.SetAttribute)
        {
            return "set-attribute";
        }
        if (kind == OdfOperationKind.RemoveAttribute)
        {
            return "remove-attribute";
        }
        if (kind == OdfOperationKind.SetText)
        {
            return "set-text";
        }
        return "unknown";
    }

    static OdfOperationKind ParseKind(string value)
    {
        if (value == "insert-node")
        {
            return OdfOperationKind.InsertNode;
        }
        if (value == "remove-node")
        {
            return OdfOperationKind.RemoveNode;
        }
        if (value == "move-node")
        {
            return OdfOperationKind.MoveNode;
        }
        if (value == "set-attribute")
        {
            return OdfOperationKind.SetAttribute;
        }
        if (value == "remove-attribute")
        {
            return OdfOperationKind.RemoveAttribute;
        }
        if (value == "set-text")
        {
            return OdfOperationKind.SetText;
        }
        return OdfOperationKind.Unknown;
    }

    static string PreconditionToken(OdfEditPreconditionKind kind)
    {
        if (kind == OdfEditPreconditionKind.NodeExists)
        {
            return "node-exists";
        }
        if (kind == OdfEditPreconditionKind.NodeAbsent)
        {
            return "node-absent";
        }
        if (kind == OdfEditPreconditionKind.ParentEquals)
        {
            return "parent-equals";
        }
        if (kind == OdfEditPreconditionKind.ChildIndexEquals)
        {
            return "child-index-equals";
        }
        if (kind == OdfEditPreconditionKind.AttributeEquals)
        {
            return "attribute-equals";
        }
        if (kind == OdfEditPreconditionKind.TextEquals)
        {
            return "text-equals";
        }
        return "unknown";
    }

    static OdfEditPreconditionKind ParsePrecondition(string value)
    {
        if (value == "node-exists")
        {
            return OdfEditPreconditionKind.NodeExists;
        }
        if (value == "node-absent")
        {
            return OdfEditPreconditionKind.NodeAbsent;
        }
        if (value == "parent-equals")
        {
            return OdfEditPreconditionKind.ParentEquals;
        }
        if (value == "child-index-equals")
        {
            return OdfEditPreconditionKind.ChildIndexEquals;
        }
        if (value == "attribute-equals")
        {
            return OdfEditPreconditionKind.AttributeEquals;
        }
        if (value == "text-equals")
        {
            return OdfEditPreconditionKind.TextEquals;
        }
        return OdfEditPreconditionKind.Unknown;
    }

    static OdfNode ExtensionElement(string localName)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Collaboration;
        result.Name.LocalName = localName;
        result.Name.Prefix = OdfNamespaces.CollaborationPrefix;
        result.WasEmptyElement = true;
        return result;
    }

    static void WriteExtensionAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        OdfAttribute attribute = new OdfAttribute();
        attribute.Name.LocalName = localName;
        attribute.Value = value;
        OdfAttribute[] attributes =
            new OdfAttribute[node.Attributes.Length + 1];
        Array.Copy(node.Attributes, attributes, node.Attributes.Length);
        attributes[attributes.Length - 1] = attribute;
        node.Attributes = attributes;
    }

    static string ReadExtensionAttribute(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName.Length == 0
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static long ReadExtensionLong(
        OdfNode node,
        string localName,
        long defaultValue)
    {
        OdfLexicalValue value = OdfLexicalParser.ParseInteger(
            ReadExtensionAttribute(node, localName));
        return value.IsValid ? value.IntegerValue : defaultValue;
    }

    static int ReadExtensionInteger(
        OdfNode node,
        string localName,
        int defaultValue)
    {
        long value = ReadExtensionLong(node, localName, defaultValue);
        if (value < int.MinValue || value > int.MaxValue)
        {
            return defaultValue;
        }
        return (int)value;
    }

    static OdfNode? FindExtensionChild(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsExtensionElement(node.Children[index], localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FirstElementChild(OdfNode node)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind == OdfNodeKind.Element)
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsExtensionElement(OdfNode node, string localName)
    {
        return node != null
            && node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Collaboration
            && node.Name.LocalName == localName;
    }

    static void AppendExtensionChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        parent.Children = children;
        child.Parent = parent;
        parent.WasEmptyElement = false;
    }

    static OdfNode CloneExtensionNode(OdfNode node)
    {
        return new OdfTreeEditor([]).CloneSubtree(node);
    }
}
