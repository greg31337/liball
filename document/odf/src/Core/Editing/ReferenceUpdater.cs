using System.Collections.Generic;
using System.Text;

namespace odf;

internal class ReferenceIdentifierTarget
{
    internal OdfNode? Node;
    internal OdfAttribute? Attribute;
    internal int Count;
}

internal static class ReferenceUpdater
{
    internal static OdfEditResult RenameIdentifier(
        OdfDocument document,
        string oldIdentifier,
        string newIdentifier)
    {
        RequireRenameInput(document, oldIdentifier, newIdentifier);
        if (!OdfNameRules.IsXmlId(newIdentifier))
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The replacement identifier is not a valid XML ID.",
                "");
        }
        ReferenceIdentifierTarget target = FindIdentifierTarget(
            document,
            oldIdentifier);
        if (target.Count != 1
            || target.Node == null
            || target.Attribute == null)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                target.Count == 0
                    ? "The source identifier does not exist."
                    : "The source identifier is not unique.",
                "");
        }
        if (oldIdentifier == newIdentifier)
        {
            return Result(
                OdfEditStatus.NoChange,
                false,
                0,
                "The identifier already has the requested value.",
                target.Node.Location.PartPath);
        }
        if (new CrossPartQueryIndex(document)
                .FindIds(newIdentifier).Length > 0)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The replacement identifier is already in use.",
                target.Node.Location.PartPath);
        }
        target.Attribute.Value = newIdentifier;
        if (target.Attribute.Name.NamespaceName == OdfNamespaces.Xml)
        {
            target.Node.XmlId = newIdentifier;
            target.Node.Location.XmlId = newIdentifier;
        }
        int updatedReferences = RewriteDocumentIdentifierReferences(
            document,
            oldIdentifier,
            newIdentifier);
        RefreshDocument(document);
        return Result(
            OdfEditStatus.Success,
            true,
            updatedReferences + 1,
            "Renamed the identifier and updated its references.",
            target.Node.Location.PartPath);
    }

    internal static void PrepareCopiedSubtree(
        OdfDocument document,
        OdfNode subtree)
    {
        if (document == null || subtree == null)
        {
            throw new OdfEditException(
                "A document and copied subtree are required.");
        }
        List<string> oldIdentifiers = new List<string>();
        List<string> newIdentifiers = new List<string>();
        List<string> assignedIdentifiers = new List<string>();
        List<string> assignedNames = new List<string>();
        AssignCollisionSafeValues(
            document,
            subtree,
            oldIdentifiers,
            newIdentifiers,
            assignedIdentifiers,
            assignedNames);
        RemapSubtreeIdentifierReferences(
            subtree,
            oldIdentifiers,
            newIdentifiers);
    }

    internal static OdfEditResult MoveResource(
        OdfDocument document,
        string sourcePath,
        string destinationPath)
    {
        return DrawingResourceManager.MovePackageResource(
            document,
            sourcePath,
            destinationPath);
    }

    internal static OdfEditResult RenameStyle(
        OdfDocument document,
        OdfStyle style,
        string newName)
    {
        if (style == null || newName == null || newName.Length == 0)
        {
            throw new OdfEditException(
                "A style and non-empty replacement name are required.");
        }
        if (style.Name == newName)
        {
            return Result(
                OdfEditStatus.NoChange,
                false,
                0,
                "The style already has the requested name.",
                style.PartPath);
        }
        StyleMaintenanceModel.RenameStyle(document, style, newName);
        return Result(
            OdfEditStatus.Success,
            true,
            1,
            "Renamed the style and updated its references.",
            style.PartPath);
    }

    internal static OdfEditResult RenameSheet(
        OdfDocument document,
        OdfTable sheet,
        string newName)
    {
        if (sheet == null || newName == null || newName.Length == 0)
        {
            throw new OdfEditException(
                "A sheet and non-empty replacement name are required.");
        }
        if (sheet.Name == newName)
        {
            return Result(
                OdfEditStatus.NoChange,
                false,
                0,
                "The sheet already has the requested name.",
                sheet.PartPath);
        }
        SpreadsheetEditorModel.RenameSheet(document, sheet, newName);
        return Result(
            OdfEditStatus.Success,
            true,
            1,
            "Renamed the sheet and updated its references.",
            sheet.PartPath);
    }

    static ReferenceIdentifierTarget FindIdentifierTarget(
        OdfDocument document,
        string identifier)
    {
        ReferenceIdentifierTarget result = new ReferenceIdentifierTarget();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = document.Parts[partIndex].Nodes;
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                FindIdentifierInNode(
                    nodes[nodeIndex],
                    identifier,
                    result);
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        return result;
    }

    static void FindIdentifierInNode(
        OdfNode node,
        string identifier,
        ReferenceIdentifierTarget result)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsIdentifierAttribute(attribute)
                && attribute.Value == identifier)
            {
                result.Node = node;
                result.Attribute = attribute;
                result.Count = result.Count + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            FindIdentifierInNode(
                node.Children[index],
                identifier,
                result);
            index = index + 1;
        }
    }

    static int RewriteDocumentIdentifierReferences(
        OdfDocument document,
        string oldIdentifier,
        string newIdentifier)
    {
        int result = 0;
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = document.Parts[partIndex].Nodes;
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                result = result + RewriteNodeIdentifierReferences(
                    nodes[nodeIndex],
                    oldIdentifier,
                    newIdentifier);
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        return result;
    }

    static int RewriteNodeIdentifierReferences(
        OdfNode node,
        string oldIdentifier,
        string newIdentifier)
    {
        int result = 0;
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            string replacement = RewrittenReferenceValue(
                attribute,
                oldIdentifier,
                newIdentifier);
            if (replacement != attribute.Value)
            {
                attribute.Value = replacement;
                result = result + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            result = result + RewriteNodeIdentifierReferences(
                node.Children[index],
                oldIdentifier,
                newIdentifier);
            index = index + 1;
        }
        return result;
    }

    static void AssignCollisionSafeValues(
        OdfDocument document,
        OdfNode node,
        List<string> oldIdentifiers,
        List<string> newIdentifiers,
        List<string> assignedIdentifiers,
        List<string> assignedNames)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsIdentifierAttribute(attribute)
                && attribute.Value.Length > 0)
            {
                string oldValue = attribute.Value;
                string newValue = DeterministicNameModel
                    .CreateReservedIdentifier(
                        document,
                        oldValue,
                        assignedIdentifiers);
                attribute.Value = newValue;
                if (attribute.Name.NamespaceName == OdfNamespaces.Xml)
                {
                    node.XmlId = newValue;
                    node.Location.XmlId = newValue;
                }
                oldIdentifiers.Add(oldValue);
                newIdentifiers.Add(newValue);
                assignedIdentifiers.Add(newValue);
            }
            else if (IsDrawingNameAttribute(attribute)
                && attribute.Value.Length > 0)
            {
                string newName = DeterministicNameModel.CreateReservedName(
                    document,
                    attribute.Value,
                    assignedNames);
                attribute.Value = newName;
                assignedNames.Add(newName);
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            AssignCollisionSafeValues(
                document,
                node.Children[index],
                oldIdentifiers,
                newIdentifiers,
                assignedIdentifiers,
                assignedNames);
            index = index + 1;
        }
    }

    static void RemapSubtreeIdentifierReferences(
        OdfNode node,
        List<string> oldIdentifiers,
        List<string> newIdentifiers)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            int mapping = 0;
            while (mapping < oldIdentifiers.Count)
            {
                string replacement = RewrittenReferenceValue(
                    attribute,
                    oldIdentifiers[mapping],
                    newIdentifiers[mapping]);
                if (replacement != attribute.Value)
                {
                    attribute.Value = replacement;
                    break;
                }
                mapping = mapping + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            RemapSubtreeIdentifierReferences(
                node.Children[index],
                oldIdentifiers,
                newIdentifiers);
            index = index + 1;
        }
    }

    static string RewrittenReferenceValue(
        OdfAttribute attribute,
        string oldIdentifier,
        string newIdentifier)
    {
        if (attribute.Name.NamespaceName == OdfNamespaces.XLink
            && attribute.Name.LocalName == "href"
            && attribute.Value == "#" + oldIdentifier)
        {
            return "#" + newIdentifier;
        }
        if (IsDirectIdentifierReference(attribute))
        {
            if (attribute.Value == oldIdentifier)
            {
                return newIdentifier;
            }
            if (attribute.Value == "#" + oldIdentifier)
            {
                return "#" + newIdentifier;
            }
        }
        if (attribute.Name.NamespaceName == OdfNamespaces.Drawing
            && attribute.Name.LocalName == "nav-order")
        {
            return ReplaceIdentifierToken(
                attribute.Value,
                oldIdentifier,
                newIdentifier);
        }
        return attribute.Value;
    }

    static string ReplaceIdentifierToken(
        string value,
        string oldIdentifier,
        string newIdentifier)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (global::System.Char.IsWhiteSpace(value[index]))
            {
                result.Append(value[index]);
                index = index + 1;
            }
            else
            {
                int start = index;
                while (index < value.Length
                    && !global::System.Char.IsWhiteSpace(value[index]))
                {
                    index = index + 1;
                }
                string token = value.Substring(start, index - start);
                result.Append(token == oldIdentifier
                    ? newIdentifier : token);
            }
        }
        return result.ToString();
    }

    static bool IsIdentifierAttribute(OdfAttribute attribute)
    {
        return attribute.Name.LocalName == "id"
            && (attribute.Name.NamespaceName == OdfNamespaces.Xml
                || attribute.Name.NamespaceName == OdfNamespaces.Drawing);
    }

    static bool IsDrawingNameAttribute(OdfAttribute attribute)
    {
        return attribute.Name.NamespaceName == OdfNamespaces.Drawing
            && attribute.Name.LocalName == "name";
    }

    static bool IsDirectIdentifierReference(OdfAttribute attribute)
    {
        if (attribute.Name.NamespaceName == OdfNamespaces.Smil
            && attribute.Name.LocalName == "targetElement")
        {
            return true;
        }
        if (attribute.Name.NamespaceName != OdfNamespaces.Drawing)
        {
            return false;
        }
        string name = attribute.Name.LocalName;
        return name == "copy-of"
            || name == "caption-id"
            || name == "start-shape"
            || name == "end-shape";
    }

    static void RefreshDocument(OdfDocument document)
    {
        OdfDocumentModel.ApplyContext(
            document,
            document.LoadOptions,
            document.SaveOptions,
            document.MediaType);
    }

    static void RequireRenameInput(
        OdfDocument document,
        string oldIdentifier,
        string newIdentifier)
    {
        if (document == null
            || oldIdentifier == null
            || oldIdentifier.Length == 0
            || newIdentifier == null
            || newIdentifier.Length == 0)
        {
            throw new OdfEditException(
                "A document and two non-empty identifiers are required.");
        }
    }

    static OdfEditResult Result(
        OdfEditStatus status,
        bool changed,
        int affectedCount,
        string message,
        string path)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affectedCount;
        result.Message = message;
        result.Location.PartPath = path;
        return result;
    }
}
