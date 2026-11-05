using System;
using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

public class RenderTreeAnnotatorTests
{
    [Test]
    public void GeneratesPreorderSequencesKeysAndStableIdentifiersWithoutMutatingInput()
    {
        DomNode source = new DomNode
        {
            Kind = DomNodeKind.Element,
            Name = "main",
            Children = new DomNode[]
            {
                new DomNode { Kind = DomNodeKind.Text, Text = "Hello" },
                new DomNode
                {
                    Kind = DomNodeKind.Element,
                    Key = "details",
                    Name = "section",
                    Children = new DomNode[] { new DomNode { Kind = DomNodeKind.Comment, Text = "note" } }
                }
            }
        };

        RenderTreeAnnotationResult result = new RenderTreeAnnotator().AnnotateTree(new DomNode[] { source }, "app-seed", 20);
        DomNode root = result.Roots[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(root.Sequence, Is.EqualTo(0));
            Assert.That(root.Children[0].Sequence, Is.EqualTo(1));
            Assert.That(root.Children[1].Sequence, Is.EqualTo(2));
            Assert.That(root.Children[1].Children[0].Sequence, Is.EqualTo(3));
            Assert.That(root.Key, Is.EqualTo("$element-0"));
            Assert.That(root.Children[0].Key, Is.EqualTo("$text-0"));
            Assert.That(root.Children[1].Key, Is.EqualTo("details"));
            Assert.That(root.Id, Does.StartWith("ui-element-"));
            Assert.That(root.Children[0].Id, Does.StartWith("ui-text-"));
            Assert.That(root.Children[1].Id, Does.StartWith("ui-element-"));
            Assert.That(source.Sequence, Is.EqualTo(0));
            Assert.That(source.Children[0].Sequence, Is.EqualTo(0));
            Assert.That(source.Key, Is.Empty);
            Assert.That(source.Id, Is.Empty);
            Assert.That(source.SubtreeHash, Is.Null);
        });
    }

    [Test]
    public void KeepsKeyedSubtreeHashesStableWhenOnlyTheirPreorderPositionChanges()
    {
        DomNode firstRoot = Element("root", "root-id");
        firstRoot.Children = new DomNode[] { Text("stable", "stable-id", "Content") };
        DomNode secondRoot = Element("root", "root-id");
        secondRoot.Children = new DomNode[]
        {
            Text("inserted", "inserted-id", "Earlier"),
            Text("stable", "stable-id", "Content")
        };
        RenderTreeAnnotator annotator = new RenderTreeAnnotator();

        RenderTreeAnnotationResult first = annotator.AnnotateTree(new DomNode[] { firstRoot }, "seed", 20);
        RenderTreeAnnotationResult second = annotator.AnnotateTree(new DomNode[] { secondRoot }, "seed", 20);

        Assert.Multiple(() =>
        {
            Assert.That(first.Roots[0].Children[0].Sequence, Is.EqualTo(1));
            Assert.That(second.Roots[0].Children[1].Sequence, Is.EqualTo(2));
            Assert.That(first.Roots[0].Children[0].SubtreeHash, Is.EqualTo(second.Roots[0].Children[1].SubtreeHash));
            Assert.That(first.ContentHash, Is.Not.EqualTo(second.ContentHash));
        });
    }

    [Test]
    public void ReportsDuplicateKeysOnlyWithinTheSameSiblingCollection()
    {
        DomNode root = Element("root", "root-id");
        root.Children = new DomNode[]
        {
            Element("same", "first-id"),
            Element("same", "second-id"),
            new DomNode
            {
                Kind = DomNodeKind.Fragment,
                Key = "branch",
                Id = "branch-id",
                Children = new DomNode[] { Element("same", "nested-id") }
            }
        };

        RenderTreeAnnotationResult result = new RenderTreeAnnotator().AnnotateTree(new DomNode[] { root }, "seed", 20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(UiDiagnosticSeverity.Error));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("render.duplicate-key"));
            Assert.That(result.Diagnostics[0].Location, Is.EqualTo("/roots/0/children/1/key"));
        });
    }

    [Test]
    public void AnnotatesEverySubtreeAndIncludesDescendantContentAndVersionsInHashes()
    {
        DomNode first = Element("root", "root-id");
        first.Version = 4;
        first.Children = new DomNode[] { new DomNode { Kind = DomNodeKind.Text, Key = "label", Id = "label-id", Text = "First", Version = 7 } };
        DomNode second = Element("root", "root-id");
        second.Version = 4;
        second.Children = new DomNode[] { new DomNode { Kind = DomNodeKind.Text, Key = "label", Id = "label-id", Text = "Second", Version = 7 } };
        DomNode third = Element("root", "root-id");
        third.Version = 4;
        third.Children = new DomNode[] { new DomNode { Kind = DomNodeKind.Text, Key = "label", Id = "label-id", Text = "First", Version = 8 } };
        RenderTreeAnnotator annotator = new RenderTreeAnnotator();

        RenderTreeAnnotationResult firstResult = annotator.AnnotateTree(new DomNode[] { first }, "seed", 20);
        RenderTreeAnnotationResult secondResult = annotator.AnnotateTree(new DomNode[] { second }, "seed", 20);
        RenderTreeAnnotationResult thirdResult = annotator.AnnotateTree(new DomNode[] { third }, "seed", 20);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Roots[0].Version, Is.EqualTo(4));
            Assert.That(firstResult.Roots[0].Children[0].Version, Is.EqualTo(7));
            Assert.That(firstResult.Roots[0].SubtreeHash, Has.Length.EqualTo(64));
            Assert.That(firstResult.Roots[0].Children[0].SubtreeHash, Has.Length.EqualTo(64));
            Assert.That(firstResult.ContentHash, Has.Length.EqualTo(64));
            Assert.That(firstResult.ContentHash, Is.Not.EqualTo(secondResult.ContentHash));
            Assert.That(firstResult.ContentHash, Is.Not.EqualTo(thirdResult.ContentHash));
        });
    }

    [Test]
    public void HashesMapLikeInputsIndependentlyOfOrderAndCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            RenderTreeAnnotator annotator = new RenderTreeAnnotator();
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            RenderTreeAnnotationResult first = annotator.AnnotateTree(new DomNode[] { OrderedNode(false) }, "seed", 20);
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            RenderTreeAnnotationResult second = annotator.AnnotateTree(new DomNode[] { OrderedNode(true) }, "seed", 20);
            RenderTreeAnnotationResult repeated = annotator.AnnotateTree(new DomNode[] { OrderedNode(false) }, "seed", 20);

            Assert.Multiple(() =>
            {
                Assert.That(first.ContentHash, Is.EqualTo(second.ContentHash));
                Assert.That(first.ContentHash, Is.EqualTo(repeated.ContentHash));
                Assert.That(first.Roots[0].SubtreeHash, Is.EqualTo(second.Roots[0].SubtreeHash));
                Assert.That(first.Roots[0].Id, Is.EqualTo(second.Roots[0].Id));
                Assert.That(Array.ConvertAll(first.Roots[0].Attributes, attribute => attribute.Name), Is.EqualTo(new string[] { "data-alpha", "data-zeta" }));
                Assert.That(Array.ConvertAll(second.Roots[0].Attributes, attribute => attribute.Name), Is.EqualTo(new string[] { "data-alpha", "data-zeta" }));
                Assert.That(Array.ConvertAll(second.Roots[0].Properties[0].Value.ObjectValue!, entry => entry.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void PreservesBehaviorallySignificantEventSubscriptionOrderInTheHash()
    {
        DomNode first = OrderedNode(false);
        DomNode second = OrderedNode(false);
        Array.Reverse(second.EventSubscriptions);
        RenderTreeAnnotator annotator = new RenderTreeAnnotator();

        RenderTreeAnnotationResult firstResult = annotator.AnnotateTree(new DomNode[] { first }, "seed", 20);
        RenderTreeAnnotationResult secondResult = annotator.AnnotateTree(new DomNode[] { second }, "seed", 20);

        Assert.That(firstResult.ContentHash, Is.Not.EqualTo(secondResult.ContentHash));
    }

    private static DomNode OrderedNode(bool reverse)
    {
        DomAttribute alpha = new DomAttribute { Name = "data-alpha", Value = "one" };
        DomAttribute zeta = new DomAttribute { Name = "data-zeta", Value = "two" };
        DomProperty settings = new DomProperty
        {
            Name = "settings",
            Value = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = reverse
                    ? new UiValueEntry[] { Entry("zeta", 2), Entry("alpha", 1) }
                    : new UiValueEntry[] { Entry("alpha", 1), Entry("zeta", 2) }
            }
        };
        DomEventSubscription click = new DomEventSubscription { Id = "click", HandlerId = "handler-a", EventName = "click" };
        DomEventSubscription input = new DomEventSubscription { Id = "input", HandlerId = "handler-b", EventName = "input" };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Name = "div",
            Attributes = reverse ? new DomAttribute[] { zeta, alpha } : new DomAttribute[] { alpha, zeta },
            Properties = new DomProperty[] { settings },
            EventSubscriptions = new DomEventSubscription[] { click, input }
        };
    }

    private static UiValueEntry Entry(string key, long value)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = value }
        };
    }

    private static DomNode Element(string key, string id)
    {
        return new DomNode { Kind = DomNodeKind.Element, Key = key, Id = id, Name = "div" };
    }

    private static DomNode Text(string key, string id, string value)
    {
        return new DomNode { Kind = DomNodeKind.Text, Key = key, Id = id, Text = value };
    }
}
