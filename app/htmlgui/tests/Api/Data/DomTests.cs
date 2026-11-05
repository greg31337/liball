using System;
using htmlgui;

namespace htmlgui.Tests;

public class DomTests
{
    [Test]
    public void DomModelsHaveSafeDefaults()
    {
        DomNode node = new DomNode();
        DomAttribute attribute = new DomAttribute();
        DomProperty property = new DomProperty();
        DomEventSubscription subscription = new DomEventSubscription();
        DomSnapshot snapshot = new DomSnapshot();
        DomMutation mutation = new DomMutation();

        Assert.Multiple(() =>
        {
            Assert.That(node.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(node.Key, Is.Empty);
            Assert.That(node.Id, Is.Empty);
            Assert.That(node.NamespaceUri, Is.Null);
            Assert.That(node.Name, Is.Empty);
            Assert.That(node.Text, Is.Null);
            Assert.That(node.Attributes, Is.Empty);
            Assert.That(node.Properties, Is.Empty);
            Assert.That(node.EventSubscriptions, Is.Empty);
            Assert.That(node.Children, Is.Empty);
            Assert.That(node.StyleScope, Is.Null);
            Assert.That(node.HydrationMarker, Is.Null);
            Assert.That(node.Version, Is.Zero);
            Assert.That(node.SubtreeHash, Is.Null);

            Assert.That(attribute.NamespaceUri, Is.Null);
            Assert.That(attribute.Prefix, Is.Null);
            Assert.That(attribute.Name, Is.Empty);
            Assert.That(attribute.Value, Is.Empty);
            Assert.That(attribute.IsBoolean, Is.False);

            Assert.That(property.Name, Is.Empty);
            Assert.That(property.Value, Is.Not.Null);
            Assert.That(property.Value.Kind, Is.EqualTo(UiValueKind.Null));

            Assert.That(subscription.Id, Is.Empty);
            Assert.That(subscription.HandlerId, Is.Empty);
            Assert.That(subscription.Kind, Is.EqualTo(UiEventKind.Click));
            Assert.That(subscription.EventName, Is.EqualTo(DomEventNames.Click));
            Assert.That(subscription.Phase, Is.EqualTo(UiEventPhase.Bubble));
            Assert.That(subscription.IsDelegated, Is.True);
            Assert.That(subscription.PreventDefault, Is.False);
            Assert.That(subscription.StopPropagation, Is.False);
            Assert.That(subscription.Once, Is.False);
            Assert.That(subscription.Passive, Is.False);
            Assert.That(subscription.HasDebounceMilliseconds, Is.False);
            Assert.That(subscription.DebounceMilliseconds, Is.Zero);
            Assert.That(subscription.HasThrottleMilliseconds, Is.False);
            Assert.That(subscription.ThrottleMilliseconds, Is.Zero);

            Assert.That(snapshot.SessionId, Is.Empty);
            Assert.That(snapshot.RootSeed, Is.Empty);
            Assert.That(snapshot.Revision, Is.Zero);
            Assert.That(snapshot.Roots, Is.Empty);
            Assert.That(snapshot.ContentHash, Is.Empty);

            Assert.That(mutation.Kind, Is.EqualTo(UiMutationKind.Insert));
            Assert.That(mutation.Sequence, Is.Zero);
            Assert.That(mutation.TargetId, Is.Empty);
            Assert.That(mutation.ParentId, Is.Null);
            Assert.That(mutation.ReferenceId, Is.Null);
            Assert.That(mutation.Node, Is.Null);
            Assert.That(mutation.Attribute, Is.Null);
            Assert.That(mutation.Property, Is.Null);
            Assert.That(mutation.EventSubscription, Is.Null);
            Assert.That(mutation.Name, Is.Null);
            Assert.That(mutation.Value, Is.Null);
            Assert.That(mutation.HasIndex, Is.False);
            Assert.That(mutation.Index, Is.Zero);
            Assert.That(mutation.HasFromIndex, Is.False);
            Assert.That(mutation.FromIndex, Is.Zero);
            Assert.That(mutation.HasToIndex, Is.False);
            Assert.That(mutation.ToIndex, Is.Zero);
        });
    }

    [Test]
    public void SnapshotCarriesNamespacedTreePropertiesAndEvents()
    {
        DomAttribute viewBox = new DomAttribute
        {
            Name = "viewBox",
            Value = "0 0 16 16"
        };
        DomAttribute hyperlink = new DomAttribute
        {
            NamespaceUri = "http://www.w3.org/1999/xlink",
            Prefix = "xlink",
            Name = "href",
            Value = "#shape"
        };
        DomAttribute hidden = new DomAttribute
        {
            Name = HtmlAttributes.Hidden,
            IsBoolean = true
        };
        DomProperty property = new DomProperty
        {
            Name = "checked",
            Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }
        };
        DomEventSubscription subscription = new DomEventSubscription
        {
            Id = "subscription-key",
            HandlerId = "handler-key",
            Kind = UiEventKind.KeyDown,
            EventName = DomEventNames.KeyDown,
            Phase = UiEventPhase.Capture,
            IsDelegated = false,
            PreventDefault = true,
            StopPropagation = true,
            Once = true,
            Passive = false,
            HasDebounceMilliseconds = true,
            DebounceMilliseconds = 0,
            HasThrottleMilliseconds = true,
            ThrottleMilliseconds = 25
        };
        DomNode text = new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = "text",
            Text = "Accessible icon"
        };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "icon",
            Id = "ui-icon",
            NamespaceUri = "http://www.w3.org/2000/svg",
            Name = "svg",
            Attributes = new DomAttribute[] { viewBox, hyperlink, hidden },
            Properties = new DomProperty[] { property },
            EventSubscriptions = new DomEventSubscription[] { subscription },
            Children = new DomNode[] { text },
            StyleScope = "scope-a1",
            HydrationMarker = "h:icon",
            Version = 4,
            SubtreeHash = "abc123"
        };
        DomSnapshot snapshot = new DomSnapshot
        {
            SessionId = "session-1",
            RootSeed = "seed-1",
            Revision = 9,
            Roots = new DomNode[] { root },
            ContentHash = "snapshot-hash"
        };

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.SessionId, Is.EqualTo("session-1"));
            Assert.That(snapshot.RootSeed, Is.EqualTo("seed-1"));
            Assert.That(snapshot.Revision, Is.EqualTo(9));
            Assert.That(snapshot.ContentHash, Is.EqualTo("snapshot-hash"));
            Assert.That(snapshot.Roots[0], Is.SameAs(root));
            Assert.That(root.NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(root.Attributes[0], Is.SameAs(viewBox));
            Assert.That(root.Attributes[1].Prefix, Is.EqualTo("xlink"));
            Assert.That(root.Attributes[2].IsBoolean, Is.True);
            Assert.That(root.Properties[0].Value.BooleanValue, Is.True);
            Assert.That(root.EventSubscriptions[0], Is.SameAs(subscription));
            Assert.That(subscription.Kind, Is.EqualTo(UiEventKind.KeyDown));
            Assert.That(subscription.Phase, Is.EqualTo(UiEventPhase.Capture));
            Assert.That(subscription.IsDelegated, Is.False);
            Assert.That(subscription.PreventDefault, Is.True);
            Assert.That(subscription.StopPropagation, Is.True);
            Assert.That(subscription.Once, Is.True);
            Assert.That(subscription.HasDebounceMilliseconds, Is.True);
            Assert.That(subscription.DebounceMilliseconds, Is.Zero);
            Assert.That(subscription.HasThrottleMilliseconds, Is.True);
            Assert.That(subscription.ThrottleMilliseconds, Is.EqualTo(25));
            Assert.That(root.Children[0].Text, Is.EqualTo("Accessible icon"));
            Assert.That(root.StyleScope, Is.EqualTo("scope-a1"));
            Assert.That(root.HydrationMarker, Is.EqualTo("h:icon"));
            Assert.That(root.Version, Is.EqualTo(4));
            Assert.That(root.SubtreeHash, Is.EqualTo("abc123"));
        });
    }

    [Test]
    public void MutationsCarryTypedPayloadsAndExplicitZeroIndexes()
    {
        DomNode insertedNode = new DomNode { Key = "new", Id = "ui-new", Name = "div" };
        DomAttribute attribute = new DomAttribute { Name = "data-state", Value = "ready" };
        DomProperty property = new DomProperty { Name = "value", Value = new UiValue { Kind = UiValueKind.String, StringValue = "typed" } };
        DomEventSubscription subscription = new DomEventSubscription { Id = "event-1", HandlerId = "handler-1" };
        DomMutation insert = new DomMutation
        {
            Kind = UiMutationKind.Insert,
            Sequence = 0,
            TargetId = "ui-new",
            ParentId = "ui-parent",
            ReferenceId = "ui-next",
            Node = insertedNode,
            HasIndex = true,
            Index = 0
        };
        DomMutation move = new DomMutation
        {
            Kind = UiMutationKind.Move,
            Sequence = 1,
            TargetId = "ui-existing",
            ParentId = "ui-parent",
            HasFromIndex = true,
            FromIndex = 0,
            HasToIndex = true,
            ToIndex = 2
        };
        DomMutation update = new DomMutation
        {
            Kind = UiMutationKind.SetAttribute,
            Sequence = 2,
            TargetId = "ui-existing",
            Attribute = attribute,
            Property = property,
            EventSubscription = subscription,
            Name = "data-state",
            Value = "ready"
        };

        Assert.Multiple(() =>
        {
            Assert.That(insert.Node, Is.SameAs(insertedNode));
            Assert.That(insert.ParentId, Is.EqualTo("ui-parent"));
            Assert.That(insert.ReferenceId, Is.EqualTo("ui-next"));
            Assert.That(insert.HasIndex, Is.True);
            Assert.That(insert.Index, Is.Zero);
            Assert.That(move.Sequence, Is.EqualTo(1));
            Assert.That(move.HasFromIndex, Is.True);
            Assert.That(move.FromIndex, Is.Zero);
            Assert.That(move.HasToIndex, Is.True);
            Assert.That(move.ToIndex, Is.EqualTo(2));
            Assert.That(update.Attribute, Is.SameAs(attribute));
            Assert.That(update.Property, Is.SameAs(property));
            Assert.That(update.EventSubscription, Is.SameAs(subscription));
            Assert.That(update.Name, Is.EqualTo("data-state"));
            Assert.That(update.Value, Is.EqualTo("ready"));
        });
    }

    [Test]
    public void MutableDefaultPayloadsAreIndependent()
    {
        DomProperty firstProperty = new DomProperty();
        DomProperty secondProperty = new DomProperty();
        DomNode firstNode = new DomNode();
        DomNode secondNode = new DomNode();

        firstProperty.Value.Kind = UiValueKind.String;
        firstProperty.Value.StringValue = "first";
        firstNode.Key = "first";
        firstNode.Attributes = new DomAttribute[] { new DomAttribute { Name = "title", Value = "first" } };

        Assert.Multiple(() =>
        {
            Assert.That(secondProperty.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(firstProperty.Value, Is.Not.SameAs(secondProperty.Value));
            Assert.That(secondNode.Key, Is.Empty);
            Assert.That(secondNode.Attributes, Is.Empty);
            Assert.That(firstNode.Attributes, Is.Not.SameAs(secondNode.Attributes));
        });
    }

    [Test]
    public void SnapshotAndMutationsCanFlowThroughDataSourceContract()
    {
        DomSnapshot snapshot = new DomSnapshot { Revision = 3 };
        DomMutation[] mutations = new DomMutation[] { new DomMutation { Kind = UiMutationKind.SetText, TargetId = "ui-text", Value = "updated" } };
        IDomDataSource source = new TestDomDataSource(snapshot, mutations);

        Assert.That(source.GetDomSnapshot(), Is.SameAs(snapshot));
        Assert.That(source.GetDomMutations(), Is.SameAs(mutations));
    }

    private sealed class TestDomDataSource : IDomDataSource
    {
        private readonly DomSnapshot snapshot;
        private readonly DomMutation[] mutations;

        public TestDomDataSource(DomSnapshot snapshot, DomMutation[] mutations)
        {
            this.snapshot = snapshot;
            this.mutations = mutations;
        }

        public DomSnapshot GetDomSnapshot()
        {
            return snapshot;
        }

        public DomMutation[] GetDomMutations()
        {
            return mutations;
        }
    }
}
