using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace odf;

internal class OdfTreeReadFrame
{
    internal OdfNode Node;
    internal OdfNamespaceScope Scope;
    internal List<OdfNode> Children = new List<OdfNode>();

    internal OdfTreeReadFrame(
        OdfNode node,
        OdfNamespaceScope scope)
    {
        Node = node;
        Scope = scope;
    }
}

internal static class OdfTreeBuilder
{
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static OdfNode[] ReadBytes(
        byte[] data,
        string partPath,
        OdfLoadOptions options)
    {
        ValidateInputs(data, partPath, options);
        List<OdfNode> roots = new List<OdfNode>();
        List<OdfTreeReadFrame> frames = new List<OdfTreeReadFrame>();
        using (MemoryStream input = new MemoryStream(data, false))
        using (XmlReader reader = SecureXmlReader.Create(
            input,
            options.SecurityLimits,
            true))
        {
            while (SecureXmlReader.Read(reader, options.SecurityLimits))
            {
                CheckCancellation(options);
                if (reader.NodeType == XmlNodeType.Element)
                {
                    StartElement(reader, partPath, roots, frames);
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    FinishElement(frames);
                }
                else if (reader.NodeType == XmlNodeType.Text
                    || reader.NodeType == XmlNodeType.CDATA
                    || reader.NodeType == XmlNodeType.Whitespace
                    || reader.NodeType == XmlNodeType.SignificantWhitespace
                    || reader.NodeType == XmlNodeType.Comment
                    || reader.NodeType == XmlNodeType.ProcessingInstruction)
                {
                    AddScalar(reader, partPath, roots, frames);
                }
            }
        }
        if (frames.Count != 0)
        {
            throw new OdfXmlException("The XML input ended before every element was closed.");
        }
        return roots.ToArray();
    }

    static void StartElement(
        XmlReader reader,
        string partPath,
        List<OdfNode> roots,
        List<OdfTreeReadFrame> frames)
    {
        OdfNamespaceScope? parentScope = null;
        if (frames.Count > 0)
        {
            parentScope = frames[frames.Count - 1].Scope;
        }
        OdfNamespaceScope scope = new OdfNamespaceScope(parentScope, true);
        OdfNamespaceDeclaration[] declarations = CollectDeclarations(
            reader,
            partPath,
            scope);
        OdfQualifiedName name = scope.ResolveQualifiedName(reader.Name, true);
        if (name.NamespaceName != reader.NamespaceURI)
        {
            throw new OdfXmlException("The element namespace does not match its in-scope prefix binding.");
        }
        OdfAttribute[] attributes = CollectAttributes(reader, partPath, scope);
        string xmlId = "";
        foreach (OdfAttribute attribute in attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                && attribute.Name.LocalName == "id")
            {
                xmlId = attribute.Value;
            }
        }

        OdfLocation location = CreateLocation(reader, partPath, "");
        if (frames.Count > 0)
        {
            location.XmlId = frames[frames.Count - 1].Node.Location.XmlId;
        }
        if (xmlId.Length > 0)
        {
            location.XmlId = xmlId;
        }
        foreach (OdfAttribute attribute in attributes)
        {
            attribute.Location.XmlId = location.XmlId;
        }
        foreach (OdfNamespaceDeclaration declaration in declarations)
        {
            declaration.Location.XmlId = location.XmlId;
        }

