using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class ComponentSchedulerCoreTests
{
    [Test]
    public void CommitsBatchedInvalidationsInParentFirstOrderAndRollsBackAtomically()
    {
        ComponentCore components = RegisteredComponents();
        ComponentOperationResult root = CreateComponent(components, null, "root");
        ComponentOperationResult child = CreateComponent(components, root.Context!.ComponentId, "child");
        ComponentOperationResult grandchild = CreateComponent(components, child.Context!.ComponentId, "grandchild");
        ComponentSchedulerCore scheduler = new ComponentSchedulerCore(components);

        ComponentScheduleResult begun = scheduler.BeginComponentInvalidationBatch("layout");
        scheduler.InvalidateComponent(grandchild.Context!.ComponentId, UiUpdatePriority.Normal, false);
        scheduler.InvalidateComponent(child.Context.ComponentId, UiUpdatePriority.Normal, false);
        scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Normal, false);
        ComponentScheduleResult committed = scheduler.CommitComponentInvalidationBatch();
        ComponentScheduleResult cycle = scheduler.BeginComponentUpdateCycle();
        ComponentScheduleResult ended = scheduler.EndComponentUpdateCycle();
        CompleteRequests(scheduler, cycle.Requests);

        scheduler.BeginComponentInvalidationBatch("discarded");
        scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Immediate, false);
        ComponentScheduleResult rolledBack = scheduler.RollbackComponentInvalidationBatch();
        ComponentScheduleResult afterRollback = scheduler.BeginComponentUpdateCycle();

        Assert.Multiple(() =>
        {
            Assert.That(begun.Success, Is.True);
            Assert.That(begun.BatchId, Is.EqualTo("layout"));
            Assert.That(committed.Applied, Is.True);
            Assert.That(committed.BatchId, Is.EqualTo("layout"));
            Assert.That(cycle.Depth, Is.EqualTo(1));
            Assert.That(cycle.Requests.Select(item => item.Context.ComponentId), Is.EqualTo(new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context.ComponentId }));
            Assert.That(cycle.Requests.Select(item => item.Priority), Is.All.EqualTo(UiUpdatePriority.Normal));
            Assert.That(ended.Applied, Is.False);
            Assert.That(rolledBack.Applied, Is.True);
            Assert.That(rolledBack.BatchId, Is.EqualTo("discarded"));
            Assert.That(afterRollback.Applied, Is.False);
            Assert.That(scheduler.PendingInvalidations, Is.Empty);
        });
    }

    [Test]
    public void CoalescesPriorityAndHydrationWhileSuppressingStalePendingContent()
    {
        ComponentCore components = RegisteredComponents();
        ComponentOperationResult root = CreateComponent(components, null, "root");
        ComponentSchedulerCore scheduler = new ComponentSchedulerCore(components);

        ComponentScheduleResult background = scheduler.InvalidateComponent(root.Context!.ComponentId, UiUpdatePriority.Background, false);
        ComponentScheduleResult promoted = scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.UserInteraction, true);
        ComponentScheduleResult duplicate = scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Background, false);
        ComponentScheduleResult first = scheduler.BeginComponentUpdateCycle();
        ComponentPendingContent[] copiedPending = scheduler.ReadPendingComponentContent();
        copiedPending[0].Generation = 999;
        scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Immediate, false);
        ComponentScheduleResult deferred = scheduler.EndComponentUpdateCycle();
        ComponentScheduleResult second = scheduler.BeginComponentUpdateCycle();
        scheduler.EndComponentUpdateCycle();

        ComponentOperationResult stale = scheduler.CompletePendingComponentContent(root.Context.ComponentId, first.Requests.Single().Generation, TextNode("stale"));
        ComponentOperationResult nullContent = scheduler.CompletePendingComponentContent(root.Context.ComponentId, second.Requests.Single().Generation, null);
        ComponentOperationResult completed = scheduler.CompletePendingComponentContent(root.Context.ComponentId, second.Requests.Single().Generation, TextNode("current"));
        ComponentOperationResult repeated = scheduler.CompletePendingComponentContent(root.Context.ComponentId, second.Requests.Single().Generation, TextNode("again"));

        Assert.Multiple(() =>
        {
            Assert.That(background.Applied, Is.True);
            Assert.That(promoted.Applied, Is.True);
            Assert.That(duplicate.Applied, Is.False);
            Assert.That(first.Requests.Single().Priority, Is.EqualTo(UiUpdatePriority.UserInteraction));
            Assert.That(first.Requests.Single().IsHydration, Is.True);
            Assert.That(deferred.Applied, Is.True);
            Assert.That(deferred.Invalidations.Single().Priority, Is.EqualTo(UiUpdatePriority.Immediate));
            Assert.That(second.Depth, Is.EqualTo(2));
            Assert.That(second.Requests.Single().Generation, Is.EqualTo(2));
            Assert.That(scheduler.ReadPendingComponentContent(), Is.Empty);
            Assert.That(stale.Code, Is.EqualTo("component.stale-pending-content"));
            Assert.That(nullContent.Code, Is.EqualTo("component.null-render"));
            Assert.That(completed.Success, Is.True);
            Assert.That(repeated.Code, Is.EqualTo("component.no-pending-content"));
            Assert.That(components.ReadComponentInstance(root.Context.ComponentId).Node!.Text, Is.EqualTo("current"));
        });
    }

    [Test]
    public void DefersReentrantInvalidationAndRejectsNestedSchedulerOperations()
    {
        ComponentCore components = RegisteredComponents();
        ComponentOperationResult root = CreateComponent(components, null, "root");
        ComponentSchedulerCore scheduler = new ComponentSchedulerCore(components);
        scheduler.InvalidateComponent(root.Context!.ComponentId, UiUpdatePriority.Normal, false);

        ComponentScheduleResult first = scheduler.BeginComponentUpdateCycle();
        ComponentScheduleResult nestedCycle = scheduler.BeginComponentUpdateCycle();
        ComponentScheduleResult nestedBatch = scheduler.BeginComponentInvalidationBatch("nested");
        ComponentScheduleResult reentrant = scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Immediate, false);
        ComponentScheduleResult ended = scheduler.EndComponentUpdateCycle();
        ComponentScheduleResult second = scheduler.BeginComponentUpdateCycle();
        ComponentScheduleResult secondEnded = scheduler.EndComponentUpdateCycle();
        ComponentScheduleResult extraEnd = scheduler.EndComponentUpdateCycle();
        CompleteRequests(scheduler, second.Requests);
        ComponentScheduleResult invalidPriority = scheduler.InvalidateComponent(root.Context.ComponentId, (UiUpdatePriority)999, false);
        ComponentScheduleResult invalidBatch = scheduler.BeginComponentInvalidationBatch(" ");

        Assert.Multiple(() =>
        {
            Assert.That(first.Depth, Is.EqualTo(1));
            Assert.That(nestedCycle.Code, Is.EqualTo("component.update-cycle-active"));
            Assert.That(nestedBatch.Code, Is.EqualTo("component.update-cycle-active"));
            Assert.That(reentrant.Applied, Is.True);
            Assert.That(ended.Invalidations, Has.Length.EqualTo(1));
            Assert.That(second.Depth, Is.EqualTo(2));
            Assert.That(second.Requests.Single().Priority, Is.EqualTo(UiUpdatePriority.Immediate));
            Assert.That(secondEnded.Depth, Is.EqualTo(0));
            Assert.That(extraEnd.Code, Is.EqualTo("component.update-cycle-not-active"));
            Assert.That(invalidPriority.Code, Is.EqualTo("component.invalid-priority"));
            Assert.That(invalidBatch.Code, Is.EqualTo("component.invalid-batch-id"));
        });
    }

    [Test]
    public void StopsRunawayReentrantUpdatesAtTheMaximumDepth()
    {
        ComponentCore components = RegisteredComponents();
        ComponentOperationResult root = CreateComponent(components, null, "root");
        ComponentSchedulerCore scheduler = new ComponentSchedulerCore(components);
        scheduler.InvalidateComponent(root.Context!.ComponentId, UiUpdatePriority.Normal, false);

        int depth = 1;
        while (depth <= ComponentSchedulerCore.MaximumUpdateDepth)
        {
            ComponentScheduleResult cycle = scheduler.BeginComponentUpdateCycle();
            Assert.That(cycle.Success, Is.True);
            Assert.That(cycle.Depth, Is.EqualTo(depth));
            scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Normal, false);
            scheduler.EndComponentUpdateCycle();
            depth++;
        }

        ComponentScheduleResult exhausted = scheduler.BeginComponentUpdateCycle();
        ComponentScheduleResult recoveredInvalidation = scheduler.InvalidateComponent(root.Context.ComponentId, UiUpdatePriority.Background, false);
        ComponentScheduleResult recovered = scheduler.BeginComponentUpdateCycle();
        scheduler.EndComponentUpdateCycle();

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Code, Is.EqualTo("component.maximum-update-depth"));
            Assert.That(exhausted.Depth, Is.EqualTo(ComponentSchedulerCore.MaximumUpdateDepth + 1));
            Assert.That(scheduler.UpdateDepth, Is.EqualTo(0));
            Assert.That(recoveredInvalidation.Success, Is.True);
            Assert.That(recovered.Depth, Is.EqualTo(1));
        });
    }

    private static ComponentCore RegisteredComponents()
    {
        ComponentCore components = new ComponentCore();
        components.RegisterComponentDefinition(new UiComponentDefinition { TypeId = "card", RenderHandlerId = "render-card", LifecycleHandlerId = "lifecycle-card" });
        return components;
    }

    private static ComponentOperationResult CreateComponent(ComponentCore components, string? parentId, string key)
    {
        return components.CreateComponentInstance("seed", parentId, "card", key, System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
    }

    private static void CompleteRequests(ComponentSchedulerCore scheduler, UiComponentRenderRequest[] requests)
    {
        int index = 0;
        while (index < requests.Length)
        {
            scheduler.CompletePendingComponentContent(requests[index].Context.ComponentId, requests[index].Generation, TextNode(requests[index].Context.Key));
            index++;
        }
    }

    private static UiNode TextNode(string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = text, SiblingIdentity = text, StableId = text + "-id" },
            Text = text
        };
    }
}
