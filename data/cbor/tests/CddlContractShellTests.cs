using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlContractShellTests
{
    private sealed class EmptySchemaSink : ICddlSchemaSink
    {
        public void BeginSchema(CddlEmissionContext context)
        {
        }

        public void AcceptRule(CddlEmissionContext context)
        {
        }

        public void AcceptNode(CddlEmissionContext context)
        {
        }

        public void EndSchema(CddlEmissionContext context)
        {
        }
    }

    [Test]
    public void EveryCddlOperationShellCanBeConstructedThroughItsContract()
    {
        ICddlParser parser = new CddlParser();
        ICddlCompiler compiler = new CddlCompiler();
        ICddlValidator validator = new CddlValidator();
        ICddlJsonValidator jsonValidator = new CddlJsonValidator();
        ICddlControlRegistry registry = new CddlControlRegistry();
        ICddlValueGenerator generator = new CddlValueGenerator();
        ICddlSchemaEmitter emitter = new CddlSchemaEmitter();

        Assert.Multiple(() =>
        {
            Assert.That(parser, Is.Not.Null);
            Assert.That(compiler, Is.Not.Null);
            Assert.That(validator, Is.Not.Null);
            Assert.That(jsonValidator, Is.Not.Null);
            Assert.That(registry, Is.Not.Null);
            Assert.That(generator, Is.Not.Null);
            Assert.That(emitter, Is.Not.Null);
        });
    }

    [Test]
    public void ParsingAndCompilationWorkThroughTheirContracts()
    {
        ICddlParser parser = new CddlParser();
        ICddlCompiler compiler = new CddlCompiler();
        CddlParseOptions parseOptions = new CddlParseOptions();
        CddlCompileOptions compileOptions = new CddlCompileOptions();
        CddlDocument parsed = parser.ParseDocument("root = uint", parseOptions);
        CddlParseResult parsedResult = parser.TryParseDocument(
            "root = uint",
            parseOptions);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.RootRuleName, Is.EqualTo("root"));
            Assert.That(parsedResult.IsSuccess, Is.True);
            Assert.That(
                compiler.CompileSchema(parsed, compileOptions).RootRule!.Name,
                Is.EqualTo("root"));
            Assert.That(
                compiler.TryCompileSchema(parsed, compileOptions).IsSuccess,
                Is.True);
        });
    }

    [Test]
    public void RemainingOperationShellsFailPredictably()
    {
        ICddlValidator validator = new CddlValidator();
        ICddlJsonValidator jsonValidator = new CddlJsonValidator();
        ICddlControlRegistry registry = new CddlControlRegistry();
        ICddlValueGenerator generator = new CddlValueGenerator();
        ICddlSchemaEmitter emitter = new CddlSchemaEmitter();
        CddlSchema schema = new CddlSchema();
        CborValue value = new CborValue();
        CddlValidationOptions validationOptions = new CddlValidationOptions();
        CddlGenerationOptions generationOptions = new CddlGenerationOptions();
        CddlEmissionOptions emissionOptions = new CddlEmissionOptions();
        CddlControlRegistration registration = new CddlControlRegistration();
        ICddlSchemaSink sink = new EmptySchemaSink();
        CddlValidationResult validation = validator.ValidateCbor(
            schema,
            value,
            validationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
            Assert.That(
                jsonValidator.ValidateJson(schema, "null", validationOptions)
                    .IsValid,
                Is.False);
            Assert.Throws<ArgumentException>(() =>
                registry.AddControl(registration));
            Assert.That(registry.FindControl(".example"), Is.Null);
            Assert.That(
                generator.GenerateValue(schema, generationOptions).IsSuccess,
                Is.False);
            Assert.DoesNotThrow(() =>
                emitter.EmitSchema(schema, sink, emissionOptions));
        });
    }
}
