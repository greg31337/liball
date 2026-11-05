using System;
using System.Collections.Generic;

namespace xml;

internal class XPathPathEvaluator
{
    internal XPathRuntime Runtime;
    internal XdmNodeAccessor Nodes;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmNumericDatatype Numerics;
    internal HashSet<XmlNode> StreamedNodes;
    internal XsltStreamedNodeRuntime? StreamedNodeRuntime;

    internal XPathPathEvaluator(XPathRuntime runtime)
    {
        Runtime = runtime;
        Nodes = new XdmNodeAccessor();
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Numerics = new XdmNumericDatatype();
        StreamedNodes = new HashSet<XmlNode>();
        StreamedNodeRuntime = runtime.Context.Snapshot.XsltStreamedNodes;
    }

    internal XdmSequence EvaluateXPathPath(XPathPlanInstruction instruction)
    {
        if (instruction.Absolute)
        {
            XdmSequence root = SelectXPathRoot(instruction.Range);
            if (instruction.Children.Length == 0)
            {
                return root;
            }

            XdmSequence starting = instruction.LeadingDescendant ? ExpandXPathDescendantOrSelf(root, instruction.Range) : root;
            XdmSequence current = EvaluateXPathStepAcrossNodes(starting, instruction.Children[0], instruction.Children.Length == 1);
            return EvaluateXPathRemainingSteps(instruction, current, 1);
        }

        if (instruction.Children.Length == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmSequence first = Runtime.EvaluateXPathInstruction(instruction.Children[0]);
        return EvaluateXPathRemainingSteps(instruction, first, 1);
    }

    internal XdmSequence EvaluateXPathRemainingSteps(XPathPlanInstruction path, XdmSequence initial, int startIndex)
    {
        XdmSequence current = initial;
        int index = startIndex;
        while (index < path.Children.Length)
        {
            string operation = path.PathOperators[index - 1];
            XdmSequence inputs = RequireXPathNodeSequence(current, XPathErrorCodes.XPTY0019, path.Children[index - 1].Range);
            if (operation == "//")
            {
                inputs = ExpandXPathDescendantOrSelf(inputs, path.Children[index].Range);
            }

            current = EvaluateXPathStepAcrossNodes(inputs, path.Children[index], index + 1 == path.Children.Length);
            index = index + 1;
        }

        if (path.Children.Length > 1 || path.Absolute)
        {
            return NormalizeXPathPathResult(current, path.Range);
        }

        return current;
    }

    internal XdmSequence EvaluateXPathStepAcrossNodes(XdmSequence input, XPathPlanInstruction step, bool finalStep)
    {
        XdmNode[] nodes = ReadXPathNodes(input, XPathErrorCodes.XPTY0019, step.Range);
        XdmSequence[] values = new XdmSequence[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            XdmSequence item = CreateXPathNodeSequence(nodes[index]);
            Runtime.Focus.PushXPathFocus(item, index + 1, nodes.Length);
            values[index] = Runtime.EvaluateXPathInstruction(step);
            Runtime.Focus.PopXPathFocus();
            index = index + 1;
        }

        XdmSequence result = Sequences.ConcatenateXdmSequences(values);
        if (!finalStep)
        {
            return RequireXPathNodeSequence(result, XPathErrorCodes.XPTY0019, step.Range);
        }

        return result;
    }

    internal XdmSequence NormalizeXPathPathResult(XdmSequence value, SourceRange range)
    {
        XdmSequenceCursor cursor = new XdmSequenceCursor(value);
        bool hasNodes = false;
        bool hasOtherItems = false;
        List<XdmNode> nodes = new List<XdmNode>();
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Kind == XdmItemKind.Node && item.Node != null)
            {
                hasNodes = true;
                nodes.Add(Nodes.WrapXmlNode(item.Node));
            }
            else
            {
                hasOtherItems = true;
            }
        }

        if (hasNodes && hasOtherItems)
        {
            throw Runtime.CreateXPathRuntimeError("A path result cannot mix nodes with non-node items.", XPathErrorCodes.XPTY0018, range);
        }

