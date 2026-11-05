using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class ExtensionSchemaCatalogTests
{
    [Test]
    public void Catalog_ContainsEverySchemaDeclaredByTheRegistry()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        ExtensionSchemaCatalog catalog = new ExtensionSchemaCatalog();
        List<GltfExtensionDescriptor> descriptors = registry.ListDescriptors();
        int schemaCount = 0;

        foreach (GltfExtensionDescriptor descriptor in descriptors)
        {
            foreach (string schemaName in descriptor.SchemaResources)
            {
                Assert.That(
                    catalog.GetSchemaNames(descriptor.Name),
                    Does.Contain(schemaName),
                    $"Missing {descriptor.Name}/{schemaName}.");
                Assert.That(
                    catalog.GetExtensionSchema(descriptor.Name, schemaName),
                    Is.Not.Null);
                schemaCount++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                ExtensionSchemaArchive.SourceRevision,
                Is.EqualTo("fdb8ce0e2e0b7ecf3466f8dacb9f1385257b8276"));
            Assert.That(catalog.ExtensionNames, Has.Count.EqualTo(59));
            Assert.That(schemaCount, Is.EqualTo(109));
        });
    }

    [Test]
    public void Catalog_IsolatesSameNamedSchemasByExtensionFamily()
    {
        ExtensionSchemaCatalog catalog = new ExtensionSchemaCatalog();
        JsonObject punctual = catalog.GetExtensionSchema(
            GltfExtensionNames.KhrLightsPunctual,
            "light.schema.json");
        JsonObject imageBased = catalog.GetExtensionSchema(
            GltfExtensionNames.ExtLightsImageBased,
            "light.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(
                punctual["title"]!.GetValue<string>(),
                Is.Not.EqualTo(imageBased["title"]!.GetValue<string>()));
            Assert.That(punctual["properties"]!["type"], Is.Not.Null);
            Assert.That(imageBased["properties"]!["specularImages"], Is.Not.Null);
        });
    }

    [Test]
    public void Validator_SelectsAnEntrySchemaForEveryDeclaredSchemaLocation()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        ExtensionSchemaValidator validator = new ExtensionSchemaValidator();

        foreach (GltfExtensionDescriptor descriptor in registry.ListDescriptors())
        {
            if (descriptor.SchemaResources.Count == 0)
            {
                continue;
            }

            foreach (string location in descriptor.ObjectLocations)
            {
                Assert.That(
                    validator.FindEntrySchemaName(descriptor, location),
                    Is.Not.Null,
                    $"No entry schema for {descriptor.Name} at {location}.");
            }
        }
    }

    [Test]
    public void Validator_ResolvesHelpersWithinTheCorrectCollidingFamily()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        ExtensionSchemaValidator validator = new ExtensionSchemaValidator();
        GltfExtensionDescriptor punctual = registry.FindDescriptor(
            GltfExtensionNames.KhrLightsPunctual)!;
        GltfExtensionDescriptor imageBased = registry.FindDescriptor(
            GltfExtensionNames.ExtLightsImageBased)!;

        Draft4SchemaValidation punctualResult = validator.CreateFamilyValidator(punctual)
            .ValidateJson(
                JsonNode.Parse("{\"lights\":[{\"type\":\"point\"}]}")!,
                "glTF.KHR_lights_punctual.schema.json");
        Draft4SchemaValidation imageBasedResult = validator.CreateFamilyValidator(imageBased)
            .ValidateJson(
                JsonNode.Parse("{\"lights\":[{}]}")!,
                "glTF.EXT_lights_image_based.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(punctualResult.IsValid, Is.True);
            Assert.That(imageBasedResult.IsValid, Is.False);
            Assert.That(imageBasedResult.Failures.Any(failure =>
                failure.Keyword == "required"
                && failure.InstancePointer.StartsWith("/lights/0/", StringComparison.Ordinal)),
                Is.True);
            Assert.That(imageBasedResult.Failures.All(failure => failure.Keyword != "$ref"),
                Is.True);
        });
    }
}
