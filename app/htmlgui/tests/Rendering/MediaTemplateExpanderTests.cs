using System;
using htmlgui;

namespace htmlgui.Tests;

public class MediaTemplateExpanderTests
{
    [TestCase("audio", HtmlElements.Audio)]
    [TestCase("video", HtmlElements.Video)]
    public void ExpandsOptedInAudioAndVideoWithPlaybackOptionsAndFallback(string kind, string elementName)
    {
        UiNode node = MediaControl(kind, "/media/sample.mp4");
        node.State.Version = 7;
        node.Media!.MimeType = "video/mp4";
        node.Media.AlternativeText = "Sample media";
        node.Media.ShowControls = true;
        node.Media.AutoPlay = true;
        node.Media.Muted = true;
        node.Media.Loop = true;
        node.Media.FallbackContent = new UiNode[] { TextNode("fallback", "fallback-id", "Media unavailable") };
        node.Children = new UiNode[] { TextNode("track", "track-id", "Track description") };
        SemanticExpander expander = EnabledExpander("media." + kind);

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Key, Is.EqualTo("media"));
            Assert.That(result.Id, Is.EqualTo("media-id"));
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(result.Version, Is.EqualTo(7));
            Assert.That(AttributeValue(result, HtmlAttributes.Source), Is.EqualTo("/media/sample.mp4"));
            Assert.That(AttributeValue(result, "data-mime-type"), Is.EqualTo("video/mp4"));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Sample media"));
            Assert.That(FindAttribute(result, HtmlAttributes.Controls).IsBoolean, Is.True);
            Assert.That(FindAttribute(result, "autoplay").IsBoolean, Is.True);
            Assert.That(FindAttribute(result, "muted").IsBoolean, Is.True);
            Assert.That(FindAttribute(result, "loop").IsBoolean, Is.True);
            Assert.That(Array.ConvertAll(result.Children, child => child.Text), Is.EqualTo(new string[] { "Track description", "Media unavailable" }));
        });
    }

    [Test]
    public void ExpandsEmbedAsSandboxedFrameWithTitleAndFallback()
    {
        UiNode node = MediaControl("embed", "https://widgets.example.test/chart");
        node.Media!.MimeType = "text/html";
        node.Media.AlternativeText = "Interactive chart";
        node.Media.FallbackContent = new UiNode[] { TextNode("fallback", "fallback-id", "Open the chart separately") };
        SemanticExpander expander = EnabledExpander("media:embed");

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.InlineFrame));
            Assert.That(AttributeValue(result, HtmlAttributes.Source), Is.EqualTo("https://widgets.example.test/chart"));
            Assert.That(AttributeValue(result, HtmlAttributes.Title), Is.EqualTo("Interactive chart"));
            Assert.That(AttributeValue(result, "sandbox"), Is.Empty);
            Assert.That(FindAttribute(result, "sandbox").IsBoolean, Is.False);
            Assert.That(result.Children[0].Text, Is.EqualTo("Open the chart separately"));
        });
    }

    [Test]
    public void OmitsOptionalExternalMediaAttributesAndKeepsPrimaryOnlyContent()
    {
        UiNode node = MediaControl("audio", "/audio/podcast.mp3");
        node.Media!.MimeType = string.Empty;
        node.Media.AlternativeText = string.Empty;
        node.Children = new UiNode[] { TextNode("transcript", "transcript-id", "Transcript") };

        DomNode result = EnabledExpander("audio").ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Audio));
            Assert.That(HasAttribute(result, "data-mime-type"), Is.False);
            Assert.That(HasAttribute(result, AriaAttributes.Label), Is.False);
            Assert.That(HasAttribute(result, HtmlAttributes.Controls), Is.False);
            Assert.That(HasAttribute(result, "autoplay"), Is.False);
            Assert.That(HasAttribute(result, "muted"), Is.False);
            Assert.That(HasAttribute(result, "loop"), Is.False);
            Assert.That(result.Children, Has.Length.EqualTo(1));
            Assert.That(result.Children[0].Text, Is.EqualTo("Transcript"));
        });
    }

    [Test]
    public void EmbedWithoutAlternativeTextOmitsTitle()
    {
        UiNode node = MediaControl("embed", "about:blank");
        node.Media!.AlternativeText = string.Empty;
        UiNode defaultAlternative = MediaControl("embed", "/embedded/default");

        DomNode result = EnabledExpander("embed").ExpandNode(node);
        DomNode defaultResult = EnabledExpander("media.embed").ExpandNode(defaultAlternative);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.InlineFrame));
            Assert.That(HasAttribute(result, HtmlAttributes.Title), Is.False);
            Assert.That(AttributeValue(result, HtmlAttributes.Source), Is.EqualTo("about:blank"));
            Assert.That(HasAttribute(defaultResult, HtmlAttributes.Title), Is.False);
        });
    }

    [Test]
    public void ExpandsStructuredInlineSvgWithNamespaceAndAccessibleName()
    {
        UiNode group = SvgElement("group", "group-id", "g");
        UiNode path = SvgElement("path", "path-id", "path");
        group.Children = new UiNode[] { path, TextNode("title-text", "title-text-id", "Vector") };
        UiNode node = MediaControl("inline-svg", string.Empty);
        node.Media!.AlternativeText = "Status icon";
        node.Children = new UiNode[] { group };
        SemanticExpander expander = EnabledExpander("inline-svg");

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Svg));
            Assert.That(result.NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-inline-svg"));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Image));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Status icon"));
            Assert.That(result.Children[0].NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(result.Children[0].Children[0].NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(result.Children[0].Children[1].Text, Is.EqualTo("Vector"));
        });
    }

    [Test]
    public void DecorativeInlineSvgIsHiddenFromAccessibilityApis()
    {
        UiNode node = MediaControl("inline-svg", string.Empty);
        node.Children = new UiNode[] { SvgElement("circle", "circle-id", "circle") };
        SemanticExpander expander = EnabledExpander("media.inline-svg");

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, AriaAttributes.Hidden), Is.EqualTo("true"));
            Assert.That(HasAttribute(result, HtmlAttributes.Role), Is.False);
            Assert.That(HasAttribute(result, AriaAttributes.Label), Is.False);
        });
    }

    [Test]
    public void MissingWindowCapabilityRendersFallbackWhileEnabledCapabilityRendersMedia()
    {
        UiNode mediaNode = MediaControl("video", "/video/intro.mp4");
        mediaNode.Media!.FallbackContent = new UiNode[] { TextNode("fallback", "fallback-id", "Video fallback") };
        UiPage page = new UiPage
        {
            Key = "page",
            Id = "page-id",
            ContentView = new UiView { Key = "content", Id = "content-id", ContentRoot = mediaNode }
        };
        UiWindow window = new UiWindow { Key = "window", Id = "window-id" };
        DocumentComposer composer = new DocumentComposer();

        DomNode fallbackDocument = composer.ComposePageDocument(window, page);
        DomNode fallback = fallbackDocument.Children[1].Children[1].Children[0].Children[0];
        window.HostCapabilities = new string[] { "video" };
        DomNode mediaDocument = composer.ComposePageDocument(window, page);
        DomNode media = mediaDocument.Children[1].Children[1].Children[0].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(fallback.Key, Is.EqualTo("media"));
            Assert.That(fallback.Id, Is.EqualTo("media-id"));
            Assert.That(fallback.Children[0].Text, Is.EqualTo("Video fallback"));
            Assert.That(media.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(media.Name, Is.EqualTo(HtmlElements.Video));
            Assert.That(media.Children[0].Text, Is.EqualTo("Video fallback"));
        });
    }

    [Test]
    public void ApplicationExpansionUsesEachWindowsIndependentMediaCapabilities()
    {
        UiApplication application = new UiApplication { Key = "application", Id = "application-id" };
        UiWindow enabledWindow = WindowWithMedia("enabled", "enabled-id", "audio");
        enabledWindow.HostCapabilities = new string[] { "media.audio" };
        UiWindow fallbackWindow = WindowWithMedia("fallback", "fallback-id", "audio");
        application.Windows = new UiWindow[] { enabledWindow, fallbackWindow };

        DomNode result = new SemanticExpander().ExpandApplication(application);
        DomNode enabledMedia = result.Children[0].Children[0].Children[0].Children[0];
        DomNode fallbackMedia = result.Children[1].Children[0].Children[0].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(enabledMedia.Name, Is.EqualTo(HtmlElements.Audio));
            Assert.That(fallbackMedia.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(fallbackMedia.Children[0].Text, Is.EqualTo("Fallback"));
        });
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,<script>alert(1)</script>")]
    [TestCase("file:///private/data.mp4")]
    [TestCase("ftp://example.test/data.mp4")]
    [TestCase("mailto:media@example.test")]
    [TestCase("/media/bad file.mp4")]
    [TestCase("/media/bad\tfile.mp4")]
    [TestCase("/media/bad\nfile.mp4")]
    [TestCase("/media\\bad.mp4")]
    public void RejectsUnsafeOrUnsupportedExternalResourceUrls(string source)
    {
        UiNode node = MediaControl("video", source);
        node.Media!.FallbackContent = new UiNode[] { TextNode("fallback", "fallback-id", "Fallback") };
        SemanticExpander expander = EnabledExpander("media");

        UiRenderException exception = Assert.Throws<UiRenderException>(() => expander.ExpandNode(node))!;

        Assert.That(exception.NodeId, Is.EqualTo("media-id"));
    }

    [TestCase("https://cdn.example.test/video.mp4", "https://cdn.example.test/video.mp4")]
    [TestCase("HTTP://cdn.example.test/video.mp4", "http://cdn.example.test/video.mp4")]
    [TestCase("blob:https://example.test/id", "blob:https://example.test/id")]
    [TestCase("about:blank", "about:blank")]
    [TestCase("/media/video.mp4", "/media/video.mp4")]
    [TestCase("../media/video.mp4?quality=high#start", "../media/video.mp4?quality=high#start")]
    [TestCase("folder/name:variant.mp4", "folder/name:variant.mp4")]
    [TestCase("?source=https:video", "?source=https:video")]
    [TestCase("#source:https:video", "#source:https:video")]
    public void AcceptsAndNormalizesAllowlistedAbsoluteAndRelativeResourceUrls(string source, string expected)
    {
        UiNode node = MediaControl("video", source);
        SemanticExpander expander = EnabledExpander("media.video");

        DomNode result = expander.ExpandNode(node);

        Assert.That(AttributeValue(result, HtmlAttributes.Source), Is.EqualTo(expected));
    }

    [Test]
    public void UnsupportedCapabilitiesRequireFallbackAndDoNotExpandPrimaryChildren()
    {
        UiNode withFallback = MediaControl("audio", "/audio/sample.mp3");
        UiNode untrustedPrimary = new UiNode
        {
            Kind = UiNodeKind.RawContent,
            Identity = new UiNodeIdentity { AuthorKey = "raw", StableId = "raw-id" },
            Text = "<script>not expanded</script>"
        };
        withFallback.Children = new UiNode[] { untrustedPrimary };
        withFallback.Media!.FallbackContent = new UiNode[] { TextNode("fallback", "fallback-id", "Audio transcript") };
        UiNode withoutFallback = MediaControl("audio", "/audio/sample.mp3");
        SemanticExpander expander = new SemanticExpander();

        DomNode fallback = expander.ExpandNode(withFallback);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(fallback.Children[0].Text, Is.EqualTo("Audio transcript"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(withoutFallback));
        });
    }

    [Test]
    public void CapabilityMatchingHandlesNullEntriesAndNullCapabilityCollections()
    {
        UiMedia media = new UiMedia { Kind = "video", Source = "/video/sample.mp4" };
        MediaTemplateExpander expander = new MediaTemplateExpander();

        Assert.Multiple(() =>
        {
            Assert.That(expander.HasMediaCapability(media, null), Is.False);
            Assert.That(expander.HasMediaCapability(media, new string[] { null!, "unrelated" }), Is.False);
            Assert.That(expander.HasMediaCapability(media, new string[] { null!, "media.video" }), Is.True);
        });
    }

    [Test]
    public void RejectsMalformedMediaPayloadsAndInlineSvgContent()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "video",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode unknown = MediaControl("panorama", "/media/panorama.bin");
        UiNode missingSource = MediaControl("audio", " ");
        UiNode sourceSvg = MediaControl("inline-svg", "<svg></svg>");
        UiNode htmlInSvg = MediaControl("inline-svg", string.Empty);
        htmlInSvg.Children = new UiNode[] { Element("html", "html-id", HtmlElements.Division, null) };
        UiNode rawInSvg = MediaControl("inline-svg", string.Empty);
        UiNode svgGroup = SvgElement("group", "group-id", "g");
        svgGroup.Children = new UiNode[]
        {
            new UiNode
            {
                Kind = UiNodeKind.RawContent,
                Identity = new UiNodeIdentity { AuthorKey = "raw", StableId = "raw-id" },
                Text = "<path />",
                RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = "test" }
            }
        };
        rawInSvg.Children = new UiNode[] { svgGroup };
        SemanticExpander expander = EnabledExpander("media");

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknown));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingSource));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(sourceSvg));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(htmlInSvg));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(rawInSvg));
        });
    }

    [Test]
    public void RepeatedExpansionIsFreshStableAndDoesNotMutateMediaModels()
    {
        UiNode node = MediaControl("video", "/video/original.mp4");
        UiMedia sourceMedia = node.Media!;
        UiNode fallback = TextNode("fallback", "fallback-id", "Original fallback");
        sourceMedia.FallbackContent = new UiNode[] { fallback };
        SemanticExpander expander = EnabledExpander("media.video");

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Attributes[1].Value = "changed";
        first.Children[0].Text = "Changed output";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(first.Id, Is.EqualTo(second.Id));
            Assert.That(sourceMedia.Source, Is.EqualTo("/video/original.mp4"));
            Assert.That(sourceMedia.FallbackContent[0], Is.SameAs(fallback));
            Assert.That(fallback.Text, Is.EqualTo("Original fallback"));
            Assert.That(second.Children[0].Text, Is.EqualTo("Original fallback"));
        });
    }

    private static SemanticExpander EnabledExpander(string capability)
    {
        SemanticExpander expander = new SemanticExpander();
        expander.ResourceCapabilities = new string[] { capability };
        return expander;
    }

    private static UiNode MediaControl(string kind, string source)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "media", SiblingIdentity = "media", StableId = "media-id" },
            Media = new UiMedia { Kind = kind, Source = source }
        };
    }

    private static UiWindow WindowWithMedia(string key, string id, string kind)
    {
        UiNode media = MediaControl(kind, "/media/sample.mp4");
        media.Media!.FallbackContent = new UiNode[] { TextNode("fallback", "fallback-text-id", "Fallback") };
        UiPage page = new UiPage
        {
            Key = key + "-page",
            Id = id + "-page-id",
            ContentView = new UiView { Key = key + "-content", Id = id + "-content-id", ContentRoot = media }
        };
        return new UiWindow { Key = key, Id = id, Pages = new UiPage[] { page } };
    }

    private static UiNode SvgElement(string key, string id, string name)
    {
        return Element(key, id, name, "http://www.w3.org/2000/svg");
    }

    private static UiNode Element(string key, string id, string name, string? namespaceUri)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Element,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            NamespaceUri = namespaceUri,
            Name = name
        };
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index];
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
