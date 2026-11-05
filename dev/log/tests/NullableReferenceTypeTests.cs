using System.Reflection;
using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class NullableReferenceTypeTests
{
    [Test]
    public void OptionalPublicState_IsAnnotatedNullable()
    {
        AssertNullableField(typeof(LogEvent), "ExceptionValue");
        AssertNullableField(typeof(LogValue), "ExceptionValue");
    }

    [Test]
    public void NullableFactoryContracts_ArePublishedInMetadata()
    {
        MethodInfo? formatterFactory = typeof(LogFactory).GetMethod("WrapFormatter", new Type[] { typeof(ILogFormatter) });
        Assert.That(formatterFactory, Is.Not.Null);
        if (formatterFactory == null)
        {
            return;
        }

        NullabilityInfoContext context = new NullabilityInfoContext();
        NullabilityInfo formatterParameter = context.Create(formatterFactory.GetParameters()[0]);
        Assert.That(formatterParameter.ReadState, Is.EqualTo(NullabilityState.Nullable));
        NullabilityInfo formatterReturn = context.Create(formatterFactory.ReturnParameter);
        Assert.That(formatterReturn.ReadState, Is.EqualTo(NullabilityState.NotNull));
    }

    private static void AssertNullableField(Type declaringType, string fieldName)
    {
        FieldInfo? field = declaringType.GetField(fieldName);
        Assert.That(field, Is.Not.Null);
        if (field == null)
        {
            return;
        }

        NullabilityInfoContext context = new NullabilityInfoContext();
        NullabilityInfo nullability = context.Create(field);
        Assert.That(nullability.ReadState, Is.EqualTo(NullabilityState.Nullable));
    }
}