        OdfNode node = new OdfNode
        {
            Name = name,
            Kind = OdfNodeKind.Element,
            NamespaceDeclarations = declarations,
            Attributes = attributes,
            XmlId = xmlId,
            Location = location,
            WasEmptyElement = reader.IsEmptyElement
        };
        AttachNode(node, roots, frames);
        if (!reader.IsEmptyElement)
        {
            frames.Add(new OdfTreeReadFrame(node, scope));
        }
    }

    static void FinishElement(List<OdfTreeReadFrame> frames)
    {
        if (frames.Count == 0)
        {
            throw new OdfXmlException("The XML input contains an unmatched closing element.");
        }
        int index = frames.Count - 1;
        OdfTreeReadFrame frame = frames[index];
        frame.Node.Children = frame.Children.ToArray();
        frames.RemoveAt(index);
    }

    static void AddScalar(
        XmlReader reader,
        string partPath,
        List<OdfNode> roots,
        List<OdfTreeReadFrame> frames)
    {
        OdfNodeKind kind = OdfNodeKind.Text;
        if (reader.NodeType == XmlNodeType.Comment)
        {
            kind = OdfNodeKind.Comment;
        }
        else if (reader.NodeType == XmlNodeType.ProcessingInstruction)
        {
            kind = OdfNodeKind.ProcessingInstruction;
        }

        string contextualXmlId = "";
        if (frames.Count > 0)
        {
            contextualXmlId = frames[frames.Count - 1].Node.Location.XmlId;
        }
        OdfLocation location = CreateLocation(reader, partPath, contextualXmlId);
        location.TextLength = reader.Value.Length;
        OdfNode node = new OdfNode
        {
            Kind = kind,
            Value = reader.Value,
            Location = location
        };
        if (kind == OdfNodeKind.ProcessingInstruction)
        {
            node.Name = new OdfQualifiedName
            {
                LocalName = reader.Name
            };
        }
        if (kind == OdfNodeKind.Text
            && IsWhitespaceValue(reader.Value)
            && (reader.NodeType == XmlNodeType.SignificantWhitespace
                || reader.XmlSpace == XmlSpace.Preserve))
        {
            node.IsSignificantWhitespace = true;
        }
        AttachNode(node, roots, frames);
    }

    static void AttachNode(
        OdfNode node,
        List<OdfNode> roots,
        List<OdfTreeReadFrame> frames)
    {
        if (frames.Count == 0)
        {
            roots.Add(node);
            return;
        }
        OdfTreeReadFrame parent = frames[frames.Count - 1];
        node.Parent = parent.Node;
        parent.Children.Add(node);
    }

    static OdfNamespaceDeclaration[] CollectDeclarations(
        XmlReader reader,
        string partPath,
        OdfNamespaceScope scope)
    {
        List<OdfNamespaceDeclaration> result = new List<OdfNamespaceDeclaration>();
        int index = 0;
        while (index < reader.AttributeCount)
        {
            reader.MoveToAttribute(index);
            if (IsNamespaceAttribute(reader))
            {
                string prefix = "";
                if (reader.Prefix == "xmlns")
                {
                    prefix = reader.LocalName;
                }
                OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration
                {
                    Prefix = prefix,
                    NamespaceName = reader.Value,
                    Location = CreateLocation(reader, partPath, "")
                };
                scope.AddDeclaration(declaration);
                result.Add(declaration);
            }
            index++;
        }
        reader.MoveToElement();
        return result.ToArray();
    }

    static OdfAttribute[] CollectAttributes(
        XmlReader reader,
        string partPath,
        OdfNamespaceScope scope)
    {
        List<OdfAttribute> result = new List<OdfAttribute>();
        int index = 0;
        while (index < reader.AttributeCount)
        {
            reader.MoveToAttribute(index);
            if (!IsNamespaceAttribute(reader))
            {
                OdfQualifiedName name = scope.ResolveQualifiedName(reader.Name, false);
                if (name.NamespaceName != reader.NamespaceURI)
                {
                    throw new OdfXmlException("The attribute namespace does not match its in-scope prefix binding.");
                }
                result.Add(new OdfAttribute
                {
                    Name = name,
                    Value = reader.Value,
                    Location = CreateLocation(reader, partPath, "")
                });
            }
            index++;
        }
        reader.MoveToElement();
        return result.ToArray();
    }

    static OdfLocation CreateLocation(
        XmlReader reader,
        string partPath,
        string xmlId)
    {
        OdfLocation result = new OdfLocation
        {
            PartPath = partPath,
            XmlId = xmlId
        };
        IXmlLineInfo lineInfo = (IXmlLineInfo)reader;
        if (lineInfo.HasLineInfo())
        {
            result.Line = lineInfo.LineNumber;
            result.Column = lineInfo.LinePosition;
        }
        return result;
    }

    static bool IsNamespaceAttribute(XmlReader reader)
    {
        return reader.NamespaceURI == XmlnsNamespace;
    }

    static bool IsWhitespaceValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' '
                && character != '\t'
                && character != '\r'
                && character != '\n')
            {
                return false;
            }
            index++;
        }
        return value.Length > 0;
    }

    static void CheckCancellation(OdfLoadOptions options)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfXmlException("XML tree loading was cancelled.");
        }
    }

    static void ValidateInputs(
        byte[] data,
        string partPath,
        OdfLoadOptions options)
    {
        if (data == null || partPath == null || options == null)
        {
            throw new OdfXmlException("XML bytes, a part path, and load options are required.");
        }
        if (options.SecurityLimits == null)
        {
            throw new OdfXmlException("XML security limits are required.");
        }
        if (data.LongLength > options.SecurityLimits.MaximumSingleEntryBytes)
        {
            throw new OdfXmlException("The XML input exceeds the configured part-size limit.");
        }
        CheckCancellation(options);
    }
}
