using System;
using System.Collections.Generic;

namespace html;

internal class HtmlSanitizerClobberingResult
{
    internal int rewrittenAttributeCount;
    internal string[] duplicateIdentifiers;

    internal HtmlSanitizerClobberingResult(int rewrittenAttributeCountValue,
        string[] duplicateIdentifiersValue)
    {
        if (rewrittenAttributeCountValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                "rewrittenAttributeCountValue");
        }

        if (duplicateIdentifiersValue == null)
        {
            throw new ArgumentNullException("duplicateIdentifiersValue");
        }

        this.duplicateIdentifiers =
            new string[duplicateIdentifiersValue.Length];
        int index = 0;
        while (index < duplicateIdentifiersValue.Length)
        {
            if (duplicateIdentifiersValue[index] == null)
            {
                throw new ArgumentException(
                    "A duplicate identifier cannot be null.",
                    "duplicateIdentifiersValue");
            }

            this.duplicateIdentifiers[index] =
                duplicateIdentifiersValue[index];
            index = index + 1;
        }

        this.rewrittenAttributeCount = rewrittenAttributeCountValue;
    }

    internal int GetRewrittenAttributeCount()
    {
        return this.rewrittenAttributeCount;
    }

    internal string[] GetDuplicateIdentifiers()
    {
        string[] result = new string[this.duplicateIdentifiers.Length];
        Array.Copy(this.duplicateIdentifiers, result,
            this.duplicateIdentifiers.Length);
        return result;
    }

    internal bool HasDuplicateIdentifiers()
    {
        return this.duplicateIdentifiers.Length > 0;
    }
}

internal static class HtmlSanitizerClobbering
{
    internal static readonly string[] dangerousGlobalNames = new string[]
    {
        "__proto__", "addEventListener", "alert", "atob", "blur", "body",
        "btoa", "cancelAnimationFrame", "children", "close", "closed",
        "confirm", "console", "constructor", "cookie", "createElement",
        "crypto", "customElements", "devicePixelRatio", "document",
        "documentElement", "event", "fetch", "focus", "forms",
        "frameElement", "frames", "getElementById", "hasOwnProperty",
        "head", "history", "images", "implementation", "indexedDB",
        "innerHeight", "innerWidth", "length", "links", "localStorage",
        "location", "locationbar", "menubar", "name", "navigator",
        "onerror", "open", "opener", "origin", "parent", "performance",
        "postMessage", "prompt", "prototype", "querySelector",
        "removeEventListener", "requestAnimationFrame", "screen", "scripts",
        "scroll", "self", "sessionStorage", "setInterval", "setTimeout",
        "status", "stop", "toString", "top", "valueOf", "window", "write",
        "writeln"
    };
    internal static readonly string[] dangerousFormNames = new string[]
    {
        "__proto__", "acceptCharset", "action", "addEventListener",
        "append", "attributes", "autocomplete", "checkValidity", "children",
        "childNodes", "className", "constructor", "elements", "encoding",
        "enctype", "firstChild", "firstElementChild", "hasAttribute",
        "hasOwnProperty", "id", "innerHTML", "insertBefore", "lastChild",
        "lastElementChild", "length", "method", "name", "nextSibling",
        "noValidate", "outerHTML", "ownerDocument", "parentElement",
        "parentNode", "prepend", "previousSibling", "prototype",
        "querySelector", "querySelectorAll", "rel", "relList", "remove",
        "removeAttribute", "removeChild", "removeEventListener", "replaceChild",
        "reportValidity", "requestSubmit", "reset", "setAttribute", "submit",
        "tagName", "target", "textContent", "toString", "valueOf"
    };

