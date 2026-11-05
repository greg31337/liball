using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace htmlgui;

internal class HtmlOutputComposition
{
    internal string Html = string.Empty;
    internal string Css = string.Empty;
    internal string Bootstrap = string.Empty;
    internal string HydrationJson = string.Empty;
    internal DomNode Root = new DomNode();
}

internal class HtmlOutputComposer
{
    internal HtmlOutputComposition ComposePageOutput(UiWindow? window, UiPage? page, UiCssCompilation? cssCompilation, UiHydrationManifest? hydration, UiRenderOptions? options)
    {
        if (window == null)
        {
            throw new ArgumentNullException("window");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        DocumentComposer documentComposer = new DocumentComposer();
        string nonce = ResolveOutputNonce(documentComposer, window, page, options);
        string css = ResolveCssOutput(cssCompilation);
        string bootstrap = options.IncludeBootstrap ? LoadBootstrapAsset() : string.Empty;
        string hydrationJson = options.IncludeHydration ? SerializeHydrationManifest(hydration ?? new UiHydrationManifest()) : string.Empty;
        DomNode document = documentComposer.ComposePageDocumentWithNonce(window, page, nonce);
        DomNode head = document.Children[1].Children[0];
        DomNode body = document.Children[1].Children[1];
        DomNode? style = css.Length > 0 ? CreateStyleAsset(css, nonce) : null;
        DomNode? hydrationScript = hydrationJson.Length > 0 ? CreateScriptAsset("$hydration", "htmlgui-hydration", "application/json", hydrationJson, nonce) : null;
        DomNode? bootstrapScript = bootstrap.Length > 0 ? CreateScriptAsset("$bootstrap", "htmlgui-bootstrap", "text/javascript", bootstrap, nonce) : null;
        HtmlOutputComposition result = new HtmlOutputComposition();
        result.Css = css;
        result.Bootstrap = bootstrap;
        result.HydrationJson = hydrationJson;
        if (options.FullDocument)
        {
            if (style != null)
            {
                AppendElementChild(head, style);
            }

            if (hydrationScript != null)
            {
                AppendElementChild(body, hydrationScript);
            }

            if (bootstrapScript != null)
            {
                AppendElementChild(body, bootstrapScript);
            }

            result.Root = document;
        }
        else
        {
            result.Root = CreateFragmentOutput(head, body, style, hydrationScript, bootstrapScript);
        }

        result.Html = new HtmlSerializer().SerializeNode(result.Root);
        return result;
    }

    internal string ResolveOutputNonce(DocumentComposer composer, UiWindow window, UiPage page, UiRenderOptions options)
    {
        string? nonce = options.ContentSecurityPolicyNonce;
        if (nonce == null || nonce.Length == 0)
        {
            nonce = composer.ResolveContentSecurityPolicyNonce(window, page);
        }

        return HtmlSecurityPolicy.NormalizeContentSecurityPolicyNonce(nonce);
    }

    internal string ResolveCssOutput(UiCssCompilation? compilation)
    {
        if (compilation == null || compilation.Css.Length == 0)
        {
            return string.Empty;
        }

        string css = InvariantFormatter.NormalizeLineEndings(compilation.Css);
        HtmlEscaper.EscapeRawText(css, HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject);
        return HtmlEscaper.NormalizeHtmlUnicode(css, HtmlInvalidCharacterPolicy.Reject);
    }

    internal string LoadBootstrapAsset()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream("HtmlGui.Browser.htmlgui.runtime.js");
        if (stream == null)
        {
            throw new InvalidOperationException("The embedded browser bootstrap asset is unavailable.");
        }

        using (stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string source = reader.ReadToEnd();
                string normalized = InvariantFormatter.NormalizeLineEndings(source);
                return HtmlEscaper.NormalizeHtmlUnicode(normalized, HtmlInvalidCharacterPolicy.Reject);
            }
        }
    }

    internal string SerializeHydrationManifest(UiHydrationManifest? manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException("manifest");
        }

        if (manifest.Version < 0 || manifest.NavigationGeneration < 0)
        {
            throw new ArgumentException("Hydration versions and navigation generations cannot be negative.", "manifest");
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber("version", manifest.Version);
                writer.WriteString("hash", NormalizeHydrationText(manifest.Hash));
                WriteHydrationEntries(writer, "nodes", manifest.Nodes);
                WriteHydrationEntries(writer, "components", manifest.Components);
                WriteHydrationEntries(writer, "events", manifest.Events);
                WriteHydrationEntries(writer, "bindings", manifest.Bindings);
                WriteHydrationEntries(writer, "state", manifest.State);
                WriteHydrationEntries(writer, "styles", manifest.Styles);
                WriteOptionalHydrationText(writer, "routeUrl", manifest.RouteUrl);
                WriteHydrationRouteData(writer, manifest.RouteData);
                WriteHydrationValue(writer, "historyState", manifest.HistoryState);
                WriteOptionalHydrationText(writer, "historyEntryId", manifest.HistoryEntryId);
                writer.WriteNumber("navigationGeneration", manifest.NavigationGeneration);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal void WriteHydrationEntries(Utf8JsonWriter writer, string name, UiValueEntry[]? entries)
    {
        UiValue value = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = entries ?? Array.Empty<UiValueEntry>()
        };
        ValueJsonCodec.ValidateValue(value, 64, 10000, 1048576, 1048576);
        ValidateHydrationEntryText(value.ObjectValue);
        writer.WritePropertyName(name);
        ValueJsonCodec.WriteNode(writer, value);
    }

    internal void WriteHydrationRouteData(Utf8JsonWriter writer, UiRouteData? routeData)
    {
        writer.WritePropertyName("routeData");
        if (routeData == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("routeId", NormalizeHydrationText(routeData.RouteId));
        WriteHydrationEntries(writer, "parameters", routeData.Parameters);
        WriteHydrationEntries(writer, "query", routeData.Query);
        WriteOptionalHydrationText(writer, "fragment", routeData.Fragment);
        writer.WriteEndObject();
    }

    internal void WriteHydrationValue(Utf8JsonWriter writer, string name, UiValue? value)
    {
        UiValue source = value ?? new UiValue();
        ValueJsonCodec.ValidateValue(source, 64, 10000, 1048576, 1048576);
        writer.WritePropertyName(name);
        ValueJsonCodec.WriteNode(writer, source);
    }

    internal void WriteOptionalHydrationText(Utf8JsonWriter writer, string name, string? value)
    {
        if (value == null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, NormalizeHydrationText(value));
        }
    }

    internal void ValidateHydrationEntryText(UiValueEntry[]? entries)
    {
        UiValueEntry[] source = entries ?? Array.Empty<UiValueEntry>();
        int index = 0;
        while (index < source.Length)
        {
            UiValueEntry entry = source[index];
            NormalizeHydrationText(entry.Key);
            index++;
        }
    }

    internal string NormalizeHydrationText(string? value)
    {
        return HtmlEscaper.NormalizeHtmlUnicode(InvariantFormatter.NormalizeLineEndings(value ?? string.Empty), HtmlInvalidCharacterPolicy.Reject);
    }

    internal DomNode CreateStyleAsset(string css, string nonce)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        if (nonce.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = BridgeProtocolNames.Nonce, Value = nonce });
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$style",
            Name = HtmlElements.Style,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { new DomNode { Kind = DomNodeKind.Text, Key = "$style-text", Text = css } }
        };
    }

    internal DomNode CreateScriptAsset(string key, string id, string type, string source, string nonce)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Id, Value = id });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = type });
        if (nonce.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = BridgeProtocolNames.Nonce, Value = nonce });
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Name = HtmlElements.Script,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { new DomNode { Kind = DomNodeKind.Text, Key = key + "-text", Text = source } }
        };
    }

    internal void AppendElementChild(DomNode parent, DomNode child)
    {
        DomNode[] existing = parent.Children ?? Array.Empty<DomNode>();
        DomNode[] children = new DomNode[existing.Length + 1];
        Array.Copy(existing, children, existing.Length);
        children[existing.Length] = child;
        parent.Children = children;
    }

    internal DomNode CreateFragmentOutput(DomNode head, DomNode body, DomNode? style, DomNode? hydrationScript, DomNode? bootstrapScript)
    {
        List<DomNode> children = new List<DomNode>();
        AddFragmentResources(children, head.Children);
        if (style != null)
        {
            children.Add(style);
        }

        DomNode[] bodyChildren = body.Children ?? Array.Empty<DomNode>();
        int index = 0;
        while (index < bodyChildren.Length)
        {
            children.Add(bodyChildren[index]);
            index++;
        }

        if (hydrationScript != null)
        {
            children.Add(hydrationScript);
        }

        if (bootstrapScript != null)
        {
            children.Add(bootstrapScript);
        }

        return new DomNode { Kind = DomNodeKind.Fragment, Key = body.Key, Id = body.Id, Children = children.ToArray() };
    }

    internal void AddFragmentResources(List<DomNode> target, DomNode[]? headChildren)
    {
        DomNode[] children = headChildren ?? Array.Empty<DomNode>();
        int index = 0;
        while (index < children.Length)
        {
            DomNode child = children[index];
            if (child.Key.StartsWith("$resource:", StringComparison.Ordinal))
            {
                target.Add(child);
            }

            index++;
        }
    }
}
