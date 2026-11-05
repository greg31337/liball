using asn1;

namespace asn1.Tests;

public class EcnBuiltInClassCatalogTests
{
    [Test]
    public void CatalogContainsEveryX692BuiltInClassExactlyOnce()
    {
        EcnBuiltInClassDefinition[] definitions =
            EcnBuiltInClassCatalog.GetClasses();

        Assert.Multiple(() =>
        {
            Assert.That(definitions, Has.Length.EqualTo(53));
            Assert.That(definitions.Select(item => item.Name).Distinct().ToArray(),
                Has.Length.EqualTo(53));
            Assert.That(definitions.All(item => item.Name.StartsWith("#")),
                Is.True);
            Assert.That(definitions.All(item => item.Category
                    != EcnClassCategory.Unknown),
                Is.True);
            Assert.That(EcnBuiltInClassCatalog.FindClass("#ENCODINGS"),
                Is.Null);
            Assert.That(EcnBuiltInClassCatalog.FindClass("#Missing"),
                Is.Null);
        });
    }

    [TestCase("#ALTERNATIVES", (int)EcnClassCategory.Alternatives)]
    [TestCase("#BITS", (int)EcnClassCategory.BitString)]
    [TestCase("#BOOL", (int)EcnClassCategory.Boolean)]
    [TestCase("#CHARS", (int)EcnClassCategory.CharacterString)]
    [TestCase("#CONCATENATION", (int)EcnClassCategory.Concatenation)]
    [TestCase("#TRANSFORM", (int)EcnClassCategory.EncodingProcedure)]
    [TestCase("#CHARACTER-STRING", (int)EcnClassCategory.EncodingStructure)]
    [TestCase("#INT", (int)EcnClassCategory.Integer)]
    [TestCase("#NUL", (int)EcnClassCategory.Null)]
    [TestCase("#OBJECT-IDENTIFIER", (int)EcnClassCategory.ObjectIdentifier)]
    [TestCase("#OCTETS", (int)EcnClassCategory.OctetString)]
    [TestCase("#OPEN-TYPE", (int)EcnClassCategory.OpenType)]
    [TestCase("#OPTIONAL", (int)EcnClassCategory.Optionality)]
    [TestCase("#PAD", (int)EcnClassCategory.Padding)]
    [TestCase("#REAL", (int)EcnClassCategory.Real)]
    [TestCase("#REPETITION", (int)EcnClassCategory.Repetition)]
    [TestCase("#TAG", (int)EcnClassCategory.Tag)]
    [TestCase("#TIME", (int)EcnClassCategory.Time)]
    public void PrimitiveAndStructureClassesHavePrescribedCategories(
        string name,
        int expected)
    {
        EcnBuiltInClassDefinition definition =
            EcnBuiltInClassCatalog.FindClass(name)
            ?? throw new AssertionException("The built-in class is missing.");

        Assert.That(definition.Category,
            Is.EqualTo((EcnClassCategory)expected));
    }

    [TestCase("#CHOICE", "#ALTERNATIVES", (int)EcnClassCategory.Alternatives)]
    [TestCase("#BIT-STRING", "#BITS", (int)EcnClassCategory.BitString)]
    [TestCase("#BOOLEAN", "#BOOL", (int)EcnClassCategory.Boolean)]
    [TestCase("#UTF8String", "#CHARS", (int)EcnClassCategory.CharacterString)]
    [TestCase("#SEQUENCE", "#CONCATENATION", (int)EcnClassCategory.Concatenation)]
    [TestCase("#ENUMERATED", "#INT", (int)EcnClassCategory.Integer)]
    [TestCase("#NULL", "#NUL", (int)EcnClassCategory.Null)]
    [TestCase("#RELATIVE-OID", "#OBJECT-IDENTIFIER", (int)EcnClassCategory.ObjectIdentifier)]
    [TestCase("#OCTET-STRING", "#OCTETS", (int)EcnClassCategory.OctetString)]
    [TestCase("#SET-OF", "#REPETITION", (int)EcnClassCategory.Repetition)]
    [TestCase("#DATE-TIME", "#TIME", (int)EcnClassCategory.Time)]
    public void DerivedClassesRetainPrimitiveCategory(
        string name,
        string primitive,
        int category)
    {
        EcnBuiltInClassDefinition definition =
            EcnBuiltInClassCatalog.FindClass(name)
            ?? throw new AssertionException("The derived class is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(definition.PrimitiveName, Is.EqualTo(primitive));
            Assert.That(definition.Category,
                Is.EqualTo((EcnClassCategory)category));
            Assert.That(definition.Primitive, Is.False);
        });
    }

    [Test]
    public void CategoryGroupsSeparateFieldsConstructorsAndProcedures()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.Boolean,
                EcnClassCategoryGroup.BitField), Is.True);
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.EncodingStructure,
                EcnClassCategoryGroup.BitField), Is.True);
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.Tag,
                EcnClassCategoryGroup.BitField), Is.True);
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.Alternatives,
                EcnClassCategoryGroup.EncodingConstructor), Is.True);
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.Concatenation,
                EcnClassCategoryGroup.EncodingConstructor), Is.True);
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.Repetition,
                EcnClassCategoryGroup.EncodingConstructor), Is.True);
            Assert.That(EcnBuiltInClassCatalog.IsInGroup(
                EcnClassCategory.EncodingProcedure,
                EcnClassCategoryGroup.EncodingProcedure), Is.True);
            Assert.That(EcnBuiltInClassCatalog.GetGroupsForCategory(
                EcnClassCategory.Optionality), Is.Empty);
        });
    }
}
