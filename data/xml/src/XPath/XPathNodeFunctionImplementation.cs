using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace xml;

internal class XPathNodeIdentityCounter
{
    internal long NextIdentifier;

    internal XPathNodeIdentityCounter()
    {
        NextIdentifier = 1;
    }
}

internal class XPathNodeGeneratedIdentifier
{
    internal long Identifier;

    internal XPathNodeGeneratedIdentifier(long identifier)
    {
        Identifier = identifier;
    }
}

internal class XPathNodeFunctionImplementation : IXPathFunction
{
    internal static readonly ConditionalWeakTable<XmlNode, XPathNodeGeneratedIdentifier> GeneratedIdentifiers = new ConditionalWeakTable<XmlNode, XPathNodeGeneratedIdentifier>();
    internal static readonly XPathNodeIdentityCounter GeneratedIdentifierCounter = new XPathNodeIdentityCounter();
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;
    internal XdmNodeAccessor Nodes;
    internal XdmStringDatatype Strings;
    internal TreeNavigation Navigation;

    internal XPathNodeFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathNodeRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
        Nodes = new XdmNodeAccessor();
        Strings = new XdmStringDatatype();
        Navigation = new TreeNavigation();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        RequireXPathStreamedFunctionNodes(arguments, context, LocalName == "data" || LocalName == "string");
        if (LocalName == "data")
        {
            XdmSequence supplied = arguments.Length == 0 ? RequireXPathContextSequence(context) : arguments[0];
            return Converter.AtomizeXdmSequence(supplied);
        }

        if (LocalName == "string")
        {
            return EvaluateXPathNodeString(arguments, context);
        }

        if (LocalName == "node-name" || LocalName == "name" || LocalName == "local-name" || LocalName == "namespace-uri")
        {
            return EvaluateXPathNodeName(arguments, context);
        }

        if (LocalName == "base-uri" || LocalName == "document-uri")
        {
            return EvaluateXPathNodeUri(arguments, context);
        }

        if (LocalName == "root")
        {
            return EvaluateXPathRoot(arguments, context);
        }

        if (LocalName == "path")
        {
            return EvaluateXPathPath(arguments, context);
        }

        if (LocalName == "has-children")
        {
            XdmNode? node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
            bool hasChildren = node != null && Nodes.GetXdmNodeChildren(node).Length != 0;
            return CreateXPathNodeBooleanResult(hasChildren);
        }

        if (LocalName == "nilled")
        {
            XdmNode? node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
            return node == null ? Sequences.CreateEmptyXdmSequence() : Nodes.GetXdmNodeNilled(node);
        }

        if (LocalName == "innermost" || LocalName == "outermost")
        {
            return EvaluateXPathExtremalNodes(arguments[0], LocalName == "innermost");
        }

        if (LocalName == "generate-id")
        {
            return EvaluateXPathGenerateId(arguments, context);
        }

        if (LocalName == "lang")
        {
            return EvaluateXPathLang(arguments, context);
        }

        if (LocalName == "id" || LocalName == "element-with-id" || LocalName == "idref")
        {
            return EvaluateXPathIdentifierFunction(arguments, context);
        }

