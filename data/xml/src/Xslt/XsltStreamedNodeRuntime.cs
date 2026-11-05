using System.Collections.Generic;

namespace xml;

internal enum XsltStreamNavigationStatus
{
    Allowed = 0,
    Expired = 1,
    AlreadyConsumed = 2,
    Prohibited = 3
}

internal class XsltStreamedNodeState
{
    internal XmlNode Node;
    internal int Generation;
    internal int DeliveryOrder;
    internal bool Active;
    internal bool Consumed;
    internal bool ForwardOnly;

    internal XsltStreamedNodeState(XmlNode node)
    {
        Node = node;
        Generation = 0;
        DeliveryOrder = 0;
        Active = false;
        Consumed = false;
        ForwardOnly = false;
    }
}

internal class XsltStreamedNodeRuntime
{
    internal Dictionary<XmlNode, XsltStreamedNodeState> Nodes;
    internal long MaximumGroundedNodes;
    internal long GroundedNodeCount;
    internal int CurrentGeneration;
    internal int NextDeliveryOrder;

    internal XsltStreamedNodeRuntime(long maximumGroundedNodes)
    {
        Nodes = new Dictionary<XmlNode, XsltStreamedNodeState>();
        MaximumGroundedNodes = maximumGroundedNodes;
        GroundedNodeCount = 0;
        CurrentGeneration = 0;
        NextDeliveryOrder = 0;
    }

    internal void RegisterXsltStreamFocus(XmlNode node)
    {
        Guard.NotNull(node, "node");
        CurrentGeneration = CurrentGeneration + 1;
        int index = 0;
        XsltStreamedNodeState[] existing = new List<XsltStreamedNodeState>(Nodes.Values).ToArray();
        while (index < existing.Length)
        {
            existing[index].Active = false;
            index = index + 1;
        }

        List<XmlNode> ancestors = new List<XmlNode>();
        XmlNode? current = node;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }

