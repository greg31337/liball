using System;
using System.Collections.Generic;
using System.IO;

namespace odf;

internal class StreamingNodeFrame
{
    internal OdfNode Node = new OdfNode();
    internal List<OdfNode> Children = new List<OdfNode>();
    internal bool PreserveWhitespace;
}

internal static class StreamingNodeMaterializer
{
    internal static OdfNode[] MaterializeSelectedSubtrees(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        ValidateXmlInputs(input, partPath, options);
        OdfLoadOptions readerOptions = CreateXmlOptions(options);
        XmlEventReader reader = new XmlEventReader(
            input,
            partPath,
            readerOptions);
        return ReadSelectedNodes(reader);
    }

    internal static OdfNode[] LoadSelectedPackageParts(
        Stream input,
        OdfLoadOptions options)
    {
        ValidatePackageInputs(input, options);
        OdfLoadOptions packageOptions = CreatePackageOptions(options);
        PackageEventReader packageReader = new PackageEventReader(
            input,
            packageOptions);
        List<OdfNode> result = new List<OdfNode>();
        OdfStreamEvent packageEvent = packageReader.ReadNextEvent();
        while (packageEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (packageEvent.Kind == OdfStreamEventKind.PackageEntryStart)
            {
                if (IsXmlPartPath(packageEvent.PartPath))
                {
                    byte[] content = packageReader.ReadCurrentEntryBytes();
                    AddPackagePartNodes(
                        result,
                        content,
                        packageEvent.PartPath,
                        options);
                }
                else
                {
                    packageReader.SkipCurrentEntry();
                }
            }
            packageEvent = packageReader.ReadNextEvent();
        }
        return result.ToArray();
    }

    static OdfNode[] ReadSelectedNodes(XmlEventReader reader)
    {
        List<OdfNode> roots = new List<OdfNode>();
        List<StreamingNodeFrame> frames =
            new List<StreamingNodeFrame>();
        OdfStreamEvent streamEvent = reader.ReadNextEvent();
        while (streamEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (streamEvent.Kind == OdfStreamEventKind.ElementStart)
            {
                StartMaterializedElement(streamEvent, roots, frames);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.ElementEnd)
            {
                FinishMaterializedElement(streamEvent, frames);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.Text
                || streamEvent.Kind == OdfStreamEventKind.Comment
                || streamEvent.Kind
                    == OdfStreamEventKind.ProcessingInstruction)
            {
                AddMaterializedScalar(streamEvent, roots, frames);
            }
            streamEvent = reader.ReadNextEvent();
        }
        if (frames.Count != 0)
        {
            throw new OdfXmlException(
                "The selected XML input ended before every element was closed.");
        }
        return roots.ToArray();
    }

    static void StartMaterializedElement(
        OdfStreamEvent streamEvent,
        List<OdfNode> roots,
        List<StreamingNodeFrame> frames)
    {
        OdfNode node = new OdfNode();
        node.Kind = OdfNodeKind.Element;
        node.Name = CopyRequiredName(streamEvent.Name);
        if (frames.Count == 0 && streamEvent.Depth > 0)
        {
            node.NamespaceDeclarations =
                CopyPromotedNamespaces(streamEvent.NamespaceScope);
        }
        else
        {
            node.NamespaceDeclarations = CopyNamespaces(
                streamEvent.NamespaceDeclarations);
        }
        node.Attributes = CopyAttributes(streamEvent.Attributes);
        node.XmlId = FindXmlId(node.Attributes);
        node.Location = CopyLocation(streamEvent.Location);
        if (frames.Count > 0)
        {
            node.Location.XmlId =
                frames[frames.Count - 1].Node.Location.XmlId;
        }
        if (node.XmlId.Length > 0)
        {
            node.Location.XmlId = node.XmlId;
        }
        ApplyContextualXmlId(node);
        node.WasEmptyElement = streamEvent.IsEmptyElement;
        AttachNode(node, roots, frames);

        StreamingNodeFrame frame = new StreamingNodeFrame();
        frame.Node = node;
        if (frames.Count > 0)
        {
            frame.PreserveWhitespace =
                frames[frames.Count - 1].PreserveWhitespace;
        }
        frame.PreserveWhitespace = ReadWhitespaceMode(
            node.Attributes,
            frame.PreserveWhitespace);
        frames.Add(frame);
    }