        throw new InvalidOperationException("The XPath node function is not implemented.");
    }

    internal void RequireXPathStreamedFunctionNodes(XdmSequence[] arguments, XPathDynamicContext context, bool consumeValue)
    {
        XsltStreamedNodeRuntime? streamed = context.XsltStreamedNodes;
        if (streamed == null)
        {
            return;
        }

        XdmSequence? supplied = arguments.Length == 0 ? context.ContextItem : arguments[0];
        if (supplied == null)
        {
            return;
        }

        XdmSequenceCursor cursor = new XdmSequenceCursor(supplied);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Node != null && streamed.IsXsltStreamedNode(item.Node))
            {
                XsltStreamNavigationStatus status = consumeValue ? streamed.AuthorizeXsltStreamedNodeValue(item.Node) : streamed.IsXsltStreamedNodeAvailable(item.Node) ? XsltStreamNavigationStatus.Allowed : XsltStreamNavigationStatus.Expired;
                if (status != XsltStreamNavigationStatus.Allowed)
                {
                    string message = status == XsltStreamNavigationStatus.Expired ? "The streamed XPath node is no longer available at the current stream position." : "The streamed XPath node value has already been consumed.";
                    throw CreateXPathNodeError(message, XPathErrorCodes.XPDY0130);
                }
            }
        }
    }

    internal XdmSequence EvaluateXPathNodeString(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence supplied = arguments.Length == 0 ? RequireXPathContextSequence(context) : arguments[0];
        if (supplied.Count == 0)
        {
            return CreateXPathNodeStringResult("");
        }

        if (supplied.Count != 1)
        {
            throw CreateXPathNodeError("fn:string requires at most one item.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            return CreateXPathNodeStringResult(Nodes.GetXdmNodeStringValue(Nodes.WrapXmlNode(item.Node)));
        }

        if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            return CreateXPathNodeStringResult(Converter.ReadXdmAtomicStringValue(item.AtomicValue));
        }

        throw CreateXPathNodeError("A function item has no string value.", XPathErrorCodes.FOTY0014);
    }

    internal XdmSequence EvaluateXPathNodeName(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmNode? node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
        XmlName? name = node == null ? null : Nodes.GetXdmNodeName(node);
        if (LocalName == "node-name")
        {
            if (name == null)
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            string lexical = CreateXPathLexicalQName(name);
            return Sequences.CreateXdmAtomicSequence(new XdmQNameDatatype().CreateXdmQNameValue(name.NamespaceUri, lexical));
        }

        if (LocalName == "namespace-uri")
        {
            string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
            return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmAnyUriAtomicValue(namespaceUri));
        }

        string value = "";
        if (name != null)
        {
            value = LocalName == "local-name" ? name.LocalName == null ? "" : name.LocalName : CreateXPathLexicalQName(name);
        }

        return CreateXPathNodeStringResult(value);
    }

    internal XdmSequence EvaluateXPathNodeUri(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmNode? node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
        if (node == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        string? uri = LocalName == "base-uri" ? Nodes.GetXdmNodeBaseUri(node) : Nodes.GetXdmNodeDocumentUri(node);
        return uri == null ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(Factory.CreateXdmAnyUriAtomicValue(uri));
    }

    internal XdmSequence EvaluateXPathRoot(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmNode? node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
        if (node == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmNode current = node;
        XdmNode? parent = Nodes.GetXdmNodeParent(current);
        while (parent != null)
        {
            current = parent;
            parent = Nodes.GetXdmNodeParent(current);
        }

        return Sequences.CreateXdmNodeSequence(current.Node);
    }

    internal XdmSequence EvaluateXPathPath(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmNode? node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
        if (node == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        return CreateXPathNodeStringResult(CreateXPathNodePath(node));
    }

    internal string CreateXPathNodePath(XdmNode node)
    {
        List<XdmNode> reverse = new List<XdmNode>();
        XdmNode current = node;
        XdmNode? parent = Nodes.GetXdmNodeParent(current);
        while (parent != null)
        {
            reverse.Add(current);
            current = parent;
            parent = Nodes.GetXdmNodeParent(current);
        }

        if (reverse.Count == 0 && current.Kind == XdmNodeKind.Document)
        {
            return "/";
        }

        StringBuilder result = new StringBuilder();
        if (current.Kind != XdmNodeKind.Document)
        {
            result.Append("Q{");
            result.Append(XPathNamespaces.Functions);
            result.Append("}root()");
        }

        int index = reverse.Count - 1;
        while (index >= 0)
        {
            XdmNode pathNode = reverse[index];
            XdmNode? pathParent = Nodes.GetXdmNodeParent(pathNode);
            result.Append('/');
            result.Append(CreateXPathNodePathStep(pathNode, pathParent));
            index = index - 1;
        }

        return result.ToString();
    }

    internal string CreateXPathNodePathStep(XdmNode node, XdmNode? parent)
    {
        if (node.Kind == XdmNodeKind.Element)
        {
            return CreateXPathElementPathStep(node, parent);
        }

        if (node.Kind == XdmNodeKind.Attribute)
        {
            XmlName? name = Nodes.GetXdmNodeName(node);
            string local = name == null || name.LocalName == null ? "" : name.LocalName;
            string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
            return namespaceUri.Length == 0 ? "@" + local : "@Q{" + namespaceUri + "}" + local;
        }

        if (node.Kind == XdmNodeKind.Namespace)
        {
            XmlName? name = Nodes.GetXdmNodeName(node);
            string prefix = name == null || name.LocalName == null ? "" : name.LocalName;
            if (prefix.Length == 0)
            {
                return "namespace::*[Q{" + XPathNamespaces.Functions + "}local-name()=\"\"]";
            }

            return "namespace::" + prefix;
        }

        int position = FindXPathNodeKindPosition(node, parent);
        if (node.Kind == XdmNodeKind.Text)
        {
            return "text()[" + position.ToString(CultureInfo.InvariantCulture) + "]";
        }

        if (node.Kind == XdmNodeKind.Comment)
        {
            return "comment()[" + position.ToString(CultureInfo.InvariantCulture) + "]";
        }

        XmlName? processingName = Nodes.GetXdmNodeName(node);
        string target = processingName == null || processingName.LocalName == null ? "" : processingName.LocalName;
        return "processing-instruction(" + target + ")[" + position.ToString(CultureInfo.InvariantCulture) + "]";
    }

    internal string CreateXPathElementPathStep(XdmNode node, XdmNode? parent)
    {
        XmlName? name = Nodes.GetXdmNodeName(node);
        string local = name == null || name.LocalName == null ? "" : name.LocalName;
        string namespaceUri = name == null || name.NamespaceUri == null ? "" : name.NamespaceUri;
        int position = 1;
        if (parent != null)
        {
            XdmNode[] siblings = Nodes.GetXdmNodeChildren(parent);
            int index = 0;
            while (index < siblings.Length)
            {
                XdmNode sibling = siblings[index];
                if (sibling.Kind == XdmNodeKind.Element && HaveSameXPathNodeName(node, sibling))
                {
                    if (sibling.Node == node.Node)
                    {
                        break;
                    }

                    position = position + 1;
                }

                index = index + 1;
            }
        }

        return "Q{" + namespaceUri + "}" + local + "[" + position.ToString(CultureInfo.InvariantCulture) + "]";
    }

    internal int FindXPathNodeKindPosition(XdmNode node, XdmNode? parent)
    {
        if (parent == null)
        {
            return 1;
        }

        int position = 1;
        XdmNode[] siblings = Nodes.GetXdmNodeChildren(parent);
        int index = 0;
        while (index < siblings.Length)
        {
            XdmNode sibling = siblings[index];
            bool sameKind = sibling.Kind == node.Kind;
            if (sameKind && node.Kind == XdmNodeKind.ProcessingInstruction)
            {
                sameKind = HaveSameXPathNodeName(node, sibling);
            }

            if (sameKind)
            {
                if (sibling.Node == node.Node)
                {
                    break;
                }

                position = position + 1;
            }

            index = index + 1;
        }

        return position;
    }

    internal bool HaveSameXPathNodeName(XdmNode left, XdmNode right)
    {
        XmlName? leftName = Nodes.GetXdmNodeName(left);
        XmlName? rightName = Nodes.GetXdmNodeName(right);
        if (leftName == null || rightName == null)
        {
            return leftName == rightName;
        }

        return string.Equals(leftName.LocalName, rightName.LocalName, StringComparison.Ordinal) && string.Equals(leftName.NamespaceUri, rightName.NamespaceUri, StringComparison.Ordinal);
    }

    internal XdmSequence EvaluateXPathExtremalNodes(XdmSequence supplied, bool innermost)
    {
        List<XmlNode> input = ReadXPathDistinctNodes(supplied);
        List<XmlNode> result = new List<XmlNode>();
        int index = 0;
        while (index < input.Count)
        {
            XmlNode candidate = input[index];
            bool excluded = false;
            int otherIndex = 0;
            while (otherIndex < input.Count && !excluded)
            {
                XmlNode other = input[otherIndex];
                if (candidate != other)
                {
                    excluded = innermost ? Navigation.IsXmlNavigationAncestor(candidate, other) : Navigation.IsXmlNavigationAncestor(other, candidate);
                }

                otherIndex = otherIndex + 1;
            }

            if (!excluded)
            {
                result.Add(candidate);
            }

            index = index + 1;
        }

        SortXPathNodesInDocumentOrder(result);
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        index = 0;
        while (index < result.Count)
        {
            builder.AddXdmNode(result[index]);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal List<XmlNode> ReadXPathDistinctNodes(XdmSequence supplied)
    {
        List<XmlNode> result = new List<XmlNode>();
        int index = 0;
        while (index < supplied.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, index);
            if (item.Kind != XdmItemKind.Node || item.Node == null)
            {
                throw CreateXPathNodeError("The function argument must contain only nodes.", XPathErrorCodes.XPTY0004);
            }

            if (!ContainsXPathNode(result, item.Node))
            {
                result.Add(item.Node);
            }

            index = index + 1;
        }

        return result;
    }

    internal bool ContainsXPathNode(List<XmlNode> nodes, XmlNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void SortXPathNodesInDocumentOrder(List<XmlNode> nodes)
    {
        int treeIndex = 0;
        while (treeIndex < nodes.Count)
        {
            XmlNode root = Navigation.FindXmlNavigationRoot(NormalizeXPathOrderNode(nodes[treeIndex]));
            Nodes.GetXdmTreeOrder(root);
            treeIndex = treeIndex + 1;
        }

        int index = 1;
        while (index < nodes.Count)
        {
            XmlNode value = nodes[index];
            int insertion = index;
            while (insertion > 0 && Nodes.CompareXdmNodeDocumentOrder(Nodes.WrapXmlNode(NormalizeXPathOrderNode(value)), Nodes.WrapXmlNode(NormalizeXPathOrderNode(nodes[insertion - 1]))) < 0)
            {
                nodes[insertion] = nodes[insertion - 1];
                insertion = insertion - 1;
            }

            nodes[insertion] = value;
            index = index + 1;
        }
    }

    internal XmlNode NormalizeXPathOrderNode(XmlNode node)
    {
        if (node.Kind != XmlNodeKind.Namespace || node.Parent == null)
        {
            return node;
        }

        XdmNode[] namespaces = Nodes.GetXdmNodeNamespaces(Nodes.WrapXmlNode(node.Parent));
        string prefix = node.Name == null || node.Name.LocalName == null ? "" : node.Name.LocalName;
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNode candidate = namespaces[index].Node;
            string candidatePrefix = candidate.Name == null || candidate.Name.LocalName == null ? "" : candidate.Name.LocalName;
            if (string.Equals(prefix, candidatePrefix, StringComparison.Ordinal) && string.Equals(node.Value, candidate.Value, StringComparison.Ordinal))
            {
                return candidate;
            }

            index = index + 1;
        }

        return node;
    }

    internal XdmSequence EvaluateXPathGenerateId(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmNode? node;
        if (arguments.Length != 0 && arguments[0].Count > 1 && StaticContext.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible)
        {
            XdmSequenceItem first = Sequences.ReadXdmSequenceItem(arguments[0], 0);
            if (first.Kind != XdmItemKind.Node || first.Node == null)
            {
                throw CreateXPathNodeError("The function argument must start with a node.", XPathErrorCodes.XPTY0004);
            }

            node = Nodes.WrapXmlNode(first.Node);
        }
        else
        {
            node = arguments.Length == 0 ? ReadXPathContextNode(context) : ReadXPathOptionalNodeArgument(arguments[0]);
        }

        if (node == null)
        {
            return CreateXPathNodeStringResult("");
        }

        XPathNodeGeneratedIdentifier generated;
        lock (GeneratedIdentifierCounter)
        {
            generated = GeneratedIdentifiers.GetValue(node.Node, key => CreateXPathGeneratedIdentifier());
        }

        return CreateXPathNodeStringResult("N" + generated.Identifier.ToString(CultureInfo.InvariantCulture));
    }

    internal XPathNodeGeneratedIdentifier CreateXPathGeneratedIdentifier()
    {
        XPathNodeGeneratedIdentifier result = new XPathNodeGeneratedIdentifier(GeneratedIdentifierCounter.NextIdentifier);
        GeneratedIdentifierCounter.NextIdentifier = GeneratedIdentifierCounter.NextIdentifier + 1;
        return result;
    }

    internal XdmSequence EvaluateXPathLang(XdmSequence[] arguments, XPathDynamicContext context)
    {
        string testLanguage = ReadXPathOptionalFunctionString(arguments[0]);
        XdmNode node = arguments.Length == 1 ? ReadXPathContextNode(context) : ReadXPathRequiredNodeArgument(arguments[1]);
        string? actualLanguage = FindXPathNodeLanguage(node);
        bool matches = false;
        if (actualLanguage != null)
        {
            matches = string.Equals(actualLanguage, testLanguage, StringComparison.OrdinalIgnoreCase);
            if (!matches && actualLanguage.Length > testLanguage.Length && actualLanguage[testLanguage.Length] == '-')
            {
                matches = string.Equals(actualLanguage.Substring(0, testLanguage.Length), testLanguage, StringComparison.OrdinalIgnoreCase);
            }
        }

        return CreateXPathNodeBooleanResult(matches);
    }

    internal string? FindXPathNodeLanguage(XdmNode node)
    {
        XdmNode? current = node;
        while (current != null)
        {
            if (current.Kind == XdmNodeKind.Element)
            {
                string? language = Navigation.FindXmlNavigationContextAttribute(current.Node, "lang");
                if (language != null)
                {
                    return language;
                }
            }

            current = Nodes.GetXdmNodeParent(current);
        }

        return null;
    }

    internal XdmSequence EvaluateXPathIdentifierFunction(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmNode target = arguments.Length == 1 ? ReadXPathContextNode(context) : ReadXPathRequiredNodeArgument(arguments[1]);
        XdmNode root = FindXPathNodeRoot(target);
        if (root.Kind != XdmNodeKind.Document)
        {
            throw CreateXPathNodeError("The target node is not in a tree rooted at a document node.", XPathErrorCodes.FODC0001);
        }

        List<string> candidates = ReadXPathIdentifierCandidates(arguments[0], LocalName != "idref");
        if (candidates.Count == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        List<XmlNode> matches = new List<XmlNode>();
        HashSet<string> foundIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        XdmNode[] children = Nodes.GetXdmNodeChildren(root);
        int index = 0;
        while (index < children.Length)
        {
            AddXPathIdentifierMatches(children[index], candidates, foundIdentifiers, matches);
            index = index + 1;
        }

        SortXPathNodesInDocumentOrder(matches);

        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        index = 0;
        while (index < matches.Count)
        {
            builder.AddXdmNode(matches[index]);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmNode FindXPathNodeRoot(XdmNode node)
    {
        XdmNode current = node;
        XdmNode? parent = Nodes.GetXdmNodeParent(current);
        while (parent != null)
        {
            current = parent;
            parent = Nodes.GetXdmNodeParent(current);
        }

        return current;
    }

    internal List<string> ReadXPathIdentifierCandidates(XdmSequence supplied, bool splitWhitespace)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ZeroOrMore;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        List<string> result = new List<string>();
        int index = 0;
        while (index < converted.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(converted, index);
            string value = item.AtomicValue == null ? "" : Converter.ReadXdmAtomicStringValue(item.AtomicValue);
            if (splitWhitespace)
            {
                AddXPathIdentifierTokens(result, value);
            }
            else if (Strings.IsValidXdmName(value, true) && !result.Contains(value))
            {
                result.Add(value);
            }

            index = index + 1;
        }

        return result;
    }

    internal void AddXPathIdentifierTokens(List<string> result, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && IsXPathXmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < value.Length && !IsXPathXmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            if (index > start)
            {
                string token = value.Substring(start, index - start);
                if (Strings.IsValidXdmName(token, true) && !result.Contains(token))
                {
                    result.Add(token);
                }
            }
        }
    }

    internal bool IsXPathXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r';
    }

    internal void AddXPathIdentifierMatches(XdmNode node, List<string> candidates, HashSet<string> foundIdentifiers, List<XmlNode> result)
    {
        if (node.Kind != XdmNodeKind.Element)
        {
            return;
        }

        if (LocalName == "idref")
        {
            if (IsXPathIdReferenceNode(node) && XPathIdReferenceMatches(node, candidates))
            {
                result.Add(node.Node);
            }
        }
        else
        {
            string? elementIdentifier = ReadXPathElementIdentifier(node);
            if (elementIdentifier != null && candidates.Contains(elementIdentifier))
            {
                if (LocalName == "id")
                {
                    if (foundIdentifiers.Add(elementIdentifier))
                    {
                        result.Add(node.Node);
                    }
                }
                else
                {
                    XdmNode? parent = Nodes.GetXdmNodeParent(node);
                    if (parent != null && parent.Kind == XdmNodeKind.Element && foundIdentifiers.Add(elementIdentifier) && !ContainsXPathNode(result, parent.Node))
                    {
                        result.Add(parent.Node);
                    }
                }
            }
        }

        XdmNode[] attributes = Nodes.GetXdmNodeAttributes(node);
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            XdmNode attribute = attributes[attributeIndex];
            if (LocalName == "idref")
            {
                if (IsXPathIdReferenceNode(attribute) && XPathIdReferenceMatches(attribute, candidates))
                {
                    result.Add(attribute.Node);
                }
            }
            else
            {
                string? identifier = ReadXPathAttributeIdentifier(node, attribute);
                if (identifier != null && candidates.Contains(identifier) && foundIdentifiers.Add(identifier) && !ContainsXPathNode(result, node.Node))
                {
                    result.Add(node.Node);
                }
            }

            attributeIndex = attributeIndex + 1;
        }

        XdmNode[] children = Nodes.GetXdmNodeChildren(node);
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            AddXPathIdentifierMatches(children[childIndex], candidates, foundIdentifiers, result);
            childIndex = childIndex + 1;
        }
    }

    internal string? ReadXPathElementIdentifier(XdmNode element)
    {
        XmlSchemaTypeInfo? type = Nodes.GetXdmNodeTypeAnnotation(element);
        if (type == null || !type.IsId)
        {
            return null;
        }

        string value = type.NormalizedValue == null ? Nodes.GetXdmNodeStringValue(element) : type.NormalizedValue;
        string normalized = Strings.CollapseXdmStringWhitespace(value);
        return Strings.IsValidXdmName(normalized, true) ? normalized : null;
    }

    internal string? ReadXPathAttributeIdentifier(XdmNode element, XdmNode attribute)
    {
        XmlDocument? document = element.Node.OwnerDocument;
        if (document == null)
        {
            return null;
        }

        return new IdIndex().SelectXmlIdentifierValue(document, element.Node, attribute.Node);
    }

    internal bool IsXPathIdReferenceNode(XdmNode node)
    {
        XmlSchemaTypeInfo? type = Nodes.GetXdmNodeTypeAnnotation(node);
        if (type != null && type.IsIdReference)
        {
            return true;
        }

        if (node.Kind != XdmNodeKind.Attribute || node.Node.Parent == null || node.Node.OwnerDocument == null || node.Node.OwnerDocument.DocumentType == null)
        {
            return false;
        }

        XmlAttributeDeclaration[] declarations = node.Node.OwnerDocument.DocumentType.AttributeDeclarations == null ? [] : node.Node.OwnerDocument.DocumentType.AttributeDeclarations;
        IdIndex identifiers = new IdIndex();
        int index = 0;
        while (index < declarations.Length)
        {
            XmlAttributeDeclaration declaration = declarations[index];
            bool nameMatches = identifiers.SameXmlIdentifierLexicalName(declaration.ElementName, node.Node.Parent.Name) && identifiers.SameXmlIdentifierLexicalName(declaration.Name, node.Node.Name);
            if (nameMatches && (declaration.TypeName == "IDREF" || declaration.TypeName == "IDREFS"))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool XPathIdReferenceMatches(XdmNode node, List<string> candidates)
    {
        string value = Nodes.GetXdmNodeStringValue(node);
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && IsXPathXmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < value.Length && !IsXPathXmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            if (index > start && candidates.Contains(value.Substring(start, index - start)))
            {
                return true;
            }
        }

        return false;
    }

    internal XdmNode? ReadXPathOptionalNodeArgument(XdmSequence supplied)
    {
        if (supplied.Count == 0)
        {
            return null;
        }

        if (supplied.Count != 1)
        {
            throw CreateXPathNodeError("A node argument must contain at most one node.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Node || item.Node == null)
        {
            throw CreateXPathNodeError("The function argument must be a node.", XPathErrorCodes.XPTY0004);
        }

        return Nodes.WrapXmlNode(item.Node);
    }

    internal XdmNode ReadXPathRequiredNodeArgument(XdmSequence supplied)
    {
        XdmNode? node = ReadXPathOptionalNodeArgument(supplied);
        if (node == null)
        {
            throw CreateXPathNodeError("A required node argument is empty.", XPathErrorCodes.XPTY0004);
        }

        return node;
    }

    internal XdmNode ReadXPathContextNode(XPathDynamicContext context)
    {
        XdmSequence supplied = RequireXPathContextSequence(context);
        return ReadXPathRequiredNodeArgument(supplied);
    }

    internal XdmSequence RequireXPathContextSequence(XPathDynamicContext context)
    {
        if (context.ContextItem == null || context.ContextItem.Count == 0)
        {
            throw CreateXPathNodeError("The context item is absent.", XPathErrorCodes.XPDY0002);
        }

        if (context.ContextItem.Count != 1)
        {
            throw CreateXPathNodeError("The context item must contain exactly one item.", XPathErrorCodes.XPTY0004);
        }

        return context.ContextItem;
    }

    internal string ReadXPathOptionalFunctionString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        if (converted.Count == 0)
        {
            return "";
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(converted, 0);
        return item.AtomicValue == null ? "" : Converter.ReadXdmAtomicStringValue(item.AtomicValue);
    }

    internal string CreateXPathLexicalQName(XmlName name)
    {
        if (name.QualifiedName != null && name.QualifiedName.Length != 0)
        {
            return name.QualifiedName;
        }

        string local = name.LocalName == null ? "" : name.LocalName;
        return name.Prefix == null || name.Prefix.Length == 0 ? local : name.Prefix + ":" + local;
    }

    internal XdmSequence CreateXPathNodeStringResult(string value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    internal XdmSequence CreateXPathNodeBooleanResult(bool value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false"));
    }

    internal XPathDynamicException CreateXPathNodeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathNodeRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathNodeRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathNodeLocation(source.Start);
        result.End = CopyXPathNodeLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathNodeLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
