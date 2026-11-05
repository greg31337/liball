using System;
using System.Text;

namespace html;

internal static class HtmlTextContent
{
    internal static string GetOwnText(HtmlNode node)
    {
        ValidateNode(node);
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text)
        {
            return node.GetDataStorage();
        }

        if (kind != HtmlNodeKind.Element
            && kind != HtmlNodeKind.Fragment)
        {
            return "";
        }

        HtmlNode container = GetContentContainer(node);
        HtmlNode[] children = container.GetChildrenStorage();
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Text)
            {
                builder.Append(child.GetDataStorage());
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string GetDescendantText(HtmlNode node)
    {
        ValidateNode(node);
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text || kind == HtmlNodeKind.Comment)
        {
            return node.GetDataStorage();
        }

        if (kind != HtmlNodeKind.Element
            && kind != HtmlNodeKind.Fragment)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        HtmlNode container = GetContentContainer(node);
        HtmlNode[] children = container.GetChildrenStorage();
        int capacity = children.Length > 8 ? children.Length : 8;
        HtmlNode[] pending = new HtmlNode[capacity];
        int pendingCount = 0;
        pendingCount = PushChildren(children, pending, pendingCount);
        while (pendingCount > 0)
        {
            pendingCount = pendingCount - 1;
            HtmlNode current = pending[pendingCount];
            HtmlNodeKind currentKind = current.GetKindStorage();
            if (currentKind == HtmlNodeKind.Text)
            {
                builder.Append(current.GetDataStorage());
            }
            else if (currentKind == HtmlNodeKind.Element
                || currentKind == HtmlNodeKind.Fragment)
            {
                container = GetContentContainer(current);
                children = container.GetChildrenStorage();
                if (pendingCount + children.Length > pending.Length)
                {
                    pending = Grow(pending, pendingCount + children.Length);
                }

                pendingCount = PushChildren(children, pending, pendingCount);
            }
        }

        return builder.ToString();
    }

    internal static string Normalize(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder builder = new StringBuilder(value.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (UnicodeTools.IsUnicodeWhitespace(current))
            {
                if (builder.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int PushChildren(HtmlNode[] children, HtmlNode[] pending,
        int pendingCount)
    {
        int index = children.Length - 1;
        while (index >= 0)
        {
            pending[pendingCount] = children[index];
            pendingCount = pendingCount + 1;
            index = index - 1;
        }

        return pendingCount;
    }

    internal static HtmlNode[] Grow(HtmlNode[] values, int requiredLength)
    {
        int newLength = BufferCapacity.GetNext(values.Length,
            requiredLength, int.MaxValue);

        HtmlNode[] result = new HtmlNode[newLength];
        Array.Copy(values, result, values.Length);
        return result;
    }

    internal static HtmlNode GetContentContainer(HtmlNode node)
    {
        HtmlNode? content = node.GetTemplateContentStorage();
        return content == null ? node : content;
    }

    internal static void ValidateNode(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }
    }
}
