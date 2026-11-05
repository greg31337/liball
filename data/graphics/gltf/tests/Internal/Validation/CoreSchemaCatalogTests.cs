using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class CoreSchemaCatalogTests
{
    [Test]
    public void Catalog_LoadsTheCompleteDraftFourCoreSchemaSet()
    {
        CoreSchemaCatalog catalog = new CoreSchemaCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.SchemaNames, Has.Count.EqualTo(33));
            Assert.That(catalog.SchemaNames,
                Is.EqualTo(catalog.SchemaNames.OrderBy(name => name, StringComparer.Ordinal)));
            Assert.That(catalog.SchemaNames, Does.Contain("glTF.schema.json"));
            Assert.That(catalog.SchemaNames, Does.Contain("accessor.sparse.indices.schema.json"));
            Assert.That(catalog.SchemaNames, Does.Contain("material.pbrMetallicRoughness.schema.json"));
            Assert.That(CoreSchemaArchive.Dialect,
                Is.EqualTo("http://json-schema.org/draft-04/schema"));
            Assert.That(CoreSchemaArchive.SourceRevision,
                Is.EqualTo("94ed6813112aa2286eb342fd9d493ba872975bc8"));
        });

        foreach (string name in catalog.SchemaNames)
        {
            JsonObject schema = catalog.GetCoreSchema(name);
            Assert.That(schema["$schema"]!.GetValue<string>(),
                Is.EqualTo(CoreSchemaArchive.Dialect),
                name);
        }
    }

    [Test]
    public void Catalog_PreservesNormativeRootAndAccessorConstraints()
    {
        CoreSchemaCatalog catalog = new CoreSchemaCatalog();
        JsonObject root = catalog.GetCoreSchema("glTF.schema.json");
        JsonObject accessor = catalog.GetCoreSchema("accessor.schema.json");

        JsonArray rootRequired = root["required"]!.AsArray();
        JsonArray accessorRequired = accessor["required"]!.AsArray();
        JsonArray componentTypeChoices = accessor["properties"]!["componentType"]!["anyOf"]!.AsArray();

        Assert.Multiple(() =>
        {
            Assert.That(root["title"]!.GetValue<string>(), Is.EqualTo("glTF"));
            Assert.That(rootRequired.Select(value => value!.GetValue<string>()),
                Is.EqualTo(new[] { "asset" }));
            Assert.That(accessorRequired.Select(value => value!.GetValue<string>()),
                Is.EqualTo(new[] { "componentType", "count", "type" }));
            Assert.That(componentTypeChoices.Take(6).Select(
                    value => value!["enum"]![0]!.GetValue<int>()),
                Is.EqualTo(new[] { 5120, 5121, 5122, 5123, 5125, 5126 }));
        });
    }

    [Test]
    public void Catalog_ResolvesEveryCoreReferenceWithoutExternalInput()
    {
        CoreSchemaCatalog catalog = new CoreSchemaCatalog();
        int referenceCount = 0;

        foreach (string sourceName in catalog.SchemaNames)
        {
            List<string> references = new List<string>();
            CollectReferences(catalog.GetCoreSchema(sourceName), references);
            foreach (string reference in references)
            {
                JsonObject resolved = catalog.ResolveCoreSchemaReference(sourceName, reference);
                Assert.That(resolved, Is.Not.Null, $"{sourceName}: {reference}");
                referenceCount++;
            }
        }

        Assert.That(referenceCount, Is.GreaterThan(80));
    }

    [Test]
    public void Catalog_CachesDocumentsAndNormalizesReferenceIdentifiersLocally()
    {
        CoreSchemaCatalog firstCatalog = new CoreSchemaCatalog();
        CoreSchemaCatalog secondCatalog = new CoreSchemaCatalog();
        JsonObject direct = firstCatalog.GetCoreSchema("node.schema.json");
        JsonObject repeated = firstCatalog.GetCoreSchema("node.schema.json");
        JsonObject fromUrl = firstCatalog.GetCoreSchema(
            "https://example.invalid/specification/2.0/schema/node.schema.json#ignored");
        JsonObject localFragment = firstCatalog.ResolveCoreSchemaReference(
            "node.schema.json",
            "#/properties/name");

        Assert.Multiple(() =>
        {
            Assert.That(repeated, Is.SameAs(direct));
            Assert.That(fromUrl, Is.SameAs(direct));
            Assert.That(localFragment, Is.SameAs(direct));
            Assert.That(secondCatalog.GetCoreSchema("node.schema.json"), Is.Not.SameAs(direct));
        });
    }

    [Test]
    public void Catalog_RejectsMissingAndNullIdentifiers()
    {
        CoreSchemaCatalog catalog = new CoreSchemaCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(() => catalog.GetCoreSchema("missing.schema.json"),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("name"));
            Assert.That(() => catalog.GetCoreSchema(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("name"));
            Assert.That(() => catalog.ResolveCoreSchemaReference(null!, "node.schema.json"),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("sourceSchemaName"));
            Assert.That(() => catalog.ResolveCoreSchemaReference("node.schema.json", null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("reference"));
        });
    }

    private static void CollectReferences(JsonNode? node, List<string> references)
    {
        if (node == null)
        {
            return;
        }

        if (node.GetValueKind() == JsonValueKind.Object)
        {
            foreach (KeyValuePair<string, JsonNode?> member in node.AsObject())
            {
                if (member.Key == "$ref" && member.Value != null)
                {
                    references.Add(member.Value.GetValue<string>());
                }
                else
                {
                    CollectReferences(member.Value, references);
                }
            }
        }
        else if (node.GetValueKind() == JsonValueKind.Array)
        {
            foreach (JsonNode? item in node.AsArray())
            {
                CollectReferences(item, references);
            }
        }
    }
}
