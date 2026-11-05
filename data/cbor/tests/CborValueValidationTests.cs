using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborValueValidationTests
{
    [Test]
    public void ValidatesApplicationCreatedNestedValues()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                new CborMapEntry
                {
                    Key = Text("items"),
                    Value = new CborValue
                    {
                        Kind = CborValueKind.Array,
                        ArrayItems = [Integer(1), Text("two")]
                    }
                }
            ]
        };

        CborValidationResult result = new CborValidator().ValidateValue(
            value,
            CborValidationLevel.Basic);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidDiscriminantFieldCombinations()
    {
        CborValue value = Integer(1);
        value.TextStringValue = "not an integer field";

        CborValidationResult result = new CborValidator().ValidateValue(
            value,
            CborValidationLevel.WellFormedness);

        AssertIssue(result, "CBOR_VALUE_FIELD_MISMATCH", "$", -1);
    }

    [Test]
    public void ReportsMissingGraphMembersWithoutThrowing()
    {
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [null!]
        };

        CborValidationResult result = new CborValidator().ValidateValue(
            map,
            CborValidationLevel.Basic);

        AssertIssue(result, "CBOR_MAP_ENTRY_MISSING", "${0}", -1);
    }

    [Test]
    public void RejectsOrdinaryCyclesAndAcceptsAnnotatedSharedCycles()
    {
        CborValue ordinary = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = []
        };
        ordinary.ArrayItems.Add(ordinary);
        CborValue shared = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [],
            IsSharedValue = true,
            SharedValueIndex = 0
        };
        shared.ArrayItems.Add(shared);
        CborValidator validator = new CborValidator();

        CborValidationResult rejected = validator.ValidateValue(
            ordinary,
            CborValidationLevel.Basic);
        CborValidationResult accepted = validator.ValidateValueWithOptions(
            shared,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.TagValidity,
                SharedValuePolicy = CborSharedValuePolicy.Resolve
            });

        Assert.Multiple(() =>
        {
            Assert.That(rejected.IsValid, Is.False);
            Assert.That(rejected.Issues[0].Code,
                Is.EqualTo("CBOR_CYCLIC_VALUE_UNSUPPORTED"));
            Assert.That(rejected.Issues[0].Path, Is.EqualTo("$[0]"));
            Assert.That(accepted.IsValid, Is.True);
            Assert.That(accepted.Issues, Is.Empty);
        });
    }

    [Test]
    public void FindsEquivalentMapKeysInValueGraphs()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                new CborMapEntry { Key = Integer(1), Value = Integer(2) },
                new CborMapEntry { Key = Integer(1), Value = Integer(3) }
            ]
        };

        CborValidationResult result = new CborValidator().ValidateValue(
            value,
            CborValidationLevel.Basic);

        AssertIssue(result, "CBOR_DUPLICATE_MAP_KEY", "$[1].key", -1);
    }

    [Test]
    public void AppliesContainerStringItemAndEncodedByteLimits()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [Text("large"), Integer(1)]
        };
        CborDecodeOptions options = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.Basic,
            MaxArrayItems = 1,
            MaxStringBytes = 2,
            MaxTotalItems = 2,
            MaxTotalBytes = 100,
            CollectValidationIssues = true,
            MaxValidationIssues = 10
        };

        CborValidationResult result = new CborValidator().ValidateValueWithOptions(
            value,
            options);
        options.MaxTotalItems = 100;
        options.MaxTotalBytes = 3;
        CborValidationResult bytes = new CborValidator().ValidateValueWithOptions(
            value,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_MAX_ARRAY_ITEMS"));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_MAX_STRING_BYTES"));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(bytes.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_MAX_TOTAL_BYTES"));
        });
    }

    [Test]
    public void InvokesRegisteredTagValidationForValueGraphs()
    {
        ValueTagHandler handler = new ValueTagHandler();
        CborDecodeOptions options = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.TagValidity,
            TagRegistrations =
            [
                new CborTagRegistration
                {
                    TagNumber = 100,
                    Handler = handler,
                    ValidateContent = true
                }
            ]
        };
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 100,
            TaggedValue = Text("wrong")
        };

        CborValidationResult result = new CborValidator().ValidateValueWithOptions(
            value,
            options);

        Assert.Multiple(() =>
        {
            AssertIssue(result, "CBOR_TEST_VALUE_TAG_INVALID", "$", -1);
            Assert.That(handler.ValidationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesApplicationPoliciesToValueGraphs()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 100,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.SimpleValue,
                SimpleValue = 32
            }
        };
        CborDecodeOptions options = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.Application,
            TagPolicy = CborTagPolicy.RejectUnknown,
            AllowUnknownSimpleValues = false,
            CollectValidationIssues = true,
            MaxValidationIssues = 10
        };

        CborValidationResult result = new CborValidator().ValidateValueWithOptions(
            value,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_UNKNOWN_TAG"));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_UNKNOWN_SIMPLE_VALUE"));
        });
    }

    [Test]
    public void ChecksRequestedDeterministicEncodabilityWithoutRequiringStoredOrder()
    {
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            LengthKind = CborLengthKind.Indefinite,
            MapEntries =
            [
                new CborMapEntry { Key = Integer(2), Value = Integer(0) },
                new CborMapEntry { Key = Integer(1), Value = Integer(0) }
            ]
        };
        CborValidator validator = new CborValidator();

        CborValidationResult encodable = validator.ValidateValueWithOptions(
            map,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Deterministic,
                DeterministicEncodingProfile =
                    CborEncodingProfile.LengthFirstCoreDeterministic
            });
        CborValidationResult invalidProfile = validator.ValidateValueWithOptions(
            Integer(1),
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Deterministic,
                DeterministicEncodingProfile = CborEncodingProfile.Basic
            });

        Assert.Multiple(() =>
        {
            Assert.That(encodable.IsValid, Is.True);
            Assert.That(encodable.Issues, Is.Empty);
            Assert.That(invalidProfile.IsValid, Is.False);
            Assert.That(invalidProfile.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_PROFILE_INVALID"));
        });
    }

    private static CborValue Integer(int value)
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

    private static void AssertIssue(
        CborValidationResult result,
        string code,
        string path,
        long offset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(code));
            Assert.That(result.Issues[0].Path, Is.EqualTo(path));
            Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(offset));
        });
    }

    private class ValueTagHandler : ICborTagHandler
    {
        public int ValidationCalls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls = ValidationCalls + 1;
            return new CborTagResult
            {
                Handled = true,
                IsValid = false,
                ErrorCode = "CBOR_TEST_VALUE_TAG_INVALID"
            };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }
}
