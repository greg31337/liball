using System;
using htmlgui;

namespace htmlgui.Tests;

public class ContentTemplateExpanderTests
{
    [TestCase(UiTextKind.Plain, HtmlElements.Span, "htmlgui-text")]
    [TestCase(UiTextKind.Paragraph, HtmlElements.Paragraph, "htmlgui-paragraph")]
    [TestCase(UiTextKind.Label, HtmlElements.Label, "htmlgui-label")]
    [TestCase(UiTextKind.Code, HtmlElements.Code, "htmlgui-code")]
    [TestCase(UiTextKind.Preformatted, HtmlElements.Preformatted, "htmlgui-preformatted")]
    [TestCase(UiTextKind.Quote, HtmlElements.BlockQuote, "htmlgui-quote")]
    [TestCase(UiTextKind.Badge, HtmlElements.Span, "htmlgui-badge")]
    [TestCase(UiTextKind.Chip, HtmlElements.Span, "htmlgui-chip")]
    public void ExpandsOrdinaryTextKindsToNativeElements(UiTextKind kind, string elementName, string className)
    {
        UiNode node = TextControl(kind, "Sample text");
        node.State.Version = 5;
        node.Children = new UiNode[] { LowLevelText("suffix", "suffix-id", " suffix") };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Key, Is.EqualTo("content"));
            Assert.That(result.Id, Is.EqualTo("content-id"));
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo(className));
            Assert.That(result.Version, Is.EqualTo(5));
            Assert.That(result.Children, Has.Length.EqualTo(2));
            Assert.That(result.Children[0].Kind, Is.EqualTo(DomNodeKind.Text));
            Assert.That(result.Children[0].Text, Is.EqualTo("Sample text"));
            Assert.That(result.Children[1].Text, Is.EqualTo(" suffix"));
        });
    }

    [TestCase(1, HtmlElements.Heading1)]
    [TestCase(2, HtmlElements.Heading2)]
    [TestCase(3, HtmlElements.Heading3)]
    [TestCase(4, HtmlElements.Heading4)]
    [TestCase(5, HtmlElements.Heading5)]
    [TestCase(6, HtmlElements.Heading6)]
    public void ExpandsEveryHeadingLevelToItsNativeElement(int level, string elementName)
    {
        UiNode node = TextControl(UiTextKind.Heading, "Heading");
        node.TextContent!.HasHeadingLevel = true;
        node.TextContent.HeadingLevel = level;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-heading"));
            Assert.That(result.Children[0].Text, Is.EqualTo("Heading"));
        });
    }

    [Test]
    public void HeadingDefaultsToLevelOneAndRejectsSuppliedLevelsOutsideOneThroughSix()
    {
        UiNode defaultHeading = TextControl(UiTextKind.Heading, "Default");
        UiNode tooLow = TextControl(UiTextKind.Heading, "Low");
        tooLow.TextContent!.HasHeadingLevel = true;
        tooLow.TextContent.HeadingLevel = 0;
        UiNode tooHigh = TextControl(UiTextKind.Heading, "High");
        tooHigh.TextContent!.HasHeadingLevel = true;
        tooHigh.TextContent.HeadingLevel = 7;
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(defaultHeading);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Heading1));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(tooLow))!.ParamName, Is.EqualTo("headingLevel"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(tooHigh))!.ParamName, Is.EqualTo("headingLevel"));
        });
    }

    [Test]
    public void ExpandsLinksAndImagesWithNativeUrlAndAlternativeTextAttributes()
    {
        UiNode link = TextControl(UiTextKind.Link, "Open documentation");
        link.TextContent!.Url = "/documentation";
        UiNode image = TextControl(UiTextKind.Image, "ignored image text");
        image.TextContent!.Url = "/images/chart.png";
        image.TextContent.AlternativeText = "Quarterly chart";
        UiNode decorativeImage = TextControl(UiTextKind.Image, string.Empty);
        decorativeImage.TextContent!.Url = "/images/divider.png";
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedLink = expander.ExpandNode(link);
        DomNode expandedImage = expander.ExpandNode(image);
        DomNode expandedDecorativeImage = expander.ExpandNode(decorativeImage);

        Assert.Multiple(() =>
        {
            Assert.That(expandedLink.Name, Is.EqualTo(HtmlElements.Anchor));
            Assert.That(AttributeValue(expandedLink, HtmlAttributes.HyperlinkReference), Is.EqualTo("/documentation"));
            Assert.That(expandedLink.Children[0].Text, Is.EqualTo("Open documentation"));
            Assert.That(expandedImage.Name, Is.EqualTo(HtmlElements.Image));
            Assert.That(AttributeValue(expandedImage, HtmlAttributes.Source), Is.EqualTo("/images/chart.png"));
            Assert.That(AttributeValue(expandedImage, HtmlAttributes.AlternativeText), Is.EqualTo("Quarterly chart"));
            Assert.That(expandedImage.Children, Is.Empty);
            Assert.That(AttributeValue(expandedDecorativeImage, HtmlAttributes.AlternativeText), Is.Empty);
        });
    }

    [Test]
    public void ExpandsNamedAndDecorativeIconsWithAppropriateAccessibilitySemantics()
    {
        UiNode named = TextControl(UiTextKind.Icon, "★");
        named.TextContent!.AlternativeText = "Favorite";
        UiNode decorative = TextControl(UiTextKind.Icon, "•");
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedNamed = expander.ExpandNode(named);
        DomNode expandedDecorative = expander.ExpandNode(decorative);

        Assert.Multiple(() =>
        {
            Assert.That(expandedNamed.Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(AttributeValue(expandedNamed, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Image));
            Assert.That(AttributeValue(expandedNamed, AriaAttributes.Label), Is.EqualTo("Favorite"));
            Assert.That(HasAttribute(expandedNamed, AriaAttributes.Hidden), Is.False);
            Assert.That(expandedNamed.Children[0].Text, Is.EqualTo("★"));
            Assert.That(AttributeValue(expandedDecorative, AriaAttributes.Hidden), Is.EqualTo("true"));
            Assert.That(HasAttribute(expandedDecorative, HtmlAttributes.Role), Is.False);
            Assert.That(HasAttribute(expandedDecorative, AriaAttributes.Label), Is.False);
        });
    }

    [Test]
    public void ExpandsFiguresWithOptionalImagesLogicalContentAndCaptions()
    {
        UiNode figure = TextControl(UiTextKind.Figure, "Architecture diagram");
        figure.TextContent!.Url = "/images/architecture.svg";
        figure.TextContent.AlternativeText = "Components connected by arrows";
        figure.Children = new UiNode[] { LowLevelText("description", "description-id", "Long description") };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(figure);
        DomNode second = expander.ExpandNode(figure);

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.EqualTo(HtmlElements.Figure));
            Assert.That(first.Children, Has.Length.EqualTo(3));
            Assert.That(first.Children[0].Name, Is.EqualTo(HtmlElements.Image));
            Assert.That(AttributeValue(first.Children[0], HtmlAttributes.Source), Is.EqualTo("/images/architecture.svg"));
            Assert.That(AttributeValue(first.Children[0], HtmlAttributes.AlternativeText), Is.EqualTo("Components connected by arrows"));
            Assert.That(first.Children[1].Text, Is.EqualTo("Long description"));
            Assert.That(first.Children[2].Name, Is.EqualTo(HtmlElements.FigureCaption));
            Assert.That(first.Children[2].Children[0].Text, Is.EqualTo("Architecture diagram"));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(first.Children[2].Id, Is.EqualTo(second.Children[2].Id));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(first.Children[2], Is.Not.SameAs(second.Children[2]));
        });
    }

    [Test]
    public void FiguresAndLandmarksOmitAbsentOptionalPresentation()
    {
        UiNode emptyFigure = TextControl(UiTextKind.Figure, string.Empty);
        UiNode contentFigure = TextControl(UiTextKind.Figure, string.Empty);
        contentFigure.TextContent!.Url = " ";
        contentFigure.Children = new UiNode[] { LowLevelText("figure-content", "figure-content-id", "Standalone figure content") };
        UiNode unlabeledLandmark = TextControl(UiTextKind.Landmark, "Region content");
        unlabeledLandmark.TextContent!.AlternativeText = string.Empty;
        UiNode defaultLandmark = TextControl(UiTextKind.Landmark, "Default region");
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedEmpty = expander.ExpandNode(emptyFigure);
        DomNode expandedContent = expander.ExpandNode(contentFigure);
        DomNode expandedLandmark = expander.ExpandNode(unlabeledLandmark);
        DomNode expandedDefaultLandmark = expander.ExpandNode(defaultLandmark);

        Assert.Multiple(() =>
        {
            Assert.That(expandedEmpty.Name, Is.EqualTo(HtmlElements.Figure));
            Assert.That(expandedEmpty.Children, Is.Empty);
            Assert.That(expandedContent.Children, Has.Length.EqualTo(1));
            Assert.That(expandedContent.Children[0].Text, Is.EqualTo("Standalone figure content"));
            Assert.That(HasAttribute(expandedContent.Children[0], HtmlAttributes.Source), Is.False);
            Assert.That(AttributeValue(expandedLandmark, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Region));
            Assert.That(HasAttribute(expandedLandmark, AriaAttributes.Label), Is.False);
            Assert.That(HasAttribute(expandedDefaultLandmark, AriaAttributes.Label), Is.False);
        });
    }

    [Test]
    public void ExpandsStatusLandmarkAndLocalizedTextSemantics()
    {
        UiNode status = TextControl(UiTextKind.Status, "Saved");
        UiNode landmark = TextControl(UiTextKind.Landmark, "Related content");
        landmark.TextContent!.AlternativeText = "Related information";
        UiNode localized = TextControl(UiTextKind.Localized, "Welcome");
        localized.TextContent!.LocalizationKey = "home.welcome";
        UiNode fallbackKey = TextControl(UiTextKind.Localized, "home.goodbye");
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedStatus = expander.ExpandNode(status);
        DomNode expandedLandmark = expander.ExpandNode(landmark);
        DomNode expandedLocalized = expander.ExpandNode(localized);
        DomNode expandedFallbackKey = expander.ExpandNode(fallbackKey);

        Assert.Multiple(() =>
        {
            Assert.That(expandedStatus.Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(AttributeValue(expandedStatus, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Status));
            Assert.That(expandedStatus.Children[0].Text, Is.EqualTo("Saved"));
            Assert.That(expandedLandmark.Name, Is.EqualTo(HtmlElements.Section));
            Assert.That(HasAttribute(expandedLandmark, HtmlAttributes.Role), Is.False);
            Assert.That(AttributeValue(expandedLandmark, AriaAttributes.Label), Is.EqualTo("Related information"));
            Assert.That(expandedLandmark.Children[0].Text, Is.EqualTo("Related content"));
            Assert.That(AttributeValue(expandedLocalized, "data-localization-key"), Is.EqualTo("home.welcome"));
            Assert.That(expandedLocalized.Children[0].Text, Is.EqualTo("Welcome"));
            Assert.That(AttributeValue(expandedFallbackKey, "data-localization-key"), Is.EqualTo("home.goodbye"));
            Assert.That(expandedFallbackKey.Children[0].Text, Is.EqualTo("home.goodbye"));
        });
    }

    [Test]
    public void RejectsMissingPayloadUrlsLocalizationKeysAndUndefinedKinds()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "text",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode missingLinkUrl = TextControl(UiTextKind.Link, "Link");
        UiNode missingImageUrl = TextControl(UiTextKind.Image, "Image");
        missingImageUrl.TextContent!.Url = " ";
        UiNode missingLocalizationKey = TextControl(UiTextKind.Localized, string.Empty);
        UiNode undefined = TextControl((UiTextKind)999, "Undefined");
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingLinkUrl));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingImageUrl));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingLocalizationKey));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(undefined))!.ParamName, Is.EqualTo("kind"));
        });
    }

    [Test]
    public void RepeatedExpansionIsFreshStableAndDoesNotMutateTextModels()
    {
        UiNode node = TextControl(UiTextKind.Paragraph, "Original");
        UiText sourceContent = node.TextContent!;
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        sourceContent.Text = "Updated";
        DomNode second = expander.ExpandNode(node);
        first.Children[0].Text = "Changed output";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(sourceContent.Text, Is.EqualTo("Updated"));
            Assert.That(second.Children[0].Text, Is.EqualTo("Updated"));
        });
    }

    [Test]
    public void LocalizedContentComposesUnderPageLocaleAndDirectionOverrides()
    {
        UiNode localized = TextControl(UiTextKind.Localized, "مرحبا");
        localized.TextContent!.LocalizationKey = "home.greeting";
        UiWindow window = new UiWindow
        {
            Key = "window",
            Id = "window-id",
            Document = new UiDocumentMetadata { Language = "en", TextDirection = "ltr" }
        };
        UiPage page = new UiPage
        {
            Key = "home",
            Id = "home-id",
            Language = "ar",
            TextDirection = "rtl",
            ContentView = new UiView { Key = "content", Id = "content-id", ContentRoot = localized }
        };

        DomNode document = new DocumentComposer().ComposePageDocument(window, page);
        DomNode html = document.Children[1];
        DomNode content = html.Children[1].Children[0].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(html, HtmlAttributes.Language), Is.EqualTo("ar"));
            Assert.That(AttributeValue(html, HtmlAttributes.Direction), Is.EqualTo("rtl"));
            Assert.That(AttributeValue(content, "data-localization-key"), Is.EqualTo("home.greeting"));
            Assert.That(content.Children[0].Text, Is.EqualTo("مرحبا"));
        });
    }

    private static UiNode TextControl(UiTextKind kind, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "content", SiblingIdentity = "content", StableId = "content-id" },
            TextContent = new UiText { Kind = kind, Text = text }
        };
    }

    private static UiNode LowLevelText(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }

    private static string AttributeValue(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
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
