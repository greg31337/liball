using fake;

namespace fake.Tests;

[TestFixture]
public class GeneralizedMetadataTests
{
    [Test]
    public void IdentifierMetadataPopulatesTheGeneralizedEnvelope()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 14900);
        options.GenerationMode = GenerationMode.Boundary;
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        IdentifierProvider identifier = Fake.Create(options).Identifier();
        string value = identifier.Serial();

        GenerationMetadata metadata = identifier.IdentifierMetadata(
            value,
            IdentifierKind.Serial);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Value, Is.SameAs(value));
            Assert.That(metadata.GeneratedValue, Is.SameAs(value));
            Assert.That(metadata.ValueKind, Is.EqualTo("Serial"));
            Assert.That(metadata.Provider, Is.EqualTo("identifier"));
            Assert.That(metadata.Locale, Is.EqualTo("fr-CA"));
            Assert.That(metadata.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(metadata.GenerationMode, Is.EqualTo(GenerationMode.Boundary));
            Assert.That(metadata.SafetyMode,
                Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(metadata.FictionalReserved, Is.True);
            Assert.That(metadata.SafeReserved, Is.True);
            Assert.That(metadata.AllocationStatus,
                Is.EqualTo(AllocationStatus.FictionalReserved));
            Assert.That(metadata.Validation.SafeReserved, Is.True);
            Assert.That(metadata.RequestedEdgeCase, Is.Empty);
            Assert.That(metadata.ViolatedRule, Is.Empty);
        });
    }

    [Test]
    public void EdgeSelectionSynchronizesRequestedEdgeAndViolatedRule()
    {
        EdgeProvider edge = Fake.Create(14901).Edge();
        EdgeSelectionResult result = edge.AdversarialFromContext(
            AdversarialTextContext.Header);
        GenerationMetadata metadata = result.Metadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Value, Is.EqualTo(result.SelectedEdge));
            Assert.That(metadata.GeneratedValue, Is.EqualTo(result.Edge.Value));
            Assert.That(metadata.ValueKind, Is.EqualTo("Injection"));
            Assert.That(metadata.Provider, Is.EqualTo("edge"));
            Assert.That(metadata.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(metadata.GenerationMode,
                Is.EqualTo(GenerationMode.Adversarial));
            Assert.That(metadata.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(metadata.ChecksumValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(metadata.ConstraintValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(metadata.RequestedEdgeCase,
                Is.EqualTo(result.SelectedEdge));
            Assert.That(metadata.ViolatedRule,
                Is.EqualTo(result.ViolatedRule).And.Not.Empty);
            Assert.That(metadata.Validation.Valid, Is.False);
            Assert.That(metadata.Validation.RequestedEdgeCase,
                Is.EqualTo(metadata.RequestedEdgeCase));
            Assert.That(metadata.Validation.ViolatedRule,
                Is.EqualTo(metadata.ViolatedRule));
        });
    }

    [Test]
    public void IdentifierEdgeSeparatesStructureFromChecksumFailure()
    {
        EdgeProvider edge = Fake.Create(14902).Edge();
        EdgeSelectionResult result = edge.Invalid();
        int attempts = 1;
        while (result.SelectedEdge != "wrong-checksum" && attempts < 2000)
        {
            result = edge.Invalid();
            attempts++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedEdge, Is.EqualTo("wrong-checksum"));
            Assert.That(result.Metadata.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.Metadata.ChecksumValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(result.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.Metadata.AllocationStatus,
                Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void SimResultsExposeDetailedGeneralizedValidation()
    {
        PhoneProvider phone = Fake.Create(14903).Phone();
        IccidResult validIccid = phone.IccidResult();
        char replacement = validIccid.Value[validIccid.Value.Length - 1] == '0'
            ? '1'
            : '0';
        string badValue = validIccid.Value.Substring(
            0,
            validIccid.Value.Length - 1) + replacement;
        IccidResult badIccid = phone.IccidResultFromValue(badValue);
        ImsiResult imsi = phone.ImsiResultFromMobileNetworkCodeLength(3);

        Assert.Multiple(() =>
        {
            Assert.That(validIccid.Metadata.GeneratedValue,
                Is.EqualTo(validIccid.Value));
            Assert.That(validIccid.Metadata.ValueKind, Is.EqualTo("iccid"));
            Assert.That(validIccid.Metadata.SourceLocale,
                Is.EqualTo("invariant"));
            Assert.That(validIccid.Metadata.Validation.Valid, Is.True);
            Assert.That(validIccid.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(badIccid.Metadata.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(badIccid.Metadata.ChecksumValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(badIccid.Metadata.Validation.Valid, Is.False);
            Assert.That(imsi.Metadata.ValueKind, Is.EqualTo("imsi"));
            Assert.That(imsi.Metadata.ChecksumValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(imsi.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(imsi.Metadata.SafeReserved, Is.True);
            Assert.That(imsi.Metadata.AllocationStatus,
                Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void PhoneRecordMetadataReportsLocaleSafetyAndCorrelation()
    {
        PhoneRecord reserved = Fake.Create(14904).Phone().Record();
        FakeOptions plausibleOptions = FakeOptions.Create("en-GB", 14905);
        plausibleOptions.SafetyMode = SafetyMode.SyntheticPlausible;
        PhoneRecord plausible = Fake.Create(plausibleOptions).Phone().Record();

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Metadata.Value,
                Is.EqualTo(reserved.Number));
            Assert.That(reserved.Metadata.GeneratedValue,
                Is.EqualTo(reserved.Number));
            Assert.That(reserved.Metadata.ValueKind,
                Is.EqualTo("phone-number"));
            Assert.That(reserved.Metadata.Locale, Is.EqualTo("en-US"));
            Assert.That(reserved.Metadata.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(reserved.Metadata.UsedLocaleFallback, Is.True);
            Assert.That(reserved.Metadata.SafeReserved, Is.True);
            Assert.That(reserved.Metadata.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(reserved.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(reserved.Metadata.Validation.Valid, Is.True);
            Assert.That(plausible.Metadata.Locale, Is.EqualTo("en-GB"));
            Assert.That(plausible.Metadata.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(plausible.Metadata.UsedLocaleFallback, Is.True);
            Assert.That(plausible.Metadata.SafeReserved, Is.False);
            Assert.That(plausible.Metadata.SafetyMode,
                Is.EqualTo(SafetyMode.SyntheticPlausible));
        });
    }

    [Test]
    public void ExplicitPhoneEdgeReportsTheRequestedCase()
    {
        PhoneEdgeCaseRecord edge = Fake.Create(14906).Phone().EdgeCase(
            PhoneEdgeCase.AmbiguousCategory);

        Assert.Multiple(() =>
        {
            Assert.That(edge.Metadata.Value, Is.EqualTo(edge.Number));
            Assert.That(edge.Metadata.ValueKind,
                Is.EqualTo("phone-edge-case"));
            Assert.That(edge.Metadata.RequestedEdgeCase,
                Is.EqualTo("AmbiguousCategory"));
            Assert.That(edge.Metadata.Validation.RequestedEdgeCase,
                Is.EqualTo("AmbiguousCategory"));
            Assert.That(edge.Metadata.Validation.Valid, Is.True);
            Assert.That(edge.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(edge.Metadata.SafeReserved, Is.True);
            Assert.That(edge.Metadata.ViolatedRule, Is.Empty);
        });
    }

    [Test]
    public void GeneralizedMetadataObjectsAreIndependentAcrossResults()
    {
        PhoneProvider firstProvider = Fake.Create(14907).Phone();
        PhoneProvider secondProvider = Fake.Create(14907).Phone();
        PhoneRecord first = firstProvider.Record();
        PhoneRecord second = secondProvider.Record();

        first.Metadata.ValueKind = "changed";
        first.Metadata.Validation.Detail = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.Metadata, Is.Not.SameAs(first.Metadata));
            Assert.That(second.Metadata.Validation,
                Is.Not.SameAs(first.Metadata.Validation));
            Assert.That(second.Metadata.ValueKind,
                Is.EqualTo("phone-number"));
            Assert.That(second.Metadata.Validation.Detail,
                Is.Not.EqualTo("changed"));
        });
    }
}
