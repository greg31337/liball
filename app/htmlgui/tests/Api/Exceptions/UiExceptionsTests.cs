using System;
using htmlgui;

namespace htmlgui.Tests;

public class UiExceptionsTests
{
    [Test]
    public void ExceptionsExposeTypedContextAndDeterministicMessages()
    {
        UiModelException model = new UiModelException("Invalid model", "MODEL001", "/windows/0");
        UiRenderException render = new UiRenderException("Render failed", "ui-node", "document");
        UiStateException state = new UiStateException("Write failed", "user", "/name");
        UiBindingException binding = new UiBindingException("Binding failed", "binding-1", "ui-input");
        UiComponentException component = new UiComponentException("Lifecycle failed", "component-1", UiLifecycleStage.AfterRender);
        UiHydrationException hydration = new UiHydrationException("Mismatch", "ui-root", UiHydrationMode.Repair, UiHydrationStatus.Failed);
        UiBridgeProtocolException bridge = new UiBridgeProtocolException("Sequence rejected", "BRIDGE_SEQUENCE", "session-1", 42);
        Exception[] failures = new Exception[] { model, render, state, binding, component, hydration, bridge };

        Assert.Multiple(() =>
        {
            Assert.That(failures, Has.Length.EqualTo(7));
            Assert.That(model.Code, Is.EqualTo("MODEL001"));
            Assert.That(model.Location, Is.EqualTo("/windows/0"));
            Assert.That(model.Message, Is.EqualTo("Invalid model (model code 'MODEL001', location '/windows/0')."));
            Assert.That(render.NodeId, Is.EqualTo("ui-node"));
            Assert.That(render.OutputKind, Is.EqualTo("document"));
            Assert.That(render.Message, Is.EqualTo("Render failed (node 'ui-node', output 'document')."));
            Assert.That(state.StateId, Is.EqualTo("user"));
            Assert.That(state.Path, Is.EqualTo("/name"));
            Assert.That(state.Message, Is.EqualTo("Write failed (state 'user', path '/name')."));
            Assert.That(binding.BindingId, Is.EqualTo("binding-1"));
            Assert.That(binding.TargetNodeId, Is.EqualTo("ui-input"));
            Assert.That(binding.Message, Is.EqualTo("Binding failed (binding 'binding-1', target 'ui-input')."));
            Assert.That(component.ComponentId, Is.EqualTo("component-1"));
            Assert.That(component.Stage, Is.EqualTo(UiLifecycleStage.AfterRender));
            Assert.That(component.Message, Is.EqualTo("Lifecycle failed (component 'component-1', lifecycle 'AfterRender')."));
            Assert.That(hydration.NodeId, Is.EqualTo("ui-root"));
            Assert.That(hydration.Mode, Is.EqualTo(UiHydrationMode.Repair));
            Assert.That(hydration.Status, Is.EqualTo(UiHydrationStatus.Failed));
            Assert.That(hydration.Message, Is.EqualTo("Mismatch (node 'ui-root', mode 'Repair', status 'Failed')."));
            Assert.That(bridge.Code, Is.EqualTo("BRIDGE_SEQUENCE"));
            Assert.That(bridge.Session, Is.EqualTo("session-1"));
            Assert.That(bridge.Sequence, Is.EqualTo(42));
            Assert.That(bridge.Message, Is.EqualTo("Sequence rejected (protocol code 'BRIDGE_SEQUENCE', session 'session-1', sequence '42')."));
        });
    }

    [Test]
    public void ExceptionsCanBeThrownAndCaughtThroughSystemException()
    {
        Exception caught = Assert.Throws<UiModelException>(() => throw new UiModelException("Invalid", "MODEL", "/"))!;

        Assert.That(caught.Message, Does.StartWith("Invalid"));
    }

