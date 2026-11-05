using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlCollectionGenerationTests
{
    [Test]
    public void GeneratesNestedArraysMapsAndArbitraryKeys()
    {
        CddlSchema schema = Compile(
            "root = [1, [2], {id: uint, 0 => bool}]"
        );
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0)
        );

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, result);
            Assert.That(result.Value!.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(result.Value.ArrayItems, Has.Count.EqualTo(3));
            Assert.That(result.Value.ArrayItems![1].ArrayItems,
                Has.Count.EqualTo(1));
            Assert.That(result.Value.ArrayItems[2].MapEntries,
                Has.Count.EqualTo(2));
            Assert.That(result.Value.ArrayItems[2].MapEntries![1].Key!
                .IntegerValue, Is.EqualTo(BigInteger.Zero));
        });
    }

    [Test]
    public void AppliesOccurrencesUsingBoundedSeededStrategies()
    {
        CddlSchema schema = Compile("root = [? 1, 2*4 2]");
        CddlGenerationResult minimal = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0)
        );
        CddlGenerationResult first = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 37)
        );
        CddlGenerationResult repeated = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 37)
        );
        HashSet<int> lengths = new HashSet<int>();
        int seed = 0;
        while (seed < 20)
        {
            CddlGenerationResult sample = Generate(
                schema,
                Options(CddlGenerationStrategy.Boundary, seed)
            );
            AssertSuccessfulAndValid(schema, sample);
            lengths.Add(sample.Value!.ArrayItems!.Count);
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, minimal);
            AssertSuccessfulAndValid(schema, first);
            Assert.That(minimal.Value!.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(repeated.Value!.ArrayItems!.Count,
                Is.EqualTo(first.Value!.ArrayItems!.Count));
            Assert.That(lengths.Count, Is.GreaterThan(1));
        });
    }

    [Test]
    public void SelectsOrderedGroupChoicesDeterministically()
    {
        CddlSchema schema = Compile("root = [1, 2 // 3 //]");
        HashSet<int> lengths = new HashSet<int>();
        int seed = 0;
        while (seed < 18)
        {
            CddlGenerationResult first = Generate(
                schema,
                Options(CddlGenerationStrategy.Representative, seed)
            );
            CddlGenerationResult repeated = Generate(
                schema,
                Options(CddlGenerationStrategy.Representative, seed)
            );
            AssertSuccessfulAndValid(schema, first);
            lengths.Add(first.Value!.ArrayItems!.Count);
            Assert.That(repeated.Value!.ArrayItems!.Count,
                Is.EqualTo(first.Value.ArrayItems.Count));
            seed = seed + 1;
        }

        Assert.That(lengths, Is.EquivalentTo(new int[] { 0, 1, 2 }));
    }

    [Test]
    public void GeneratesIncludedGenericAndUnwrappedArrayGroups()
    {
        CddlSchema schema = Compile(
            "root = [0, header, pair<2>, ~base, 5]\n"
            + "header = (1, ? tstr)\n"
            + "pair<t> = (t, t)\n"
            + "base = [3, ? 4]"
        );
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 13)
        );

        AssertSuccessfulAndValid(schema, result);
        Assert.That(result.Value!.ArrayItems!.Count,
            Is.InRange(7, 9));
    }

    [Test]
    public void GeneratesNamedArbitraryGenericAndCutMapGroups()
    {
        CddlSchema generic = Compile(
            "root = {id: uint, pair<1, 2>, 2*3 tstr => uint}\n"
            + "pair<a, b> = (first: a, second: b)"
        );
        CddlGenerationResult genericResult = Generate(
            generic,
            Options(CddlGenerationStrategy.Minimal, 0)
        );
        CddlSchema cut = Compile(
            "root = {#0 ^ => #1 // \"x\" => #3}"
        );
        int seed = 0;
        while (seed < 12)
        {
            CddlGenerationResult sample = Generate(
                cut,
                Options(CddlGenerationStrategy.Representative, seed)
            );
            AssertSuccessfulAndValid(cut, sample);
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(generic, genericResult);
            Assert.That(genericResult.Value!.MapEntries,
                Has.Count.EqualTo(5));
        });
    }

    [Test]
    public void GeneratesMergedTypeAndGroupExtensions()
    {
        CddlSchema typeSchema = Compile(
            "root = choice\n"
            + "choice = 1\n"
            + "choice /= [2]"
        );
        CddlSchema groupSchema = Compile(
            "root = {fields}\n"
            + "fields = (first: 1)\n"
            + "fields //= (second: 2)"
        );
        HashSet<CborValueKind> kinds = new HashSet<CborValueKind>();
        HashSet<string> keys = new HashSet<string>();
        int seed = 0;
        while (seed < 16)
        {
            CddlGenerationResult typeResult = Generate(
                typeSchema,
                Options(CddlGenerationStrategy.Representative, seed)
            );
            CddlGenerationResult groupResult = Generate(
                groupSchema,
                Options(CddlGenerationStrategy.Representative, seed)
            );
            AssertSuccessfulAndValid(typeSchema, typeResult);
            AssertSuccessfulAndValid(groupSchema, groupResult);
            kinds.Add(typeResult.Value!.Kind);
            keys.Add(groupResult.Value!.MapEntries![0].Key!
                .TextStringValue!);
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(kinds, Does.Contain(CborValueKind.UnsignedInteger));
            Assert.That(kinds, Does.Contain(CborValueKind.Array));
            Assert.That(keys, Is.EquivalentTo(
                new string[] { "first", "second" }));
        });
    }

    [Test]
    public void GeneratesNamedAndInlineGroupEnumerations()
    {
        CddlSchema named = Compile(
            "root = &colors\n"
            + "colors = (black: 0, red: 1, extra)\n"
            + "extra = (blue: 2,)"
        );
        CddlSchema inline = Compile("root = &(green: 3, yellow: 4)");
        HashSet<BigInteger> namedValues = new HashSet<BigInteger>();
        int seed = 0;
        while (seed < 18)
        {
            CddlGenerationResult sample = Generate(
                named,
                Options(CddlGenerationStrategy.Representative, seed)
            );
            AssertSuccessfulAndValid(named, sample);
            namedValues.Add(sample.Value!.IntegerValue);
            seed = seed + 1;
        }
        CddlGenerationResult inlineResult = Generate(
            inline,
            Options(CddlGenerationStrategy.Boundary, 2)
        );

        Assert.Multiple(() =>
        {
            Assert.That(namedValues, Is.EquivalentTo(
                new BigInteger[]
                {
                    BigInteger.Zero,
                    BigInteger.One,
                    new BigInteger(2)
                }));
            AssertSuccessfulAndValid(inline, inlineResult);
        });
    }

    [Test]
    public void GeneratesExplicitContainerRepresentationsAndArrayPreludes()
    {
        AssertRepresentation("root = #4.3", CborValueKind.Array, 3,
            CborLengthKind.Definite);
        AssertRepresentation("root = #5.2", CborValueKind.Map, 2,
            CborLengthKind.Definite);
        AssertRepresentation("root = #4.31", CborValueKind.Array, 0,
            CborLengthKind.Indefinite);
        AssertGeneratedAndValid("root = decfrac");
        AssertGeneratedAndValid("root = bigfloat");
    }

    [Test]
    public void EnforcesCollectionAndNestingLimitsWithStructuredFailures()
    {
        CddlGenerationOptions collectionOptions = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        collectionOptions.MaxCollectionItems = 2;
        CddlGenerationResult collection = Generate(
            Compile("root = [3*3 1]"),
            collectionOptions
        );
        CddlGenerationOptions depthOptions = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        depthOptions.MaxNestingDepth = 2;
        CddlGenerationResult depth = Generate(
            Compile("root = [[[1]]]"),
            depthOptions
        );

        Assert.Multiple(() =>
        {
            AssertFailure(collection, "CDDL_GENERATION_COLLECTION_LIMIT");
            AssertFailure(depth, "CDDL_GENERATION_DEPTH_LIMIT");
        });
    }

    [Test]
    public void BoundsUnboundedIncludedGroupsAtWholeOccurrences()
    {
        CddlSchema schema = Compile(
            "root = [* pair]\n"
            + "pair = (1, 2)"
        );
        CddlGenerationOptions options = Options(
            CddlGenerationStrategy.Boundary,
            0
        );
        options.MaxCollectionItems = 5;
        CddlGenerationResult result = Generate(schema, options);

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, result);
            Assert.That(result.Value!.ArrayItems, Has.Count.EqualTo(4));
        });
    }

    private static void AssertRepresentation(
        string source,
        CborValueKind kind,
        int count,
        CborLengthKind lengthKind)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0)
        );
        AssertSuccessfulAndValid(schema, result);
        Assert.That(result.Value!.Kind, Is.EqualTo(kind));
        Assert.That(result.Value.LengthKind, Is.EqualTo(lengthKind));
        int actual = kind == CborValueKind.Array
            ? result.Value.ArrayItems!.Count
            : result.Value.MapEntries!.Count;
        Assert.That(actual, Is.EqualTo(count));
    }

    private static void AssertGeneratedAndValid(string source)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0)
        );
        AssertSuccessfulAndValid(schema, result);
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
                SourceName = "collection-generation.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions()
        );
    }
}
