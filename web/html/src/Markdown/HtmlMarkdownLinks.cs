using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlMarkdownReference
{
    internal string identifier;
    internal string key;
    internal string destination;
    internal string title;

    internal HtmlMarkdownReference(string identifierValue, string keyValue,
        string destinationValue, string titleValue)
    {
        if (identifierValue == null)
        {
            throw new ArgumentNullException("identifierValue");
        }

        if (keyValue == null)
        {
            throw new ArgumentNullException("keyValue");
        }

        if (destinationValue == null)
        {
            throw new ArgumentNullException("destinationValue");
        }

        if (titleValue == null)
        {
            throw new ArgumentNullException("titleValue");
        }

        this.identifier = identifierValue;
        this.key = keyValue;
        this.destination = destinationValue;
        this.title = titleValue;
    }

    internal string GetIdentifier()
    {
        return this.identifier;
    }

    internal string GetKey()
    {
        return this.key;
    }

    internal string GetDestination()
    {
        return this.destination;
    }

    internal string GetTitle()
    {
        return this.title;
    }
}

internal class HtmlMarkdownReferenceSelection
{
    internal HtmlMarkdownReference reference;
    internal bool collapsed;

    internal HtmlMarkdownReferenceSelection(HtmlMarkdownReference referenceValue,
        bool collapsedValue)
    {
        this.reference = referenceValue;
        this.collapsed = collapsedValue;
    }

    internal HtmlMarkdownReference GetReference()
    {
        return this.reference;
    }

    internal bool IsCollapsed()
    {
        return this.collapsed;
    }
}

internal class HtmlMarkdownLinks
{
    internal const string ValidationBaseUrl = "https://markdown.invalid/";

    internal HtmlCompiledMarkdownOptions options;
    internal CancellationToken cancellationToken;
    internal List<HtmlMarkdownReference> references;

    internal HtmlMarkdownLinks(HtmlCompiledMarkdownOptions optionsValue,
        CancellationToken cancellationTokenValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        this.options = optionsValue;
        this.cancellationToken = cancellationTokenValue;
        this.references = new List<HtmlMarkdownReference>();
    }

    internal string RenderLink(HtmlNode link, string label)
    {
        ValidateElement(link, "a");
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        string destination = this.GetSafeDestination(link, "href", false);
        if (destination.Length == 0)
        {
            return this.GetUnsafeDestinationFallback(label);
        }

        string title = GetNormalizedTitle(link);
        if (title.Length == 0 && this.options.GetEnableAutolinks())
        {
            if (CanUseExtendedWwwAutolink(label, destination))
            {
                return label;
            }

            if (this.CanUseAutolink(label, destination))
            {
                if (destination.StartsWith("mailto:",
                    StringComparison.Ordinal))
                {
                    return "<" + destination.Substring(7) + ">";
                }

                return "<" + destination + ">";
            }
        }

        return this.RenderLinkedValue(label, label,
            destination, title, false);
    }

    internal string RenderImage(HtmlNode image)
    {
        ValidateElement(image, "img");
        string? alt = image.GetAttribute("alt");
        if (alt == null)
        {
            alt = "";
        }

        string label = this.EscapeLinkText(HtmlTextContent.Normalize(alt));
        string destination = this.GetSafeDestination(image, "src", true);
        if (destination.Length == 0)
        {
            return this.GetUnsafeDestinationFallback(label);
        }

        string title = GetNormalizedTitle(image);
        return this.RenderLinkedValue(label, label, destination, title,
            true);
    }

    internal string GetUnsafeDestinationFallback(string label)
    {
        return this.options.GetUnsafeLinkFallback()
                == HtmlMarkdownUnsafeLinkFallback.Text
            ? label : "";
    }

