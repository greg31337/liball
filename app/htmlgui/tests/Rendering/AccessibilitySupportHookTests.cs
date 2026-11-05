using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class AccessibilitySupportHookTests
{
    [Test]
    public void ProjectsEveryAccessibilitySupportHookAndTextualStatus()
    {
        UiNode node = ButtonNode();
        node.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[]
            {
                UiAccessibilityFeature.NonColorStatus,
                UiAccessibilityFeature.ScreenReaderOnly,
                UiAccessibilityFeature.ReducedTransparency,
                UiAccessibilityFeature.ReducedMotion,
                UiAccessibilityFeature.TargetSize,
                UiAccessibilityFeature.FocusVisible,
                UiAccessibilityFeature.Reflow,
                UiAccessibilityFeature.Zoom,
                UiAccessibilityFeature.ForcedColors,
                UiAccessibilityFeature.Busy
            },
            MinimumTargetSizePixels = 44,
            StatusText = "Error"
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo(
                "htmlgui-button htmlgui-button-push " +
                AccessibilityHooks.ForcedColorsClass + " " +
                AccessibilityHooks.ZoomClass + " " +
                AccessibilityHooks.ReflowClass + " " +
                AccessibilityHooks.FocusVisibleClass + " " +
                AccessibilityHooks.TargetSizeClass + " " +
                AccessibilityHooks.ReducedMotionClass + " " +
                AccessibilityHooks.ReducedTransparencyClass + " " +
                AccessibilityHooks.ScreenReaderOnlyClass + " " +
                AccessibilityHooks.NonColorStatusClass));
            Assert.That(AttributeValue(result, AccessibilityHooks.TargetSizeAttribute), Is.EqualTo("44"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(result.Children, Has.Length.EqualTo(2));
            Assert.That(AttributeValue(result.Children[1], HtmlAttributes.Class), Is.EqualTo(AccessibilityHooks.StatusTextClass));
            Assert.That(result.Children[1].Children[0].Text, Is.EqualTo("Error"));
        });
    }

    [Test]
    public void CanonicalizesReorderedAndDuplicateFeatureHooks()
    {
        UiNode first = ButtonNode();
        first.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[]
            {
                UiAccessibilityFeature.Reflow,
                UiAccessibilityFeature.ForcedColors,
                UiAccessibilityFeature.Reflow,
                UiAccessibilityFeature.FocusVisible
            }
        };
        UiNode second = ButtonNode();
        second.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[]
            {
                UiAccessibilityFeature.FocusVisible,
                UiAccessibilityFeature.Reflow,
                UiAccessibilityFeature.ForcedColors
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode firstResult = expander.ExpandNode(first);
        DomNode secondResult = expander.ExpandNode(second);

        Assert.That(AttributeValue(firstResult, HtmlAttributes.Class), Is.EqualTo(AttributeValue(secondResult, HtmlAttributes.Class)));
    }

    [Test]
    public void ModelValidationReportsUnsafeTargetSizeAndMissingNonColorStatusText()
    {
        UiApplication application = ApplicationWithButton();
        UiNode button = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        button.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[]
            {
                UiAccessibilityFeature.TargetSize,
                UiAccessibilityFeature.NonColorStatus
            },
            MinimumTargetSizePixels = 23,
            StatusText = " "
        };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("accessibility.target-size"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("accessibility.non-color-status"));
            Assert.That(result.Diagnostics.Single(item => item.Code == "accessibility.target-size").Location, Does.EndWith("/accessibility/minimumTargetSizePixels"));
            Assert.That(result.Diagnostics.Single(item => item.Code == "accessibility.non-color-status").Location, Does.EndWith("/accessibility/statusText"));
        });
    }

    [Test]
    public void RenderingRejectsInvalidFeaturePayloads()
    {
        UiNode target = ButtonNode();
        target.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[] { UiAccessibilityFeature.TargetSize },
            MinimumTargetSizePixels = 10
        };
        UiNode status = ButtonNode();
        status.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[] { UiAccessibilityFeature.NonColorStatus }
        };
        UiNode unknown = ButtonNode();
        unknown.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[] { (UiAccessibilityFeature)99 }
        };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(target));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(status));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknown));
        });
    }

    private static UiNode ButtonNode()
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "button",
            Button = new UiButton { Label = "Save" },
            Identity = new UiNodeIdentity
            {
                AuthorKey = "save",
                SiblingIdentity = "save",
                StableId = "save-id",
                ParentStableId = "view-id"
            }
        };
    }

    private static UiApplication ApplicationWithButton()
    {
        UiNode button = ButtonNode();
        UiView view = new UiView { Id = "view-id", Key = "view", ContentRoot = button };
        UiPage page = new UiPage { Id = "page-id", Key = "page", ContentView = view };
        UiWindow window = new UiWindow { Id = "window-id", Key = "window", Pages = new UiPage[] { page } };
        return new UiApplication { Id = "application-id", Key = "application", RootSeed = "seed", Windows = new UiWindow[] { window } };
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        return node.Attributes.FirstOrDefault(attribute => attribute.Name == name)?.Value;
    }
}
