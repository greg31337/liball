using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlEncodedValidationTests
{
    [Test]
    public void ValidatesEncodedCborAndReportsNestedSourceOffsets()
    {
        CddlSchema schema = Compile("root = [1, #3]");
        CborDecodeOptions decodeOptions = new CborDecodeOptions
        {
            AllowTrailingData = true,
            RetainSourceRanges = false
        };
        ICddlValidator validator = new CddlValidator();

        CddlValidationResult valid = validator.ValidateEncodedCbor(
            schema,
            new byte[] { 0x82, 0x01, 0x61, 0x78 },
            decodeOptions,
            new CddlValidationOptions());
        CddlValidationResult invalid = validator.ValidateEncodedCbor(
            schema,
            new byte[] { 0x82, 0x01, 0x02 },
            decodeOptions,
            new CddlValidationOptions());
        CddlValidationIssue? nested = FindIssue(
            invalid.Issues[0],
            CddlValidationCode.TypeMismatch,
            "$[1]");

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Issues, Is.Empty);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(nested, Is.Not.Null);
            Assert.That(nested!.ByteOffset, Is.EqualTo(2));
            Assert.That(decodeOptions.AllowTrailingData, Is.True);
            Assert.That(decodeOptions.RetainSourceRanges, Is.False);
        });
    }

    [Test]
    public void ExistingValuesRetainAvailableOffsetsAndUseNegativeOneOtherwise()
    {
        CddlSchema schema = Compile("root = [1, #3]");
        CborReadResult decoded = new CborDecoder().Decode(
            new byte[] { 0x82, 0x01, 0x02 },
            new CborDecodeOptions { RetainSourceRanges = true });
        Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
        Assert.That(decoded.Value, Is.Not.Null);

        CddlValidationResult retained = new CddlValidator().ValidateCbor(
            schema,
            decoded.Value!,
            new CddlValidationOptions());
        CddlValidationResult unavailable = new CddlValidator().ValidateCbor(
            schema,
            Array(Unsigned(1), Unsigned(2)),
            new CddlValidationOptions());
        CddlValidationIssue? retainedNested = FindIssue(
            retained.Issues[0],
            CddlValidationCode.TypeMismatch,
            "$[1]");
        CddlValidationIssue? unavailableNested = FindIssue(
            unavailable.Issues[0],
            CddlValidationCode.TypeMismatch,
            "$[1]");

        Assert.Multiple(() =>
        {
            Assert.That(retainedNested, Is.Not.Null);
            Assert.That(retainedNested!.ByteOffset, Is.EqualTo(2));
            Assert.That(unavailableNested, Is.Not.Null);
            Assert.That(unavailableNested!.ByteOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void EncodedValidationRequiresOneCompleteCborItem()
    {
        CddlSchema schema = Compile("root = 1");
        ICddlValidator validator = new CddlValidator();
        CborDecodeOptions decodeOptions = new CborDecodeOptions
        {
            AllowTrailingData = true
        };

        CddlValidationResult trailing = validator.ValidateEncodedCbor(
            schema,
            new byte[] { 0x01, 0x02 },
            decodeOptions,
            new CddlValidationOptions());
        CddlValidationResult truncated = validator.ValidateEncodedCbor(
            schema,
            new byte[] { 0x82, 0x01 },
            decodeOptions,
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(trailing.IsValid, Is.False);
            Assert.That(trailing.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            Assert.That(trailing.Issues[0].ByteOffset, Is.EqualTo(1));
            Assert.That(trailing.Issues[0].Actual,
                Is.EqualTo("CBOR_TRAILING_DATA"));
            Assert.That(truncated.IsValid, Is.False);
            Assert.That(truncated.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            Assert.That(truncated.Issues[0].ByteOffset, Is.EqualTo(2));
            Assert.That(truncated.Issues[0].Actual,
                Is.EqualTo("CBOR_UNEXPECTED_END_OF_DATA"));
        });
    }

    [Test]
    public void EncodedValidationHonorsDecodeLimits()
    {
        CddlValidationResult result = new CddlValidator().ValidateEncodedCbor(
            Compile("root = [1]"),
            new byte[] { 0x81, 0x01 },
            new CborDecodeOptions { MaxArrayItems = 0 },
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            Assert.That(result.Issues[0].Actual,
                Is.EqualTo("CBOR_MAX_ARRAY_ITEMS"));
            Assert.That(result.Issues[0].ByteOffset, Is.Zero);
        });
    }

    private static CddlValidationIssue? FindIssue(
        CddlValidationIssue issue,
        CddlValidationCode code,
        string path)
    {
        if (issue.Code == code && issue.InstancePath == path)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue? found = FindIssue(
                issue.Details[index],
                code,
                path);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static CddlSchema Compile(string source)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions());
        Assert.That(parse.IsSuccess, Is.True);
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions { IncludeStandardPrelude = false });
        Assert.That(compile.IsSuccess, Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return compile.Schema!;
    }

    private static CborValue Unsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Array(params CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(values)
        };
    }
}
