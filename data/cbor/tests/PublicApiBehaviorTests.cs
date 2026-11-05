using cbor;

namespace cbor.Tests;

[TestFixture]
public class PublicApiBehaviorTests
{
    [Test]
    public void ValueKindsCoverTheGenericCborModel()
    {
        CborValueKind[] expected =
        [
            CborValueKind.None,
            CborValueKind.UnsignedInteger,
            CborValueKind.NegativeInteger,
            CborValueKind.ByteString,
            CborValueKind.TextString,
            CborValueKind.Array,
            CborValueKind.Map,
            CborValueKind.Tag,
            CborValueKind.Float,
            CborValueKind.SimpleValue
        ];

        Assert.That(Enum.GetValues<CborValueKind>(), Is.EqualTo(expected));
    }

    [Test]
    public void TokenKindsCoverScalarAndContainerTransitions()
    {
        CborTokenKind[] expected =
        [
            CborTokenKind.None,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.NegativeInteger,
            CborTokenKind.ByteString,
            CborTokenKind.TextString,
            CborTokenKind.StartByteString,
            CborTokenKind.ByteStringChunk,
            CborTokenKind.EndByteString,
            CborTokenKind.StartTextString,
            CborTokenKind.TextStringChunk,
            CborTokenKind.EndTextString,
            CborTokenKind.StartArray,
            CborTokenKind.EndArray,
            CborTokenKind.StartMap,
            CborTokenKind.EndMap,
            CborTokenKind.Tag,
            CborTokenKind.SimpleValue,
            CborTokenKind.Float
        ];

        Assert.That(Enum.GetValues<CborTokenKind>(), Is.EqualTo(expected));
    }

