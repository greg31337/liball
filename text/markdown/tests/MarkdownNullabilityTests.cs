using System.Reflection;

namespace markdown.Tests;

[TestFixture]
public class MarkdownNullabilityTests
{
    [Test]
    public void OptionalPublicFields_AreMarkedNullable()
    {
        NullabilityInfoContext context = new NullabilityInfoContext();

        Assert.That(context.Create(GetField(typeof(MarkdownNode), "Parent")).WriteState, Is.EqualTo(NullabilityState.Nullable));
        Assert.That(context.Create(GetField(typeof(MarkdownNode), "Trivia")).WriteState, Is.EqualTo(NullabilityState.Nullable));
        Assert.That(context.Create(GetField(typeof(MarkdownVisitContext), "Parent")).WriteState, Is.EqualTo(NullabilityState.Nullable));
        Assert.That(context.Create(GetField(typeof(MarkdownEvent), "Node")).WriteState, Is.EqualTo(NullabilityState.Nullable));
        Assert.That(context.Create(GetField(typeof(MarkdownEvent), "Trivia")).WriteState, Is.EqualTo(NullabilityState.Nullable));
    }

    [Test]
    public void RequiredPublicDataFields_AreMarkedNotNullable()
    {
        NullabilityInfoContext context = new NullabilityInfoContext();

        Assert.That(context.Create(GetField(typeof(MarkdownNode), "Literal")).WriteState, Is.EqualTo(NullabilityState.NotNull));
        Assert.That(context.Create(GetField(typeof(MarkdownNode), "Children")).WriteState, Is.EqualTo(NullabilityState.NotNull));
        Assert.That(context.Create(GetField(typeof(MarkdownEvent), "Attributes")).WriteState, Is.EqualTo(NullabilityState.NotNull));
        Assert.That(context.Create(GetField(typeof(MarkdownDiagnostic), "Code")).WriteState, Is.EqualTo(NullabilityState.NotNull));
    }

    [Test]
    public void OptionalPublicConstructorParameters_AreMarkedNullable()
    {
        NullabilityInfoContext context = new NullabilityInfoContext();
        ConstructorInfo constructor = GetConstructor(
            typeof(MarkdownVisitContext),
            new Type[] { typeof(MarkdownNode), typeof(MarkdownNode), typeof(int), typeof(int) });

        Assert.That(context.Create(constructor.GetParameters()[1]).ReadState, Is.EqualTo(NullabilityState.Nullable));
    }

    static FieldInfo GetField(Type type, string name)
    {
        FieldInfo? field = type.GetField(name);
        if (field == null)
        {
            throw new InvalidOperationException("Expected public field was not found: " + type.Name + "." + name);
        }

        return field;
    }

    static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes)
    {
        ConstructorInfo? constructor = type.GetConstructor(parameterTypes);
        if (constructor == null)
        {
            throw new InvalidOperationException("Expected public constructor was not found: " + type.Name);
        }

        return constructor;
    }
}
