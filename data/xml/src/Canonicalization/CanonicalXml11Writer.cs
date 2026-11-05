using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class CanonicalNamespaceValue
{
    internal string Prefix;
    internal string NamespaceUri;

    internal CanonicalNamespaceValue(string prefix, string namespaceUri)
    {
        Prefix = prefix;
        NamespaceUri = namespaceUri;
    }
}

internal class CanonicalAttributeValue
{
    internal string QualifiedName;
    internal string NamespaceUri;
    internal string LocalName;
    internal string Value;

    internal CanonicalAttributeValue(string qualifiedName, string namespaceUri, string localName, string value)
    {
        QualifiedName = qualifiedName;
        NamespaceUri = namespaceUri;
        LocalName = localName;
        Value = value;
    }
}

internal class CanonicalXml11Context
{
    internal List<CanonicalNamespaceValue> Namespaces;
    internal string? SyntheticBase;

    internal CanonicalXml11Context(List<CanonicalNamespaceValue> namespaces, string? syntheticBase)
    {
        Namespaces = namespaces;
        SyntheticBase = syntheticBase;
    }
}

internal class CanonicalOutputBuffer
{
    internal XmlOutputWriter Writer;
    internal StringBuilder Characters;
    internal int MaximumCharacterCount;

    internal CanonicalOutputBuffer(XmlOutputWriter writer, int maximumCharacterCount)
    {
        Guard.NotNull(writer, "writer");
        Guard.Positive(maximumCharacterCount, "maximumCharacterCount");
        Writer = writer;
        Characters = new StringBuilder(maximumCharacterCount);
        MaximumCharacterCount = maximumCharacterCount;
    }

    internal void AppendCanonicalText(string value)
    {
        Guard.NotNull(value, "value");
        int position = 0;
        while (position < value.Length)
        {
            int available = MaximumCharacterCount - Characters.Length;
            if (available == 0)
            {
                FlushCanonicalText();
                available = MaximumCharacterCount;
            }

            int remaining = value.Length - position;
            int count = remaining < available ? remaining : available;
            if (position + count < value.Length && char.IsHighSurrogate(value[position + count - 1]))
            {
                count = count - 1;
            }

            if (count == 0)
            {
                FlushCanonicalText();
                continue;
            }

            Characters.Append(value, position, count);
            position = position + count;
        }
    }

    internal void AppendCanonicalCharacter(char value)
    {
        if (Characters.Length == MaximumCharacterCount)
        {
            FlushCanonicalText();
        }

        Characters.Append(value);
    }

    internal void AppendCanonicalSegment(string value, int start, int count)
    {
        if (count > MaximumCharacterCount)
        {
            AppendCanonicalText(value.Substring(start, count));
            return;
        }

        if (Characters.Length + count > MaximumCharacterCount)
        {
            FlushCanonicalText();
        }

        Characters.Append(value, start, count);
    }

    internal void FlushCanonicalText()
    {
        if (Characters.Length == 0)
        {
            return;
        }

        string value = Characters.ToString();
        Characters.Clear();
        Writer.WriteXmlOutputText(value);
    }

    internal void CompleteCanonicalOutput()
    {
        FlushCanonicalText();
        Writer.CompleteXmlOutput();
    }
}

internal class CanonicalXml11Writer
{
    internal XmlDocument Document;
    internal XmlCanonicalizationOptions Options;
    internal bool FullDocument;
    internal bool ExclusiveXml10;
    internal bool ExperimentalXml20;
    internal List<string> InclusivePrefixes;
    internal List<XmlNode> SelectedNodes;
    internal CanonicalOutputBuffer Result;
    internal TreeNavigation Navigation;

