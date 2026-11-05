using System;

namespace html;

internal enum HtmlSanitizerElementAction
{
    Keep = 0,
    DropSubtree = 1,
    Unwrap = 2,
    ReplaceWithText = 3,
    EscapeAsText = 4
}

internal static class HtmlSanitizerElements
{
    internal static HtmlSanitizerElementAction GetAction(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy)
    {
        ValidateElementAndPolicy(element, policy);
        string namespaceUri = element.GetNamespaceUriStorage();
        string localName = element.GetLocalNameStorage();
        if (!HtmlSanitizerPolicyNames.IsSafeElementLocalName(namespaceUri,
            localName))
        {
            return HtmlSanitizerElementAction.DropSubtree;
        }

        if (policy.IsElementAllowedInNamespace(namespaceUri, localName))
        {
            return HtmlSanitizerElementAction.Keep;
        }

        if (policy.IsElementBlockedInNamespace(namespaceUri, localName))
        {
            return ConvertAction(policy.GetBlockedElementActionInNamespace(namespaceUri,
                localName));
        }

        if (namespaceUri != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri()
            || IsDropSubtreeByDefault(localName))
        {
            return HtmlSanitizerElementAction.DropSubtree;
        }

        return HtmlSanitizerElementAction.Unwrap;
    }

    internal static HtmlSanitizerElementAction Apply(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy)
    {
        HtmlSanitizerElementAction action = GetAction(element, policy);
        Apply(element, action);
        return action;
    }

    internal static void Apply(HtmlNode element,
        HtmlSanitizerElementAction action)
    {
        ValidateElement(element);
        ValidateAction(action);
        if (action == HtmlSanitizerElementAction.Keep)
        {
            return;
        }

        if (element.GetParentStorage() == null)
        {
            throw new InvalidOperationException(
                "A detached element cannot be replaced by a sanitizer action.");
        }

        if (action == HtmlSanitizerElementAction.DropSubtree)
        {
            element.Remove();
            return;
        }

        if (action == HtmlSanitizerElementAction.Unwrap)
        {
            element.Unwrap();
            return;
        }

        string value = action == HtmlSanitizerElementAction.ReplaceWithText
            ? element.GetTextContent() : HtmlSerializer.Serialize(element);
        HtmlNode replacement = HtmlNode.CreateTextStorage(
            element.GetOwnerDocumentStorage(), value,
            element.GetSourceSpanStorage());
        element.ReplaceWith(replacement);
    }

    internal static HtmlSanitizerElementAction ConvertAction(
        HtmlBlockedElementAction action)
    {
        switch (action)
        {
            case HtmlBlockedElementAction.DropSubtree:
                return HtmlSanitizerElementAction.DropSubtree;
            case HtmlBlockedElementAction.Unwrap:
                return HtmlSanitizerElementAction.Unwrap;
            case HtmlBlockedElementAction.ReplaceWithText:
                return HtmlSanitizerElementAction.ReplaceWithText;
            case HtmlBlockedElementAction.EscapeAsText:
                return HtmlSanitizerElementAction.EscapeAsText;
            default:
                throw new InvalidOperationException(
                    "A compiled policy has an unknown element action.");
        }
    }

    internal static bool IsDropSubtreeByDefault(string localName)
    {
        string name = FoldAscii(localName);
        return HtmlNames.IsRawTextElement(name.AsSpan())
            || HtmlNames.IsEscapableRawTextElement(name.AsSpan())
            || name == "plaintext" || name == "noscript"
            || name == "iframe" || name == "noembed"
            || name == "noframes" || name == "object"
            || name == "embed" || name == "applet"
            || name == "frame" || name == "frameset"
            || name == "template";
    }

    internal static void ValidateElementAndPolicy(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy)
    {
        ValidateElement(element);
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }

    internal static void ValidateElement(HtmlNode element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "A sanitizer element action requires an element node.",
                "element");
        }
    }

    internal static void ValidateAction(HtmlSanitizerElementAction action)
    {
        if ((int)action < (int)HtmlSanitizerElementAction.Keep
            || (int)action > (int)HtmlSanitizerElementAction.EscapeAsText)
        {
            throw new ArgumentOutOfRangeException("action", (int)action,
                "Unknown sanitizer element action.");
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
