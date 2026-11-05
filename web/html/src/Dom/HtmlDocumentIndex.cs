using System;
using System.Collections.Generic;

namespace html;

internal class HtmlNodeBucket
{
    internal HtmlNode[] nodes;
    internal int count;

    internal HtmlNodeBucket()
    {
        this.nodes = new HtmlNode[4];
        this.count = 0;
    }

    internal void Add(HtmlNode node)
    {
        if (this.count > 0 && this.nodes[this.count - 1] == node)
        {
            return;
        }

        this.EnsureCapacity(this.count + 1);
        this.nodes[this.count] = node;
        this.count = this.count + 1;
    }

    internal HtmlNode? GetFirst()
    {
        return this.count == 0 ? null : this.nodes[0];
    }

    internal HtmlNode[] GetNodes()
    {
        HtmlNode[] result = new HtmlNode[this.count];
        Array.Copy(this.nodes, result, this.count);
        return result;
    }

    internal void EnsureCapacity(int required)
    {
        if (required <= this.nodes.Length)
        {
            return;
        }

        int capacity = BufferCapacity.GetNext(this.nodes.Length, required,
            int.MaxValue);

        HtmlNode[] expanded = new HtmlNode[capacity];
        Array.Copy(this.nodes, expanded, this.count);
        this.nodes = expanded;
    }
}

internal class HtmlDocumentIndex
{
    internal long mutationVersion;
    internal Dictionary<string, HtmlNodeBucket> ids;
    internal Dictionary<string, HtmlNodeBucket> classes;
    internal Dictionary<string, HtmlNodeBucket> htmlTags;
    internal Dictionary<string, HtmlNodeBucket> foreignTags;
    internal Dictionary<HtmlNode, int> documentOrders;
    internal HtmlNodeBucket allElements;

    internal HtmlDocumentIndex(long mutationVersionValue)
    {
        this.mutationVersion = mutationVersionValue;
        this.ids = new Dictionary<string, HtmlNodeBucket>(
            StringComparer.Ordinal);
        this.classes = new Dictionary<string, HtmlNodeBucket>(
            StringComparer.Ordinal);
        this.htmlTags = new Dictionary<string, HtmlNodeBucket>(
            StringComparer.Ordinal);
        this.foreignTags = new Dictionary<string, HtmlNodeBucket>(
            StringComparer.Ordinal);
        this.documentOrders = new Dictionary<HtmlNode, int>();
        this.allElements = new HtmlNodeBucket();
    }

    internal static HtmlDocumentIndex Create(HtmlNode documentRoot,
        long mutationVersion)
    {
        if (documentRoot == null)
        {
            throw new ArgumentNullException("documentRoot");
        }

        HtmlDocumentIndex result = new HtmlDocumentIndex(mutationVersion);
        List<HtmlNode> pending = new List<HtmlNode>();
        AddChildrenInReverse(pending, documentRoot);
        int order = 0;
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            HtmlNode node = pending[last];
            pending.RemoveAt(last);
            if (node.GetKindStorage() == HtmlNodeKind.Element)
            {
                result.AddElement(node, order);
                order = order + 1;
            }

            AddChildrenInReverse(pending, node);
        }

