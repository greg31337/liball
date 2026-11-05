using asn1;

namespace asn1.Tests;

public class CSharpNameMapperTests
{
    [Test]
    public void TypeAndMemberNamesUsePortableDeterministicIdentifiers()
    {
        CSharpNameMapper mapper = new CSharpNameMapper();
        CSharpNameScope typeScope = new CSharpNameScope(false);
        CSharpNameScope memberScope = new CSharpNameScope(false);

        CSharpNameMapping type = mapper.MapTypeName(
            "9-hostile type--name",
            "type:hostile",
            typeScope);
        CSharpNameMapping member = mapper.MapMemberName(
            "---",
            "field:empty",
            memberScope);

        Assert.Multiple(() =>
        {
            Assert.That(type.GeneratedName, Is.EqualTo("N9HostileTypeName"));
            Assert.That(member.GeneratedName, Is.EqualTo("UnnamedMember"));
            Assert.That(type.GeneratedName, Does.Match("^[A-Za-z][A-Za-z0-9_]*$"));
            Assert.That(member.GeneratedName, Does.Match("^[A-Za-z][A-Za-z0-9_]*$"));
        });
    }

    [Test]
    public void KeywordsAreSuffixedWithoutVerbatimIdentifiers()
    {
        CSharpNameMapper mapper = new CSharpNameMapper();
        CSharpNameScope typeScope = new CSharpNameScope(false);
        CSharpNameScope memberScope = new CSharpNameScope(false);
        CSharpNameScope namespaceScope = new CSharpNameScope(false);

        CSharpNameMapping type = mapper.MapTypeName(
            "class",
            "type:class",
            typeScope);
        CSharpNameMapping member = mapper.MapMemberName(
            "required",
            "field:required",
            memberScope);
        CSharpNameMapping namespaceName = mapper.MapNamespaceName(
            "namespace.event",
            "module:keywords",
            namespaceScope);

        Assert.Multiple(() =>
        {
            Assert.That(type.GeneratedName, Is.EqualTo("ClassValue"));
            Assert.That(member.GeneratedName, Is.EqualTo("RequiredValue"));
            Assert.That(namespaceName.GeneratedName,
                Is.EqualTo("NamespaceValue.EventValue"));
            Assert.That(type.GeneratedName, Does.Not.Contain("@"));
            Assert.That(member.GeneratedName, Does.Not.Contain("@"));
            Assert.That(namespaceName.GeneratedName, Does.Not.Contain("@"));
        });
    }

    [Test]
    public void CanonicallyEquivalentUnicodeNamesShareOneCollisionBase()
    {
        CSharpNameMapper mapper = new CSharpNameMapper();
        CSharpNameScope scope = new CSharpNameScope(false);

        CSharpNameMapping decomposed = mapper.MapTypeName(
            "cafe\u0301-name",
            "type:decomposed",
            scope);
        CSharpNameMapping composed = mapper.MapTypeName(
            "caf\u00e9-name",
            "type:composed",
            scope);
        CSharpNameMapping supplementary = mapper.MapTypeName(
            "\U0001f600-value",
            "type:supplementary",
            scope);

        Assert.Multiple(() =>
        {
            Assert.That(decomposed.NormalizedSourceName,
                Is.EqualTo(composed.NormalizedSourceName));
            Assert.That(decomposed.GeneratedName, Is.EqualTo("CafU00E9Name"));
            Assert.That(composed.GeneratedName, Is.EqualTo("CafU00E9Name_2"));
            Assert.That(composed.CollisionIndex, Is.EqualTo(2));
            Assert.That(supplementary.GeneratedName,
                Is.EqualTo("U1F600Value"));
        });
    }

    [Test]
    public void CollisionSuffixesAreStableForTheSameInputOrder()
    {
        CSharpNameMapper mapper = new CSharpNameMapper();
        CSharpNameScope firstScope = new CSharpNameScope(false);
        CSharpNameScope secondScope = new CSharpNameScope(false);
        string[] sourceNames = ["foo-bar", "foo_bar", "foo bar"];
        string[] first = new string[sourceNames.Length];
        string[] second = new string[sourceNames.Length];

        int index = 0;
        while (index < sourceNames.Length)
        {
            first[index] = mapper.MapTypeName(
                sourceNames[index],
                "first:" + index,
                firstScope).GeneratedName;
            second[index] = mapper.MapTypeName(
                sourceNames[index],
                "second:" + index,
                secondScope).GeneratedName;
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(
                new string[] { "FooBar", "FooBar_2", "FooBar_3" }));
            Assert.That(second, Is.EqualTo(first));
        });
    }

    [Test]
    public void FileNamesAvoidCaseInsensitiveAndPlatformReservedCollisions()
    {
        CSharpNameMapper mapper = new CSharpNameMapper();
        CSharpNameScope typeScope = new CSharpNameScope(false);
        CSharpNameScope fileScope = new CSharpNameScope(true);
        CSharpNameMapping foo = mapper.MapTypeName(
            "foo",
            "type:foo",
            typeScope);
        CSharpNameMapping upperFoo = mapper.MapTypeName(
            "FOO",
            "type:upper-foo",
            typeScope);
        CSharpNameMapping reserved = mapper.MapTypeName(
            "con",
            "type:con",
            typeScope);

        CSharpNameMapping fooFile = mapper.MapFileName(foo, fileScope);
        CSharpNameMapping upperFooFile = mapper.MapFileName(
            upperFoo,
            fileScope);
        CSharpNameMapping reservedFile = mapper.MapFileName(
            reserved,
            fileScope);

        Assert.Multiple(() =>
        {
            Assert.That(fooFile.GeneratedName, Is.EqualTo("Foo.cs"));
            Assert.That(upperFooFile.GeneratedName, Is.EqualTo("FOO_2.cs"));
            Assert.That(upperFooFile.CollisionIndex, Is.EqualTo(2));
            Assert.That(reservedFile.GeneratedName, Is.EqualTo("ConFile.cs"));
        });
    }

    [Test]
    public void MappingsRetainSourceAndStableIdentityMetadata()
    {
        CSharpNameMapper mapper = new CSharpNameMapper();
        CSharpNameMapping mapping = mapper.MapNamespaceName(
            "company.generated-module",
            "module:source-id",
            new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(mapping.SourceName,
                Is.EqualTo("company.generated-module"));
            Assert.That(mapping.NormalizedSourceName,
                Is.EqualTo("company.generated-module"));
            Assert.That(mapping.StableId, Is.EqualTo("module:source-id"));
            Assert.That(mapping.GeneratedName,
                Is.EqualTo("Company.GeneratedModule"));
            Assert.That(mapping.CollisionIndex, Is.EqualTo(1));
            Assert.That(mapping.Changed, Is.True);
        });
    }
}