    [Test]
    public void FloatWidthsUseTheirEncodedBitCounts()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)CborFloatWidth.None, Is.Zero);
            Assert.That((int)CborFloatWidth.Half, Is.EqualTo(16));
            Assert.That((int)CborFloatWidth.Single, Is.EqualTo(32));
            Assert.That((int)CborFloatWidth.Double, Is.EqualTo(64));
        });
    }

    [Test]
    public void LengthAndOperationStatusesExposeAllOrdinaryOutcomes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<CborLengthKind>(), Is.EqualTo(new[]
            {
                CborLengthKind.None, CborLengthKind.Definite, CborLengthKind.Indefinite
            }));
            Assert.That(Enum.GetValues<CborReadStatus>(), Is.EqualTo(new[]
            {
                CborReadStatus.None, CborReadStatus.Done, CborReadStatus.NeedMoreData,
                CborReadStatus.EndOfData, CborReadStatus.Error
            }));
            Assert.That(Enum.GetValues<CborWriteStatus>(), Is.EqualTo(new[]
            {
                CborWriteStatus.None, CborWriteStatus.Done,
                CborWriteStatus.DestinationTooSmall, CborWriteStatus.Error
            }));
        });
    }

    [Test]
    public void PolicyEnumsExposeThePlannedProcessingChoices()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<CborEncodingProfile>(), Is.EqualTo(new[]
            {
                CborEncodingProfile.Basic, CborEncodingProfile.Preferred,
                CborEncodingProfile.CoreDeterministic,
                CborEncodingProfile.LengthFirstCoreDeterministic
            }));
            Assert.That(Enum.GetValues<CborMapKeyOrder>(), Is.EqualTo(new[]
            {
                CborMapKeyOrder.Preserve, CborMapKeyOrder.Lexicographic,
                CborMapKeyOrder.LengthFirst
            }));
            Assert.That(Enum.GetValues<CborDuplicateKeyPolicy>(), Is.EqualTo(new[]
            {
                CborDuplicateKeyPolicy.Preserve, CborDuplicateKeyPolicy.Reject,
                CborDuplicateKeyPolicy.KeepFirst, CborDuplicateKeyPolicy.KeepLast
            }));
            Assert.That(Enum.GetValues<CborTagPolicy>(), Is.EqualTo(new[]
            {
                CborTagPolicy.Preserve, CborTagPolicy.ValidateKnown,
                CborTagPolicy.RejectUnknown, CborTagPolicy.Forbid
            }));
            Assert.That(Enum.GetValues<CborSharedValuePolicy>(), Is.EqualTo(new[]
            {
                CborSharedValuePolicy.Disabled, CborSharedValuePolicy.Preserve,
                CborSharedValuePolicy.Resolve
            }));
            Assert.That(Enum.GetValues<CborValidationLevel>(), Is.EqualTo(new[]
            {
                CborValidationLevel.WellFormedness, CborValidationLevel.Basic,
                CborValidationLevel.TagValidity, CborValidationLevel.Deterministic,
                CborValidationLevel.Application
            }));
            Assert.That(Enum.GetValues<CborIssueSeverity>(), Is.EqualTo(new[]
            {
                CborIssueSeverity.Information, CborIssueSeverity.Warning,
                CborIssueSeverity.Error
            }));
            Assert.That(Enum.GetValues<CborDiagnosticStyle>(), Is.EqualTo(new[]
            {
                CborDiagnosticStyle.Compact, CborDiagnosticStyle.Indented
            }));
            Assert.That(Enum.GetValues<CborJsonPolicy>(), Is.EqualTo(new[]
            {
                CborJsonPolicy.Strict, CborJsonPolicy.PreserveInformation,
                CborJsonPolicy.AllowLossy
            }));
        });
    }

    [Test]
    public void CoreTagNumbersMatchTheRegisteredAssignments()
    {
        ulong[] actual =
        [
            CborTagNumbers.DateTimeString,
            CborTagNumbers.EpochDateTime,
            CborTagNumbers.PositiveBignum,
            CborTagNumbers.NegativeBignum,
            CborTagNumbers.DecimalFraction,
            CborTagNumbers.Bigfloat,
            CborTagNumbers.ExpectedBase64Url,
            CborTagNumbers.ExpectedBase64,
            CborTagNumbers.ExpectedBase16,
            CborTagNumbers.EncodedCbor,
            CborTagNumbers.StringReference,
            CborTagNumbers.ShareableValue,
            CborTagNumbers.SharedValueReference,
            CborTagNumbers.Uri,
            CborTagNumbers.Base64Url,
            CborTagNumbers.Base64,
            CborTagNumbers.RegularExpression,
            CborTagNumbers.MimeMessage,
            CborTagNumbers.Uuid,
            CborTagNumbers.StringReferenceNamespace,
            CborTagNumbers.SelfDescribedCbor
        ];
        ulong[] expected =
        [0, 1, 2, 3, 4, 5, 21, 22, 23, 24, 25, 28, 29, 32, 33, 34, 35, 36, 37, 256, 55799];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void SimpleValuesMatchRfc8949AssignmentsAndReservedRange()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CborSimpleValues.False, Is.EqualTo(20));
            Assert.That(CborSimpleValues.True, Is.EqualTo(21));
            Assert.That(CborSimpleValues.Null, Is.EqualTo(22));
            Assert.That(CborSimpleValues.Undefined, Is.EqualTo(23));
            Assert.That(CborSimpleValues.FirstReserved, Is.EqualTo(24));
            Assert.That(CborSimpleValues.LastReserved, Is.EqualTo(31));
        });
    }

    [Test]
    public void MediaTypesAndStructuredSuffixesMatchTheirRegistrations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CborMediaTypes.Cbor, Is.EqualTo("application/cbor"));
            Assert.That(CborMediaTypes.CborSequence, Is.EqualTo("application/cbor-seq"));
            Assert.That(CborMediaTypes.CborStructuredSyntaxSuffix, Is.EqualTo("+cbor"));
            Assert.That(CborMediaTypes.CborSequenceStructuredSyntaxSuffix, Is.EqualTo("+cbor-seq"));
        });
    }
}
