using System;
using System.Collections.Generic;

namespace xml;

internal enum XsltKeyIndexStatus
{
    Pending = 0,
    Building = 1,
    Ready = 2
}

internal class XsltIndexedKeyNode
{
    internal XmlNode Node;
    internal List<XdmSequence> Values;

    internal XsltIndexedKeyNode(XmlNode node)
    {
        Node = node;
        Values = new List<XdmSequence>();
    }
}

internal class XsltDocumentKeyIndex
{
    internal XmlDocument Document;
    internal XsltKeyPlan Key;
    internal long MutationVersion;
    internal XsltKeyIndexStatus Status;
    internal List<XsltIndexedKeyNode> Nodes;

    internal XsltDocumentKeyIndex(XmlDocument document, XsltKeyPlan key)
    {
        Document = document;
        Key = key;
        MutationVersion = document.MutationVersion;
        Status = XsltKeyIndexStatus.Pending;
        Nodes = new List<XsltIndexedKeyNode>();
    }
}

internal class XsltKeyRuntime
{
    internal XsltTransformationState State;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmMapOperations Maps;
    internal XsltPatternMatcher Patterns;
    internal List<XsltDocumentKeyIndex> Indexes;

    internal XsltKeyRuntime(XsltTransformationState state)
    {
        State = state;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Maps = new XdmMapOperations();
        Patterns = new XsltPatternMatcher();
        Indexes = new List<XsltDocumentKeyIndex>();
    }

    internal XdmSequence EvaluateXsltKey(XdmSequence[] arguments, XPathDynamicContext context, XmlNamespaceBinding[] namespaces)
    {
        string lexicalName = ReadXsltKeyNameArgument(arguments[0]);
        string? expandedName = ResolveXsltKeyName(lexicalName, namespaces);
        XsltKeyPlan? key = expandedName == null ? null : FindXsltKeyPlan(expandedName);
        if (key == null)
        {
            throw CreateXsltKeyRuntimeError("The key name is invalid or does not identify a declared key.", XsltErrorCodes.XTDE1260);
        }

        XmlNode top = SelectXsltKeyTop(arguments, context);
        XmlNode root = FindXsltKeyRoot(top);
        if (root.Kind != XmlNodeKind.Document || root.OwnerDocument == null)
        {
            throw CreateXsltKeyRuntimeError("The selected key search tree must be rooted at a document node.", XsltErrorCodes.XTDE1270);
        }

        XdmSequence requested = Converter.AtomizeXdmSequence(arguments[1]);
        XsltDocumentKeyIndex index = GetXsltDocumentKeyIndex(root.OwnerDocument, key);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int nodeIndex = 0;
        while (nodeIndex < index.Nodes.Count)
        {
            XsltIndexedKeyNode candidate = index.Nodes[nodeIndex];
            if (IsXsltKeyDescendantOrSelf(candidate.Node, top) && MatchesXsltRequestedKey(candidate, requested, key))
            {
                result.AddXdmNode(candidate.Node);
            }

            nodeIndex = nodeIndex + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal string ReadXsltKeyNameArgument(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXsltKeyRuntimeError("The key name must be a single string.", XsltErrorCodes.XTDE1260);
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null || !Maps.IsXdmStringMapKey(value.Type))
        {
            throw CreateXsltKeyTypeError("The key name cannot be converted to a string.");
        }

        return Converter.ReadXdmAtomicStringValue(value).Trim();
    }

    internal string? ResolveXsltKeyName(string lexicalName, XmlNamespaceBinding[] namespaces)
    {
        string namespaceUri = "";
        string localName = lexicalName;
        if (lexicalName.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = lexicalName.IndexOf('}');
            if (close < 2 || close == lexicalName.Length - 1 || lexicalName.IndexOf('{', 2) >= 0)
            {
                return null;
            }

            namespaceUri = lexicalName.Substring(2, close - 2);
            localName = lexicalName.Substring(close + 1);
        }
        else
        {
            int colon = lexicalName.IndexOf(':');
            if (colon >= 0)
            {
                if (colon == 0 || colon == lexicalName.Length - 1 || colon != lexicalName.LastIndexOf(':'))
                {
                    return null;
                }

                string prefix = lexicalName.Substring(0, colon);
                localName = lexicalName.Substring(colon + 1);
                string? resolved = FindXsltKeyNamespace(prefix, namespaces);
                if (resolved == null)
                {
                    return null;
                }

                namespaceUri = resolved;
            }
        }

        QualifiedNameParser names = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, null);
        return names.IsValidXmlNcName(localName) ? "Q{" + namespaceUri + "}" + localName : null;
    }