        return result;
    }

    internal long GetMutationVersion()
    {
        return this.mutationVersion;
    }

    internal HtmlNode? FindById(string id)
    {
        HtmlNodeBucket? bucket = this.GetBucket(this.ids, id);
        return bucket == null ? null : bucket.GetFirst();
    }

    internal HtmlNode[] FindAllById(string id)
    {
        return this.GetNodes(this.ids, id);
    }

    internal HtmlNode[] FindByClass(string className)
    {
        return this.GetNodes(this.classes, className);
    }

    internal HtmlNode[] FindByTag(string tagName)
    {
        if (tagName == "*")
        {
            return this.allElements.GetNodes();
        }

        string htmlName = FoldAscii(tagName);
        HtmlNodeBucket? html = this.GetBucket(this.htmlTags, htmlName);
        HtmlNodeBucket? foreign = this.GetBucket(this.foreignTags, tagName);
        if (html == null)
        {
            return foreign == null ? new HtmlNode[0] : foreign.GetNodes();
        }

        if (foreign == null)
        {
            return html.GetNodes();
        }

        return this.Merge(html.GetNodes(), foreign.GetNodes());
    }

    internal HtmlNode[] GetAllElements()
    {
        return this.allElements.GetNodes();
    }

    internal int GetDocumentOrder(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int result;
        if (!this.documentOrders.TryGetValue(node, out result))
        {
            throw new ArgumentException(
                "The node is not indexed by this document.", "node");
        }

        return result;
    }

    internal void AddElement(HtmlNode element, int order)
    {
        this.documentOrders.Add(element, order);
        this.allElements.Add(element);
        if (element.GetNamespaceStorage() == HtmlNamespace.Html)
        {
            this.Add(this.htmlTags, FoldAscii(
                element.GetLocalNameStorage()), element);
        }
        else
        {
            this.Add(this.foreignTags, element.GetLocalNameStorage(),
                element);
        }

        HtmlAttribute[] attributes = element.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute attribute = attributes[index];
            if (attribute.GetNamespaceUri().Length == 0
                && attribute.GetLocalName() == "id")
            {
                this.Add(this.ids, attribute.GetValue(), element);
            }
            else if (attribute.GetNamespaceUri().Length == 0
                && attribute.GetLocalName() == "class")
            {
                this.AddClasses(attribute.GetValue(), element);
            }

            index = index + 1;
        }
    }

    internal void AddClasses(string value, HtmlNode element)
    {
        int position = 0;
        while (position < value.Length)
        {
            while (position < value.Length
                && Ascii.IsHtmlWhitespace(value[position]))
            {
                position = position + 1;
            }

            int start = position;
            while (position < value.Length
                && !Ascii.IsHtmlWhitespace(value[position]))
            {
                position = position + 1;
            }

            if (start < position)
            {
                this.Add(this.classes,
                    value.Substring(start, position - start), element);
            }
        }
    }

    internal void Add(Dictionary<string, HtmlNodeBucket> index, string key,
        HtmlNode element)
    {
        if (!index.ContainsKey(key))
        {
            index.Add(key, new HtmlNodeBucket());
        }

        index[key].Add(element);
    }

    internal HtmlNodeBucket? GetBucket(Dictionary<string, HtmlNodeBucket> index,
        string key)
    {
        return index.ContainsKey(key) ? index[key] : null;
    }

    internal HtmlNode[] GetNodes(Dictionary<string, HtmlNodeBucket> index,
        string key)
    {
        HtmlNodeBucket? bucket = this.GetBucket(index, key);
        return bucket == null ? new HtmlNode[0] : bucket.GetNodes();
    }

    internal HtmlNode[] Merge(HtmlNode[] left, HtmlNode[] right)
    {
        HtmlNode[] result = new HtmlNode[left.Length + right.Length];
        int leftIndex = 0;
        int rightIndex = 0;
        int outputIndex = 0;
        while (leftIndex < left.Length || rightIndex < right.Length)
        {
            bool takeLeft = rightIndex == right.Length
                || leftIndex < left.Length
                    && this.documentOrders[left[leftIndex]]
                        < this.documentOrders[right[rightIndex]];
            if (takeLeft)
            {
                result[outputIndex] = left[leftIndex];
                leftIndex = leftIndex + 1;
            }
            else
            {
                result[outputIndex] = right[rightIndex];
                rightIndex = rightIndex + 1;
            }

            outputIndex = outputIndex + 1;
        }

        return result;
    }

    internal static void AddChildrenInReverse(List<HtmlNode> pending,
        HtmlNode parent)
    {
        HtmlNode[] children = parent.GetChildrenStorage();
        int index = children.Length - 1;
        while (index >= 0)
        {
            pending.Add(children[index]);
            index = index - 1;
        }
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
