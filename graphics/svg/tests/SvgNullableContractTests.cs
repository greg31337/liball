namespace svg.Tests;

public class SvgNullableContractTests
{
    [Test]
    public void PublicContractsDistinguishOptionalAndRequiredReferences()
    {
        System.Reflection.NullabilityInfoContext context = new System.Reflection.NullabilityInfoContext();
        System.Reflection.FieldInfo parent = typeof(SvgNode).GetField("Parent");
        System.Reflection.FieldInfo intersections = typeof(SvgIntersectionResult).GetField("Intersections");
        System.Reflection.MethodInfo lookup = typeof(SvgQuery).GetMethod("GetById");
        System.Reflection.MethodInfo parser = typeof(SvgAnimationInterpolator).GetMethod("ParseWithKindAndText");

        Assert.Multiple(() =>
        {
            Assert.That(context.Create(parent).ReadState, Is.EqualTo(System.Reflection.NullabilityState.Nullable));
            Assert.That(context.Create(intersections).ReadState, Is.EqualTo(System.Reflection.NullabilityState.Nullable));
            Assert.That(context.Create(lookup.ReturnParameter).ReadState, Is.EqualTo(System.Reflection.NullabilityState.Nullable));
            Assert.That(context.Create(parser.GetParameters()[1]).ReadState, Is.EqualTo(System.Reflection.NullabilityState.NotNull));
        });
    }

    [Test]
    public void StagedLoadSourceAnnotatesVariantPayloads()
    {
        System.Reflection.NullabilityInfoContext context = new System.Reflection.NullabilityInfoContext();
        System.Reflection.ConstructorInfo constructor = typeof(SvgLoadSource).GetConstructors().Single();
        System.Reflection.ParameterInfo[] parameters = constructor.GetParameters();

        Assert.Multiple(() =>
        {
            Assert.That(context.Create(parameters[1]).ReadState, Is.EqualTo(System.Reflection.NullabilityState.Nullable));
            Assert.That(context.Create(parameters[2]).ReadState, Is.EqualTo(System.Reflection.NullabilityState.Nullable));
            Assert.That(context.Create(parameters[3]).ReadState, Is.EqualTo(System.Reflection.NullabilityState.Nullable));
            Assert.That(context.Create(parameters[4]).ReadState, Is.EqualTo(System.Reflection.NullabilityState.NotNull));
        });
    }
}
