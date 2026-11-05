using raw;

namespace raw.Tests;

public class RationalTests
{
    [Test]
    public void ConstructorsPreserveExactUnsignedAndSignedComponents()
    {
        RawRational unsignedValue = new RawRational(uint.MaxValue, 4000000000U);
        RawRational signedValue = RawRational.CreateSigned(int.MinValue, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValue.Numerator, Is.EqualTo(uint.MaxValue));
            Assert.That(unsignedValue.Denominator, Is.EqualTo(4000000000U));
            Assert.That(unsignedValue.Signed, Is.False);
            Assert.That(unsignedValue.Validate().IsValid(), Is.True);
            Assert.That(signedValue.Numerator, Is.EqualTo(int.MinValue));
            Assert.That(signedValue.Denominator, Is.EqualTo(int.MaxValue));
            Assert.That(signedValue.Signed, Is.True);
            Assert.That(signedValue.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void FloatingConversionsHandleUnsignedAndNegativeSignedValues()
    {
        RawRational unsignedValue = new RawRational(1U, 8U);
        RawRational signedValue = RawRational.CreateSigned(-3, 2);
        RawRational negativeDenominator = RawRational.CreateSigned(3, -2);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValue.ToDouble(), Is.EqualTo(0.125));
            Assert.That(unsignedValue.ToSingle(), Is.EqualTo(0.125F));
            Assert.That(signedValue.ToDouble(), Is.EqualTo(-1.5));
            Assert.That(negativeDenominator.ToDouble(), Is.EqualTo(-1.5));
        });
    }

    [Test]
    public void ExactEqualityIncludesRepresentationAndSignedness()
    {
        RawRational value = new RawRational(1U, 2U);
        RawRational same = new RawRational(1U, 2U);
        RawRational scaled = new RawRational(2U, 4U);
        RawRational signed = RawRational.CreateSigned(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(value.EqualsExact(same), Is.True);
            Assert.That(value.EqualsExact(scaled), Is.False);
            Assert.That(value.EqualsExact(signed), Is.False);
            Assert.That(value.EqualsExact(null), Is.False);
        });
    }

    [Test]
    public void NumericEqualityUsesExactCrossProductsWithoutOverflow()
    {
        RawRational maximumRatio = new RawRational(uint.MaxValue, uint.MaxValue);
        RawRational one = new RawRational(1U, 1U);
        RawRational scaled = new RawRational(2U, 4U);
        RawRational negativeSigns = RawRational.CreateSigned(-1, -2);
        RawRational different = new RawRational(3U, 4U);

        Assert.Multiple(() =>
        {
            Assert.That(maximumRatio.EqualsValue(one), Is.True);
            Assert.That(scaled.EqualsValue(negativeSigns), Is.True);
            Assert.That(scaled.EqualsValue(different), Is.False);
            Assert.That(scaled.EqualsValue(null), Is.False);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ZeroDenominatorIsPreservedAndReportedWithDedicatedCode(bool signed)
    {
        RawRational value;
        if (signed)
        {
            value = RawRational.CreateSigned(7, 0);
        }
        else
        {
            value = new RawRational(7U, 0U);
        }

        RawValidationReport report = value.Validate();
        RawFormatException? conversion = Assert.Throws<RawFormatException>(() => value.ToDouble());
        RawFormatException? equality = Assert.Throws<RawFormatException>(() => value.EqualsValue(new RawRational(7U, 1U)));

        Assert.Multiple(() =>
        {
            Assert.That(value.Denominator, Is.EqualTo(0));
            Assert.That(report.IsValid(), Is.False);
            Assert.That(report.Messages, Has.Length.EqualTo(1));
            Assert.That((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Code, Is.EqualTo(RawDiagnosticCode.InvalidRationalDenominator));
            Assert.That(report.Messages[0].Context, Is.EqualTo("Rational.Denominator"));
            Assert.That((conversion ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidRationalDenominator));
            Assert.That((equality ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidRationalDenominator));
        });
    }

    [Test]
    public void ValidationDetectsComponentsOutsideDeclaredRepresentation()
    {
        RawRational unsignedValue = new RawRational(1U, 1U);
        unsignedValue.Numerator = -1;
        RawRational signedValue = RawRational.CreateSigned(1, 1);
        signedValue.Denominator = (long)int.MaxValue + 1;

        RawValidationReport unsignedReport = unsignedValue.Validate();
        RawValidationReport signedReport = signedValue.Validate();
        RawFormatException? conversion = Assert.Throws<RawFormatException>(() => signedValue.ToDouble());

        Assert.Multiple(() =>
        {
            Assert.That(unsignedReport.IsValid(), Is.False);
            Assert.That((unsignedReport.Messages ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That(unsignedReport.Messages[0].Context, Is.EqualTo("Rational.Numerator"));
            Assert.That(signedReport.IsValid(), Is.False);
            Assert.That((signedReport.Messages ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Context, Is.EqualTo("Rational.Denominator"));
            Assert.That((conversion ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        });
    }
}
