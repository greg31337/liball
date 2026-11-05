using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlRecursiveGenerationTests
{
    [Test]
    public void GeneratesSeededFiniteRecursiveTypes()
    {
        CddlSchema schema = Compile(
            "root = node\n"
            + "node = 0 / [node]"
        );
        HashSet<int> depths = new HashSet<int>();
        int seed = 0;
        while (seed < 32)
        {
            CddlGenerationOptions options = Options(
                CddlGenerationStrategy.Representative,
                seed
            );
            options.MaxNestingDepth = 4;
            CddlGenerationResult first = Generate(schema, options);
            CddlGenerationResult repeated = Generate(schema, options);
            AssertSuccessfulAndValid(schema, first);
            int depth = FindArrayDepth(first.Value!);
            depths.Add(depth);
            Assert.That(depth, Is.LessThanOrEqualTo(4));
            Assert.That(FindArrayDepth(repeated.Value!), Is.EqualTo(depth));
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(depths, Does.Contain(0));
            Assert.That(depths.Any(item => item > 0), Is.True);
        });
    }

    [Test]
    public void GeneratesMutuallyRecursiveAndGenericRules()
    {
        CddlSchema mutual = Compile(
            "root = first\n"
            + "first = second / 1\n"
            + "second = first"
        );
        CddlSchema generic = Compile(
            "root = chain<7>\n"
            + "chain<t> = t / [chain<t>]"
        );
        CddlGenerationOptions mutualOptions = Options(
            CddlGenerationStrategy.Representative,
            0
        );
        mutualOptions.MaxSteps = 20;
        CddlGenerationResult mutualResult = Generate(
            mutual,
            mutualOptions
        );
        CddlGenerationOptions genericOptions = Options(
            CddlGenerationStrategy.Boundary,
            0
        );
        genericOptions.MaxNestingDepth = 3;
        CddlGenerationResult genericResult = Generate(
            generic,
            genericOptions
        );

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(mutual, mutualResult);
            Assert.That(mutualResult.Value!.IntegerValue,
                Is.EqualTo(BigInteger.One));
            AssertSuccessfulAndValid(generic, genericResult);
            Assert.That(FindArrayDepth(genericResult.Value!),
                Is.LessThanOrEqualTo(3));
        });
    }

    [Test]
    public void GeneratesProductiveRecursiveArrayAndMapGroups()
    {
        CddlSchema array = Compile(
            "root = [items]\n"
            + "items = (1, ? items)"
        );
        CddlSchema map = Compile(
            "root = {members}\n"
            + "members = (value: 1, ? members)"
        );
        CddlGenerationOptions arrayOptions = Options(
            CddlGenerationStrategy.Boundary,
            0
        );
        arrayOptions.MaxCollectionItems = 5;
        CddlGenerationOptions mapOptions = Options(
            CddlGenerationStrategy.Representative,
            7
        );
        mapOptions.MaxCollectionItems = 6;
        CddlGenerationResult arrayResult = Generate(array, arrayOptions);
        CddlGenerationResult mapResult = Generate(map, mapOptions);

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(array, arrayResult);
            Assert.That(arrayResult.Value!.ArrayItems!.Count,
                Is.InRange(1, 5));
            AssertSuccessfulAndValid(map, mapResult);
            Assert.That(mapResult.Value!.MapEntries!.Count,
                Is.InRange(1, 6));
        });
    }

    [Test]
    public void UsesEmptyOccurrencesAsRecursiveBaseCases()
    {
        CddlSchema schema = Compile("root = [? root]");
        int seed = 0;
        while (seed < 16)
        {
            CddlGenerationOptions options = Options(
                CddlGenerationStrategy.Representative,
                seed
            );
            options.MaxNestingDepth = 3;
            CddlGenerationResult result = Generate(schema, options);
            AssertSuccessfulAndValid(schema, result);
            Assert.That(FindArrayDepth(result.Value!),
                Is.LessThanOrEqualTo(3));
            seed = seed + 1;
        }
    }

    [Test]
    public void SelectsABaseCaseBeforeNestingOrStepExhaustion()
    {
        CddlSchema nested = Compile("root = 0 / [root]");
        CddlGenerationOptions depthOptions = Options(
            CddlGenerationStrategy.Boundary,
            0
        );
        depthOptions.MaxNestingDepth = 0;
        CddlGenerationResult depthResult = Generate(
            nested,
            depthOptions
        );
        CddlSchema aliases = Compile("root = 1 / root");
        CddlGenerationOptions stepOptions = Options(
            CddlGenerationStrategy.Boundary,
            0
        );
        stepOptions.MaxSteps = 12;
        CddlGenerationResult stepResult = Generate(
            aliases,
            stepOptions
        );
        CddlSchema reversed = Compile("root = [root] / 0");
        CddlGenerationResult minimal = Generate(
            reversed,
            Options(CddlGenerationStrategy.Minimal, 0)
        );

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(nested, depthResult);
            Assert.That(depthResult.Value!.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
            AssertSuccessfulAndValid(aliases, stepResult);
            Assert.That(stepResult.Value!.IntegerValue,
                Is.EqualTo(BigInteger.One));
            AssertSuccessfulAndValid(reversed, minimal);
            Assert.That(minimal.Value!.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
        });
    }

    [Test]
    public void ReturnsClearFailureWhenNoFiniteValueExists()
    {
        CddlSchema direct = CreateDirectCycleSchema();
        CddlSchema contained = CreateContainedCycleSchema();
        CddlGenerationResult directResult = Generate(
            direct,
            Options(CddlGenerationStrategy.Representative, 0)
        );
        CddlGenerationResult containedResult = Generate(
            contained,
            Options(CddlGenerationStrategy.Minimal, 0)
        );

        Assert.Multiple(() =>
        {
            AssertFailure(
                directResult,
                "CDDL_GENERATION_NO_FINITE_VALUE"
            );
            Assert.That(directResult.Span.SourceName,
                Is.EqualTo("non-finite.cddl"));
            AssertFailure(
                containedResult,
                "CDDL_GENERATION_NO_FINITE_VALUE"
            );
        });
    }

    private static CddlSchema CreateDirectCycleSchema()
    {
        CddlNode reference = new CddlNode();
        reference.Kind = CddlNodeKind.RuleReference;
        reference.Name = "root";
        reference.Span.SourceName = "non-finite.cddl";
        reference.Span.Length = 4;
        CddlRule rule = new CddlRule();
        rule.Name = "root";
        rule.Kind = CddlRuleKind.Type;
        rule.Value = reference;
        rule.Span = reference.Span;
        CddlSchema schema = new CddlSchema();
        schema.Rules = new CddlRule[] { rule };
        schema.RootRule = rule;
        return schema;
    }

    private static CddlSchema CreateContainedCycleSchema()
    {
        CddlNode reference = new CddlNode();
        reference.Kind = CddlNodeKind.RuleReference;
        reference.Name = "root";
        CddlNode entry = new CddlNode();
        entry.Kind = CddlNodeKind.GroupEntry;
        entry.Children.Add(reference);
        CddlNode group = new CddlNode();
        group.Kind = CddlNodeKind.Group;
        group.Children.Add(entry);
        CddlNode array = new CddlNode();
        array.Kind = CddlNodeKind.Array;
        array.Children.Add(group);
        CddlRule rule = new CddlRule();
        rule.Name = "root";
        rule.Kind = CddlRuleKind.Type;
        rule.Value = array;
        CddlSchema schema = new CddlSchema();
        schema.Rules = new CddlRule[] { rule };
        schema.RootRule = rule;
        return schema;
    }

    private static int FindArrayDepth(CborValue value)
    {
        int depth = 0;
        CborValue current = value;
        while (current.Kind == CborValueKind.Array
            && current.ArrayItems != null
            && current.ArrayItems.Count == 1)
        {
            depth = depth + 1;
            current = current.ArrayItems[0];
        }
        return depth;
    }

    private static void AssertSuccessfulAndValid(
        CddlSchema schema,
        CddlGenerationResult result)
    {
        Assert.That(
            result.IsSuccess,
            Is.True,
            result.ErrorMessage ?? "generation failed"
        );
        Assert.That(result.Value, Is.Not.Null);
        CddlValidationResult validation = new CddlValidator().ValidateCbor(
            schema,
            result.Value!,
            new CddlValidationOptions
            {
                RootRuleName = result.RuleName
            }
        );
        Assert.That(
            validation.IsValid,
            Is.True,
            validation.Issues.Length == 0
                ? "generated value did not validate"
                : validation.Issues[0].Message
        );
    }

    private static void AssertFailure(
        CddlGenerationResult result,
        string expectedCode)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    private static CddlGenerationResult Generate(
        CddlSchema schema,
        CddlGenerationOptions options)
    {
        return new CddlValueGenerator().GenerateValue(schema, options);
    }

    private static CddlGenerationOptions Options(
        CddlGenerationStrategy strategy,
        int seed)
    {
        return new CddlGenerationOptions
        {
            Strategy = strategy,
            Seed = seed
        };
    }

    private static CddlSchema Compile(string source)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "recursive-generation.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions()
        );
    }
}
