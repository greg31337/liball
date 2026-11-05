using asn1;

namespace asn1.Tests;

public class XsdSchemaGraphResolverTests
{
    [Test]
    public void ResolvesIncludeImportAndRedefineWithStableComponents()
    {
        AsnXsdInput root = Input(
            "root.xsd",
            "https://schemas.test/root.xsd",
            Schema(
                "urn:a",
                "<xs:include schemaLocation=\"common.xsd\"/>"
                    + "<xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>"
                    + "<xs:redefine schemaLocation=\"base.xsd\">"
                    + "<xs:complexType name=\"Base\"/>"
                    + "</xs:redefine>"
                    + "<xs:complexType name=\"Root\"/>"));
        AsnXsdInput common = Input(
            "common.xsd",
            "https://schemas.test/common.xsd",
            Schema("", "<xs:simpleType name=\"Shared\"/>"));
        AsnXsdInput imported = Input(
            "b.xsd",
            "https://schemas.test/b.xsd",
            Schema("urn:b", "<xs:element name=\"Foreign\"/>"));
        AsnXsdInput baseSchema = Input(
            "base.xsd",
            "https://schemas.test/base.xsd",
            Schema("urn:a", "<xs:complexType name=\"Base\"/>"));

        XsdSchemaGraphResult graph = Resolve(
            [root, common, imported, baseSchema]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Success, Is.True, Format(graph.Diagnostics));
            Assert.That(graph.Diagnostics, Is.Empty);
            Assert.That(graph.DocumentInstances, Has.Length.EqualTo(4));
            Assert.That(graph.Edges.Select(value => value.Kind),
                Is.EqualTo(new[] { "include", "import", "redefine" }));
            Assert.That(graph.Edges.Single(value => value.Kind == "include")
                .Chameleon, Is.True);
            Assert.That(graph.Components.Select(value =>
                value.NamespaceUri + ":" + value.Kind + ":" + value.Name),
                Is.EquivalentTo(new[]
                {
                    "urn:a:complexType:Root",
                    "urn:a:complexType:Base",
                    "urn:a:simpleType:Shared",
                    "urn:b:element:Foreign",
                    "urn:a:complexType:Base"
                }));
            Assert.That(graph.Components.Select(value => value.StableId)
                .Distinct().ToArray(),
                Has.Length.EqualTo(graph.Components.Length));
            Assert.That(graph.Components.Single(value =>
                value.Name == "Shared").SourceDocumentId,
                Does.StartWith("xsd:document:"));
            Assert.That(graph.Components.Single(value =>
                value.Name == "Shared").SourceInstanceId,
                Does.Contain("urn_003Aa"));
            Assert.That(graph.Components.Single(value =>
                value.Name == "Base" && value.Redefinition).StableId,
                Is.Not.EqualTo(graph.Components.Single(value =>
                    value.Name == "Base" && !value.Redefinition).StableId));
        });
    }

    [Test]
    public void InstantiatesChameleonSchemasPerNamespaceAndOrderIndependently()
    {
        AsnXsdInput alpha = Input(
            "alpha.xsd",
            "urn:alpha-doc",
            Schema(
                "urn:alpha",
                "<xs:include schemaLocation=\"common.xsd\"/>"));
        AsnXsdInput beta = Input(
            "beta.xsd",
            "urn:beta-doc",
            Schema(
                "urn:beta",
                "<xs:include schemaLocation=\"common.xsd\"/>"));
        AsnXsdInput common = Input(
            "common.xsd",
            "common.xsd",
            Schema("", "<xs:simpleType name=\"Shared\"/>"),
            false);

        XsdSchemaGraphResult forward = Resolve([alpha, beta, common]);
        XsdSchemaGraphResult reverse = Resolve([common, beta, alpha]);
        XsdComponentDefinition[] shared = forward.Components.Where(value =>
            value.Name == "Shared").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(forward.Success, Is.True, Format(forward.Diagnostics));
            Assert.That(shared.Select(value => value.NamespaceUri),
                Is.EquivalentTo(new[] { "urn:alpha", "urn:beta" }));
            Assert.That(shared.Select(value => value.SourceDocumentId)
                .Distinct().ToArray(), Has.Length.EqualTo(1));
            Assert.That(shared.Select(value => value.SourceInstanceId)
                .Distinct().ToArray(), Has.Length.EqualTo(2));
            Assert.That(forward.DocumentInstances.Select(value =>
                    value.StableId).OrderBy(value => value),
                Is.EqualTo(reverse.DocumentInstances.Select(value =>
                    value.StableId).OrderBy(value => value)));
            Assert.That(forward.Components.Select(value =>
                    value.StableId).OrderBy(value => value),
                Is.EqualTo(reverse.Components.Select(value =>
                    value.StableId).OrderBy(value => value)));
        });
    }

    [Test]
    public void DiagnosesMissingInputsNamespaceMismatchesAndCycles()
    {
        AsnXsdInput alpha = Input(
            "alpha.xsd",
            "https://graph.test/alpha.xsd",
            Schema(
                "urn:alpha",
                "<xs:include schemaLocation=\"beta.xsd\"/>"
                    + "<xs:import namespace=\"urn:missing\" "
                    + "schemaLocation=\"missing.xsd\"/>"));
        AsnXsdInput beta = Input(
            "beta.xsd",
            "https://graph.test/beta.xsd",
            Schema(
                "urn:beta",
                "<xs:include schemaLocation=\"alpha.xsd\"/>"));

        XsdSchemaGraphResult graph = Resolve([alpha, beta]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Success, Is.False);
            Assert.That(graph.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNXSD0009"));
            Assert.That(graph.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNXSD0010"));
            Assert.That(graph.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNXSD0011"));
            Assert.That(graph.Edges.Any(value => value.Cycle), Is.True);
            Assert.That(graph.Diagnostics.Where(value =>
                value.Code == "ASNXSD0010"
                    || value.Code == "ASNXSD0011").All(value =>
                value.RelatedLocations.Length == 1), Is.True);
        });
    }

    [Test]
    public void DiagnosesDuplicateAndMissingRedefinedComponents()
    {
        AsnXsdInput root = Input(
            "root.xsd",
            "https://duplicates.test/root.xsd",
            Schema(
                "urn:duplicate",
                "<xs:include schemaLocation=\"one.xsd\"/>"
                    + "<xs:include schemaLocation=\"two.xsd\"/>"
                    + "<xs:redefine schemaLocation=\"base.xsd\">"
                    + "<xs:complexType name=\"Absent\"/>"
                    + "</xs:redefine>"));
        AsnXsdInput one = Input(
            "one.xsd",
            "https://duplicates.test/one.xsd",
            Schema(
                "urn:duplicate",
                "<xs:simpleType name=\"Repeated\"/>"));
        AsnXsdInput two = Input(
            "two.xsd",
            "https://duplicates.test/two.xsd",
            Schema(
                "urn:duplicate",
                "<xs:simpleType name=\"Repeated\"/>"));
        AsnXsdInput baseSchema = Input(
            "base.xsd",
            "https://duplicates.test/base.xsd",
            Schema(
                "urn:duplicate",
                "<xs:complexType name=\"Present\"/>"));

        XsdSchemaGraphResult graph = Resolve(
            [root, one, two, baseSchema]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Success, Is.False);
            Assert.That(graph.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNXSD0012"));
            Assert.That(graph.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNXSD0014"));
            Assert.That(graph.Diagnostics.Single(value =>
                value.Code == "ASNXSD0012").RelatedLocations,
                Has.Length.EqualTo(1));
            Assert.That(graph.Components.Select(value => value.StableId)
                .Distinct().ToArray(),
                Has.Length.EqualTo(graph.Components.Length));
        });
    }

    private static XsdSchemaGraphResult Resolve(AsnXsdInput[] inputs)
    {
        AsnXsdImportOptions options = new AsnXsdImportOptions();
        XsdDocumentLoadResult loaded = new XsdDocumentLoader()
            .LoadDocuments(inputs, options);
        Assert.That(loaded.Success, Is.True, Format(loaded.Diagnostics));
        return new XsdSchemaGraphResolver().ResolveGraph(loaded, options);
    }

    private static AsnXsdInput Input(
        string name,
        string baseUri,
        string text)
    {
        return Input(name, baseUri, text, true);
    }

    private static AsnXsdInput Input(
        string name,
        string baseUri,
        string text,
        bool hasBaseUri)
    {
        return new AsnXsdInput
        {
            Name = name,
            Text = text,
            HasBaseUri = hasBaseUri,
            BaseUri = baseUri
        };
    }

    private static string Schema(string targetNamespace, string content)
    {
        string target = targetNamespace.Length == 0
            ? ""
            : " targetNamespace=\"" + targetNamespace + "\"";
        return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\""
            + target + ">" + content + "</xs:schema>";
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(value =>
            value.Code + ":" + value.Message));
    }
}
