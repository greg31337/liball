using System.Reflection;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxNullableReferenceTests
{
    [Test]
    public void OptionalLookupResults_ExposeNullableMetadata()
    {
        AssertNullableReturn(typeof(global::docx.DocxPackage), "FindPart", new Type[] { typeof(string) });
        AssertNullableReturn(typeof(global::docx.DocxRelationshipSet), "FindById", new Type[] { typeof(string) });
        AssertNullableReturn(typeof(global::docx.DocxDocument), "FindStyleById", new Type[] { typeof(string) });
        AssertNullableReturn(typeof(global::docx.IDocxPackageInspector), "FindPart", new Type[] { typeof(string) });
    }

    [Test]
    public void OptionalModelFields_ExposeNullableMetadata()
    {
        AssertNullableField(typeof(global::docx.DocxBlock), "Paragraph");
        AssertNullableField(typeof(global::docx.DocxBody), "FinalSection");
        AssertNullableField(typeof(global::docx.DocxHyperlink), "Relationship");
    }

    static void AssertNullableReturn(Type type, string methodName, Type[] parameterTypes)
    {
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, parameterTypes)
            ?? throw new InvalidOperationException("Expected method was not found: " + type.FullName + "." + methodName);
        NullabilityInfo nullability = new NullabilityInfoContext().Create(method.ReturnParameter);

        Assert.That(nullability.ReadState, Is.EqualTo(NullabilityState.Nullable));
    }

    static void AssertNullableField(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Expected field was not found: " + type.FullName + "." + fieldName);
        NullabilityInfo nullability = new NullabilityInfoContext().Create(field);

        Assert.That(nullability.ReadState, Is.EqualTo(NullabilityState.Nullable));
    }
}