    internal static HtmlSanitizerClobberingResult Apply(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        HashSet<string> identifiers = new HashSet<string>(
            StringComparer.Ordinal);
        HashSet<string> duplicateIdentifiers = new HashSet<string>(
            StringComparer.Ordinal);
        List<string> duplicates = new List<string>();
        int rewritten = 0;
        if (root.GetKindStorage() == HtmlNodeKind.Element)
        {
            rewritten = RewriteElement(root, policy);
            RecordIdentifier(root, identifiers, duplicateIdentifiers,
                duplicates);
        }

        HtmlNode[] descendants = root.GetDescendants();
        int index = 0;
        while (index < descendants.Length)
        {
            HtmlNode current = descendants[index];
            if (current.GetKindStorage() == HtmlNodeKind.Element)
            {
                rewritten = rewritten + RewriteElement(current, policy);
                RecordIdentifier(current, identifiers, duplicateIdentifiers,
                    duplicates);
            }

            index = index + 1;
        }

        return new HtmlSanitizerClobberingResult(rewritten,
            duplicates.ToArray());
    }

    internal static HtmlAttribute RewriteAttribute(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (!IsNamedPropertyAttribute(element, attribute))
        {
            return attribute;
        }

        string prefix = policy.GetDomClobberPrefix();
        if (attribute.GetValue().StartsWith(prefix,
            StringComparison.Ordinal))
        {
            return attribute;
        }

        return HtmlAttribute.CreateNamespaced(attribute.GetName(),
            attribute.GetLocalName(), attribute.GetPrefix(),
            attribute.GetNamespaceUri(), prefix + attribute.GetValue(),
            attribute.GetSourceSpan());
    }

    internal static bool IsDangerousName(string value)
    {
        return IsDangerousGlobalName(value) || IsDangerousFormName(value);
    }

    internal static bool IsDangerousGlobalName(string value)
    {
        return Contains(dangerousGlobalNames, value,
            "value");
    }

    internal static bool IsDangerousFormName(string value)
    {
        return Contains(dangerousFormNames, value,
            "value");
    }

    internal static string[] GetDangerousGlobalNames()
    {
        return Copy(dangerousGlobalNames);
    }

    internal static string[] GetDangerousFormNames()
    {
        return Copy(dangerousFormNames);
    }

    internal static int RewriteElement(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy)
    {
        HtmlAttribute[] original = element.GetAttributesStorage();
        HtmlAttribute[] rewritten = new HtmlAttribute[original.Length];
        bool changed = false;
        int changedCount = 0;
        int index = 0;
        while (index < original.Length)
        {
            rewritten[index] = RewriteAttribute(element, original[index],
                policy);
            if (rewritten[index] != original[index])
            {
                changed = true;
                changedCount = changedCount + 1;
            }

            index = index + 1;
        }

        if (changed)
        {
            element.ReplaceAttributesStorage(rewritten);
        }

        return changedCount;
    }

    internal static void RecordIdentifier(HtmlNode element,
        HashSet<string> identifiers, HashSet<string> duplicateIdentifiers,
        List<string> duplicates)
    {
        HtmlAttribute[] attributes = element.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute attribute = attributes[index];
            if (attribute.GetNamespaceUri().Length == 0
                && IsIdentifierAttribute(element, attribute))
            {
                string value = attribute.GetValue();
                if (value.Length > 0)
                {
                    if (!identifiers.Add(value)
                        && duplicateIdentifiers.Add(value))
                    {
                        duplicates.Add(value);
                    }
                }

                return;
            }

            index = index + 1;
        }
    }

    internal static bool IsIdentifierAttribute(HtmlNode element,
        HtmlAttribute attribute)
    {
        if (element.GetNamespaceUriStorage()
            == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            return Ascii.EqualsIgnoreCase(
                attribute.GetLocalName().AsSpan(), "id".AsSpan());
        }

        return attribute.GetLocalName() == "id";
    }

    internal static bool IsNamedPropertyAttribute(HtmlNode element,
        HtmlAttribute attribute)
    {
        if (element.GetKindStorage() != HtmlNodeKind.Element
            || element.GetNamespaceUriStorage()
                != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri()
            || attribute.GetNamespaceUri().Length != 0)
        {
            return false;
        }

        return Ascii.EqualsIgnoreCase(attribute.GetLocalName().AsSpan(),
                "id".AsSpan())
            || Ascii.EqualsIgnoreCase(attribute.GetLocalName().AsSpan(),
                "name".AsSpan());
    }

    internal static bool Contains(string[] values, string expected,
        string parameterName)
    {
        if (expected == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string[] Copy(string[] values)
    {
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