    internal CanonicalXml11Writer(XmlDocument document, XmlCanonicalizationOptions options, XmlOutputWriter output)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(options, "options");
        Document = document;
        Options = options;
        FullDocument = options.Selection == null;
        ExclusiveXml10 = options.Method != XmlCanonicalizationMethod.CanonicalXml11;
        ExperimentalXml20 = options.Method == XmlCanonicalizationMethod.CanonicalXml20Experimental;
        InclusivePrefixes = new List<string>();
        SelectedNodes = new List<XmlNode>();
        Result = new CanonicalOutputBuffer(output, 1024);
        Navigation = new TreeNavigation();
        PrepareExclusiveXml10Prefixes();
        PrepareCanonicalXml11Selection();
    }

    internal void WriteCanonicalDocument()
    {
        XmlNode? root = Document.DocumentNode;
        if (root == null)
        {
            throw CreateCanonicalXml11Error("The document has no document node.");
        }

        List<CanonicalNamespaceValue> initialNamespaces = new List<CanonicalNamespaceValue>();
        initialNamespaces.Add(new CanonicalNamespaceValue("", ""));
        CanonicalXml11Context context = new CanonicalXml11Context(initialNamespaces, null);
        WriteCanonicalXml11Node(root, context);
        Result.CompleteCanonicalOutput();
    }

    internal void PrepareExclusiveXml10Prefixes()
    {
        if (!ExclusiveXml10)
        {
            return;
        }

        string[] prefixes = Options.InclusivePrefixes == null ? [] : Options.InclusivePrefixes;
        int index = 0;
        while (index < prefixes.Length)
        {
            string? supplied = prefixes[index];
            if (supplied == null)
            {
                throw CreateCanonicalXml11Error("The inclusive namespace prefix list contains a null prefix.");
            }

            if (supplied.Length == 0)
            {
                throw CreateCanonicalXml11Error("The inclusive namespace prefix list contains an empty prefix token.");
            }

            string prefix = string.Equals(supplied, "#default", StringComparison.Ordinal) ? "" : supplied;
            if (prefix.Length > 0 && !IsExclusiveXml10Prefix(prefix))
            {
                throw CreateCanonicalXml11Error("The inclusive namespace prefix list contains an invalid prefix.");
            }

            if (!ContainsCanonicalXml11String(InclusivePrefixes, prefix))
            {
                InclusivePrefixes.Add(prefix);
            }

            index = index + 1;
        }

        if (ExperimentalXml20 && Options.PropagateDefaultNamespace && !ContainsCanonicalXml11String(InclusivePrefixes, ""))
        {
            InclusivePrefixes.Add("");
        }
    }

    internal bool IsExclusiveXml10Prefix(string prefix)
    {
        int position = 0;
        int first = ReadCanonicalXml11Scalar(prefix, position);
        if (first == ':' || !Xml10CharacterRules.IsXml10NameStartCharacter(first))
        {
            return false;
        }

        position = position + (first > 0xFFFF ? 2 : 1);
        while (position < prefix.Length)
        {
            int scalar = ReadCanonicalXml11Scalar(prefix, position);
            if (scalar == ':' || !Xml10CharacterRules.IsXml10NameCharacter(scalar))
            {
                return false;
            }

            position = position + (scalar > 0xFFFF ? 2 : 1);
        }

        return true;
    }

    internal void PrepareCanonicalXml11Selection()
    {
        XmlNodeSelection? selection = Options.Selection;
        if (selection == null)
        {
            return;
        }

        if (selection.Document != null && selection.Document != Document)
        {
            throw CreateCanonicalXml11Error("The node selection belongs to a different document.");
        }

        if (!selection.IsFrozen && selection.MutationVersion != Document.MutationVersion)
        {
            throw CreateCanonicalXml11Error("The node selection was invalidated by a document mutation.");
        }

        XmlNode[] nodes = selection.Nodes == null ? [] : selection.Nodes;
        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode? node = nodes[index];
            if (node == null || node.OwnerDocument != null && node.OwnerDocument != Document)
            {
                throw CreateCanonicalXml11Error("The node selection contains a node from a different document.");
            }

            if (!ContainsCanonicalXml11Node(SelectedNodes, node))
            {
                SelectedNodes.Add(node);
            }

            index = index + 1;
        }
    }

    internal void WriteCanonicalXml11Node(XmlNode node, CanonicalXml11Context context)
    {
        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            WriteCanonicalXml11Children(node, context);
            return;
        }

        if (node.Kind == XmlNodeKind.DocumentType)
        {
            return;
        }

        if (node.Kind == XmlNodeKind.Element)
        {
            WriteCanonicalXml11Element(node, context);
            return;
        }

        bool visible = IsCanonicalXml11Visible(node);
        if (!visible)
        {
            if (node.Kind == XmlNodeKind.EntityReference)
            {
                WriteCanonicalXml11Children(node, context);
            }

            return;
        }

        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            WriteCanonicalXml11Text(node.Value == null ? "" : node.Value);
        }
        else if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            WriteCanonicalXml11ProcessingInstruction(node);
        }
        else if (node.Kind == XmlNodeKind.Comment && Options.IncludeComments)
        {
            WriteCanonicalXml11Comment(node);
        }
        else if (node.Kind == XmlNodeKind.EntityReference)
        {
            XmlNode[] children = node.Children == null ? [] : node.Children;
            if (children.Length == 0)
            {
                WriteCanonicalXml11Text(node.Value == null ? "" : node.Value);
            }
            else
            {
                WriteCanonicalXml11Children(node, context);
            }
        }
    }

    internal void WriteCanonicalXml11Children(XmlNode parent, CanonicalXml11Context context)
    {
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            WriteCanonicalXml11Node(children[index], context);
            index = index + 1;
        }
    }

    internal void WriteCanonicalXml11Element(XmlNode element, CanonicalXml11Context parentContext)
    {
        bool visible = IsCanonicalXml11Visible(element);
        CanonicalXml11Context context = visible ? CopyCanonicalXml11Context(parentContext) : parentContext;
        if (visible)
        {
            Result.AppendCanonicalCharacter('<');
            Result.AppendCanonicalText(CanonicalXml11NodeName(element));
        }

        List<CanonicalNamespaceValue> namespaces = CollectCanonicalXml11Namespaces(element);
        AddCanonicalXml11DefaultUndeclaration(element, namespaces, parentContext, visible);
        SortCanonicalXml11Namespaces(namespaces);
        WriteCanonicalXml11Namespaces(namespaces, context, visible);

        List<CanonicalAttributeValue> attributes = CollectCanonicalXml11Attributes(element, context, visible);
        SortCanonicalXml11Attributes(attributes);
        WriteCanonicalXml11Attributes(attributes);
        if (visible)
        {
            Result.AppendCanonicalCharacter('>');
        }

        WriteCanonicalXml11Children(element, context);
        if (visible)
        {
            Result.AppendCanonicalText("</");
            Result.AppendCanonicalText(CanonicalXml11NodeName(element));
            Result.AppendCanonicalCharacter('>');
        }
    }

    internal List<CanonicalNamespaceValue> CollectCanonicalXml11Namespaces(XmlNode element)
    {
        List<CanonicalNamespaceValue> result = new List<CanonicalNamespaceValue>();
        if (!FullDocument)
        {
            int selectedIndex = 0;
            while (selectedIndex < SelectedNodes.Count)
            {
                XmlNode selected = SelectedNodes[selectedIndex];
                if (selected.Kind == XmlNodeKind.Namespace && selected.Parent == element)
                {
                    string prefix = selected.Name == null || selected.Name.LocalName == null ? "" : selected.Name.LocalName;
                    string namespaceUri = selected.Value == null ? "" : selected.Value;
                    AddCanonicalXml11Namespace(result, prefix, namespaceUri);
                }

                selectedIndex = selectedIndex + 1;
            }

            FilterExclusiveXml10Namespaces(element, result);
            return result;
        }

        List<string> hiddenPrefixes = new List<string>();
        XmlNode? current = element;
        while (current != null)
        {
            if (current.Kind == XmlNodeKind.Element)
            {
                XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
                int bindingIndex = 0;
                while (bindingIndex < bindings.Length)
                {
                    XmlNamespaceBinding? binding = bindings[bindingIndex];
                    if (binding == null || binding.Prefix == null || binding.NamespaceUri == null)
                    {
                        throw CreateCanonicalXml11Error("The document contains an incomplete namespace binding.");
                    }

                    if (!ContainsCanonicalXml11String(hiddenPrefixes, binding.Prefix))
                    {
                        hiddenPrefixes.Add(binding.Prefix);
                        AddCanonicalXml11Namespace(result, binding.Prefix, binding.IsUndeclaration ? "" : binding.NamespaceUri);
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            current = current.Parent;
        }

        XmlName? elementName = element.Name;
        if (elementName != null)
        {
            AddCanonicalXml11NamespaceFromName(result, elementName, true);
        }

        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            XmlNode attribute = attributes[attributeIndex];
            XmlName? attributeName = attribute.Name;
            if (attributeName != null && !IsCanonicalXml11NamespaceAttribute(attribute))
            {
                AddCanonicalXml11NamespaceFromName(result, attributeName, false);
            }

            attributeIndex = attributeIndex + 1;
        }

        if (!ContainsCanonicalXml11Namespace(result, ""))
        {
            AddCanonicalXml11Namespace(result, "", "");
        }

        FilterExclusiveXml10Namespaces(element, result);

        return result;
    }

    internal void FilterExclusiveXml10Namespaces(XmlNode element, List<CanonicalNamespaceValue> values)
    {
        if (!ExclusiveXml10)
        {
            return;
        }

        int index = values.Count - 1;
        while (index >= 0)
        {
            if (!IsExclusiveXml10PrefixUsed(element, values[index].Prefix))
            {
                values.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal bool IsExclusiveXml10PrefixUsed(XmlNode element, string prefix)
    {
        if (ContainsCanonicalXml11String(InclusivePrefixes, prefix))
        {
            return true;
        }

        XmlName? elementName = element.Name;
        string elementPrefix = elementName == null || elementName.Prefix == null ? "" : elementName.Prefix;
        if (string.Equals(elementPrefix, prefix, StringComparison.Ordinal))
        {
            return true;
        }

        if (prefix.Length == 0)
        {
            return false;
        }

        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            XmlName? name = attribute.Name;
            bool visible = FullDocument || IsCanonicalXml11Visible(attribute);
            if (visible && !IsCanonicalXml11NamespaceAttribute(attribute) && name != null && string.Equals(name.Prefix, prefix, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddCanonicalXml11NamespaceFromName(List<CanonicalNamespaceValue> values, XmlName name, bool allowDefault)
    {
        string prefix = name.Prefix == null ? "" : name.Prefix;
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if ((!allowDefault && prefix.Length == 0) || prefix.Length == 0 && namespaceUri.Length == 0)
        {
            return;
        }

        if (!ContainsCanonicalXml11Namespace(values, prefix))
        {
            AddCanonicalXml11Namespace(values, prefix, namespaceUri);
        }
    }

    internal void AddCanonicalXml11DefaultUndeclaration(XmlNode element, List<CanonicalNamespaceValue> values, CanonicalXml11Context parentContext, bool visible)
    {
        if (!visible || ContainsCanonicalXml11Namespace(values, ""))
        {
            return;
        }

        string parentDefault = FindCanonicalXml11RenderedNamespace(parentContext.Namespaces, "");
        if (ExclusiveXml10)
        {
            if (parentDefault.Length > 0 && IsExclusiveXml10PrefixUsed(element, ""))
            {
                AddCanonicalXml11Namespace(values, "", "");
            }

            return;
        }

        string actualDefault = FindCanonicalXml11InScopeNamespace(element, "");
        if (actualDefault.Length == 0 && parentDefault.Length > 0)
        {
            AddCanonicalXml11Namespace(values, "", "");
        }
    }

    internal void WriteCanonicalXml11Namespaces(List<CanonicalNamespaceValue> values, CanonicalXml11Context context, bool ownerVisible)
    {
        if (!ownerVisible)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            CanonicalNamespaceValue value = values[index];
            if (value.Prefix.Length > 0 && !string.Equals(value.Prefix, "xml", StringComparison.Ordinal) && value.NamespaceUri.Length > 0 && !UriTools.HasUriScheme(value.NamespaceUri))
            {
                throw CreateCanonicalXml11Error("Canonical XML does not permit relative namespace URIs.");
            }

            if (value.Prefix.Length == 0 && value.NamespaceUri.Length > 0 && !UriTools.HasUriScheme(value.NamespaceUri))
            {
                throw CreateCanonicalXml11Error("Canonical XML does not permit relative default namespace URIs.");
            }

            bool implicitXml = string.Equals(value.Prefix, "xml", StringComparison.Ordinal) && string.Equals(value.NamespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal);
            string rendered = FindCanonicalXml11RenderedNamespace(context.Namespaces, value.Prefix);
            if (!implicitXml && !string.Equals(rendered, value.NamespaceUri, StringComparison.Ordinal))
            {
                Result.AppendCanonicalText(" xmlns");
                if (value.Prefix.Length > 0)
                {
                    Result.AppendCanonicalCharacter(':');
                    Result.AppendCanonicalText(value.Prefix);
                }

                Result.AppendCanonicalText("=\"");
                WriteCanonicalXml11AttributeValue(value.NamespaceUri);
                Result.AppendCanonicalCharacter('"');
            }

            SetCanonicalXml11RenderedNamespace(context.Namespaces, value.Prefix, value.NamespaceUri);

            index = index + 1;
        }
    }

    internal List<CanonicalAttributeValue> CollectCanonicalXml11Attributes(XmlNode element, CanonicalXml11Context context, bool visible)
    {
        List<CanonicalAttributeValue> result = new List<CanonicalAttributeValue>();
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (!IsCanonicalXml11NamespaceAttribute(attribute) && (FullDocument || IsCanonicalXml11Visible(attribute)))
            {
                result.Add(CreateCanonicalXml11Attribute(attribute));
            }

            index = index + 1;
        }

        if (!ExclusiveXml10 && !FullDocument && visible && !IsCanonicalXml11VisibleElementParent(element))
        {
            AddCanonicalXml11InheritedAttribute(element, result, "lang");
            AddCanonicalXml11InheritedAttribute(element, result, "space");
            AddCanonicalXml11FixedBase(element, result, context);
        }

        return result;
    }

    internal void AddCanonicalXml11InheritedAttribute(XmlNode element, List<CanonicalAttributeValue> values, string localName)
    {
        if (FindCanonicalXml11XmlAttribute(element, localName) != null)
        {
            return;
        }

        XmlNode? current = element.Parent;
        while (current != null)
        {
            if (current.Kind == XmlNodeKind.Element)
            {
                XmlNode? inherited = FindCanonicalXml11XmlAttribute(current, localName);
                if (inherited != null)
                {
                    values.Add(CreateCanonicalXml11Attribute(inherited));
                    return;
                }
            }

            current = current.Parent;
        }
    }

    internal void AddCanonicalXml11FixedBase(XmlNode element, List<CanonicalAttributeValue> values, CanonicalXml11Context context)
    {
        if (!HasCanonicalXml11OmittedBase(element))
        {
            return;
        }

        string? fixedBase = ComputeCanonicalXml11FixedBase(element, context.SyntheticBase);
        RemoveCanonicalXml11Attribute(values, XmlNamespaces.Xml, "base");
        if (fixedBase != null && fixedBase.Length > 0)
        {
            values.Add(new CanonicalAttributeValue("xml:base", XmlNamespaces.Xml, "base", fixedBase));
            context.SyntheticBase = fixedBase;
        }
    }

    internal bool HasCanonicalXml11OmittedBase(XmlNode element)
    {
        XmlNode? current = element.Parent;
        while (current != null && current.Kind != XmlNodeKind.Document && current.Kind != XmlNodeKind.DocumentFragment)
        {
            if (current.Kind == XmlNodeKind.Element)
            {
                if (IsCanonicalXml11Visible(current))
                {
                    return false;
                }

                if (FindCanonicalXml11XmlAttribute(current, "base") != null)
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    internal string? ComputeCanonicalXml11FixedBase(XmlNode element, string? inheritedSyntheticBase)
    {
        List<XmlNode> chain = new List<XmlNode>();
        XmlNode? current = element;
        while (current != null && current.Kind != XmlNodeKind.Document && current.Kind != XmlNodeKind.DocumentFragment)
        {
            if (current.Kind == XmlNodeKind.Element)
            {
                chain.Insert(0, current);
                if (current != element && IsCanonicalXml11Visible(current))
                {
                    break;
                }
            }

            current = current.Parent;
        }

        string? result = inheritedSyntheticBase;
        int index = chain.Count > 0 && chain[0] != element && IsCanonicalXml11Visible(chain[0]) ? 1 : 0;
        while (index < chain.Count)
        {
            XmlNode? attribute = FindCanonicalXml11XmlAttribute(chain[index], "base");
            if (attribute != null)
            {
                result = JoinCanonicalXml11Uri(result, attribute.Value == null ? "" : attribute.Value);
            }

            index = index + 1;
        }

        return result;
    }

    internal string JoinCanonicalXml11Uri(string? baseValue, string reference)
    {
        string value = RemoveCanonicalXml11Fragment(reference);
        if (baseValue == null || baseValue.Length == 0 || UriTools.HasUriScheme(value))
        {
            return NormalizeCanonicalXml11UriPath(value);
        }

        if (value.StartsWith("/", StringComparison.Ordinal))
        {
            int authorityEnd = FindCanonicalXml11AuthorityEnd(baseValue);
            string prefix = authorityEnd < 0 ? "" : baseValue.Substring(0, authorityEnd);
            return prefix + NormalizeCanonicalXml11UriPath(value);
        }

        string baseWithoutQuery = RemoveCanonicalXml11Query(baseValue);
        int slash = baseWithoutQuery.LastIndexOf('/');
        string directory = slash < 0 ? "" : baseWithoutQuery.Substring(0, slash + 1);
        return NormalizeCanonicalXml11UriPath(directory + value);
    }

    internal string NormalizeCanonicalXml11UriPath(string value)
    {
        string query = "";
        int queryIndex = value.IndexOf('?');
        if (queryIndex >= 0)
        {
            query = value.Substring(queryIndex);
            value = value.Substring(0, queryIndex);
        }

        string prefix = "";
        string path = value;
        int pathStart = FindCanonicalXml11PathStart(value);
        if (pathStart > 0)
        {
            prefix = value.Substring(0, pathStart);
            path = value.Substring(pathStart);
        }

        bool absolutePath = path.StartsWith("/", StringComparison.Ordinal);
        bool trailingSlash = path.EndsWith("/", StringComparison.Ordinal) || path.EndsWith("/..", StringComparison.Ordinal) || string.Equals(path, "..", StringComparison.Ordinal);
        string[] segments = path.Split('/');
        List<string> normalized = new List<string>();
        int index = 0;
        while (index < segments.Length)
        {
            string segment = segments[index];
            if (segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal))
            {
                index = index + 1;
                continue;
            }

            if (string.Equals(segment, "..", StringComparison.Ordinal))
            {
                if (normalized.Count > 0 && !string.Equals(normalized[normalized.Count - 1], "..", StringComparison.Ordinal))
                {
                    normalized.RemoveAt(normalized.Count - 1);
                }
                else if (!absolutePath)
                {
                    normalized.Add("..");
                }
            }
            else
            {
                normalized.Add(segment);
            }

            index = index + 1;
        }

        StringBuilder result = new StringBuilder();
        result.Append(prefix);
        if (absolutePath)
        {
            result.Append('/');
        }

        index = 0;
        while (index < normalized.Count)
        {
            if (index > 0)
            {
                result.Append('/');
            }

            result.Append(normalized[index]);
            index = index + 1;
        }

        if (trailingSlash && (result.Length == 0 || result[result.Length - 1] != '/'))
        {
            result.Append('/');
        }

        result.Append(query);
        return result.ToString();
    }

    internal int FindCanonicalXml11PathStart(string value)
    {
        int scheme = value.IndexOf(':');
        if (scheme < 0)
        {
            return 0;
        }

        if (scheme + 2 < value.Length && value[scheme + 1] == '/' && value[scheme + 2] == '/')
        {
            int slash = value.IndexOf('/', scheme + 3);
            return slash < 0 ? value.Length : slash;
        }

        return scheme + 1;
    }

    internal int FindCanonicalXml11AuthorityEnd(string value)
    {
        int scheme = value.IndexOf(':');
        if (scheme < 0 || scheme + 2 >= value.Length || value[scheme + 1] != '/' || value[scheme + 2] != '/')
        {
            return -1;
        }

        int slash = value.IndexOf('/', scheme + 3);
        return slash < 0 ? value.Length : slash;
    }

    internal string RemoveCanonicalXml11Fragment(string value)
    {
        int index = value.IndexOf('#');
        return index < 0 ? value : value.Substring(0, index);
    }

    internal string RemoveCanonicalXml11Query(string value)
    {
        int index = value.IndexOf('?');
        return index < 0 ? RemoveCanonicalXml11Fragment(value) : value.Substring(0, index);
    }

    internal void WriteCanonicalXml11Attributes(List<CanonicalAttributeValue> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            CanonicalAttributeValue attribute = attributes[index];
            Result.AppendCanonicalCharacter(' ');
            Result.AppendCanonicalText(attribute.QualifiedName);
            Result.AppendCanonicalText("=\"");
            WriteCanonicalXml11AttributeValue(attribute.Value);
            Result.AppendCanonicalCharacter('"');
            index = index + 1;
        }
    }

    internal void WriteCanonicalXml11AttributeValue(string value)
    {
        int position = 0;
        while (position < value.Length)
        {
            int scalar = ReadCanonicalXml11Scalar(value, position);
            int characterCount = scalar > 0xFFFF ? 2 : 1;
            if (scalar == '&') Result.AppendCanonicalText("&amp;");
            else if (scalar == '<') Result.AppendCanonicalText("&lt;");
            else if (scalar == '"') Result.AppendCanonicalText("&quot;");
            else if (scalar == 0x9) Result.AppendCanonicalText("&#x9;");
            else if (scalar == 0xA) Result.AppendCanonicalText("&#xA;");
            else if (scalar == 0xD) Result.AppendCanonicalText("&#xD;");
            else if (!ExclusiveXml10 && Xml11CharacterRules.IsRestrictedXml11Character(scalar)) WriteCanonicalXml11Reference(scalar);
            else Result.AppendCanonicalSegment(value, position, characterCount);
            position = position + characterCount;
        }
    }

    internal void WriteCanonicalXml11Text(string value)
    {
        int position = 0;
        while (position < value.Length)
        {
            int scalar = ReadCanonicalXml11Scalar(value, position);
            int characterCount = scalar > 0xFFFF ? 2 : 1;
            if (scalar == '&') Result.AppendCanonicalText("&amp;");
            else if (scalar == '<') Result.AppendCanonicalText("&lt;");
            else if (scalar == '>') Result.AppendCanonicalText("&gt;");
            else if (scalar == 0xD) Result.AppendCanonicalText("&#xD;");
            else if (!ExclusiveXml10 && Xml11CharacterRules.IsRestrictedXml11Character(scalar)) WriteCanonicalXml11Reference(scalar);
            else Result.AppendCanonicalSegment(value, position, characterCount);
            position = position + characterCount;
        }
    }

    internal void WriteCanonicalXml11ProcessingInstruction(XmlNode node)
    {
        bool documentChild = node.Parent != null && node.Parent.Kind == XmlNodeKind.Document;
        bool afterElement = documentChild && IsCanonicalXml11AfterDocumentElement(node);
        if (afterElement)
        {
            Result.AppendCanonicalCharacter('\n');
        }

        Result.AppendCanonicalText("<?");
        Result.AppendCanonicalText(CanonicalXml11NodeName(node));
        string value = node.Value == null ? "" : node.Value;
        if (value.Length > 0)
        {
            Result.AppendCanonicalCharacter(' ');
            WriteCanonicalXml11ConstructValue(value);
        }

        Result.AppendCanonicalText("?>");
        if (documentChild && !afterElement)
        {
            Result.AppendCanonicalCharacter('\n');
        }
    }

    internal void WriteCanonicalXml11Comment(XmlNode node)
    {
        bool documentChild = node.Parent != null && node.Parent.Kind == XmlNodeKind.Document;
        bool afterElement = documentChild && IsCanonicalXml11AfterDocumentElement(node);
        if (afterElement)
        {
            Result.AppendCanonicalCharacter('\n');
        }

        Result.AppendCanonicalText("<!--");
        WriteCanonicalXml11ConstructValue(node.Value == null ? "" : node.Value);
        Result.AppendCanonicalText("-->");
        if (documentChild && !afterElement)
        {
            Result.AppendCanonicalCharacter('\n');
        }
    }

    internal void WriteCanonicalXml11ConstructValue(string value)
    {
        int position = 0;
        while (position < value.Length)
        {
            int scalar = ReadCanonicalXml11Scalar(value, position);
            int characterCount = scalar > 0xFFFF ? 2 : 1;
            if (scalar == 0xD)
            {
                Result.AppendCanonicalText("&#xD;");
            }
            else if (!ExclusiveXml10 && Xml11CharacterRules.IsRestrictedXml11Character(scalar))
            {
                throw CreateCanonicalXml11Error("A restricted XML 1.1 character cannot be represented in this canonical construct.");
            }
            else
            {
                Result.AppendCanonicalSegment(value, position, characterCount);
            }

            position = position + characterCount;
        }
    }

    internal void WriteCanonicalXml11Reference(int scalar)
    {
        Result.AppendCanonicalText("&#x");
        Result.AppendCanonicalText(scalar.ToString("X"));
        Result.AppendCanonicalCharacter(';');
    }

    internal int ReadCanonicalXml11Scalar(string value, int position)
    {
        int first = value[position];
        int scalar = first;
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (position + 1 >= value.Length || value[position + 1] < 0xDC00 || value[position + 1] > 0xDFFF)
            {
                throw CreateCanonicalXml11Error("Canonical XML input contains malformed Unicode.");
            }

            scalar = 0x10000 + (first - 0xD800) * 0x400 + value[position + 1] - 0xDC00;
        }
        else if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw CreateCanonicalXml11Error("Canonical XML input contains malformed Unicode.");
        }

        bool legal = ExclusiveXml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar) : Xml11CharacterRules.IsLegalXml11Character(scalar);
        if (!legal)
        {
            throw CreateCanonicalXml11Error(ExclusiveXml10 ? "Canonical XML input contains a character that is not legal in XML 1.0." : "Canonical XML input contains a character that is not legal in XML 1.1.");
        }

        return scalar;
    }

    internal bool IsCanonicalXml11AfterDocumentElement(XmlNode node)
    {
        XmlNode? root = node.Parent;
        XmlNode[] siblings = root == null || root.Children == null ? [] : root.Children;
        bool foundElement = false;
        int index = 0;
        while (index < siblings.Length)
        {
            if (siblings[index] == node)
            {
                return foundElement;
            }

            if (siblings[index].Kind == XmlNodeKind.Element)
            {
                foundElement = true;
            }

            index = index + 1;
        }

        return false;
    }

    internal CanonicalAttributeValue CreateCanonicalXml11Attribute(XmlNode attribute)
    {
        XmlName? name = attribute.Name;
        if (name == null || name.LocalName == null)
        {
            throw CreateCanonicalXml11Error("Canonical XML input contains an unnamed attribute.");
        }

        string value = attribute.Value == null ? "" : attribute.Value;
        value = RewriteCanonicalXml20Uri(attribute, value);
        return new CanonicalAttributeValue(CanonicalXml11NodeName(attribute), name.NamespaceUri == null ? "" : name.NamespaceUri, name.LocalName, value);
    }

    internal string RewriteCanonicalXml20Uri(XmlNode attribute, string value)
    {
        if (!ExperimentalXml20 || !Options.RewriteUris)
        {
            return value;
        }

        XmlName? name = attribute.Name;
        bool xmlBase = name != null && string.Equals(name.LocalName, "base", StringComparison.Ordinal) && (string.Equals(name.NamespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal) || string.Equals(name.Prefix, "xml", StringComparison.Ordinal));
        XmlSchemaTypeInfo? typeInfo = attribute.TypeInfo;
        bool anyUri = typeInfo != null && typeInfo.AtomicType == XdmAtomicType.AnyUri;
        if (!xmlBase && !anyUri)
        {
            return value;
        }

        string? effectiveBase = Navigation.GetXmlNodeBaseUriOperation(attribute);
        if (xmlBase && effectiveBase != null)
        {
            return effectiveBase;
        }

        if (UriTools.HasUriScheme(value))
        {
            return UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(value));
        }

        if (effectiveBase != null && UriTools.HasUriScheme(effectiveBase))
        {
            return UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(effectiveBase), value));
        }

        return UriTools.NormalizePercentEncoding(value);
    }

    internal string CanonicalXml11NodeName(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null || name.LocalName == null)
        {
            throw CreateCanonicalXml11Error("Canonical XML input contains an unnamed node.");
        }

        if (name.QualifiedName != null && name.QualifiedName.Length > 0)
        {
            return name.QualifiedName;
        }

        return name.Prefix == null || name.Prefix.Length == 0 ? name.LocalName : name.Prefix + ":" + name.LocalName;
    }

    internal XmlNode? FindCanonicalXml11XmlAttribute(XmlNode element, string localName)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && string.Equals(name.LocalName, localName, StringComparison.Ordinal) && (string.Equals(name.NamespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal) || string.Equals(name.Prefix, "xml", StringComparison.Ordinal)))
            {
                return attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsCanonicalXml11NamespaceAttribute(XmlNode attribute)
    {
        XmlName? name = attribute.Name;
        return name != null && (string.Equals(name.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal) || string.Equals(name.Prefix, "xmlns", StringComparison.Ordinal) || name.Prefix == null && string.Equals(name.LocalName, "xmlns", StringComparison.Ordinal));
    }

    internal bool IsCanonicalXml11Visible(XmlNode node)
    {
        return FullDocument || ContainsCanonicalXml11Node(SelectedNodes, node);
    }

    internal bool IsCanonicalXml11VisibleElementParent(XmlNode element)
    {
        XmlNode? parent = element.Parent;
        return parent != null && parent.Kind == XmlNodeKind.Element && IsCanonicalXml11Visible(parent);
    }

    internal bool ContainsCanonicalXml11Node(List<XmlNode> values, XmlNode node)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == node)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsCanonicalXml11String(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsCanonicalXml11Namespace(List<CanonicalNamespaceValue> values, string prefix)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index].Prefix, prefix, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddCanonicalXml11Namespace(List<CanonicalNamespaceValue> values, string prefix, string namespaceUri)
    {
        if (!ContainsCanonicalXml11Namespace(values, prefix))
        {
            values.Add(new CanonicalNamespaceValue(prefix, namespaceUri));
        }
    }

    internal string FindCanonicalXml11InScopeNamespace(XmlNode element, string prefix)
    {
        XmlNode? current = element;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = 0;
            while (index < bindings.Length)
            {
                XmlNamespaceBinding? binding = bindings[index];
                if (binding != null && binding.Prefix != null && string.Equals(binding.Prefix, prefix, StringComparison.Ordinal))
                {
                    return binding.IsUndeclaration || binding.NamespaceUri == null ? "" : binding.NamespaceUri;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return "";
    }

    internal string FindCanonicalXml11RenderedNamespace(List<CanonicalNamespaceValue> values, string prefix)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(values[index].Prefix, prefix, StringComparison.Ordinal))
            {
                return values[index].NamespaceUri;
            }

            index = index - 1;
        }

        return "";
    }

    internal void SetCanonicalXml11RenderedNamespace(List<CanonicalNamespaceValue> values, string prefix, string namespaceUri)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(values[index].Prefix, prefix, StringComparison.Ordinal))
            {
                values[index] = new CanonicalNamespaceValue(prefix, namespaceUri);
                return;
            }

            index = index - 1;
        }

        values.Add(new CanonicalNamespaceValue(prefix, namespaceUri));
    }

    internal CanonicalXml11Context CopyCanonicalXml11Context(CanonicalXml11Context source)
    {
        List<CanonicalNamespaceValue> namespaces = new List<CanonicalNamespaceValue>();
        int index = 0;
        while (index < source.Namespaces.Count)
        {
            CanonicalNamespaceValue item = source.Namespaces[index];
            namespaces.Add(new CanonicalNamespaceValue(item.Prefix, item.NamespaceUri));
            index = index + 1;
        }

        return new CanonicalXml11Context(namespaces, source.SyntheticBase);
    }

    internal void RemoveCanonicalXml11Attribute(List<CanonicalAttributeValue> values, string namespaceUri, string localName)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(values[index].NamespaceUri, namespaceUri, StringComparison.Ordinal) && string.Equals(values[index].LocalName, localName, StringComparison.Ordinal))
            {
                values.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal void SortCanonicalXml11Namespaces(List<CanonicalNamespaceValue> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            CanonicalNamespaceValue value = values[index];
            int position = index;
            while (position > 0 && string.CompareOrdinal(values[position - 1].Prefix, value.Prefix) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }

            values[position] = value;
            index = index + 1;
        }
    }

    internal void SortCanonicalXml11Attributes(List<CanonicalAttributeValue> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            CanonicalAttributeValue value = values[index];
            int position = index;
            while (position > 0 && CompareCanonicalXml11Attributes(values[position - 1], value) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }

            values[position] = value;
            index = index + 1;
        }
    }

    internal int CompareCanonicalXml11Attributes(CanonicalAttributeValue left, CanonicalAttributeValue right)
    {
        int namespaceComparison = string.CompareOrdinal(left.NamespaceUri, right.NamespaceUri);
        return namespaceComparison == 0 ? string.CompareOrdinal(left.LocalName, right.LocalName) : namespaceComparison;
    }

    internal XmlCanonicalizationException CreateCanonicalXml11Error(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.CanonicalizationFailure;
        diagnostic.Category = XmlErrorCategory.Canonicalization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlCanonicalizationException(message, diagnostic, null);
    }
}
