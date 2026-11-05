using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class Draft4SchemaValidatorTests
{
    [Test]
    public void ValidateJson_ValidatesTheEmbeddedGltfSchemaThroughReferencesAndAllOf()
    {
        Draft4SchemaValidator validator = new Draft4SchemaValidator();
        JsonNode valid = JsonNode.Parse("{\"asset\":{\"version\":\"2.0\"}}")!;
        JsonNode invalid = JsonNode.Parse(
            "{\"asset\":{\"version\":\"two\"},\"buffers\":[{\"byteLength\":0}]}")!;

        Draft4SchemaValidation validResult = validator.ValidateJson(valid, "glTF.schema.json");
        Draft4SchemaValidation invalidResult = validator.ValidateJson(invalid, "glTF.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(validResult.IsValid, Is.True);
            Assert.That(validResult.Failures, Is.Empty);
            Assert.That(invalidResult.IsValid, Is.False);
            Assert.That(invalidResult.Failures.Any(
                failure => failure.Keyword == "pattern"
                    && failure.InstancePointer == "/asset/version"), Is.True);
            Assert.That(invalidResult.Failures.Any(
                failure => failure.Keyword == "minimum"
                    && failure.InstancePointer == "/buffers/0/byteLength"), Is.True);
        });
    }

    [Test]
    public void ValidateJson_ReportsRequiredTypeDependenciesAndAdditionalProperties()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "object.schema.json",
            """
            {
              "type":"object",
              "minProperties":2,
              "maxProperties":4,
              "required":["name"],
              "properties":{"name":{"type":"string"},"enabled":{"type":"boolean"}},
              "patternProperties":{"^x-":{"type":"integer"}},
              "dependencies":{"enabled":["name"]},
              "additionalProperties":false
            }
            """);
        JsonNode valid = JsonNode.Parse("{\"name\":\"ok\",\"x-count\":2}")!;
        JsonNode invalid = JsonNode.Parse(
            "{\"enabled\":true,\"x-count\":2.5,\"extra\":0,\"more\":0,\"last\":0}")!;

        Draft4SchemaValidation validResult = validator.ValidateJson(valid, "object.schema.json");
        Draft4SchemaValidation invalidResult = validator.ValidateJson(invalid, "object.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(validResult.IsValid, Is.True);
            AssertFailure(invalidResult, "required", "/name");
            AssertFailure(invalidResult, "dependencies", "/name");
            AssertFailure(invalidResult, "type", "/x-count");
            AssertFailure(invalidResult, "additionalProperties", "/extra");
            AssertFailure(invalidResult, "maxProperties", "");
        });
    }

    [Test]
    public void ValidateJson_AppliesSchemaDependenciesAndPropertyNameConstraints()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "dependency.schema.json",
            """
            {
              "type":"object",
              "propertyNames":{"pattern":"^[a-z]+$"},
              "dependencies":{
                "credit":{"required":["billing"],"properties":{"billing":{"type":"string"}}}
              }
            }
            """);
        JsonNode invalid = JsonNode.Parse(
            "{\"credit\":true,\"Billing\":3,\"billing\":4}")!;

        Draft4SchemaValidation result = validator.ValidateJson(
            invalid,
            "dependency.schema.json");

        Assert.Multiple(() =>
        {
            AssertFailure(result, "pattern", "/Billing");
            AssertFailure(result, "type", "/billing");
        });
    }

    [Test]
    public void ValidateJson_AppliesDependentRequiredAndConditionalBranches()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "conditional.schema.json",
            """
            {
              "type":"object",
              "dependentRequired":{"trigger":["companion"]},
              "if":{"properties":{"mode":{"const":"strict"}},"required":["mode"]},
              "then":{"required":["value"]},
              "else":{"properties":{"value":{"type":"string"}}}
            }
            """);

        Draft4SchemaValidation strict = validator.ValidateJson(
            JsonNode.Parse("{\"trigger\":true,\"mode\":\"strict\"}")!,
            "conditional.schema.json");
        Draft4SchemaValidation relaxed = validator.ValidateJson(
            JsonNode.Parse("{\"mode\":\"relaxed\",\"value\":4}")!,
            "conditional.schema.json");
        Draft4SchemaValidation valid = validator.ValidateJson(
            JsonNode.Parse("{\"trigger\":true,\"companion\":false,\"mode\":\"strict\",\"value\":4}")!,
            "conditional.schema.json");

        Assert.Multiple(() =>
        {
            AssertFailure(strict, "dependentRequired", "/companion");
            AssertFailure(strict, "required", "/value");
            AssertFailure(relaxed, "type", "/value");
            Assert.That(valid.IsValid, Is.True);
        });
    }

    [Test]
    public void ValidateJson_EvaluatesAllCompositionKeywords()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "composition.schema.json",
            """
            {
              "allOf":[{"type":"integer"},{"minimum":1}],
              "anyOf":[{"maximum":3},{"minimum":8}],
              "oneOf":[{"multipleOf":2},{"multipleOf":3}],
              "not":{"const":12}
            }
            """);

        Draft4SchemaValidation valid = validator.ValidateJson(
            JsonValue.Create(2),
            "composition.schema.json");
        Draft4SchemaValidation overlap = validator.ValidateJson(
            JsonValue.Create(6),
            "composition.schema.json");
        Draft4SchemaValidation forbidden = validator.ValidateJson(
            JsonValue.Create(12),
            "composition.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            AssertFailure(overlap, "anyOf", "");
            AssertFailure(overlap, "oneOf", "");
            AssertFailure(forbidden, "oneOf", "");
            AssertFailure(forbidden, "not", "");
        });
    }

    [Test]
    public void ValidateJson_AppliesNumericAndUnicodeStringConstraints()
    {
        Draft4SchemaValidator numberValidator = CreateValidator(
            "number.schema.json",
            """
            {"type":"number","multipleOf":0.25,"minimum":0,"exclusiveMinimum":true,"maximum":2}
            """);
        Draft4SchemaValidator stringValidator = CreateValidator(
            "string.schema.json",
            """
            {"type":"string","minLength":2,"maxLength":3,"pattern":"^[A-Z]"}
            """);

        Draft4SchemaValidation zero = numberValidator.ValidateJson(
            JsonValue.Create(0.0),
            "number.schema.json");
        Draft4SchemaValidation fraction = numberValidator.ValidateJson(
            JsonValue.Create(0.3),
            "number.schema.json");
        Draft4SchemaValidation maximum = numberValidator.ValidateJson(
            JsonValue.Create(2.25),
            "number.schema.json");
        Draft4SchemaValidation unicodeValid = stringValidator.ValidateJson(
            JsonValue.Create("A😀"),
            "string.schema.json");
        Draft4SchemaValidation stringInvalid = stringValidator.ValidateJson(
            JsonValue.Create("abcd"),
            "string.schema.json");

        Assert.Multiple(() =>
        {
            AssertFailure(zero, "exclusiveMinimum", "");
            AssertFailure(fraction, "multipleOf", "");
            AssertFailure(maximum, "maximum", "");
            Assert.That(unicodeValid.IsValid, Is.True);
            AssertFailure(stringInvalid, "maxLength", "");
            AssertFailure(stringInvalid, "pattern", "");
        });
    }

    [Test]
    public void ValidateJson_AppliesArrayItemsTuplesUniquenessAndLimits()
    {
        Draft4SchemaValidator listValidator = CreateValidator(
            "list.schema.json",
            """
            {"type":"array","minItems":2,"maxItems":3,"uniqueItems":true,"items":{"type":"integer"}}
            """);
        Draft4SchemaValidator tupleValidator = CreateValidator(
            "tuple.schema.json",
            """
            {"type":"array","items":[{"type":"string"},{"type":"boolean"}],"additionalItems":false}
            """);

        Draft4SchemaValidation list = listValidator.ValidateJson(
            JsonNode.Parse("[1,1,2.5,4]")!,
            "list.schema.json");
        Draft4SchemaValidation tuple = tupleValidator.ValidateJson(
            JsonNode.Parse("[\"ok\",false,3]")!,
            "tuple.schema.json");

        Assert.Multiple(() =>
        {
            AssertFailure(list, "maxItems", "");
            AssertFailure(list, "uniqueItems", "/1");
            AssertFailure(list, "type", "/2");
            AssertFailure(tuple, "additionalItems", "/2");
        });
    }

    [Test]
    public void ValidateJson_AcceptsArrayLimitsBeyondInt32Range()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "large-array.schema.json",
            "{\"type\":\"array\",\"maxItems\":2147483648}");

        Draft4SchemaValidation result = validator.ValidateJson(
            new JsonArray(),
            "large-array.schema.json");

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateJson_ResolvesExternalAndEscapedLocalReferences()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "shared.schema.json",
            """
            {"definitions":{"positive/value":{"type":"integer","minimum":1}}}
            """);
        validator.RegisterSchemaDocument(
            "entry.schema.json",
            ParseObject(
                "{\"properties\":{\"external\":{\"$ref\":\"shared.schema.json#/definitions/positive~1value\"},"
                + "\"local\":{\"$ref\":\"#/definitions/flag\"}},\"definitions\":{\"flag\":{\"const\":true}}}"));
        JsonNode invalid = JsonNode.Parse("{\"external\":0,\"local\":false}")!;

        Draft4SchemaValidation result = validator.ValidateJson(invalid, "entry.schema.json");

        Assert.Multiple(() =>
        {
            AssertFailure(result, "minimum", "/external");
            AssertFailure(result, "const", "/local");
            Assert.That(result.Failures.All(failure => failure.Keyword != "$ref"), Is.True);
        });
    }

    [Test]
    public void ValidateJson_UsesDeepNumericEqualityForEnumConstAndUniqueItems()
    {
        Draft4SchemaValidator validator = CreateValidator(
            "equality.schema.json",
            """
            {
              "type":"object",
              "properties":{
                "choice":{"enum":[1,{"a":[2]}]},
                "fixed":{"const":{"x":1}},
                "values":{"type":"array","uniqueItems":true}
              }
            }
            """);
        JsonNode valid = JsonNode.Parse(
            "{\"choice\":1.0,\"fixed\":{\"x\":1.0},\"values\":[1,{\"x\":1}]}")!;
        JsonNode invalid = JsonNode.Parse(
            "{\"choice\":2,\"fixed\":{\"x\":2},\"values\":[1,1.0]}")!;

        Draft4SchemaValidation validResult = validator.ValidateJson(valid, "equality.schema.json");
        Draft4SchemaValidation invalidResult = validator.ValidateJson(invalid, "equality.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(validResult.IsValid, Is.True);
            AssertFailure(invalidResult, "enum", "/choice");
            AssertFailure(invalidResult, "const", "/fixed");
            AssertFailure(invalidResult, "uniqueItems", "/values/1");
        });
    }

    [Test]
    public void Validator_RejectsUnknownSchemasAndInvalidRegistrationArguments()
    {
        Draft4SchemaValidator validator = new Draft4SchemaValidator();

        Assert.Multiple(() =>
        {
            Assert.That(() => validator.ValidateJson(new JsonObject(), "missing.schema.json"),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("schemaName"));
            Assert.That(() => validator.ValidateJson(new JsonObject(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("schemaName"));
            Assert.That(() => validator.RegisterSchemaDocument(null!, new JsonObject()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("name"));
            Assert.That(() => validator.RegisterSchemaDocument("schema.json", null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("schema"));
        });
    }

    private static Draft4SchemaValidator CreateValidator(string name, string schema)
    {
        Draft4SchemaValidator validator = new Draft4SchemaValidator();
        validator.RegisterSchemaDocument(name, ParseObject(schema));
        return validator;
    }

    private static JsonObject ParseObject(string json)
    {
        return JsonNode.Parse(json)!.AsObject();
    }

    private static void AssertFailure(
        Draft4SchemaValidation result,
        string keyword,
        string instancePointer)
    {
        Assert.That(
            result.Failures.Any(
                failure => failure.Keyword == keyword
                    && failure.InstancePointer == instancePointer),
            Is.True,
            $"Expected {keyword} at '{instancePointer}', got: "
                + string.Join(", ", result.Failures.Select(
                    failure => $"{failure.Keyword}@{failure.InstancePointer}")));
    }
}
