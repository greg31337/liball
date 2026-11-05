using System;
using System.Collections.Generic;

namespace htmlgui;

internal class DocumentComposer
{
    internal DomNode ComposePageDocument(UiWindow? window, UiPage? page)
    {
        if (window == null)
        {
            throw new ArgumentNullException("window");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        return ComposePageDocumentWithNonce(window, page, ResolveContentSecurityPolicyNonce(window, page));
    }

    internal DomNode ComposePageDocumentWithNonce(UiWindow window, UiPage page, string? contentSecurityPolicyNonce)
    {
        DomNode doctype = new DomNode
        {
            Kind = DomNodeKind.RawContent,
            Key = "$doctype",
            Text = "<!doctype html>",
            RawContentPolicyId = "$renderer:doctype"
        };
        DomNode head = ComposeHead(page, contentSecurityPolicyNonce);
        DomNode body = ComposeBody(window, page);
        List<DomAttribute> htmlAttributes = new List<DomAttribute>();
        AddOptionalAttribute(htmlAttributes, HtmlAttributes.Language, ResolveLanguage(window, page));
        AddOptionalAttribute(htmlAttributes, HtmlAttributes.Direction, ResolveDirection(window, page));
        DomNode html = CreateElementNode("$html", HtmlElements.Html, htmlAttributes.ToArray(), new DomNode[] { head, body });
        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = page.Key,
            Id = page.Id,
            Children = new DomNode[] { doctype, html }
        };
    }

    internal DomNode ComposeHead(UiPage page, string? contentSecurityPolicyNonce)
    {
        UiDocumentMetadata metadata = page.Metadata;
        List<DomNode> children = new List<DomNode>();
        children.Add(CreateMetaNode("$charset", HtmlAttributes.CharacterSet, metadata.CharacterSet));
        children.Add(CreateNamedMetaNode("$viewport", "viewport", metadata.Viewport));
        children.Add(CreateTitleNode(ResolveTitle(page)));
        if (metadata.BaseUrl != null && metadata.BaseUrl.Length > 0)
        {
            DomAttribute href = CreateAttributeNode(HtmlAttributes.HyperlinkReference, HtmlSecurityPolicy.NormalizeResourceUrl(metadata.BaseUrl));
            children.Add(CreateElementNode("$base", HtmlElements.Base, new DomAttribute[] { href }, Array.Empty<DomNode>()));
        }

        AddOptionalMeta(children, "$description", "description", metadata.Description);
        AddOptionalMeta(children, "$application-name", "application-name", metadata.ApplicationName);
        AddCustomMetadata(children, metadata.Values);
        AddResources(children, page.Resources, contentSecurityPolicyNonce);
        return CreateElementNode("$head", HtmlElements.Head, Array.Empty<DomAttribute>(), children.ToArray());
    }

    internal DomNode ComposeBody(UiWindow window, UiPage page)
    {
        SemanticExpander expander = new SemanticExpander();
        expander.ResourceCapabilities = window.HostCapabilities ?? Array.Empty<string>();
        expander.BeginPortalCollection(window.Overlays);
        List<DomNode> children = new List<DomNode>();
        if (page.ShellView != null)
        {
            children.Add(expander.ExpandView(page.ShellView));
        }

        if (page.ContentView != null)
        {
            children.Add(expander.ExpandView(page.ContentView));
        }

        UiView[]? overlays = window.Overlays;
        if (overlays != null)
        {
            List<DomNode> expandedOverlays = new List<DomNode>();
            int expansionIndex = 0;
            while (expansionIndex < overlays.Length)
            {
                UiView? overlay = overlays[expansionIndex];
                if (overlay != null)
                {
                    expandedOverlays.Add(expander.ExpandView(overlay));
                }

                expansionIndex++;
            }

            int overlayIndex = 0;
            int expandedIndex = 0;
            while (overlayIndex < overlays.Length)
            {
                UiView? overlay = overlays[overlayIndex];
                if (overlay != null)
                {
                    DomNode expandedOverlay = expandedOverlays[expandedIndex];
                    expander.AppendChildren(expandedOverlay, expander.TakePortalContent(overlay.Key));
                    children.Add(expandedOverlay);
                    expandedIndex++;
                }

                overlayIndex++;
            }
        }

        expander.EndPortalCollection();
        return CreateElementNode("$body", HtmlElements.Body, Array.Empty<DomAttribute>(), children.ToArray());
    }

