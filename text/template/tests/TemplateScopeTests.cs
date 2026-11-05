using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateScopeTests
{
    [Test]
    public void Constructor_CreatesRootFrameWithRootAsCurrentValue()
    {
        TemplateValue root = TemplateValue.FromMap(CreateMap("name", TemplateValue.FromString("Ada")));

        TemplateScopeStack stack = new TemplateScopeStack(root);

        Assert.That(stack.RootValue, Is.SameAs(root));
        Assert.That(stack.GetFrameCount(), Is.EqualTo(1));
        Assert.That(stack.GetCurrentFrame().Kind, Is.EqualTo(TemplateScopeFrameKind.Root));
        Assert.That(stack.GetCurrentValue(), Is.SameAs(root));
        Assert.That(stack.GetParentCurrentValue(), Is.SameAs(root));
    }

    [Test]
    public void PushAndPopFrames_UpdateCurrentAndPreventRootPop()
    {
        TemplateScopeStack stack = TemplateScopeStack.Create(TemplateValue.FromString("root"));
        TemplateValue loopCurrent = TemplateValue.FromString("item");

        TemplateScopeFrame loop = stack.PushLoopScope(loopCurrent);
        TemplateScopeFrame block = stack.PushBlockScope("content");

        Assert.That(loop.Kind, Is.EqualTo(TemplateScopeFrameKind.Loop));
        Assert.That(stack.GetFrameCount(), Is.EqualTo(3));
        Assert.That(stack.GetCurrentFrame(), Is.SameAs(block));
        Assert.That(stack.GetCurrentValue(), Is.SameAs(loopCurrent));
        Assert.That(stack.GetParentCurrentValue(), Is.SameAs(loopCurrent));
        Assert.That(stack.PopFrame(), Is.SameAs(block));
        Assert.That(stack.PopFrame(), Is.SameAs(loop));
        Assert.That(stack.GetFrameCount(), Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => stack.PopFrame());
    }

    [Test]
    public void Locals_SearchNearestFrameAndRestoreAfterPop()
    {
        TemplateScopeStack stack = TemplateScopeStack.Create(TemplateValue.FromString("root"));
        stack.SetLocal("name", TemplateValue.FromString("root-local"));
        stack.PushLocalScope();
        stack.SetLocal("name", TemplateValue.FromString("inner-local"));
        stack.SetLocal("onlyInner", TemplateValue.FromBoolean(true));

        Assert.That(stack.HasLocal("name"), Is.True);
        Assert.That(stack.GetLocal("name").StringValue, Is.EqualTo("inner-local"));
        Assert.That(stack.Resolve("name").StringValue, Is.EqualTo("inner-local"));
        Assert.That(stack.Resolve("onlyInner").BooleanValue, Is.True);

        stack.PopFrame();

        Assert.That(stack.GetLocal("name").StringValue, Is.EqualTo("root-local"));
        Assert.That(stack.GetLocal("onlyInner").IsMissing(), Is.True);
        Assert.That(stack.HasLocal("onlyInner"), Is.False);
    }

    [Test]
    public void Resolve_FallsBackToCurrentMapThenRootMapAndThis()
    {
        TemplateValue root = TemplateValue.FromMap(CreateMap("site", TemplateValue.FromString("root-site")));
        TemplateValue current = TemplateValue.FromMap(CreateMap("title", TemplateValue.FromString("current-title")));
        TemplateScopeStack stack = TemplateScopeStack.Create(root);
        stack.PushLoopScope(current);

        Assert.That(stack.Resolve("this"), Is.SameAs(current));
        Assert.That(stack.Resolve("title").StringValue, Is.EqualTo("current-title"));
        Assert.That(stack.Resolve("site").StringValue, Is.EqualTo("root-site"));
        Assert.That(stack.Resolve("missing").IsMissing(), Is.True);
    }

    [Test]
    public void NamedFrames_RecordBlockMacroAndIncludeScopes()
    {
        TemplateScopeStack stack = TemplateScopeStack.Create(TemplateValue.FromString("root"));
        TemplateScopeFrame block = stack.PushBlockScope("body");
        TemplateScopeFrame macro = stack.PushMacroScope("format");
        TemplateScopeFrame include = stack.PushIncludeScope("partial", TemplateValue.FromString("include-current"));

        Assert.That(block.Kind, Is.EqualTo(TemplateScopeFrameKind.Block));
        Assert.That(block.Name, Is.EqualTo("body"));
        Assert.That(macro.Kind, Is.EqualTo(TemplateScopeFrameKind.Macro));
        Assert.That(macro.Name, Is.EqualTo("format"));
        Assert.That(include.Kind, Is.EqualTo(TemplateScopeFrameKind.Include));
        Assert.That(include.Name, Is.EqualTo("partial"));
        Assert.That(stack.GetCurrentValue().StringValue, Is.EqualTo("include-current"));
        Assert.That(stack.FindNearestFrame(TemplateScopeFrameKind.Include), Is.SameAs(include));
        Assert.That(stack.FindNearestFrame(TemplateScopeFrameKind.Macro), Is.SameAs(macro));
        Assert.That(stack.FindNearestFrame(TemplateScopeFrameKind.Loop), Is.Null);
    }

    [Test]
    public void ConstructorsAndMutators_RejectNullRequiredValues()
    {
        TemplateValue value = TemplateValue.FromString("x");
        TemplateScopeStack stack = TemplateScopeStack.Create(value);
        TemplateScopeFrame frame = new TemplateScopeFrame(TemplateScopeFrameKind.Local, "", value);

        Assert.Throws<ArgumentNullException>(() => new TemplateScopeStack(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateScopeFrame(TemplateScopeFrameKind.Local, null!, value));
        Assert.Throws<ArgumentNullException>(() => new TemplateScopeFrame(TemplateScopeFrameKind.Local, "", null!));
        Assert.Throws<ArgumentNullException>(() => frame.SetLocal(null!, value));
        Assert.Throws<ArgumentNullException>(() => frame.SetLocal("x", null!));
        Assert.Throws<ArgumentNullException>(() => frame.HasLocal(null!));
        Assert.Throws<ArgumentNullException>(() => frame.GetLocal(null!));
        Assert.Throws<ArgumentNullException>(() => stack.PushFrame(TemplateScopeFrameKind.Local, null!, value));
        Assert.Throws<ArgumentNullException>(() => stack.PushFrame(TemplateScopeFrameKind.Local, "", null!));
        Assert.Throws<ArgumentNullException>(() => stack.SetLocal(null!, value));
        Assert.Throws<ArgumentNullException>(() => stack.SetLocal("x", null!));
        Assert.Throws<ArgumentNullException>(() => stack.HasLocal(null!));
        Assert.Throws<ArgumentNullException>(() => stack.GetLocal(null!));
        Assert.Throws<ArgumentNullException>(() => stack.Resolve(null!));
    }

    static Dictionary<string, TemplateValue> CreateMap(string key, TemplateValue value)
    {
        Dictionary<string, TemplateValue> map = new Dictionary<string, TemplateValue>();
        map[key] = value;
        return map;
    }
}
