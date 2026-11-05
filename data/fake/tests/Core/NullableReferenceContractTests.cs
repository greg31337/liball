using System.Reflection;
using fake;

namespace fake.Tests.Core;

[TestFixture]
public class NullableReferenceContractTests
{
    [Test]
    public void MaybeAdvertisesNullableInputAndOutput()
    {
        MethodInfo method = RequiredMethod(
            typeof(FakeRandom),
            "Maybe",
            new Type[] { typeof(object) });
        NullabilityInfoContext context = new NullabilityInfoContext();

        Assert.Multiple(() =>
        {
            Assert.That(
                context.Create(method.ReturnParameter).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
            Assert.That(
                context.Create(method.GetParameters()[0]).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
        });
    }

    [Test]
    public void OptionalModelFieldsAdvertiseNullability()
    {
        NullabilityInfoContext context = new NullabilityInfoContext();

        Assert.Multiple(() =>
        {
            Assert.That(
                context.Create(RequiredField(typeof(LanguageTag), "Script")).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
            Assert.That(
                context.Create(RequiredField(typeof(TimeZoneDetails), "Comment")).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
            Assert.That(
                context.Create(RequiredField(typeof(PhoneSafetyMetadata), "SafeRange")).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
        });
    }

    [Test]
    public void SchemaConstantsAndResultsAdvertiseNullableValues()
    {
        MethodInfo constant = RequiredMethod(
            typeof(SchemaGenerator),
            "Constant",
            new Type[] { typeof(object) });
        MethodInfo generate = RequiredMethod(
            typeof(SchemaGenerator),
            "GenerateFromRoot",
            new Type[] { typeof(SchemaNode) });
        NullabilityInfoContext context = new NullabilityInfoContext();

        Assert.Multiple(() =>
        {
            Assert.That(
                context.Create(constant.GetParameters()[0]).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
            Assert.That(
                context.Create(generate.ReturnParameter).ReadState,
                Is.EqualTo(NullabilityState.Nullable));
        });
    }

    static MethodInfo RequiredMethod(Type type, string name, Type[] parameterTypes)
    {
        MethodInfo? result = type.GetMethod(name, parameterTypes);
        if (result == null)
            throw new InvalidOperationException("Expected method was not found.");
        return result;
    }

    static FieldInfo RequiredField(Type type, string name)
    {
        FieldInfo? result = type.GetField(name);
        if (result == null)
            throw new InvalidOperationException("Expected field was not found.");
        return result;
    }
}
