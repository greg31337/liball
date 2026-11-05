using System;

namespace html;

internal enum HtmlSanitizerNodeAction
{
    Keep = 0,
    Drop = 1
}

internal static class HtmlSanitizerNodes
{
    internal static HtmlSanitizerNodeAction GetAction(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy)
    {
        ValidateNodeAndPolicy(node, policy);
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Comment)
        {
            return policy.GetAllowComments()
                ? HtmlSanitizerNodeAction.Keep
                : HtmlSanitizerNodeAction.Drop;
        }

        if (kind == HtmlNodeKind.DocumentType)
        {
            return policy.GetAllowDoctype() && IsSafeHtmlDoctype(node)
                ? HtmlSanitizerNodeAction.Keep
                : HtmlSanitizerNodeAction.Drop;
        }

        return HtmlSanitizerNodeAction.Keep;
    }

    internal static HtmlSanitizerNodeAction Apply(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy)
    {
        HtmlSanitizerNodeAction action = GetAction(node, policy);
        if (action == HtmlSanitizerNodeAction.Drop)
        {
            node.Remove();
        }

        return action;
    }

    internal static bool IsSafeHtmlDoctype(HtmlNode node)
    {
        if (!node.HasDoctypeNameStorage()
            || node.HasPublicIdentifierStorage()
            || node.HasSystemIdentifierStorage()
            || node.IsForceQuirksStorage())
        {
            return false;
        }

        string name = node.GetNameStorage();
        if (name.Length != 4)
        {
            return false;
        }

        return Ascii.ToLower(name[0]) == 'h'
            && Ascii.ToLower(name[1]) == 't'
            && Ascii.ToLower(name[2]) == 'm'
            && Ascii.ToLower(name[3]) == 'l';
    }

    internal static void ValidateNodeAndPolicy(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }
}