    static void FinishMaterializedElement(
        OdfStreamEvent streamEvent,
        List<StreamingNodeFrame> frames)
    {
        if (frames.Count == 0)
        {
            throw new OdfXmlException(
                "The selected XML input contains an unmatched closing element.");
        }
        int index = frames.Count - 1;
        StreamingNodeFrame frame = frames[index];
        if (!NamesEqual(frame.Node.Name, streamEvent.Name))
        {
            throw new OdfXmlException(
                "The selected XML input contains mismatched element boundaries.");
        }
        frame.Node.Children = frame.Children.ToArray();
        frames.RemoveAt(index);
    }

    static void AddMaterializedScalar(
        OdfStreamEvent streamEvent,
        List<OdfNode> roots,
        List<StreamingNodeFrame> frames)
    {
        OdfNode node = new OdfNode();
        node.Kind = ScalarNodeKind(streamEvent.Kind);
        node.Value = streamEvent.Value;
        node.Location = CopyLocation(streamEvent.Location);
        if (frames.Count > 0)
        {
            StreamingNodeFrame frame = frames[frames.Count - 1];
            node.Location.XmlId = frame.Node.Location.XmlId;
            if (node.Kind == OdfNodeKind.Text
                && IsWhitespaceValue(node.Value))
            {
                node.IsSignificantWhitespace =
                    streamEvent.IsSignificantWhitespace
                    || frame.PreserveWhitespace;
            }
        }
        if (node.Kind == OdfNodeKind.ProcessingInstruction)
        {
            node.Name = CopyRequiredName(streamEvent.Name);
        }
        AttachNode(node, roots, frames);
    }

    static void AttachNode(
        OdfNode node,
        List<OdfNode> roots,
        List<StreamingNodeFrame> frames)
    {
        if (frames.Count == 0)
        {
            roots.Add(node);
            return;
        }
        StreamingNodeFrame parent = frames[frames.Count - 1];
        node.Parent = parent.Node;
        parent.Children.Add(node);
    }

    static OdfNodeKind ScalarNodeKind(OdfStreamEventKind kind)
    {
        if (kind == OdfStreamEventKind.Comment)
        {
            return OdfNodeKind.Comment;
        }
        if (kind == OdfStreamEventKind.ProcessingInstruction)
        {
            return OdfNodeKind.ProcessingInstruction;
        }
        return OdfNodeKind.Text;
    }

