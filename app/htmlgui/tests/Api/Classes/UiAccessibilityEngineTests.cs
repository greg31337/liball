using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiAccessibilityEngineTests
{
    [Test]
    public void ExpandsAccessibilityMetadataThroughThePublicContract()
    {
        UiNode node = ButtonNode();
        node.Accessibility = new UiAccessibility
        {
            Label = "Store changes",
            Features = new UiAccessibilityFeature[]
            {
                UiAccessibilityFeature.Busy,
                UiAccessibilityFeature.TargetSize,
                UiAccessibilityFeature.NonColorStatus
            },
            MinimumTargetSizePixels = 36,
            StatusText = "Required"
        };
        IUiAccessibilityEngine engine = new UiAccessibilityEngine();

        DomNode result = engine.ExpandAccessibility(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Store changes"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AccessibilityHooks.TargetSizeAttribute), Is.EqualTo("36"));
            Assert.That(result.Children.Last().Children[0].Text, Is.EqualTo("Required"));
        });
    }

    [Test]
    public void CreatesValidAndInvalidLocationAwareReports()
    {
        UiApplication validApplication = ApplicationWithRoot(ButtonNode());
        UiNode invalidRoot = ElementNode("root", "root-id", "view-id", "div");
        invalidRoot.Accessibility = new UiAccessibility { Role = AriaRoles.Button };
        invalidRoot.State.IsFocusable = true;
        invalidRoot.EventBindings = new UiEventBinding[]
        {
            new UiEventBinding { Id = "key", HandlerId = "key-handler", Kind = UiEventKind.KeyDown }
        };
        UiApplication invalidApplication = ApplicationWithRoot(invalidRoot);
        IUiAccessibilityEngine engine = new UiAccessibilityEngine();

        UiAccessibilityReport valid = engine.CreateAccessibilityReport(validApplication);
        UiAccessibilityReport invalid = engine.CreateAccessibilityReport(invalidApplication);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Diagnostics, Is.Empty);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.Diagnostics.Select(item => item.Code), Does.Contain("accessibility.missing-name"));
            Assert.That(invalid.Diagnostics.Single(item => item.Code == "accessibility.missing-name").Location, Is.EqualTo("/application/windows/0/pages/0/contentView/contentRoot/accessibility/role"));
        });
    }

    [Test]
    public void ReportsNullApplicationsAndReturnsIndependentSnapshots()
    {
        UiNode root = ElementNode("root", "root-id", "view-id", "div");
        root.Accessibility = new UiAccessibility { Role = "unsupported" };
        UiApplication application = ApplicationWithRoot(root);
        IUiAccessibilityEngine engine = new UiAccessibilityEngine();

        UiAccessibilityReport first = engine.CreateAccessibilityReport(application);
        first.Diagnostics[0].Code = "changed";
        UiAccessibilityReport second = engine.CreateAccessibilityReport(application);
        UiAccessibilityReport missing = engine.CreateAccessibilityReport(null);

        Assert.Multiple(() =>
        {
            Assert.That(second.Diagnostics[0].Code, Is.EqualTo("accessibility.invalid-role"));
            Assert.That(root.Accessibility.Report, Is.Null);
            Assert.That(missing.IsValid, Is.False);
            Assert.That(missing.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo("model.null-root"));
            Assert.That(missing.Diagnostics[0].Location, Is.EqualTo("/"));
        });
    }

    [Test]
    public void ExpansionRejectsNullAndInvalidAccessibilityInputs()
    {
        UiNode invalid = ButtonNode();
        invalid.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[] { UiAccessibilityFeature.TargetSize },
            MinimumTargetSizePixels = 8
        };
        IUiAccessibilityEngine engine = new UiAccessibilityEngine();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => engine.ExpandAccessibility(null!))!.ParamName, Is.EqualTo("node"));
            Assert.Throws<UiRenderException>(() => engine.ExpandAccessibility(invalid));
        });
    }

    private static UiNode ButtonNode()
    {
        UiNode node = ElementNode("save", "save-id", "view-id", "button");
        node.Kind = UiNodeKind.Control;
        node.Button = new UiButton { Label = "Save" };
        return node;
    }

    private static UiNode ElementNode(string key, string id, string parentId, string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Element,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = key,
                SiblingIdentity = key,
                StableId = id,
                ParentStableId = parentId
            }
        };
    }

    private static UiApplication ApplicationWithRoot(UiNode root)
    {
        root.Identity.ParentStableId = "view-id";
        UiView view = new UiView { Id = "view-id", Key = "view", ContentRoot = root };
        UiPage page = new UiPage { Id = "page-id", Key = "page", ContentView = view };
        UiWindow window = new UiWindow { Id = "window-id", Key = "window", Pages = new UiPage[] { page } };
        return new UiApplication { Id = "application-id", Key = "application", RootSeed = "seed", Windows = new UiWindow[] { window } };
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        return node.Attributes.FirstOrDefault(attribute => attribute.Name == name)?.Value;
    }
}