    internal string AppendDefinitions(string body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        if (this.references.Count == 0)
        {
            return body;
        }

        StringBuilder builder = new StringBuilder(body);
        string newLine = this.options.GetNewLine();
        if (builder.Length > 0)
        {
            builder.Append(newLine);
            builder.Append(newLine);
        }

        int index = 0;
        while (index < this.references.Count)
        {
            if (index > 0)
            {
                builder.Append(newLine);
            }

            HtmlMarkdownReference reference = this.references[index];
            builder.Append('[');
            builder.Append(reference.GetIdentifier());
            builder.Append("]: ");
            builder.Append(this.EscapeDestination(
                reference.GetDestination()));
            AppendTitle(builder, reference.GetTitle(), this.options);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string RenderLinkedValue(string label, string preferredIdentifier,
        string destination, string title, bool image)
    {
        string prefix = image ? "!" : "";
        HtmlMarkdownLinkStyle style = this.options.GetLinkStyle();
        if (style == HtmlMarkdownLinkStyle.Inline)
        {
            StringBuilder inline = new StringBuilder();
            inline.Append(prefix);
            inline.Append('[');
            inline.Append(label);
            inline.Append("](");
            inline.Append(this.EscapeDestination(destination));
            AppendTitle(inline, title, this.options);
            inline.Append(')');
            return inline.ToString();
        }

        HtmlMarkdownReferenceSelection selection = this.GetOrAddReference(
            destination, title, preferredIdentifier,
            style == HtmlMarkdownLinkStyle.CollapsedReference);
        HtmlMarkdownReference reference = selection.GetReference();
        StringBuilder result = new StringBuilder();
        result.Append(prefix);
        result.Append('[');
        result.Append(label);
        result.Append(']');
        if (selection.IsCollapsed())
        {
            result.Append("[]");
        }
        else
        {
            result.Append('[');
            result.Append(reference.GetIdentifier());
            result.Append(']');
        }

        return result.ToString();
    }

    internal HtmlMarkdownReferenceSelection GetOrAddReference(
        string destination, string title, string preferredIdentifier,
        bool preferCollapsed)
    {
        int index = 0;
        while (index < this.references.Count)
        {
            HtmlMarkdownReference existing = this.references[index];
            if (existing.GetDestination() == destination
                && existing.GetTitle() == title)
            {
                bool existingCollapsed = preferCollapsed
                    && existing.GetKey()
                        == NormalizeReferenceKey(preferredIdentifier);
                return new HtmlMarkdownReferenceSelection(existing,
                    existingCollapsed);
            }

            index = index + 1;
        }

        string identifier;
        string key;
        bool collapsed = false;
        if (preferCollapsed && IsUsableCollapsedIdentifier(
            preferredIdentifier))
        {
            key = NormalizeReferenceKey(preferredIdentifier);
            if (!this.HasReferenceKey(key))
            {
                identifier = preferredIdentifier;
                collapsed = true;
            }
            else
            {
                identifier = this.CreateGeneratedIdentifier();
                key = NormalizeReferenceKey(identifier);
            }
        }
        else
        {
            identifier = preferCollapsed
                ? this.CreateGeneratedIdentifier()
                : (this.references.Count + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            key = NormalizeReferenceKey(identifier);
        }

        HtmlMarkdownReference added = new HtmlMarkdownReference(identifier,
            key, destination, title);
        this.references.Add(added);
        return new HtmlMarkdownReferenceSelection(added, collapsed);
    }

    internal string CreateGeneratedIdentifier()
    {
        int number = this.references.Count + 1;
        while (true)
        {
            string identifier = "link-" + number.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            if (!this.HasReferenceKey(
                NormalizeReferenceKey(identifier)))
            {
                return identifier;
            }

            number = number + 1;
        }
    }

    internal bool HasReferenceKey(string key)
    {
        int index = 0;
        while (index < this.references.Count)
        {
            if (this.references[index].GetKey() == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string GetSafeDestination(HtmlNode node, string attributeName,
        bool image)
    {
        string? value = node.GetAttribute(attributeName);
        if (value == null)
        {
            return "";
        }

        HtmlLimits limits = this.options.GetLimits();
        HtmlUrlSchemeResult scheme = HtmlUrls.ExtractScheme(value, limits,
            this.cancellationToken);
        if (!scheme.IsComplete())
        {
            return "";
        }

        string candidate = scheme.GetDeobfuscatedValue();
        if (scheme.HasScheme()
            && !IsAllowedScheme(scheme.GetScheme(), image))
        {
            return "";
        }

        HtmlUrlResult resolved = node.GetAbsoluteUrlWithLimits(attributeName, limits,
            this.cancellationToken);
        if (resolved.IsSuccess())
        {
            return IsAllowedResolvedUrl(resolved, image)
                ? resolved.GetUrl() : "";
        }

        if (scheme.HasScheme())
        {
            return "";
        }

        HtmlUrlResult validation = HtmlUrls.Resolve(candidate,
            ValidationBaseUrl, limits, this.cancellationToken);
        if (!validation.IsSuccess()
            || !IsAllowedResolvedUrl(validation, image))
        {
            return "";
        }

        return candidate;
    }

    internal bool CanUseAutolink(string renderedLabel, string destination)
    {
        if (!IsAutolinkDestination(destination))
        {
            return false;
        }

        if (destination.StartsWith("mailto:", StringComparison.Ordinal))
        {
            string address = destination.Substring(7);
            return IsSimpleEmailAddress(address)
                && renderedLabel == this.EscapeLinkText(address);
        }

        return renderedLabel == this.EscapeLinkText(destination);
    }

    internal string EscapeLinkText(string value)
    {
        return HtmlMarkdownEscaper.Escape(value,
            HtmlMarkdownContextKind.LinkText, "", false, this.options);
    }

    internal string EscapeDestination(string value)
    {
        return HtmlMarkdownEscaper.Escape(value,
            HtmlMarkdownContextKind.LinkDestination, "", false,
            this.options);
    }

    internal static void AppendTitle(StringBuilder builder, string title,
        HtmlCompiledMarkdownOptions options)
    {
        if (title.Length == 0)
        {
            return;
        }

        builder.Append(" \"");
        builder.Append(HtmlMarkdownEscaper.Escape(title,
            HtmlMarkdownContextKind.LinkTitle, "\"", false, options));
        builder.Append('\"');
    }

    internal static string GetNormalizedTitle(HtmlNode node)
    {
        string? title = node.GetAttribute("title");
        return title == null ? "" : HtmlTextContent.Normalize(title);
    }

    internal static bool IsAllowedResolvedUrl(HtmlUrlResult result, bool image)
    {
        if (!IsAllowedScheme(result.GetScheme(), image))
        {
            return false;
        }

        string scheme = result.GetScheme();
        if (scheme == "http" || scheme == "https")
        {
            Uri uri = new Uri(result.GetUrl(), UriKind.Absolute);
            if (uri.UserInfo.Length > 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsAllowedScheme(string scheme, bool image)
    {
        return scheme == "http" || scheme == "https"
            || !image && (scheme == "mailto" || scheme == "tel");
    }

    internal static bool IsAutolinkDestination(string value)
    {
        if (!value.StartsWith("http://", StringComparison.Ordinal)
            && !value.StartsWith("https://", StringComparison.Ordinal)
            && !value.StartsWith("mailto:", StringComparison.Ordinal)
            && !value.StartsWith("tel:", StringComparison.Ordinal))
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '<' || current == '>'
                || UnicodeTools.IsUnicodeWhitespace(current)
                || char.IsControl(current))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsSimpleEmailAddress(string value)
    {
        int at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@')
            || at == value.Length - 1)
        {
            return false;
        }

        int index = 0;
        while (index < at)
        {
            char current = value[index];
            bool allowed = current >= 'a' && current <= 'z'
                || current >= 'A' && current <= 'Z'
                || current >= '0' && current <= '9'
                || current == '.' || current == '!' || current == '#'
                || current == '$' || current == '%' || current == '&'
                || current == '\'' || current == '*' || current == '+'
                || current == '-' || current == '/' || current == '='
                || current == '?' || current == '^' || current == '_'
                || current == '`' || current == '{' || current == '|'
                || current == '}' || current == '~';
            if (!allowed)
            {
                return false;
            }

            index = index + 1;
        }

        int labelStart = at + 1;
        index = labelStart;
        bool sawDot = false;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '.')
            {
                int length = index - labelStart;
                if (length == 0 || value[labelStart] == '-'
                    || value[index - 1] == '-')
                {
                    return false;
                }

                sawDot = sawDot || index < value.Length;
                labelStart = index + 1;
            }
            else
            {
                char current = value[index];
                if (!(current >= 'a' && current <= 'z')
                    && !(current >= 'A' && current <= 'Z')
                    && !(current >= '0' && current <= '9')
                    && current != '-')
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return sawDot;
    }

    internal static bool CanUseExtendedWwwAutolink(string renderedLabel,
        string destination)
    {
        Uri? uri;
        if (!renderedLabel.StartsWith("www.",
            StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(destination, UriKind.Absolute, out uri)
            || uri == null
            || uri.Scheme != "http" || uri.UserInfo.Length > 0
            || uri.Port != 80 || uri.AbsolutePath != "/"
            || uri.Query.Length > 0 || uri.Fragment.Length > 0
            || !string.Equals(uri.Host, renderedLabel,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] labels = renderedLabel.Split('.');
        if (labels.Length < 3)
        {
            return false;
        }

        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            string label = labels[labelIndex];
            if (label.Length == 0)
            {
                return false;
            }

            int characterIndex = 0;
            while (characterIndex < label.Length)
            {
                char current = label[characterIndex];
                bool allowed = current >= 'a' && current <= 'z'
                    || current >= 'A' && current <= 'Z'
                    || current >= '0' && current <= '9'
                    || current == '-' || current == '_';
                if (!allowed || current == '_'
                    && labelIndex >= labels.Length - 2)
                {
                    return false;
                }

                characterIndex = characterIndex + 1;
            }

            labelIndex = labelIndex + 1;
        }

        return true;
    }

    internal static bool IsUsableCollapsedIdentifier(string value)
    {
        if (value.Length == 0 || value.Length > 999)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n'
                || current == '[' || current == ']')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string NormalizeReferenceKey(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (UnicodeTools.IsUnicodeWhitespace(current))
            {
                pendingSpace = builder.Length > 0;
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(char.ToUpperInvariant(current));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void ValidateElement(HtmlNode node, string name)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.GetKindStorage() != HtmlNodeKind.Element
            || node.GetNamespaceStorage() != HtmlNamespace.Html
            || node.GetLocalNameStorage() != name)
        {
            throw new ArgumentException(
                "Markdown link rendering requires an HTML " + name
                + " element.", "node");
        }
    }
}