    static bool ReadWhitespaceMode(
        OdfAttribute[] attributes,
        bool inheritedMode)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                && attribute.Name.LocalName == "space")
            {
                if (attribute.Value == "preserve")
                {
                    return true;
                }
                if (attribute.Value == "default")
                {
                    return false;
                }
            }
            index = index + 1;
        }
        return inheritedMode;
    }

    static bool IsWhitespaceValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!char.IsWhiteSpace(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    static bool NamesEqual(
        OdfQualifiedName? left,
        OdfQualifiedName? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return left.NamespaceName == right.NamespaceName
            && left.LocalName == right.LocalName;
    }

    static string FindXmlId(OdfAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                && attribute.Name.LocalName == "id")
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static void ApplyContextualXmlId(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            node.Attributes[index].Location.XmlId = node.Location.XmlId;
            index = index + 1;
        }
        index = 0;
        while (index < node.NamespaceDeclarations.Length)
        {
            node.NamespaceDeclarations[index].Location.XmlId =
                node.Location.XmlId;
            index = index + 1;
        }
    }

    static OdfQualifiedName CopyRequiredName(OdfQualifiedName? source)
    {
        if (source == null)
        {
            throw new OdfXmlException(
                "A selected XML name is required for this event.");
        }
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static OdfAttribute[] CopyAttributes(OdfAttribute[] source)
    {
        OdfAttribute[] result = new OdfAttribute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfAttribute attribute = new OdfAttribute();
            attribute.Name = CopyRequiredName(source[index].Name);
            attribute.Value = source[index].Value;
            attribute.Location = CopyLocation(source[index].Location);
            result[index] = attribute;
            index = index + 1;
        }
        return result;
    }

    static OdfNamespaceDeclaration[] CopyNamespaces(
        OdfNamespaceDeclaration[] source)
    {
        OdfNamespaceDeclaration[] result =
            new OdfNamespaceDeclaration[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyNamespace(source[index]);
            index = index + 1;
        }
        return result;
    }

    static OdfNamespaceDeclaration[] CopyPromotedNamespaces(
        OdfNamespaceDeclaration[] source)
    {
        List<OdfNamespaceDeclaration> result =
            new List<OdfNamespaceDeclaration>();
        int index = 0;
        while (index < source.Length)
        {
            if (source[index].Prefix != OdfNamespaces.XmlPrefix)
            {
                result.Add(CopyNamespace(source[index]));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfNamespaceDeclaration CopyNamespace(
        OdfNamespaceDeclaration source)
    {
        OdfNamespaceDeclaration result = new OdfNamespaceDeclaration();
        result.Prefix = source.Prefix;
        result.NamespaceName = source.NamespaceName;
        result.Location = CopyLocation(source.Location);
        return result;
    }

    static OdfLocation CopyLocation(OdfLocation source)
    {
        OdfLocation result = new OdfLocation();
        result.PartPath = source.PartPath;
        result.NodePath = source.NodePath;
        result.XmlId = source.XmlId;
        result.Line = source.Line;
        result.Column = source.Column;
        result.ByteOffset = source.ByteOffset;
        result.TextOffset = source.TextOffset;
        result.TextLength = source.TextLength;
        return result;
    }

    static void AddPackagePartNodes(
        List<OdfNode> result,
        byte[] content,
        string partPath,
        OdfLoadOptions options)
    {
        OdfLoadOptions xmlOptions = CreateXmlOptions(options);
        xmlOptions.LeaveInputOpen = true;
        using (MemoryStream input = new MemoryStream(content, false))
        {
            OdfNode[] nodes = MaterializeSelectedSubtrees(
                input,
                partPath,
                xmlOptions);
            int index = 0;
            while (index < nodes.Length)
            {
                result.Add(nodes[index]);
                index = index + 1;
            }
        }
    }

    static bool IsXmlPartPath(string partPath)
    {
        return partPath.EndsWith(
            ".xml",
            StringComparison.OrdinalIgnoreCase);
    }

    static OdfLoadOptions CreateXmlOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = CopyBaseOptions(source);
        result.StreamSelection = CopySelection(source.StreamSelection);
        result.StreamSelection.IncludeElementEvents = true;
        result.StreamSelection.IncludeTextEvents = true;
        result.StreamSelection.IncludeCommentEvents = true;
        result.StreamSelection.IncludeProcessingInstructionEvents = true;
        return result;
    }

    static OdfLoadOptions CreatePackageOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = CopyBaseOptions(source);
        result.StreamSelection = CopySelection(source.StreamSelection);
        result.StreamSelection.IncludePackageEntryEvents = true;
        result.StreamSelection.IncludeBinaryContent = false;
        return result;
    }

    static OdfLoadOptions CopyBaseOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = new OdfLoadOptions();
        result.SecurityLimits = source.SecurityLimits;
        result.Cancellation = source.Cancellation;
        result.LeaveInputOpen = source.LeaveInputOpen;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveUnknownContent = source.PreserveUnknownContent;
        result.MaterializeXmlTrees = source.MaterializeXmlTrees;
        result.IndexPackageEntries = source.IndexPackageEntries;
        result.ValidateOnLoad = source.ValidateOnLoad;
        result.AllowRecovery = source.AllowRecovery;
        return result;
    }

    static OdfStreamSelection CopySelection(OdfStreamSelection source)
    {
        OdfStreamSelection result = new OdfStreamSelection();
        result.PartPaths = CopyStrings(source.PartPaths);
        result.ElementNames = CopyNames(source.ElementNames);
        result.IncludePackageEntryEvents = source.IncludePackageEntryEvents;
        result.IncludeElementEvents = source.IncludeElementEvents;
        result.IncludeTextEvents = source.IncludeTextEvents;
        result.IncludeCommentEvents = source.IncludeCommentEvents;
        result.IncludeProcessingInstructionEvents =
            source.IncludeProcessingInstructionEvents;
        result.IncludeBinaryContent = source.IncludeBinaryContent;
        result.SkipUnselectedSubtrees = source.SkipUnselectedSubtrees;
        return result;
    }

    static string[] CopyStrings(string[] source)
    {
        if (source == null)
        {
            return [];
        }
        string[] result = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    static OdfQualifiedName[] CopyNames(OdfQualifiedName[] source)
    {
        if (source == null)
        {
            return [];
        }
        List<OdfQualifiedName> result = new List<OdfQualifiedName>();
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] != null)
            {
                result.Add(CopyRequiredName(source[index]));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ValidateXmlInputs(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead || partPath == null
            || options == null || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "A readable XML stream, part path, and load options are required.");
        }
    }

    static void ValidatePackageInputs(
        Stream input,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead || options == null
            || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "A readable package stream and load options are required.");
        }
    }
}