    internal string? FindXsltKeyNamespace(string prefix, XmlNamespaceBinding[] namespaces)
    {
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding binding = namespaces[index];
            if (!binding.IsUndeclaration && binding.Prefix == prefix)
            {
                return binding.NamespaceUri;
            }

            index = index + 1;
        }

        return prefix == "xml" ? XmlNamespaces.Xml : null;
    }

    internal XsltKeyPlan? FindXsltKeyPlan(string expandedName)
    {
        int index = 0;
        while (index < State.Stylesheet.Keys.Length)
        {
            if (State.Stylesheet.Keys[index].Name == expandedName)
            {
                return State.Stylesheet.Keys[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlNode SelectXsltKeyTop(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence? supplied = arguments.Length == 3 ? arguments[2] : context.ContextItem;
        if (supplied == null || supplied.Count != 1)
        {
            throw CreateXsltKeyRuntimeError("The key function requires a single context or top node.", XsltErrorCodes.XTDE1270);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Node == null)
        {
            throw CreateXsltKeyRuntimeError("The key function context or top item must be a node.", XsltErrorCodes.XTDE1270);
        }

        XmlNode top = item.Node;
        if (arguments.Length == 2)
        {
            top = FindXsltKeyRoot(top);
        }

        return top;
    }

    internal XmlNode FindXsltKeyRoot(XmlNode node)
    {
        XmlNode result = node;
        while (result.Parent != null)
        {
            result = result.Parent;
        }

        return result;
    }

    internal XsltDocumentKeyIndex GetXsltDocumentKeyIndex(XmlDocument document, XsltKeyPlan key)
    {
        XsltDocumentKeyIndex? index = FindXsltDocumentKeyIndex(document, key);
        if (index == null)
        {
            index = new XsltDocumentKeyIndex(document, key);
            Indexes.Add(index);
        }
        else if (index.MutationVersion != document.MutationVersion)
        {
            index.MutationVersion = document.MutationVersion;
            index.Status = XsltKeyIndexStatus.Pending;
            index.Nodes = new List<XsltIndexedKeyNode>();
        }

        if (index.Status == XsltKeyIndexStatus.Building)
        {
            throw CreateXsltKeyRuntimeError("A circular key definition was evaluated.", XsltErrorCodes.XTDE0640);
        }

        if (index.Status == XsltKeyIndexStatus.Pending)
        {
            BuildXsltDocumentKeyIndex(index);
        }

        return index;
    }

    internal XsltDocumentKeyIndex? FindXsltDocumentKeyIndex(XmlDocument document, XsltKeyPlan key)
    {
        int index = 0;
        while (index < Indexes.Count)
        {
            if (object.ReferenceEquals(Indexes[index].Document, document) && object.ReferenceEquals(Indexes[index].Key, key))
            {
                return Indexes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void BuildXsltDocumentKeyIndex(XsltDocumentKeyIndex index)
    {
        XmlNode? root = index.Document.DocumentNode;
        if (root == null)
        {
            index.Status = XsltKeyIndexStatus.Ready;
            return;
        }

        index.Status = XsltKeyIndexStatus.Building;
        List<XmlNode> documentNodes = new List<XmlNode>();
        CollectXsltKeyDocumentNodes(documentNodes, root);
        int nodeIndex = 0;
        while (nodeIndex < documentNodes.Count)
        {
            XmlNode node = documentNodes[nodeIndex];
            XsltIndexedKeyNode values = new XsltIndexedKeyNode(node);
            int declarationIndex = 0;
            while (declarationIndex < index.Key.Declarations.Length)
            {
                XsltKeyDeclarationPlan declaration = index.Key.Declarations[declarationIndex];
                if (MatchesXsltKeyDeclaration(node, declaration))
                {
                    AddXsltKeySpecifierValues(values, EvaluateXsltKeySpecifier(node, declaration), index.Key.Composite, declaration);
                }

                declarationIndex = declarationIndex + 1;
            }

            if (values.Values.Count != 0)
            {
                index.Nodes.Add(values);
            }

            nodeIndex = nodeIndex + 1;
        }

        index.Status = XsltKeyIndexStatus.Ready;
    }

    internal void CollectXsltKeyDocumentNodes(List<XmlNode> result, XmlNode node)
    {
        result.Add(node);
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            result.Add(attributes[index]);
            index = index + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        index = 0;
        while (index < children.Length)
        {
            CollectXsltKeyDocumentNodes(result, children[index]);
            index = index + 1;
        }
    }

    internal bool MatchesXsltKeyDeclaration(XmlNode node, XsltKeyDeclarationPlan declaration)
    {
        XdmSequence candidate = Sequences.CreateXdmNodeSequence(node);
        XPathDynamicContext focused = CreateXsltKeyFocus(candidate);
        return Patterns.MatchesXsltPattern(declaration.Match, candidate, focused);
    }

    internal XdmSequence EvaluateXsltKeySpecifier(XmlNode node, XsltKeyDeclarationPlan declaration)
    {
        XdmSequence candidate = Sequences.CreateXdmNodeSequence(node);
        XPathDynamicContext focused = CreateXsltKeyFocus(candidate);
        XsltVariableFrame frame = new XsltVariableFrame(null, [], focused);
        XdmSequence value;
        if (declaration.ValueKind == XsltKeyValueKind.Expression)
        {
            XPathPlan plan = declaration.Use == null ? throw new InvalidOperationException("A key use expression has no compiled plan.") : declaration.Use;
            value = State.TemplateRuntime == null ? Sequences.CreateEmptyXdmSequence() : State.TemplateRuntime.EvaluateXsltInstructionExpression(plan, frame);
        }
        else
        {
            XsltSequenceConstructorPlan content = declaration.Content == null ? throw new InvalidOperationException("A key sequence constructor has no compiled plan.") : declaration.Content;
            XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Named, null, null, State.Stylesheet.InitialMode, candidate, 1, 1, State.TemplateRuntime == null ? 0 : State.TemplateRuntime.Depth);
            invocation.Frame = frame;
            value = State.TemplateRuntime == null ? Sequences.CreateEmptyXdmSequence() : State.TemplateRuntime.SequenceRuntime.EvaluateXsltSequenceConstructor(content, invocation);
        }

        return Converter.AtomizeXdmSequence(value);
    }

    internal XPathDynamicContext CreateXsltKeyFocus(XdmSequence candidate)
    {
        XPathDynamicContext focused = State.TemplateRuntime == null ? new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(State.DynamicContext) : State.TemplateRuntime.CreateXsltTemplateFocusContext(State.DynamicContext, candidate, 1, 1);
        focused.ContextItem = candidate;
        focused.Position = 1;
        focused.Size = 1;
        XsltVariableFrame frame = new XsltVariableFrame(null, [], focused);
        return State.VariableRuntime == null ? focused : State.VariableRuntime.CreateXsltVariableDynamicContext(frame);
    }

    internal void AddXsltKeySpecifierValues(XsltIndexedKeyNode node, XdmSequence value, bool composite, XsltKeyDeclarationPlan declaration)
    {
        XdmSequence normalized = UsesXsltBackwardsKeyBehavior(declaration) ? ConvertXsltKeyValuesToStrings(value) : value;
        if (composite)
        {
            node.Values.Add(normalized);
            return;
        }

        int index = 0;
        while (index < normalized.Count)
        {
            node.Values.Add(CreateXsltKeyAtomicSequence(normalized, index));
            index = index + 1;
        }
    }

    internal bool UsesXsltBackwardsKeyBehavior(XsltKeyDeclarationPlan declaration)
    {
        return declaration.StaticContext.CompatibilityBehavior == XsltCompatibilityBehavior.Backwards;
    }

    internal XdmSequence ConvertXsltKeyValuesToStrings(XdmSequence value)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < value.Count)
        {
            XdmAtomicValue? atomic = Sequences.ReadXdmSequenceItem(value, index).AtomicValue;
            if (atomic != null)
            {
                result.AddXdmAtomicValue(new XdmFactory().CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(atomic), XdmAtomicType.String));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXsltKeyAtomicSequence(XdmSequence value, int index)
    {
        XdmAtomicValue? atomic = Sequences.ReadXdmSequenceItem(value, index).AtomicValue;
        return atomic == null ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(atomic);
    }

    internal bool MatchesXsltRequestedKey(XsltIndexedKeyNode node, XdmSequence requested, XsltKeyPlan key)
    {
        XdmSequence effective = UsesXsltBackwardsKeyPlan(key) ? ConvertXsltKeyValuesToStrings(requested) : requested;
        int valueIndex = 0;
        while (valueIndex < node.Values.Count)
        {
            XdmSequence indexed = node.Values[valueIndex];
            if (key.Composite ? SameXsltCompositeKey(indexed, effective, key.Collation) : AnyXsltKeyValueEqual(indexed, effective, key.Collation))
            {
                return true;
            }

            valueIndex = valueIndex + 1;
        }

        return false;
    }

    internal bool UsesXsltBackwardsKeyPlan(XsltKeyPlan key)
    {
        int index = 0;
        while (index < key.Declarations.Length)
        {
            if (UsesXsltBackwardsKeyBehavior(key.Declarations[index]))
            {
                return !key.Composite;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AnyXsltKeyValueEqual(XdmSequence indexed, XdmSequence requested, string collation)
    {
        if (indexed.Count == 0)
        {
            return false;
        }

        XdmAtomicValue? left = Sequences.ReadXdmSequenceItem(indexed, 0).AtomicValue;
        int index = 0;
        while (left != null && index < requested.Count)
        {
            XdmAtomicValue? right = Sequences.ReadXdmSequenceItem(requested, index).AtomicValue;
            if (right != null && SameXsltKeyAtomic(left, right, collation, State.DynamicContext.ImplicitTimezoneMinutes, false))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool SameXsltCompositeKey(XdmSequence left, XdmSequence right, string collation)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            XdmAtomicValue? leftValue = Sequences.ReadXdmSequenceItem(left, index).AtomicValue;
            XdmAtomicValue? rightValue = Sequences.ReadXdmSequenceItem(right, index).AtomicValue;
            if (leftValue == null || rightValue == null || !SameXsltKeyAtomic(leftValue, rightValue, collation, State.DynamicContext.ImplicitTimezoneMinutes, true))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool SameXsltKeyAtomic(XdmAtomicValue left, XdmAtomicValue right, string collation, int implicitTimezoneMinutes, bool deep)
    {
        bool leftString = Maps.IsXdmStringMapKey(left.Type);
        bool rightString = Maps.IsXdmStringMapKey(right.Type);
        if (leftString && rightString)
        {
            string leftText = Converter.ReadXdmAtomicStringValue(left);
            string rightText = Converter.ReadXdmAtomicStringValue(right);
            return collation == XPathAsciiCaseInsensitiveCollation.Uri ? new XPathAsciiCaseInsensitiveCollation().CompareXPathCollation(leftText, rightText) == 0 : string.Equals(leftText, rightText, StringComparison.Ordinal);
        }

        if (left.Type == XdmAtomicType.UntypedAtomic || right.Type == XdmAtomicType.UntypedAtomic)
        {
            return false;
        }

        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            bool leftNan = (left.Type == XdmAtomicType.Float || left.Type == XdmAtomicType.Double) && double.IsNaN(left.FloatingValue);
            bool rightNan = (right.Type == XdmAtomicType.Float || right.Type == XdmAtomicType.Double) && double.IsNaN(right.FloatingValue);
            if (leftNan || rightNan)
            {
                return deep && leftNan && rightNan;
            }
        }

        if (left.Type >= XdmAtomicType.DateTime && left.Type <= XdmAtomicType.GDay && right.Type >= XdmAtomicType.DateTime && right.Type <= XdmAtomicType.GDay)
        {
            bool compatible = left.Type == right.Type || left.Type == XdmAtomicType.DateTime && right.Type == XdmAtomicType.DateTimeStamp || left.Type == XdmAtomicType.DateTimeStamp && right.Type == XdmAtomicType.DateTime;
            return compatible && new XdmDateTimeDatatype().EqualXdmDateTime(left, right, implicitTimezoneMinutes);
        }

        return Maps.SameXdmMapKey(left, right);
    }

    internal bool IsXsltKeyDescendantOrSelf(XmlNode node, XmlNode top)
    {
        XmlNode? current = node;
        while (current != null)
        {
            if (object.ReferenceEquals(current, top))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal XPathDynamicException CreateXsltKeyRuntimeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }

    internal XPathTypeException CreateXsltKeyTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathTypeException(message, diagnostic, null);
    }
}
