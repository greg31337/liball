using System;

namespace html;

internal class HtmlOpenElementStack
{
    internal HtmlBudget budget;
    internal HtmlNode?[] nodes;
    internal int count;
    internal HtmlNode? fragmentContext;
    internal bool depthExceeded;

    internal HtmlOpenElementStack(HtmlBudget budget, HtmlNode? fragmentContext)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        ValidateFragmentContext(fragmentContext);
        this.budget = budget;
        this.nodes = new HtmlNode[16];
        this.count = 0;
        this.fragmentContext = fragmentContext;
        this.depthExceeded = false;
    }

    internal int GetCount()
    {
        return this.count;
    }

    internal HtmlNode[] GetNodes()
    {
        HtmlNode[] result = new HtmlNode[this.count];
        Array.Copy(this.nodes, result, this.count);
        return result;
    }

    internal HtmlNode? GetNode(int index)
    {
        if (index < 0 || index >= this.count)
        {
            throw new ArgumentOutOfRangeException("index", index,
                "The open-element index is outside the stack.");
        }

        return this.nodes[index];
    }

    internal HtmlNode? GetCurrentNode()
    {
        return this.count == 0 ? null : this.nodes[this.count - 1];
    }

    internal HtmlNode? GetAdjustedCurrentNode()
    {
        if (this.fragmentContext != null && this.count == 1)
        {
            return this.fragmentContext;
        }

        return this.GetCurrentNode();
    }

    internal HtmlNode? GetFragmentContext()
    {
        return this.fragmentContext;
    }

    internal bool HasExceededDepth()
    {
        return this.depthExceeded;
    }

    internal bool CanPush()
    {
        return this.AllowDepth(this.count + 1);
    }

    internal bool Push(HtmlNode node)
    {
        ValidateElement(node, "node");
        if (this.GetIndex(node) >= 0)
        {
            throw new InvalidOperationException(
                "An element cannot occur more than once in the open-element stack.");
        }

        if (!this.AllowDepth(this.count + 1))
        {
            return false;
        }

        this.EnsureCapacity(this.count + 1);
        this.nodes[this.count] = node;
        this.count = this.count + 1;
        return true;
    }

    internal HtmlNode? Pop()
    {
        if (this.count == 0)
        {
            return null;
        }

        this.count = this.count - 1;
        HtmlNode? result = this.nodes[this.count];
        this.nodes[this.count] = null;
        return result;
    }

    internal bool Contains(HtmlNode? node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return this.GetIndex(node) >= 0;
    }

    internal int GetIndex(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int index = 0;
        while (index < this.count)
        {
            if (this.nodes[index] == node)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool Remove(HtmlNode node)
    {
        int index = this.GetIndex(node);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    internal bool Replace(HtmlNode node, HtmlNode replacement)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ValidateElement(replacement, "replacement");
        int index = this.GetIndex(node);
        if (index < 0)
        {
            return false;
        }

        int replacementIndex = this.GetIndex(replacement);
        if (replacementIndex >= 0 && replacement != node)
        {
            throw new InvalidOperationException(
                "An element cannot occur more than once in the open-element stack.");
        }

        this.nodes[index] = replacement;
        return true;
    }

    internal bool InsertAfter(HtmlNode referenceNode, HtmlNode node)
    {
        if (referenceNode == null)
        {
            throw new ArgumentNullException("referenceNode");
        }

        ValidateElement(node, "node");
        int referenceIndex = this.GetIndex(referenceNode);
        if (referenceIndex < 0)
        {
            return false;
        }

        if (this.GetIndex(node) >= 0)
        {
            throw new InvalidOperationException(
                "An element cannot occur more than once in the open-element stack.");
        }

        if (!this.AllowDepth(this.count + 1))
        {
            return false;
        }

        this.EnsureCapacity(this.count + 1);
        int index = this.count;
        while (index > referenceIndex + 1)
        {
            this.nodes[index] = this.nodes[index - 1];
            index = index - 1;
        }

        this.nodes[referenceIndex + 1] = node;
        this.count = this.count + 1;
        return true;
    }

    internal bool PopUntil(HtmlNode node)
    {
        int index = this.GetIndex(node);
        if (index < 0)
        {
            return false;
        }

        while (this.count > index)
        {
            this.Pop();
        }

        return true;
    }

    internal bool PopUntilHtmlElement(string localName)
    {
        ValidateLocalName(localName);
        int index = this.count - 1;
        while (index >= 0)
        {
            if (IsHtmlElement(this.nodes[index], localName))
            {
                while (this.count > index)
                {
                    this.Pop();
                }

                return true;
            }

            index = index - 1;
        }

        return false;
    }

    internal bool HasInScope(HtmlNode target)
    {
        return this.HasNodeInScope(target, 0);
    }

    internal bool HasInListItemScope(HtmlNode target)
    {
        return this.HasNodeInScope(target, 1);
    }

    internal bool HasInButtonScope(HtmlNode target)
    {
        return this.HasNodeInScope(target, 2);
    }

    internal bool HasInTableScope(HtmlNode target)
    {
        return this.HasNodeInScope(target, 3);
    }

    internal bool HasInSelectScope(HtmlNode target)
    {
        return this.HasNodeInScope(target, 4);
    }

    internal bool HasHtmlElementInScope(string localName)
    {
        return this.HasHtmlNameInScope(localName, 0);
    }

    internal bool HasHtmlElementInListItemScope(string localName)
    {
        return this.HasHtmlNameInScope(localName, 1);
    }

    internal bool HasHtmlElementInButtonScope(string localName)
    {
        return this.HasHtmlNameInScope(localName, 2);
    }

    internal bool HasHtmlElementInTableScope(string localName)
    {
        return this.HasHtmlNameInScope(localName, 3);
    }

    internal bool HasHtmlElementInSelectScope(string localName)
    {
        return this.HasHtmlNameInScope(localName, 4);
    }

    internal HtmlNode? GetLastHtmlElement(string localName)
    {
        ValidateLocalName(localName);
        int index = this.count - 1;
        while (index >= 0)
        {
            HtmlNode? node = this.nodes[index];
            if (IsHtmlElement(node, localName))
            {
                return node;
            }

            index = index - 1;
        }

        return null;
    }

    internal HtmlNode? GetLastSpecialElement()
    {
        int index = this.count - 1;
        while (index >= 0)
        {
            HtmlNode? node = this.nodes[index];
            if (IsSpecialElement(node))
            {
                return node;
            }

            index = index - 1;
        }

        return null;
    }

    internal HtmlNode? GetFirstSpecialElementAfter(HtmlNode node)
    {
        int index = this.GetIndex(node);
        if (index < 0)
        {
            return null;
        }

        index = index + 1;
        while (index < this.count)
        {
            HtmlNode? candidate = this.nodes[index];
            if (IsSpecialElement(candidate))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    internal int GenerateImpliedEndTags()
    {
        return this.GenerateImpliedEndTagsCore("", false, false);
    }

    internal int GenerateImpliedEndTagsExcept(string localName)
    {
        ValidateLocalName(localName);
        return this.GenerateImpliedEndTagsCore(localName, true, false);
    }

    internal int GenerateAllImpliedEndTagsThoroughly()
    {
        return this.GenerateImpliedEndTagsCore("", false, true);
    }

    internal static bool IsSpecialElement(HtmlNode? node)
    {
        ValidateElement(node, "node");
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string localName = node.GetLocalNameStorage();
        if (namespaceKind == HtmlNamespace.Html)
        {
            return HtmlNames.IsSpecialElement(localName);
        }

        if (namespaceKind == HtmlNamespace.MathMl)
        {
            return localName == "mi" || localName == "mo"
                || localName == "mn" || localName == "ms"
                || localName == "mtext" || localName == "annotation-xml";
        }

        if (namespaceKind == HtmlNamespace.Svg)
        {
            return localName == "foreignObject" || localName == "desc"
                || localName == "title";
        }

        return false;
    }

    internal bool HasNodeInScope(HtmlNode target, int scopeKind)
    {
        ValidateElement(target, "target");

        int index = this.count - 1;
        while (index >= 0)
        {
            HtmlNode? node = this.nodes[index];
            if (node == target)
            {
                return true;
            }

            if (IsScopeBoundary(node, scopeKind))
            {
                return false;
            }

            index = index - 1;
        }

        return false;
    }

    internal bool HasHtmlNameInScope(string localName, int scopeKind)
    {
        ValidateLocalName(localName);
        int index = this.count - 1;
        while (index >= 0)
        {
            HtmlNode? node = this.nodes[index];
            if (IsHtmlElement(node, localName))
            {
                return true;
            }

            if (IsScopeBoundary(node, scopeKind))
            {
                return false;
            }

            index = index - 1;
        }

        return false;
    }

    internal int GenerateImpliedEndTagsCore(string excludedName, bool hasExclusion,
        bool thorough)
    {
        int result = 0;
        bool running = true;
        while (this.count > 0 && running)
        {
            HtmlNode current = this.GetRequiredNode(this.count - 1);
            string localName = current.GetLocalNameStorage();
            bool implied = IsImpliedEndElement(current, thorough);
            bool excluded = hasExclusion
                && IsHtmlElement(current, excludedName);
            if (!implied || excluded)
            {
                running = false;
            }
            else
            {
                this.Pop();
                result = result + 1;
            }
        }

        return result;
    }

    internal void RemoveAt(int removedIndex)
    {
        int index = removedIndex;
        while (index + 1 < this.count)
        {
            this.nodes[index] = this.nodes[index + 1];
            index = index + 1;
        }

        this.count = this.count - 1;
        this.nodes[this.count] = null;
    }

    internal bool AllowDepth(int depth)
    {
        if (this.budget.Allows(HtmlLimitKind.Depth, depth))
        {
            return true;
        }

        this.depthExceeded = true;
        return false;
    }

    internal void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= this.nodes.Length)
        {
            return;
        }

        int newCapacity = BufferCapacity.GetNext(this.nodes.Length,
            requiredCapacity, int.MaxValue);

        HtmlNode[] result = new HtmlNode[newCapacity];
        Array.Copy(this.nodes, result, this.count);
        this.nodes = result;
    }

    internal static bool IsScopeBoundary(HtmlNode? node, int scopeKind)
    {
        if (scopeKind == 4)
        {
            return !IsHtmlElement(node, "option")
                && !IsHtmlElement(node, "optgroup");
        }

        if (scopeKind == 3)
        {
            return node != null
                && node.GetNamespaceStorage() == HtmlNamespace.Html
                && HtmlNames.IsTableScopingElement(
                    node.GetLocalNameStorage());
        }

        bool boundary = node != null && IsOrdinaryScopeBoundary(node);
        if (boundary)
        {
            return true;
        }

        if (node == null
            || node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string localName = node.GetLocalNameStorage();
        if (scopeKind == 1)
        {
            return localName == "ol" || localName == "ul";
        }

        return scopeKind == 2 && localName == "button";
    }

    internal static bool IsOrdinaryScopeBoundary(HtmlNode node)
    {
        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string localName = node.GetLocalNameStorage();
        if (namespaceKind == HtmlNamespace.Html)
        {
            return HtmlNames.IsScopingElement(localName);
        }

        if (namespaceKind == HtmlNamespace.MathMl)
        {
            return localName == "mi" || localName == "mo"
                || localName == "mn" || localName == "ms"
                || localName == "mtext" || localName == "annotation-xml";
        }

        if (namespaceKind == HtmlNamespace.Svg)
        {
            return localName == "foreignObject" || localName == "desc"
                || localName == "title";
        }

        return false;
    }

    internal static bool IsImpliedEndElement(HtmlNode node, bool thorough)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string localName = node.GetLocalNameStorage();
        bool regular = localName == "dd" || localName == "dt"
            || localName == "li" || localName == "optgroup"
            || localName == "option" || localName == "p"
            || localName == "rb" || localName == "rp"
            || localName == "rt" || localName == "rtc";
        if (regular || !thorough)
        {
            return regular;
        }

        return localName == "caption" || localName == "colgroup"
            || localName == "tbody" || localName == "td"
            || localName == "tfoot" || localName == "th"
            || localName == "thead" || localName == "tr";
    }

    internal static bool IsHtmlElement(HtmlNode? node, string localName)
    {
        return node != null
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == localName;
    }

    internal HtmlNode GetRequiredNode(int index)
    {
        HtmlNode? result = this.GetNode(index);
        if (result == null)
        {
            throw new InvalidOperationException(
                "An occupied open-element stack slot cannot be null.");
        }

        return result;
    }

    internal HtmlNode GetRequiredCurrentNode()
    {
        HtmlNode? result = this.GetCurrentNode();
        if (result == null)
        {
            throw new InvalidOperationException(
                "The open-element stack cannot be empty here.");
        }

        return result;
    }

    internal HtmlNode PopRequired()
    {
        HtmlNode? result = this.Pop();
        if (result == null)
        {
            throw new InvalidOperationException(
                "The open-element stack cannot be empty here.");
        }

        return result;
    }

    internal static void ValidateElement(HtmlNode? node, string parameterName)
    {
        if (node == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "The open-element stack can contain only element nodes.",
                parameterName);
        }
    }

    internal static void ValidateFragmentContext(HtmlNode? fragmentContext)
    {
        if (fragmentContext != null
            && fragmentContext.GetKindStorage() != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "A fragment context must be an element node.",
                "fragmentContext");
        }
    }

    internal static void ValidateLocalName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (localName.Length == 0)
        {
            throw new ArgumentException(
                "An element local name cannot be empty.", "localName");
        }
    }
}