    internal void AddCustomMetadata(List<DomNode> children, UiValueEntry[]? entries)
    {
        if (entries == null)
        {
            return;
        }

        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry? entry = entries[index];
            if (entry != null)
            {
                children.Add(CreateNamedMetaNode("$metadata:" + entry.Key, entry.Key, FormatMetadataValue(entry.Value)));
            }

            index++;
        }
    }

    internal void AddResources(List<DomNode> children, UiResource[]? resources, string? contentSecurityPolicyNonce)
    {
        if (resources == null)
        {
            return;
        }

        List<string> resourceKeys = new List<string>();
        int index = 0;
        while (index < resources.Length)
        {
            UiResource? resource = resources[index];
            if (resource != null)
            {
                AddResourceKey(resourceKeys, resource.Key);
                children.Add(CreateResourceNode(resource, contentSecurityPolicyNonce));
            }

            index++;
        }
    }

    internal void AddResourceKey(List<string> resourceKeys, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new ArgumentException("Document resources require non-empty stable keys.", "resources");
        }

        int index = 0;
        while (index < resourceKeys.Count)
        {
            if (resourceKeys[index] == candidate)
            {
                throw new ArgumentException("Document resource keys must be unique.", "resources");
            }

            index++;
        }

        resourceKeys.Add(candidate);
    }

    internal DomNode CreateResourceNode(UiResource resource, string? contentSecurityPolicyNonce)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        string relationship = HtmlSecurityPolicy.NormalizeResourceRelationship(resource.Relationship, resource.IsModule);
        string url = HtmlSecurityPolicy.NormalizeResourceUrl(resource.Url);
        HtmlSecurityPolicy.ValidateResourceMimeType(resource.MimeType);
        HtmlSecurityPolicy.ValidateResourceIntegrity(resource.Integrity);
        HtmlSecurityPolicy.ValidateCrossOrigin(resource.CrossOrigin);
        HtmlSecurityPolicy.ValidateReferrerPolicy(resource.ReferrerPolicy);
        string nonce = HtmlSecurityPolicy.NormalizeContentSecurityPolicyNonce(contentSecurityPolicyNonce);
        bool isScript = relationship == "script";
        if (isScript)
        {
            attributes.Add(CreateAttributeNode(HtmlAttributes.Source, url));
            if (resource.IsModule)
            {
                attributes.Add(CreateAttributeNode(HtmlAttributes.Type, "module"));
            }
            else
            {
                AddOptionalAttribute(attributes, HtmlAttributes.Type, resource.MimeType);
            }
        }
        else
        {
            attributes.Add(CreateAttributeNode(HtmlAttributes.Relationship, relationship));
            attributes.Add(CreateAttributeNode(HtmlAttributes.HyperlinkReference, url));
            AddOptionalAttribute(attributes, HtmlAttributes.Type, resource.MimeType);
            AddOptionalAttribute(attributes, HtmlAttributes.Media, resource.Media);
        }

        AddOptionalAttribute(attributes, "integrity", resource.Integrity);
        AddOptionalAttribute(attributes, "crossorigin", resource.CrossOrigin);
        AddOptionalAttribute(attributes, "referrerpolicy", resource.ReferrerPolicy);
        if (nonce.Length > 0 && (isScript || relationship == "stylesheet"))
        {
            attributes.Add(CreateAttributeNode(BridgeProtocolNames.Nonce, nonce));
        }

        AddResourceAttributes(attributes, resource.Attributes, isScript);
        string name = HtmlElements.Link;
        if (isScript)
        {
            name = HtmlElements.Script;
        }

        return CreateElementNode("$resource:" + resource.Key, name, attributes.ToArray(), Array.Empty<DomNode>());
    }

    internal void AddResourceAttributes(List<DomAttribute> attributes, UiValueEntry[]? entries, bool isScript)
    {
        if (entries == null)
        {
            return;
        }

        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry? entry = entries[index];
            if (entry != null)
            {
                if (!HtmlSecurityPolicy.IsSafeAdditionalResourceAttribute(entry.Key, isScript))
                {
                    throw new ArgumentException("The additional resource attribute is not allowed.", "entries");
                }

                string value = HtmlEscaper.NormalizeHtmlUnicode(FormatMetadataValue(entry.Value), HtmlInvalidCharacterPolicy.Reject);
                attributes.Add(CreateAttributeNode(entry.Key, value));
            }

            index++;
        }
    }

    internal void AddOptionalMeta(List<DomNode> children, string key, string name, string? value)
    {
        if (value != null && value.Length > 0)
        {
            children.Add(CreateNamedMetaNode(key, name, value));
        }
    }

    internal void AddOptionalAttribute(List<DomAttribute> attributes, string name, string? value)
    {
        if (value != null && value.Length > 0)
        {
            attributes.Add(CreateAttributeNode(name, value));
        }
    }

    internal DomNode CreateMetaNode(string key, string name, string value)
    {
        DomAttribute attribute = CreateAttributeNode(name, value);
        return CreateElementNode(key, HtmlElements.Meta, new DomAttribute[] { attribute }, Array.Empty<DomNode>());
    }

    internal DomNode CreateNamedMetaNode(string key, string name, string value)
    {
        DomAttribute nameAttribute = CreateAttributeNode(HtmlAttributes.Name, name);
        DomAttribute contentAttribute = CreateAttributeNode(HtmlAttributes.Content, value);
        return CreateElementNode(key, HtmlElements.Meta, new DomAttribute[] { nameAttribute, contentAttribute }, Array.Empty<DomNode>());
    }

    internal DomNode CreateTitleNode(string title)
    {
        DomNode text = new DomNode { Kind = DomNodeKind.Text, Key = "$title-text", Text = title };
        return CreateElementNode("$title", HtmlElements.Title, Array.Empty<DomAttribute>(), new DomNode[] { text });
    }

    internal DomNode CreateElementNode(string key, string name, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Name = name,
            Attributes = attributes,
            Children = children
        };
    }

    internal DomAttribute CreateAttributeNode(string name, string value)
    {
        return new DomAttribute { Name = name, Value = value };
    }

    internal string ResolveTitle(UiPage page)
    {
        if (page.Title.Length > 0)
        {
            return page.Title;
        }

        if (page.Metadata.Title.Length > 0)
        {
            return page.Metadata.Title;
        }

        return string.Empty;
    }

    internal string? ResolveLanguage(UiWindow window, UiPage page)
    {
        if (page.Language != null && page.Language.Length > 0)
        {
            return page.Language;
        }

        if (page.Metadata.Language != null && page.Metadata.Language.Length > 0)
        {
            return page.Metadata.Language;
        }

        return window.Document.Language;
    }

    internal string? ResolveDirection(UiWindow window, UiPage page)
    {
        if (page.TextDirection != null && page.TextDirection.Length > 0)
        {
            return page.TextDirection;
        }

        if (page.Metadata.TextDirection != null && page.Metadata.TextDirection.Length > 0)
        {
            return page.Metadata.TextDirection;
        }

        return window.Document.TextDirection;
    }

    internal string? ResolveContentSecurityPolicyNonce(UiWindow window, UiPage page)
    {
        if (page.Metadata.ContentSecurityPolicyNonce != null && page.Metadata.ContentSecurityPolicyNonce.Length > 0)
        {
            return page.Metadata.ContentSecurityPolicyNonce;
        }

        return window.Document.ContentSecurityPolicyNonce;
    }

    internal string FormatMetadataValue(UiValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        switch (value.Kind)
        {
            case UiValueKind.Null:
                return string.Empty;
            case UiValueKind.Boolean:
                return InvariantFormatter.FormatBoolean(value.BooleanValue);
            case UiValueKind.Integer:
                return InvariantFormatter.FormatInt64(value.IntegerValue);
            case UiValueKind.FloatingPoint:
                return InvariantFormatter.FormatDouble(value.FloatingPointValue);
            case UiValueKind.Decimal:
                return InvariantFormatter.FormatDecimal(value.DecimalValue);
            case UiValueKind.String:
                return value.StringValue ?? string.Empty;
            case UiValueKind.DateTime:
                return value.DateTimeValue ?? string.Empty;
            case UiValueKind.Binary:
                return Convert.ToBase64String(value.BinaryValue ?? Array.Empty<byte>());
            case UiValueKind.Array:
            case UiValueKind.Object:
                return ValueJsonCodec.SerializeValue(value, 64, 10000, 1048576, 1048576);
            default:
                throw new ArgumentOutOfRangeException("kind", value.Kind, "The metadata value kind is not supported.");
        }
    }
}