        index = ancestors.Count - 1;
        while (index >= 0)
        {
            if (index == 0)
            {
                ActivateXsltStreamedNode(ancestors[index]);
            }
            else
            {
                ReactivateXsltStreamAncestor(ancestors[index]);
            }

            index = index - 1;
        }
    }

    internal void ReactivateXsltStreamAncestor(XmlNode node)
    {
        if (!Nodes.ContainsKey(node))
        {
            ActivateXsltStreamedNode(node);
            return;
        }

        XsltStreamedNodeState state = Nodes[node];
        state.Generation = CurrentGeneration;
        state.Active = true;
        state.ForwardOnly = false;
    }

    internal void ActivateXsltStreamedNode(XmlNode node)
    {
        XsltStreamedNodeState state;
        if (Nodes.ContainsKey(node))
        {
            state = Nodes[node];
        }
        else
        {
            state = new XsltStreamedNodeState(node);
            Nodes.Add(node, state);
        }

        state.Generation = CurrentGeneration;
        state.DeliveryOrder = NextDeliveryOrder;
        state.Active = true;
        state.Consumed = false;
        state.ForwardOnly = false;
        NextDeliveryOrder = NextDeliveryOrder + 1;
    }

    internal void ActivateXsltStreamNavigationNode(XmlNode node)
    {
        ActivateXsltStreamedNode(node);
        Nodes[node].ForwardOnly = true;
    }

    internal void RegisterXsltStreamNavigationResult(XmlNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            ActivateXsltStreamNavigationNode(nodes[index]);
            index = index + 1;
        }
    }

    internal void ExpireXsltStreamedNode(XmlNode node)
    {
        if (Nodes.ContainsKey(node))
        {
            Nodes[node].Active = false;
        }
    }

    internal bool IsXsltStreamedNode(XmlNode node)
    {
        return Nodes.ContainsKey(node);
    }

    internal bool IsXsltStreamedNodeAvailable(XmlNode node)
    {
        return !Nodes.ContainsKey(node) || Nodes[node].Active;
    }

    internal bool IsXsltForwardOnlyNode(XmlNode node)
    {
        return Nodes.ContainsKey(node) && Nodes[node].ForwardOnly;
    }

    internal bool IsXsltStreamedNodeConsumed(XmlNode node)
    {
        return Nodes.ContainsKey(node) && Nodes[node].Consumed;
    }

    internal XsltStreamNavigationStatus AuthorizeXsltStreamNavigation(XmlNode node, XPathAxis axis)
    {
        if (!Nodes.ContainsKey(node))
        {
            return XsltStreamNavigationStatus.Allowed;
        }

        XsltStreamedNodeState state = Nodes[node];
        if (!state.Active)
        {
            return XsltStreamNavigationStatus.Expired;
        }

        if (axis == XPathAxis.Self || axis == XPathAxis.Attribute || axis == XPathAxis.Namespace || axis == XPathAxis.Parent || axis == XPathAxis.Ancestor || axis == XPathAxis.AncestorOrSelf)
        {
            return XsltStreamNavigationStatus.Allowed;
        }

        if (axis == XPathAxis.Child || axis == XPathAxis.Descendant || axis == XPathAxis.DescendantOrSelf)
        {
            if (state.Consumed)
            {
                return XsltStreamNavigationStatus.AlreadyConsumed;
            }

            state.Consumed = true;
            return XsltStreamNavigationStatus.Allowed;
        }

        return XsltStreamNavigationStatus.Prohibited;
    }

    internal XsltStreamNavigationStatus AuthorizeXsltStreamedNodeValue(XmlNode node)
    {
        if (!Nodes.ContainsKey(node))
        {
            return XsltStreamNavigationStatus.Allowed;
        }

        XsltStreamedNodeState state = Nodes[node];
        if (!state.Active)
        {
            return XsltStreamNavigationStatus.Expired;
        }

        if (node.Kind != XmlNodeKind.Document && node.Kind != XmlNodeKind.DocumentFragment && node.Kind != XmlNodeKind.Element)
        {
            return XsltStreamNavigationStatus.Allowed;
        }

        if (state.Consumed)
        {
            return XsltStreamNavigationStatus.AlreadyConsumed;
        }

        state.Consumed = true;
        return XsltStreamNavigationStatus.Allowed;
    }

    internal bool IsXsltForwardStreamSequence(XmlNode[] nodes)
    {
        int previous = -1;
        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode node = nodes[index];
            if (!Nodes.ContainsKey(node))
            {
                return false;
            }

            XsltStreamedNodeState state = Nodes[node];
            if (!state.Active || state.DeliveryOrder <= previous)
            {
                return false;
            }

            previous = state.DeliveryOrder;
            index = index + 1;
        }

        return true;
    }

    internal bool ReserveXsltStreamGrounding(XmlNode node, bool includeAncestors)
    {
        if (!Nodes.ContainsKey(node))
        {
            return true;
        }

        XsltStreamedNodeState state = Nodes[node];
        if (!state.Active || state.Consumed && (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment || node.Kind == XmlNodeKind.Element))
        {
            return false;
        }

        long amount = CountXsltStreamGroundedNodes(node, new HashSet<XmlNode>());
        if (includeAncestors)
        {
            XmlNode? ancestor = node.Parent;
            while (ancestor != null)
            {
                amount = amount + 1;
                ancestor = ancestor.Parent;
            }
        }

        if (MaximumGroundedNodes > 0 && amount > MaximumGroundedNodes - GroundedNodeCount)
        {
            return false;
        }

        GroundedNodeCount = GroundedNodeCount + amount;
        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment || node.Kind == XmlNodeKind.Element)
        {
            state.Consumed = true;
        }

        return true;
    }

    internal long CountXsltStreamGroundedNodes(XmlNode node, HashSet<XmlNode> visited)
    {
        if (!visited.Add(node))
        {
            return 0;
        }

        long result = 1;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            result = result + CountXsltStreamGroundedNodes(attributes[index], visited);
            index = index + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        index = 0;
        while (index < children.Length)
        {
            result = result + CountXsltStreamGroundedNodes(children[index], visited);
            index = index + 1;
        }

        return result;
    }

    internal XsltStreamedNodeRuntime CloneXsltStreamedNodeRuntime()
    {
        XsltStreamedNodeRuntime result = new XsltStreamedNodeRuntime(MaximumGroundedNodes);
        result.GroundedNodeCount = GroundedNodeCount;
        result.CurrentGeneration = CurrentGeneration;
        result.NextDeliveryOrder = NextDeliveryOrder;
        foreach (XsltStreamedNodeState source in Nodes.Values)
        {
            XsltStreamedNodeState copy = new XsltStreamedNodeState(source.Node);
            copy.Generation = source.Generation;
            copy.DeliveryOrder = source.DeliveryOrder;
            copy.Active = source.Active;
            copy.Consumed = source.Consumed;
            copy.ForwardOnly = source.ForwardOnly;
            result.Nodes.Add(copy.Node, copy);
        }

        return result;
    }
}