        return hasNodes ? CreateNormalizedXPathNodeSequence(nodes, range) : value;
    }

    internal XdmSequence SelectXPathRoot(SourceRange range)
    {
        XPathFocusFrame focus = Runtime.Focus.ReadXPathFocus();
        if (focus.ContextItem == null || focus.ContextItem.Count == 0)
        {
            throw Runtime.CreateXPathRuntimeError("An absolute XPath path requires a context item.", XPathErrorCodes.XPDY0002, range);
        }

        XdmNode[] context = ReadXPathNodes(focus.ContextItem, XPathErrorCodes.XPTY0020, range);
        if (context.Length != 1)
        {
            throw Runtime.CreateXPathRuntimeError("An absolute XPath path requires one context node.", XPathErrorCodes.XPTY0020, range);
        }

        XdmNode current = context[0];
        RequireXPathStreamNavigation(current, XPathAxis.Parent, range);
        XdmNode? parent = Nodes.GetXdmNodeParent(current);
        while (parent != null)
        {
            current = parent;
            parent = Nodes.GetXdmNodeParent(current);
        }

        return CreateXPathNodeSequence(current);
    }

    internal XdmSequence EvaluateXPathAxis(XPathPlanInstruction instruction)
    {
        XPathFocusFrame focus = Runtime.Focus.ReadXPathFocus();
        if (focus.ContextItem == null || focus.ContextItem.Count == 0)
        {
            throw Runtime.CreateXPathRuntimeError("An XPath axis step requires a context item.", XPathErrorCodes.XPDY0002, instruction.Range);
        }

        XdmNode[] context = ReadXPathNodes(focus.ContextItem, XPathErrorCodes.XPTY0020, instruction.Range);
        if (context.Length != 1)
        {
            throw Runtime.CreateXPathRuntimeError("An XPath axis step requires one context node.", XPathErrorCodes.XPTY0020, instruction.Range);
        }

        XdmNode node = context[0];
        RequireXPathStreamNavigation(node, instruction.Axis, instruction.Range);
        XPathAxis effectiveAxis = instruction.Axis == XPathAxis.Child && instruction.NodeTest != null && instruction.NodeTest.Kind == XPathNodeTestKind.NamespaceNode ? XPathAxis.Namespace : instruction.Axis;
        List<XdmNode> candidates = SelectXPathAxisCandidates(node, effectiveAxis);
        List<XdmNode> selected = new List<XdmNode>();
        int index = 0;
        while (index < candidates.Count)
        {
            if (MatchesXPathNodeTest(candidates[index], instruction.NodeTest, effectiveAxis))
            {
                selected.Add(candidates[index]);
            }

            index = index + 1;
        }

        RegisterXPathStreamNavigationResult(selected, instruction.Axis);

        return CreateNormalizedXPathNodeSequence(selected, instruction.Range);
    }

    internal List<XdmNode> SelectXPathAxisCandidates(XdmNode node, XPathAxis axis)
    {
        List<XdmNode> result = new List<XdmNode>();
        if (axis == XPathAxis.Self)
        {
            result.Add(node);
        }
        else if (axis == XPathAxis.Child)
        {
            AddXPathNodes(result, Nodes.GetXdmNodeChildren(node));
        }
        else if (axis == XPathAxis.Descendant)
        {
            AddXPathDescendants(result, node, new HashSet<XmlNode>());
        }
        else if (axis == XPathAxis.DescendantOrSelf)
        {
            result.Add(node);
            AddXPathDescendants(result, node, new HashSet<XmlNode>());
        }
        else if (axis == XPathAxis.Attribute)
        {
            AddXPathNodes(result, Nodes.GetXdmNodeAttributes(node));
        }
        else if (axis == XPathAxis.Namespace)
        {
            AddXPathNodes(result, Nodes.GetXdmNodeNamespaces(node));
        }
        else if (axis == XPathAxis.Parent)
        {
            XdmNode? parent = Nodes.GetXdmNodeParent(node);
            if (parent != null)
            {
                result.Add(parent);
            }
        }
        else if (axis == XPathAxis.Ancestor || axis == XPathAxis.AncestorOrSelf)
        {
            AddXPathAncestors(result, node, axis == XPathAxis.AncestorOrSelf);
        }
        else if (axis == XPathAxis.FollowingSibling || axis == XPathAxis.PrecedingSibling)
        {
            AddXPathSiblings(result, node, axis == XPathAxis.FollowingSibling);
        }
        else if (axis == XPathAxis.Following || axis == XPathAxis.Preceding)
        {
            AddXPathFollowingOrPreceding(result, node, axis == XPathAxis.Following);
        }

        return result;
    }

    internal void AddXPathDescendants(List<XdmNode> result, XdmNode node, HashSet<XmlNode> visited)
    {
        if (!visited.Add(node.Node))
        {
            throw new ArgumentException("The XML tree contains a cycle.", "node");
        }

        XdmNode[] children = Nodes.GetXdmNodeChildren(node);
        int index = 0;
        while (index < children.Length)
        {
            result.Add(children[index]);
            AddXPathDescendants(result, children[index], visited);
            index = index + 1;
        }
    }

    internal void AddXPathAncestors(List<XdmNode> result, XdmNode node, bool includeSelf)
    {
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        XdmNode? current = includeSelf ? node : Nodes.GetXdmNodeParent(node);
        while (current != null)
        {
            if (!visited.Add(current.Node))
            {
                throw new ArgumentException("The XML tree contains a parent cycle.", "node");
            }

            result.Add(current);
            current = Nodes.GetXdmNodeParent(current);
        }
    }

    internal void AddXPathSiblings(List<XdmNode> result, XdmNode node, bool following)
    {
        if (node.Kind == XdmNodeKind.Attribute || node.Kind == XdmNodeKind.Namespace)
        {
            return;
        }

        XdmNode? parent = Nodes.GetXdmNodeParent(node);
        if (parent == null)
        {
            return;
        }

        XdmNode[] siblings = Nodes.GetXdmNodeChildren(parent);
        int position = IndexXPathNode(siblings, node);
        if (position < 0)
        {
            throw new ArgumentException("The node is not present among its projected siblings.", "node");
        }

        int index = following ? position + 1 : 0;
        int end = following ? siblings.Length : position;
        while (index < end)
        {
            result.Add(siblings[index]);
            index = index + 1;
        }
    }

    internal void AddXPathFollowingOrPreceding(List<XdmNode> result, XdmNode node, bool following)
    {
        XdmNode root = FindXPathRootNode(node);
        List<XdmNode> order = new List<XdmNode>();
        Nodes.AddXdmDocumentOrder(order, root, new HashSet<XmlNode>());
        int position = IndexXPathNodeList(order, node);
        if (position < 0)
        {
            throw new ArgumentException("The node is not present in its projected tree.", "node");
        }

        int index = following ? position + 1 : 0;
        int end = following ? order.Count : position;
        while (index < end)
        {
            XdmNode candidate = order[index];
            bool principalNode = candidate.Kind != XdmNodeKind.Attribute && candidate.Kind != XdmNodeKind.Namespace;
            bool excluded = following ? IsXPathDescendant(candidate, node) : IsXPathAncestor(candidate, node);
            if (principalNode && !excluded)
            {
                result.Add(candidate);
            }

            index = index + 1;
        }
    }

    internal XdmNode FindXPathRootNode(XdmNode node)
    {
        XdmNode current = node;
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        XdmNode? parent = Nodes.GetXdmNodeParent(current);
        while (parent != null)
        {
            if (!visited.Add(current.Node))
            {
                throw new ArgumentException("The XML tree contains a parent cycle.", "node");
            }

            current = parent;
            parent = Nodes.GetXdmNodeParent(current);
        }

        return current;
    }

    internal bool IsXPathDescendant(XdmNode candidate, XdmNode ancestor)
    {
        XdmNode? current = Nodes.GetXdmNodeParent(candidate);
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        while (current != null)
        {
            if (HasSameXPathNodeIdentity(current, ancestor))
            {
                return true;
            }

            if (!visited.Add(current.Node))
            {
                throw new ArgumentException("The XML tree contains a parent cycle.", "candidate");
            }

            current = Nodes.GetXdmNodeParent(current);
        }

        return false;
    }

    internal bool IsXPathAncestor(XdmNode candidate, XdmNode node)
    {
        return IsXPathDescendant(node, candidate);
    }

    internal bool MatchesXPathNodeTest(XdmNode node, XPathNodeTest? test, XPathAxis axis)
    {
        if (test == null)
        {
            return true;
        }

        if (test.Kind == XPathNodeTestKind.AnyKind)
        {
            return true;
        }

        if (test.Kind == XPathNodeTestKind.Document)
        {
            return MatchesXPathDocumentTest(node, test.InnerTest);
        }

        if (test.Kind == XPathNodeTestKind.Text)
        {
            return node.Kind == XdmNodeKind.Text;
        }

        if (test.Kind == XPathNodeTestKind.Comment)
        {
            return node.Kind == XdmNodeKind.Comment;
        }

        if (test.Kind == XPathNodeTestKind.NamespaceNode)
        {
            return node.Kind == XdmNodeKind.Namespace;
        }

        if (test.Kind == XPathNodeTestKind.ProcessingInstruction)
        {
            return MatchesXPathProcessingInstruction(node, test.Value);
        }

        if (test.Kind == XPathNodeTestKind.Attribute || test.Kind == XPathNodeTestKind.SchemaAttribute)
        {
            return MatchesXPathAttributeTest(node, test);
        }

        if (test.Kind == XPathNodeTestKind.Element || test.Kind == XPathNodeTestKind.SchemaElement)
        {
            return MatchesXPathElementTest(node, test);
        }

        if (!MatchesXPathPrincipalKind(node, axis))
        {
            return false;
        }

        XmlName? name = Nodes.GetXdmNodeName(node);
        if (test.Kind == XPathNodeTestKind.Wildcard)
        {
            return true;
        }

        if (name == null)
        {
            return false;
        }

        if (test.Kind == XPathNodeTestKind.NamespaceWildcard)
        {
            return NormalizeXPathNamespace(name.NamespaceUri) == NormalizeXPathNamespace(test.NamespaceUri);
        }

        if (test.Kind == XPathNodeTestKind.LocalNameWildcard)
        {
            return name.LocalName == test.LocalName;
        }

        if (test.Kind == XPathNodeTestKind.UriWildcard)
        {
            return NormalizeXPathNamespace(name.NamespaceUri) == NormalizeXPathNamespace(test.NamespaceUri);
        }

        return SameXPathName(name, test.ResolvedName);
    }

    internal bool MatchesXPathDocumentTest(XdmNode node, XPathNodeTest? innerTest)
    {
        if (node.Kind != XdmNodeKind.Document)
        {
            return false;
        }

        if (innerTest == null)
        {
            return true;
        }

        XdmNode[] children = Nodes.GetXdmNodeChildren(node);
        int elementCount = 0;
        bool matched = false;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XdmNodeKind.Element)
            {
                elementCount = elementCount + 1;
                matched = MatchesXPathNodeTest(children[index], innerTest, XPathAxis.Child);
            }

            index = index + 1;
        }

        return elementCount == 1 && matched;
    }

    internal bool MatchesXPathProcessingInstruction(XdmNode node, string? target)
    {
        if (node.Kind != XdmNodeKind.ProcessingInstruction)
        {
            return false;
        }

        if (target == null || target.Length == 0)
        {
            return true;
        }

        XmlName? name = Nodes.GetXdmNodeName(node);
        return name != null && name.LocalName == target;
    }

    internal bool MatchesXPathAttributeTest(XdmNode node, XPathNodeTest test)
    {
        if (node.Kind != XdmNodeKind.Attribute)
        {
            return false;
        }

        if (test.Kind == XPathNodeTestKind.SchemaAttribute)
        {
            XPathSchemaNodeTest? schema = Runtime.Plan.Environment.SchemaTests;
            return schema != null && test.ResolvedName != null && schema.MatchesXPathSchemaAttribute(node.Node, test.ResolvedName);
        }

        if (!MatchesXPathOptionalName(node, test))
        {
            return false;
        }

        return MatchesXPathNodeType(node, test.Type, false);
    }

    internal bool MatchesXPathElementTest(XdmNode node, XPathNodeTest test)
    {
        if (node.Kind != XdmNodeKind.Element)
        {
            return false;
        }

        if (test.Kind == XPathNodeTestKind.SchemaElement)
        {
            XPathSchemaNodeTest? schema = Runtime.Plan.Environment.SchemaTests;
            return schema != null && test.ResolvedName != null && schema.MatchesXPathSchemaElement(node.Node, test.ResolvedName);
        }

        if (!MatchesXPathOptionalName(node, test) || !MatchesXPathNodeType(node, test.Type, test.Nillable))
        {
            return false;
        }

        return test.Nillable || node.Node.TypeInfo == null || !node.Node.TypeInfo.IsNilled;
    }

    internal bool MatchesXPathOptionalName(XdmNode node, XPathNodeTest test)
    {
        if (test.LocalName == null || test.LocalName == "*")
        {
            return true;
        }

        return SameXPathName(Nodes.GetXdmNodeName(node), test.ResolvedName);
    }

    internal bool MatchesXPathNodeType(XdmNode node, XPathTypeSyntax? type, bool nillable)
    {
        if (type == null)
        {
            return true;
        }

        if (!nillable && node.Node.TypeInfo != null && node.Node.TypeInfo.IsNilled)
        {
            return false;
        }

        XmlName? expected = type.ResolvedName;
        if (expected == null)
        {
            return false;
        }

        XPathSchemaNodeTest? schema = Runtime.Plan.Environment.SchemaTests;
        if (schema != null && type.SchemaType)
        {
            return schema.MatchesXPathSchemaType(node.Node, expected);
        }

        XmlSchemaTypeInfo? annotation = node.Node.TypeInfo;
        if (annotation != null && SameXPathName(annotation.TypeName, expected))
        {
            return true;
        }

        if (expected.NamespaceUri == XPathNamespaces.XmlSchema && node.Kind == XdmNodeKind.Element && expected.LocalName == "untyped")
        {
            return annotation == null;
        }

        return expected.NamespaceUri == XPathNamespaces.XmlSchema && node.Kind == XdmNodeKind.Attribute && expected.LocalName == "untypedAtomic" && annotation == null;
    }

    internal bool MatchesXPathPrincipalKind(XdmNode node, XPathAxis axis)
    {
        if (axis == XPathAxis.Attribute)
        {
            return node.Kind == XdmNodeKind.Attribute;
        }

        if (axis == XPathAxis.Namespace)
        {
            return node.Kind == XdmNodeKind.Namespace;
        }

        return node.Kind == XdmNodeKind.Element;
    }

    internal XdmSequence EvaluateXPathPredicate(XPathPlanInstruction instruction)
    {
        if (instruction.Left == null || instruction.Right == null)
        {
            throw new InvalidOperationException("An XPath predicate instruction requires an input and predicate expression.");
        }

        XdmSequence input = Runtime.EvaluateXPathInstruction(instruction.Left);
        bool axisPredicate = IsXPathAxisPredicate(instruction.Left);
        bool reverse = axisPredicate && IsXPathReverseAxisPredicate(instruction.Left);
        XdmSequence ordered = axisPredicate ? OrderXPathPredicateInput(input, reverse, instruction.Range) : input;
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XdmSequenceCursor cursor = new XdmSequenceCursor(ordered);
        int position = 0;
        while (cursor.MoveNextXdmSequenceItem())
        {
            position = position + 1;
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            XdmSequence focusItem = CreateXPathItemSequence(item);
            Runtime.Focus.PushXPathFocus(focusItem, position, ordered.Count);
            XdmSequence predicate = Runtime.EvaluateXPathInstruction(instruction.Right);
            Runtime.Focus.PopXPathFocus();
            if (MatchesXPathPredicatePosition(predicate, position, instruction.Right.Range))
            {
                AddXPathSequenceItem(result, item);
            }
        }

        XdmSequence filtered = result.BuildImmutableXdmSequence();
        return axisPredicate ? OrderXPathPredicateInput(filtered, false, instruction.Range) : filtered;
    }

    internal bool MatchesXPathPredicatePosition(XdmSequence predicate, int position, SourceRange range)
    {
        if (predicate.Count == 1)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(predicate, 0);
            if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null && new XdmTypeSystem().IsXdmNumericAtomicType(item.AtomicValue.Type))
            {
                XdmAtomicValue expected = Runtime.Factory.CreateXdmIntegerAtomicValue(position.ToString(System.Globalization.CultureInfo.InvariantCulture), XdmAtomicType.Integer);
                return Numerics.EqualXdmNumeric(item.AtomicValue, expected);
            }
        }

        return Runtime.Operators.EvaluateXPathEffectiveBooleanValue(predicate, range);
    }

    internal XdmSequence OrderXPathPredicateInput(XdmSequence input, bool reverse, SourceRange range)
    {
        XdmNode[] nodes = ReadXPathNodes(input, XPathErrorCodes.XPTY0004, range);
        List<XdmNode> ordered = new List<XdmNode>();
        AddXPathNodes(ordered, nodes);
        SortXPathNodes(ordered, reverse, range);
        return CreateXPathNodeSequenceFromList(ordered);
    }

    internal bool IsXPathAxisPredicate(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction current = instruction;
        while (current.Opcode == XPathPlanOpcode.Predicate && current.Left != null)
        {
            current = current.Left;
        }

        return current.Opcode == XPathPlanOpcode.Name || current.Opcode == XPathPlanOpcode.ParentStep || current.Opcode == XPathPlanOpcode.AttributeStep || current.Opcode == XPathPlanOpcode.AxisStep;
    }

    internal bool IsXPathReverseAxisPredicate(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction current = instruction;
        while (current.Opcode == XPathPlanOpcode.Predicate && current.Left != null)
        {
            current = current.Left;
        }

        return current.ReverseAxis;
    }

    internal XdmSequence ExpandXPathDescendantOrSelf(XdmSequence input, SourceRange range)
    {
        XdmNode[] roots = ReadXPathNodes(input, XPathErrorCodes.XPTY0019, range);
        List<XdmNode> result = new List<XdmNode>();
        int index = 0;
        while (index < roots.Length)
        {
            RequireXPathStreamNavigation(roots[index], XPathAxis.DescendantOrSelf, range);
            result.Add(roots[index]);
            AddXPathDescendants(result, roots[index], new HashSet<XmlNode>());
            index = index + 1;
        }

        RegisterXPathStreamNavigationResult(result, XPathAxis.DescendantOrSelf);

        return CreateNormalizedXPathNodeSequence(result, range);
    }

    internal XdmSequence RequireXPathNodeSequence(XdmSequence sequence, string code, SourceRange range)
    {
        XdmNode[] nodes = ReadXPathNodes(sequence, code, range);
        List<XdmNode> values = new List<XdmNode>();
        AddXPathNodes(values, nodes);
        return CreateNormalizedXPathNodeSequence(values, range);
    }

    internal XdmNode[] ReadXPathNodes(XdmSequence sequence, string code, SourceRange range)
    {
        List<XdmNode> result = new List<XdmNode>();
        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Kind != XdmItemKind.Node || item.Node == null)
            {
                throw Runtime.CreateXPathRuntimeError("An XPath path or axis operand must contain only nodes.", code, range);
            }

            result.Add(Nodes.WrapXmlNode(item.Node));
        }

        return result.ToArray();
    }

    internal XdmSequence CreateNormalizedXPathNodeSequence(List<XdmNode> nodes, SourceRange range)
    {
        List<XdmNode> normalized = new List<XdmNode>();
        HashSet<XmlNode> identities = new HashSet<XmlNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            if (identities.Add(nodes[index].Node))
            {
                normalized.Add(nodes[index]);
            }

            index = index + 1;
        }

        SortXPathNodes(normalized, false, range);
        return CreateXPathNodeSequenceFromList(normalized);
    }

    internal void SortXPathNodes(List<XdmNode> nodes, bool reverse, SourceRange range)
    {
        if (nodes.Count > 1)
        {
            int streamedCount = 0;
            int managedCount = 0;
            int forwardOnlyCount = 0;
            int streamedIndex = 0;
            while (streamedIndex < nodes.Count)
            {
                if (StreamedNodes.Contains(nodes[streamedIndex].Node))
                {
                    streamedCount = streamedCount + 1;
                }

                if (StreamedNodeRuntime != null && StreamedNodeRuntime.IsXsltStreamedNode(nodes[streamedIndex].Node))
                {
                    managedCount = managedCount + 1;
                    if (StreamedNodeRuntime.IsXsltForwardOnlyNode(nodes[streamedIndex].Node))
                    {
                        forwardOnlyCount = forwardOnlyCount + 1;
                    }
                }

                streamedIndex = streamedIndex + 1;
            }

            if (streamedCount > 0)
            {
                throw Runtime.CreateXPathRuntimeError("Streamed XPath nodes cannot be reordered or retained as a multi-node result.", XPathErrorCodes.XPDY0130, range);
            }

            if (forwardOnlyCount > 0)
            {
                XmlNode[] managed = new XmlNode[nodes.Count];
                streamedIndex = 0;
                while (streamedIndex < nodes.Count)
                {
                    managed[streamedIndex] = nodes[streamedIndex].Node;
                    streamedIndex = streamedIndex + 1;
                }

                if (reverse || managedCount != nodes.Count || forwardOnlyCount != nodes.Count || StreamedNodeRuntime == null || !StreamedNodeRuntime.IsXsltForwardStreamSequence(managed))
                {
                    throw Runtime.CreateXPathRuntimeError("Streamed XPath nodes can only be delivered once in forward document order.", XPathErrorCodes.XPDY0130, range);
                }

                return;
            }
        }

        Dictionary<XmlNode, int>? documentOrder = nodes.Count > 32 ? BuildXPathNodeDocumentOrder(nodes) : null;
        int index = 1;
        while (index < nodes.Count)
        {
            XdmNode item = nodes[index];
            int insertion = index;
            while (insertion > 0)
            {
                int order = documentOrder == null ? CompareXPathNodeOrder(nodes[insertion - 1], item) : CompareXPathIndexedNodeOrder(nodes[insertion - 1], item, documentOrder);
                bool move = reverse ? order < 0 : order > 0;
                if (!move)
                {
                    break;
                }

                nodes[insertion] = nodes[insertion - 1];
                insertion = insertion - 1;
            }

            nodes[insertion] = item;
            index = index + 1;
        }
    }

    internal Dictionary<XmlNode, int> BuildXPathNodeDocumentOrder(List<XdmNode> nodes)
    {
        Dictionary<XmlNode, int> result = new Dictionary<XmlNode, int>();
        HashSet<XmlNode> roots = new HashSet<XmlNode>();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            XmlNode root = Nodes.Navigation.FindXmlNavigationRoot(nodes[nodeIndex].Node);
            if (roots.Add(root))
            {
                List<XdmNode> ordered = new List<XdmNode>();
                Nodes.AddXdmDocumentOrder(ordered, Nodes.WrapXmlNode(root), new HashSet<XmlNode>());
                int orderIndex = 0;
                while (orderIndex < ordered.Count)
                {
                    result[ordered[orderIndex].Node] = orderIndex;
                    orderIndex = orderIndex + 1;
                }
            }

            nodeIndex = nodeIndex + 1;
        }

        return result;
    }

    internal int CompareXPathIndexedNodeOrder(XdmNode left, XdmNode right, Dictionary<XmlNode, int> documentOrder)
    {
        XmlNode leftRoot = Nodes.Navigation.FindXmlNavigationRoot(left.Node);
        XmlNode rightRoot = Nodes.Navigation.FindXmlNavigationRoot(right.Node);
        if (leftRoot == rightRoot && documentOrder.ContainsKey(left.Node) && documentOrder.ContainsKey(right.Node))
        {
            int leftIndex = documentOrder[left.Node];
            int rightIndex = documentOrder[right.Node];
            return leftIndex < rightIndex ? -1 : leftIndex > rightIndex ? 1 : 0;
        }

        return CompareXPathNodeOrder(left, right);
    }

    internal bool ContainsXPathNode(List<XdmNode> nodes, XdmNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (HasSameXPathNodeIdentity(nodes[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasSameXPathNodeIdentity(XdmNode left, XdmNode right)
    {
        return Nodes.HasSameXdmNodeIdentity(left, right);
    }

    internal int CompareXPathNodeOrder(XdmNode left, XdmNode right)
    {
        return Nodes.CompareXdmNodeDocumentOrder(left, right);
    }

    internal int IndexXPathNode(XdmNode[] nodes, XdmNode candidate)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (HasSameXPathNodeIdentity(nodes[index], candidate))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int IndexXPathNodeList(List<XdmNode> nodes, XdmNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (HasSameXPathNodeIdentity(nodes[index], candidate))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void AddXPathNodes(List<XdmNode> result, XdmNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            result.Add(nodes[index]);
            index = index + 1;
        }
    }

    internal XdmSequence CreateXPathNodeSequence(XdmNode node)
    {
        return Sequences.CreateXdmNodeSequence(node.Node);
    }

    internal XdmSequence CreateXPathNodeSequenceFromList(List<XdmNode> nodes)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < nodes.Count)
        {
            builder.AddXdmNode(nodes[index].Node);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXPathItemSequence(XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            return Sequences.CreateXdmNodeSequence(item.Node);
        }

        if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            return Sequences.CreateXdmAtomicSequence(item.AtomicValue);
        }

        if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            return Sequences.CreateXdmFunctionSequence(item.Function);
        }

        if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            return Sequences.CreateXdmMapSequence(item.Map);
        }

        if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            return Sequences.CreateXdmArraySequence(item.Array);
        }

        throw new ArgumentException("The XPath sequence item is incomplete.", "item");
    }

    internal void AddXPathSequenceItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
        else
        {
            throw new ArgumentException("The XPath sequence item is incomplete.", "item");
        }
    }

    internal bool SameXPathName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.LocalName == right.LocalName && NormalizeXPathNamespace(left.NamespaceUri) == NormalizeXPathNamespace(right.NamespaceUri);
    }

    internal string NormalizeXPathNamespace(string? namespaceUri)
    {
        return namespaceUri == null ? "" : namespaceUri;
    }

    internal void MarkXPathStreamedNode(XmlNode node)
    {
        Guard.NotNull(node, "node");
        StreamedNodes.Add(node);
    }

    internal void RequireXPathStreamNavigation(XdmNode node, XPathAxis axis, SourceRange range)
    {
        if (StreamedNodeRuntime != null && StreamedNodeRuntime.IsXsltStreamedNode(node.Node))
        {
            XsltStreamNavigationStatus status = StreamedNodeRuntime.AuthorizeXsltStreamNavigation(node.Node, axis);
            if (status == XsltStreamNavigationStatus.Allowed)
            {
                return;
            }

            string message = status == XsltStreamNavigationStatus.Expired ? "The streamed XPath node is no longer available at the current stream position." : status == XsltStreamNavigationStatus.AlreadyConsumed ? "Downward navigation from the streamed XPath node has already been consumed." : "The requested XPath axis is not available while processing a streamed node.";
            throw Runtime.CreateXPathRuntimeError(message, XPathErrorCodes.XPDY0130, range);
        }

        if (!StreamedNodes.Contains(node.Node))
        {
            return;
        }

        if (axis == XPathAxis.Self || axis == XPathAxis.Attribute || axis == XPathAxis.Namespace)
        {
            return;
        }

        throw Runtime.CreateXPathRuntimeError("The requested XPath axis cannot navigate from a streamed node without grounding it.", XPathErrorCodes.XPDY0130, range);
    }

    internal void RequireXPathStreamOrdering(XdmNode node, SourceRange range)
    {
        if (StreamedNodes.Contains(node.Node) || StreamedNodeRuntime != null && StreamedNodeRuntime.IsXsltStreamedNode(node.Node))
        {
            throw Runtime.CreateXPathRuntimeError("A streamed XPath node cannot participate in a document-order comparison without grounding it.", XPathErrorCodes.XPDY0130, range);
        }
    }

    internal void RegisterXPathStreamNavigationResult(List<XdmNode> nodes, XPathAxis axis)
    {
        if (StreamedNodeRuntime == null || axis != XPathAxis.Child && axis != XPathAxis.Descendant && axis != XPathAxis.DescendantOrSelf)
        {
            return;
        }

        XmlNode[] result = new XmlNode[nodes.Count];
        int index = 0;
        while (index < nodes.Count)
        {
            result[index] = nodes[index].Node;
            index = index + 1;
        }

        StreamedNodeRuntime.RegisterXsltStreamNavigationResult(result);
    }

    internal void RequireXPathStreamSequenceAvailable(XdmSequence sequence, SourceRange range)
    {
        if (StreamedNodeRuntime == null)
        {
            return;
        }

        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Node != null && StreamedNodeRuntime.IsXsltStreamedNode(item.Node) && !StreamedNodeRuntime.IsXsltStreamedNodeAvailable(item.Node))
            {
                throw Runtime.CreateXPathRuntimeError("The streamed XPath node is no longer available at the current stream position.", XPathErrorCodes.XPDY0130, range);
            }
        }
    }
}
