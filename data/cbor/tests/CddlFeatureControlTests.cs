using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlFeatureControlTests
{
    [Test]
    public void AcceptsAndReportsAnEnabledFeature()
    {
        CddlValidationResult result = Validate(
            "root = 1 .feature \"alpha\"",
            Unsigned(1),
            Features("alpha"));
        CddlControlResult feature = result.ControlResults.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(feature.Status,
                Is.EqualTo(CddlControlStatus.Applied));
            Assert.That(feature.FeatureName, Is.EqualTo("alpha"));
            Assert.That(feature.FeatureDetail, Is.Not.Null);
            Assert.That(feature.FeatureDetail!.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
            Assert.That(feature.FeatureDetail.IntegerValue,
                Is.EqualTo(BigInteger.One));
        });
    }

    [Test]
    public void RejectsDisabledFeaturesWithoutReportingThemAsUsed()
    {
        CddlValidationResult result = Validate(
            "root = 1 .feature \"alpha\"",
            Unsigned(1),
            Features("beta"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.UnsupportedFeature));
            Assert.That(result.Issues[0].Actual, Is.EqualTo("alpha"));
            Assert.That(result.ControlResults, Is.Empty);
        });
    }

    [Test]
    public void SelectsTheEnabledFeatureAlternativeCleanly()
    {
        string source =
            "root = (1 .feature \"alpha\") / "
            + "(1 .feature \"beta\")";
        CddlValidationResult result = Validate(
            source,
            Unsigned(1),
            Features("beta"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ControlResults, Has.Length.EqualTo(1));
            Assert.That(result.ControlResults[0].FeatureName,
                Is.EqualTo("beta"));
        });

        CddlValidationResult disabled = Validate(
            source,
            Unsigned(1),
            Features("gamma"));
        Assert.Multiple(() =>
        {
            Assert.That(disabled.IsValid, Is.False);
            Assert.That(ContainsCode(
                disabled.Issues[0],
                CddlValidationCode.UnsupportedFeature), Is.True);
            Assert.That(disabled.ControlResults, Is.Empty);
        });
    }

    [Test]
    public void DoesNotReportFeaturesFromRejectedAlternatives()
    {
        string source =
            "root = [1 .feature \"alpha\", 9] / [1, 2]";
        CddlValidationResult result = Validate(
            source,
            ArrayValue(Unsigned(1), Unsigned(2)),
            Features("alpha"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ControlResults, Is.Empty);
        });
    }

    [Test]
    public void ReportsAnExplicitFeatureDetailFromTheControllerArray()
    {
        string source =
            "root = tstr .feature "
            + "[\"foo-extensions\", \"bazify\"]";
        CddlSchema schema = Compile(source);
        CddlValidationResult result = new CddlValidator().ValidateCbor(
            schema,
            Text("complex baz value"),
            Features("foo-extensions"));
        CddlControlResult feature = result.ControlResults.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(schema.Features,
                Is.EqualTo(new string[] { "foo-extensions" }));
            Assert.That(feature.FeatureName,
                Is.EqualTo("foo-extensions"));
            Assert.That(feature.FeatureDetail, Is.Not.Null);
            Assert.That(feature.FeatureDetail!.Kind,
                Is.EqualTo(CborValueKind.TextString));
            Assert.That(feature.FeatureDetail.TextStringValue,
                Is.EqualTo("bazify"));
        });
    }

    [Test]
    public void ComputesFeatureNamesAndExplicitDetails()
    {
        string source =
            "root = 2 .feature "
            + "[(\"be\" .cat \"ta\"), (1 .plus 2)]";
        CddlValidationResult result = Validate(
            source,
            Unsigned(2),
            Features("beta"));
        CddlControlResult feature = result.ControlResults.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(feature.FeatureName, Is.EqualTo("beta"));
            Assert.That(feature.FeatureDetail, Is.Not.Null);
            Assert.That(feature.FeatureDetail!.IntegerValue,
                Is.EqualTo(new BigInteger(3)));
        });
    }

    [Test]
    public void UsesMatchedExtensionMapKeysAsImplicitDetails()
    {
        string source =
            "root = {* (tstr .feature "
            + "\"further-person-extension\") => any}";
        CddlValidationResult result = Validate(
            source,
            Map(
                Entry(Text("organisation"), Text("example")),
                Entry(Text("nickname"), Text("sample"))
            ),
            Features("further-person-extension"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ControlResults.Select(item =>
                    item.FeatureName),
                Is.All.EqualTo("further-person-extension"));
            Assert.That(result.ControlResults.Select(item =>
                    item.FeatureDetail!.TextStringValue),
                Is.EquivalentTo(new string[]
                {
                    "organisation", "nickname"
                }));
        });
    }

    [Test]
    public void FeatureNamesAreCaseSensitive()
    {
        CddlValidationResult result = Validate(
            "root = 1 .feature \"alpha\"",
            Unsigned(1),
            Features("Alpha"));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.UnsupportedFeature));
    }

    [TestCase("root = 1 .feature [\"alpha\"]")]
    [TestCase("root = 1 .feature [\"alpha\", \"detail\", 3]")]
    [TestCase("root = 1 .feature [1, \"detail\"]")]
    [TestCase("root = 1 .feature [\"alpha\", tstr]")]
    public void RejectsInvalidFeatureControllerShapes(string source)
    {
        CddlCompileResult result = TryCompile(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("CDDL_COMPILE_CONTROL_CONTROLLER_INVALID"));
        });
    }

    private static bool ContainsCode(
        CddlValidationIssue issue,
        CddlValidationCode code)
    {
        if (issue.Code == code)
        {
            return true;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            if (ContainsCode(issue.Details[index], code))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    private static CddlValidationOptions Features(params string[] names)
    {
        return new CddlValidationOptions
        {
            EnabledFeatures = names
        };
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions options)
    {
        return new CddlValidator().ValidateCbor(
            Compile(source),
            value,
            options);
    }

    private static CddlSchema Compile(string source)
    {
        CddlCompileResult result = TryCompile(source);
        Assert.That(
            result.IsSuccess,
            Is.True,
            result.Diagnostics.Length == 0
                ? ""
                : result.Diagnostics[0].Message);
        return result.Schema!;
    }

    private static CddlCompileResult TryCompile(string source)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "feature-control.cddl" });
        Assert.That(parse.Diagnostics, Is.Empty);
        return new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions());
    }

    private static CborValue Unsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue ArrayValue(params CborValue[] items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(items)
        };
    }

    private static CborMapEntry Entry(CborValue key, CborValue value)
    {
        return new CborMapEntry
        {
            Key = key,
            Value = value
        };
    }

    private static CborValue Map(params CborMapEntry[] entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry>(entries)
        };
    }
}
