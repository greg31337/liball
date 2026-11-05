using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborBoundedPropertyTests
{
    private const int ArbitrarySeedCount = 128;
    private const int JsonSeedCount = 128;
    private const int SchemaSeedCount = 48;

    [Test]
    public void BoundedValuesRoundTripThroughBinaryAndDiagnosticNotation()
    {
        int seed = 0;
        while (seed < ArbitrarySeedCount)
        {
            CborValue original = new BoundedValueGenerator(seed).Generate();
            byte[] encoded = new CborEncoder().Encode(
                original,
                new CborEncodeOptions { ReduceFloatWidth = false });
            CborReadResult decoded = new CborDecoder().Decode(
                encoded,
                DecodeOptions());

            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done),
                Failure("binary decode", seed, decoded.ErrorMessage));
            Assert.That(decoded.Value, Is.Not.Null,
                Failure("binary value", seed, decoded.ErrorMessage));
            Assert.That(CanonicalBytes(decoded.Value!),
                Is.EqualTo(CanonicalBytes(original)),
                Failure("binary round trip", seed, null));

            CborDiagnosticConverter diagnostic = new CborDiagnosticConverter();
            string text = diagnostic.FormatDiagnostic(
                original,
                new CborDiagnosticOptions());
            CborReadResult parsed = diagnostic.ParseDiagnostic(
                text,
                new CborDiagnosticOptions());

            Assert.That(parsed.Status, Is.EqualTo(CborReadStatus.Done),
                Failure("diagnostic parse", seed, parsed.ErrorMessage));
            Assert.That(parsed.Value, Is.Not.Null,
                Failure("diagnostic value", seed, parsed.ErrorMessage));
            Assert.That(CanonicalBytes(parsed.Value!),
                Is.EqualTo(CanonicalBytes(original)),
                Failure("diagnostic round trip", seed, text));
            seed = seed + 1;
        }
    }

    [Test]
    public void BoundedEncodingsCanBeSkippedAcrossEveryFragmentBoundary()
    {
        int seed = 0;
        while (seed < ArbitrarySeedCount)
        {
            CborValue value = new BoundedValueGenerator(seed).Generate();
            byte[] encoded = new CborEncoder().Encode(
                value,
                new CborEncodeOptions { ReduceFloatWidth = false });
            int split = 0;
            while (split < encoded.Length)
            {
                CborTokenReader reader = new CborTokenReader(DecodeOptions());
                CborReadResult first = reader.SkipValue(
                    encoded.AsSpan(0, split).ToArray());
                byte[] finalFragment = new byte[encoded.Length - split + 1];
                encoded.AsSpan(split).CopyTo(finalFragment);
                finalFragment[finalFragment.Length - 1] = 0x00;
                CborReadResult completed = reader.SkipValue(finalFragment);

                Assert.Multiple(() =>
                {
                    Assert.That(first.Status,
                        Is.EqualTo(CborReadStatus.NeedMoreData),
                        Failure("fragment prefix", seed, "split " + split));
                    Assert.That(first.Consumed, Is.EqualTo(split),
                        Failure("fragment prefix consumption", seed,
                            "split " + split));
                    Assert.That(first.Remaining, Is.Zero,
                        Failure("fragment prefix remaining", seed,
                            "split " + split));
                    Assert.That(completed.Status,
                        Is.EqualTo(CborReadStatus.Done),
                        Failure("fragment completion", seed,
                            completed.ErrorMessage));
                    Assert.That(completed.Consumed,
                        Is.EqualTo(encoded.Length - split),
                        Failure("fragment completion consumption", seed,
                            "split " + split));
                    Assert.That(completed.Remaining, Is.EqualTo(1),
                        Failure("fragment trailing byte", seed,
                            "split " + split));
                });
                split = split + 1;
            }
            seed = seed + 1;
        }
    }

    [Test]
    public void JsonRepresentableValuesRoundTripUnderStrictPolicy()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborJsonOptions options = new CborJsonOptions
        {
            Policy = CborJsonPolicy.Strict
        };
        int seed = 0;
        while (seed < JsonSeedCount)
        {
            CborValue original = new JsonValueGenerator(seed).Generate();
            string json = converter.ConvertToJson(original, options);
            CborReadResult parsed = converter.ConvertFromJson(json, options);

            Assert.That(parsed.Status, Is.EqualTo(CborReadStatus.Done),
                Failure("JSON parse", seed, parsed.ErrorMessage));
            Assert.That(parsed.Value, Is.Not.Null,
                Failure("JSON value", seed, parsed.ErrorMessage));
            Assert.That(CanonicalBytes(parsed.Value!),
                Is.EqualTo(CanonicalBytes(original)),
                Failure("JSON round trip", seed, json));
            seed = seed + 1;
        }
    }

    [Test]
    public void SchemaGeneratedValuesRemainValidAcrossRepresentations()
    {
        CddlSchema schema = CompileSchema(
            "root = envelope / reading / tagged\n"
            + "envelope = {id: uint, name: tstr, values: [1*4 int], ? active: bool}\n"
            + "reading = [tstr, -100..100, bstr .size 4]\n"
            + "tagged = #6.1000([uint, bool])");
        CddlGenerationStrategy[] strategies =
        [
            CddlGenerationStrategy.Minimal,
            CddlGenerationStrategy.Boundary,
            CddlGenerationStrategy.Representative
        ];
        int strategyIndex = 0;
        while (strategyIndex < strategies.Length)
        {
            int seed = 0;
            while (seed < SchemaSeedCount)
            {
                CddlGenerationOptions generationOptions = new CddlGenerationOptions
                {
                    Strategy = strategies[strategyIndex],
                    Seed = seed,
                    MaxNestingDepth = 8,
                    MaxCollectionItems = 8
                };
                CddlGenerationResult generated = new CddlValueGenerator().GenerateValue(
                    schema,
                    generationOptions);
                string context = strategies[strategyIndex] + " seed " + seed;

                Assert.That(generated.IsSuccess, Is.True,
                    context + ": " + generated.ErrorMessage);
                Assert.That(generated.Value, Is.Not.Null, context);
                AssertValid(schema, generated.Value!, context + " generated");

                byte[] encoded = new CborEncoder().Encode(
                    generated.Value!,
                    new CborEncodeOptions());
                CborReadResult decoded = new CborDecoder().Decode(
                    encoded,
                    DecodeOptions());
                Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done),
                    context + ": " + decoded.ErrorMessage);
                Assert.That(decoded.Value, Is.Not.Null, context);
                AssertValid(schema, decoded.Value!, context + " binary");

                CborDiagnosticConverter diagnostic = new CborDiagnosticConverter();
                string text = diagnostic.FormatDiagnostic(
                    generated.Value!,
                    new CborDiagnosticOptions());
                CborReadResult parsed = diagnostic.ParseDiagnostic(
                    text,
                    new CborDiagnosticOptions());
                Assert.That(parsed.Status, Is.EqualTo(CborReadStatus.Done),
                    context + ": " + parsed.ErrorMessage);
                Assert.That(parsed.Value, Is.Not.Null, context);
                AssertValid(schema, parsed.Value!, context + " diagnostic");
                seed = seed + 1;
            }
            strategyIndex = strategyIndex + 1;
        }
    }

    private static byte[] CanonicalBytes(CborValue value)
    {
        return new CborEncoder().Encode(
            value,
            new CborEncodeOptions
            {
                EncodingProfile = CborEncodingProfile.CoreDeterministic
            });
    }

    private static CborDecodeOptions DecodeOptions()
    {
        return new CborDecodeOptions
        {
            DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
            TagPolicy = CborTagPolicy.Preserve
        };
    }

    private static CddlSchema CompileSchema(string source)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions { SourceName = "bounded-properties.cddl" });
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions());
    }

    private static void AssertValid(
        CddlSchema schema,
        CborValue value,
        string context)
    {
        CddlValidationResult validation = new CddlValidator().ValidateCbor(
            schema,
            value,
            new CddlValidationOptions());
        string detail = validation.Issues.Length == 0
            ? context
            : context + ": " + validation.Issues[0].Message;
        Assert.That(validation.IsValid, Is.True, detail);
    }

    private static string Failure(string operation, int seed, string? detail)
    {
        string message = operation + " failed for seed " + seed;
        if (!string.IsNullOrEmpty(detail))
        {
            message = message + ": " + detail;
        }
        return message;
    }

    private class BoundedValueGenerator
    {
        private readonly Random Random;
        private readonly int RootChoice;
        private int KeySerial;

        public BoundedValueGenerator(int seed)
        {
            Random = new Random(seed);
            RootChoice = seed % 10;
        }

        public CborValue Generate()
        {
            return GenerateValue(0, RootChoice);
        }

        private CborValue GenerateValue(int depth, int forcedChoice)
        {
            int choice = forcedChoice;
            if (choice < 0)
            {
                choice = depth >= 4 ? Random.Next(5) : Random.Next(10);
            }
            switch (choice)
            {
                case 0:
                    return GenerateInteger();
                case 1:
                    return GenerateBytes();
                case 2:
                    return GenerateText();
                case 3:
                    return GenerateFloat();
                case 4:
                    return GenerateSimple();
                case 5:
                case 8:
                    return GenerateArray(depth);
                case 6:
                case 9:
                    return GenerateMap(depth);
                default:
                    return GenerateTag(depth);
            }
        }

        private CborValue GenerateInteger()
        {
            BigInteger[] values =
            [
                BigInteger.Zero,
                new BigInteger(23),
                new BigInteger(24),
                new BigInteger(255),
                new BigInteger(256),
                new BigInteger(uint.MaxValue),
                new BigInteger(ulong.MaxValue),
                new BigInteger(-1),
                new BigInteger(-25),
                -new BigInteger(ulong.MaxValue) - BigInteger.One
            ];
            BigInteger selected = values[Random.Next(values.Length)];
            return Integer(selected);
        }

        private CborValue GenerateBytes()
        {
            byte[] bytes = new byte[Random.Next(9)];
            Random.NextBytes(bytes);
            return new CborValue
            {
                Kind = CborValueKind.ByteString,
                ByteStringValue = bytes,
                LengthKind = CborLengthKind.Definite
            };
        }

        private CborValue GenerateText()
        {
            string[] values =
            [
                "",
                "plain ASCII",
                "quote \\\" and slash \\\\",
                "line\\nfeed",
                "€",
                "日本語",
                "emoji 😀"
            ];
            return Text(values[Random.Next(values.Length)]);
        }

        private CborValue GenerateFloat()
        {
            double[] values =
            [
                0.0,
                -0.0,
                1.5,
                -100.25,
                double.Epsilon,
                double.MaxValue,
                double.PositiveInfinity,
                double.NegativeInfinity,
                double.NaN
            ];
            return Float(values[Random.Next(values.Length)]);
        }

        private CborValue GenerateSimple()
        {
            byte[] values = [0, 16, 20, 21, 22, 23, 32, 255];
            return Simple(values[Random.Next(values.Length)]);
        }

        private CborValue GenerateArray(int depth)
        {
            List<CborValue> items = new List<CborValue>();
            int count = Random.Next(5);
            int index = 0;
            while (index < count)
            {
                items.Add(GenerateValue(depth + 1, -1));
                index = index + 1;
            }
            return new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems = items,
                LengthKind = CborLengthKind.Definite
            };
        }

        private CborValue GenerateMap(int depth)
        {
            List<CborMapEntry> entries = new List<CborMapEntry>();
            int count = Random.Next(5);
            int index = 0;
            while (index < count)
            {
                entries.Add(new CborMapEntry
                {
                    Key = GenerateUniqueKey(),
                    Value = GenerateValue(depth + 1, -1)
                });
                index = index + 1;
            }
            return new CborValue
            {
                Kind = CborValueKind.Map,
                MapEntries = entries,
                LengthKind = CborLengthKind.Definite
            };
        }

        private CborValue GenerateUniqueKey()
        {
            int serial = KeySerial;
            KeySerial = KeySerial + 1;
            int choice = serial % 3;
            if (choice == 0)
            {
                return Text("key-" + serial);
            }
            if (choice == 1)
            {
                return Integer(new BigInteger(1000000 + serial));
            }
            return new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems =
                [
                    Integer(new BigInteger(serial)),
                    Simple(CborSimpleValues.False)
                ],
                LengthKind = CborLengthKind.Definite
            };
        }

        private CborValue GenerateTag(int depth)
        {
            ulong[] tags = [0, 23, 32, 1000, 55799, ulong.MaxValue];
            return new CborValue
            {
                Kind = CborValueKind.Tag,
                TagNumber = tags[Random.Next(tags.Length)],
                TaggedValue = GenerateValue(depth + 1, -1)
            };
        }
    }

    private class JsonValueGenerator
    {
        private readonly Random Random;
        private readonly int RootChoice;
        private int KeySerial;

        public JsonValueGenerator(int seed)
        {
            Random = new Random(seed);
            RootChoice = seed % 7;
        }

        public CborValue Generate()
        {
            return GenerateValue(0, RootChoice);
        }

        private CborValue GenerateValue(int depth, int forcedChoice)
        {
            int choice = forcedChoice;
            if (choice < 0)
            {
                choice = depth >= 4 ? Random.Next(5) : Random.Next(7);
            }
            switch (choice)
            {
                case 0:
                    return GenerateInteger();
                case 1:
                    return GenerateText();
                case 2:
                    return GenerateFloat();
                case 3:
                    return GenerateSimple();
                case 4:
                case 5:
                    return GenerateArray(depth);
                default:
                    return GenerateMap(depth);
            }
        }

        private CborValue GenerateInteger()
        {
            long[] values =
            [
                -9007199254740991L,
                -1,
                0,
                1,
                23,
                9007199254740991L
            ];
            return Integer(new BigInteger(values[Random.Next(values.Length)]));
        }

        private CborValue GenerateText()
        {
            string[] values = ["", "json", "escaped \\\" text", "€", "😀"];
            return Text(values[Random.Next(values.Length)]);
        }

        private CborValue GenerateFloat()
        {
            double[] values = [0.5, -1.25, 1.5, 100000.5];
            return Float(values[Random.Next(values.Length)]);
        }

        private CborValue GenerateSimple()
        {
            byte[] values =
            [
                CborSimpleValues.False,
                CborSimpleValues.True,
                CborSimpleValues.Null
            ];
            return Simple(values[Random.Next(values.Length)]);
        }

        private CborValue GenerateArray(int depth)
        {
            List<CborValue> items = new List<CborValue>();
            int count = Random.Next(5);
            int index = 0;
            while (index < count)
            {
                items.Add(GenerateValue(depth + 1, -1));
                index = index + 1;
            }
            return new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems = items,
                LengthKind = CborLengthKind.Definite
            };
        }

        private CborValue GenerateMap(int depth)
        {
            List<CborMapEntry> entries = new List<CborMapEntry>();
            int count = Random.Next(5);
            int index = 0;
            while (index < count)
            {
                string key = "property-" + KeySerial;
                KeySerial = KeySerial + 1;
                entries.Add(new CborMapEntry
                {
                    Key = Text(key),
                    Value = GenerateValue(depth + 1, -1)
                });
                index = index + 1;
            }
            return new CborValue
            {
                Kind = CborValueKind.Map,
                MapEntries = entries,
                LengthKind = CborLengthKind.Definite
            };
        }
    }

    private static CborValue Integer(BigInteger value)
    {
        return new CborValue
        {
            Kind = value < BigInteger.Zero
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = value
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value,
            LengthKind = CborLengthKind.Definite
        };
    }

    private static CborValue Float(double value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Double,
            FloatValue = new CborFloatValue
            {
                Value = value,
                Width = CborFloatWidth.Double,
                RawBits = BitConverter.DoubleToUInt64Bits(value),
                IsNaN = double.IsNaN(value)
            }
        };
    }

    private static CborValue Simple(byte value)
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = value
        };
    }
}