    [Test]
    public void RequiredMessagesAndContextRejectNullOrWhitespace()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new UiModelException(null!, "code", "/"));
            Assert.Throws<ArgumentException>(() => new UiModelException("message", " ", "/"));
            Assert.Throws<ArgumentException>(() => new UiRenderException("message", " ", "html"));
            Assert.Throws<ArgumentException>(() => new UiStateException("message", "state", " "));
            Assert.Throws<ArgumentException>(() => new UiBindingException("message", "binding", " "));
            Assert.Throws<ArgumentException>(() => new UiComponentException("message", " ", UiLifecycleStage.Create));
            Assert.Throws<ArgumentException>(() => new UiHydrationException("message", " ", UiHydrationMode.Exact, UiHydrationStatus.Matched));
            Assert.Throws<ArgumentException>(() => new UiBridgeProtocolException("message", "code", " ", 0));
        });
    }

    [Test]
    public void BridgeSequenceMustBeNonNegative()
    {
        UiBridgeProtocolException zero = new UiBridgeProtocolException("Accepted", "code", "session", 0);

        Assert.That(zero.Sequence, Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UiBridgeProtocolException("Rejected", "code", "session", -1));
    }

    [Test]
    public void EveryRequiredExceptionContextRejectsNullAndWhitespace()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new UiModelException(null!, "code", "location"));
            Assert.Throws<ArgumentNullException>(() => new UiModelException("message", null!, "location"));
            Assert.Throws<ArgumentNullException>(() => new UiModelException("message", "code", null!));
            Assert.Throws<ArgumentException>(() => new UiModelException(" ", "code", "location"));
            Assert.Throws<ArgumentException>(() => new UiModelException("message", " ", "location"));
            Assert.Throws<ArgumentException>(() => new UiModelException("message", "code", " "));

            Assert.Throws<ArgumentNullException>(() => new UiRenderException(null!, "node", "document"));
            Assert.Throws<ArgumentNullException>(() => new UiRenderException("message", null!, "document"));
            Assert.Throws<ArgumentNullException>(() => new UiRenderException("message", "node", null!));
            Assert.Throws<ArgumentException>(() => new UiRenderException(" ", "node", "document"));
            Assert.Throws<ArgumentException>(() => new UiRenderException("message", " ", "document"));
            Assert.Throws<ArgumentException>(() => new UiRenderException("message", "node", " "));

            Assert.Throws<ArgumentNullException>(() => new UiStateException(null!, "state", "/"));
            Assert.Throws<ArgumentNullException>(() => new UiStateException("message", null!, "/"));
            Assert.Throws<ArgumentNullException>(() => new UiStateException("message", "state", null!));
            Assert.Throws<ArgumentException>(() => new UiStateException(" ", "state", "/"));
            Assert.Throws<ArgumentException>(() => new UiStateException("message", " ", "/"));
            Assert.Throws<ArgumentException>(() => new UiStateException("message", "state", " "));

            Assert.Throws<ArgumentNullException>(() => new UiBindingException(null!, "binding", "target"));
            Assert.Throws<ArgumentNullException>(() => new UiBindingException("message", null!, "target"));
            Assert.Throws<ArgumentNullException>(() => new UiBindingException("message", "binding", null!));
            Assert.Throws<ArgumentException>(() => new UiBindingException(" ", "binding", "target"));
            Assert.Throws<ArgumentException>(() => new UiBindingException("message", " ", "target"));
            Assert.Throws<ArgumentException>(() => new UiBindingException("message", "binding", " "));

            Assert.Throws<ArgumentNullException>(() => new UiComponentException(null!, "component", UiLifecycleStage.Create));
            Assert.Throws<ArgumentNullException>(() => new UiComponentException("message", null!, UiLifecycleStage.Create));
            Assert.Throws<ArgumentException>(() => new UiComponentException(" ", "component", UiLifecycleStage.Create));
            Assert.Throws<ArgumentException>(() => new UiComponentException("message", " ", UiLifecycleStage.Create));

            Assert.Throws<ArgumentNullException>(() => new UiHydrationException(null!, "node", UiHydrationMode.Exact, UiHydrationStatus.Matched));
            Assert.Throws<ArgumentNullException>(() => new UiHydrationException("message", null!, UiHydrationMode.Exact, UiHydrationStatus.Matched));
            Assert.Throws<ArgumentException>(() => new UiHydrationException(" ", "node", UiHydrationMode.Exact, UiHydrationStatus.Matched));
            Assert.Throws<ArgumentException>(() => new UiHydrationException("message", " ", UiHydrationMode.Exact, UiHydrationStatus.Matched));

            Assert.Throws<ArgumentNullException>(() => new UiBridgeProtocolException(null!, "code", "session", 0));
            Assert.Throws<ArgumentNullException>(() => new UiBridgeProtocolException("message", null!, "session", 0));
            Assert.Throws<ArgumentNullException>(() => new UiBridgeProtocolException("message", "code", null!, 0));
            Assert.Throws<ArgumentException>(() => new UiBridgeProtocolException(" ", "code", "session", 0));
            Assert.Throws<ArgumentException>(() => new UiBridgeProtocolException("message", " ", "session", 0));
            Assert.Throws<ArgumentException>(() => new UiBridgeProtocolException("message", "code", " ", 0));
        });
    }

    [Test]
    public void UndefinedEnumContextIsPreservedForFailureDiagnostics()
    {
        UiComponentException component = new UiComponentException("Failed", "component", (UiLifecycleStage)999);
        UiHydrationException hydration = new UiHydrationException("Failed", "node", (UiHydrationMode)999, (UiHydrationStatus)998);

        Assert.Multiple(() =>
        {
            Assert.That(component.Stage, Is.EqualTo((UiLifecycleStage)999));
            Assert.That(component.Message, Does.Contain("lifecycle '999'"));
            Assert.That(hydration.Mode, Is.EqualTo((UiHydrationMode)999));
            Assert.That(hydration.Status, Is.EqualTo((UiHydrationStatus)998));
            Assert.That(hydration.Message, Does.Contain("mode '999', status '998'"));
        });
    }
}
