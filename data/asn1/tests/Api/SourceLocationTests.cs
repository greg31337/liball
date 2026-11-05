using asn1;

namespace asn1.Tests;

public class SourceLocationTests
{
    [TestCase(0, 1, 1)]
    [TestCase(1, 1, 2)]
    [TestCase(3, 1, 3)]
    [TestCase(4, 1, 4)]
    [TestCase(5, 2, 1)]
    [TestCase(6, 2, 1)]
    [TestCase(7, 2, 2)]
    [TestCase(8, 3, 1)]
    [TestCase(9, 3, 2)]
    [TestCase(10, 4, 1)]
    [TestCase(11, 4, 2)]
    public void GetPositionNormalizesUnicodeAndLineEndings(int offset, int expectedLine, int expectedColumn)
    {
        AsnSourceUnit source = CreateUnicodeSource();

        AsnSourcePosition position = source.GetPosition(offset);

        Assert.That(position.SourceName, Is.EqualTo("unicode.asn"));
        Assert.That(position.Offset, Is.EqualTo(offset));
        Assert.That(position.Line, Is.EqualTo(expectedLine));
        Assert.That(position.Column, Is.EqualTo(expectedColumn));
    }

    [Test]
    public void GetPositionRejectsOffsetInsideSurrogatePair()
    {
        AsnSourceUnit source = CreateUnicodeSource();

        Assert.That(() => source.GetPosition(2), Throws.TypeOf<ArgumentException>());
    }

    [TestCase(-1)]
    [TestCase(12)]
    public void GetPositionRejectsOffsetOutsideText(int offset)
    {
        AsnSourceUnit source = CreateUnicodeSource();

        Assert.That(() => source.GetPosition(offset), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void GetSpanCreatesHalfOpenRangeInOneSource()
    {
        AsnSourceUnit source = CreateUnicodeSource();

        AsnSourceSpan span = source.GetSpan(1, 7);

        Assert.That(span.Start.SourceName, Is.EqualTo("unicode.asn"));
        Assert.That(span.End.SourceName, Is.EqualTo("unicode.asn"));
        Assert.That(span.Start.Offset, Is.EqualTo(1));
        Assert.That(span.End.Offset, Is.EqualTo(7));
        Assert.That(span.Start.Line, Is.EqualTo(1));
        Assert.That(span.Start.Column, Is.EqualTo(2));
        Assert.That(span.End.Line, Is.EqualTo(2));
        Assert.That(span.End.Column, Is.EqualTo(2));
    }

    [Test]
    public void GetSpanRejectsReversedOffsets()
    {
        AsnSourceUnit source = CreateUnicodeSource();

        Assert.That(() => source.GetSpan(7, 1), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void PositionsRetainDistinctMultiFileIdentity()
    {
        AsnSourceUnit first = new AsnSourceUnit { Name = "first.asn", Text = "A" };
        AsnSourceUnit second = new AsnSourceUnit { Name = "second.asn", Text = "A" };

        AsnSourcePosition firstPosition = first.GetPosition(1);
        AsnSourcePosition secondPosition = second.GetPosition(1);

        Assert.That(firstPosition.SourceName, Is.EqualTo("first.asn"));
        Assert.That(secondPosition.SourceName, Is.EqualTo("second.asn"));
        Assert.That(firstPosition.Offset, Is.EqualTo(secondPosition.Offset));
    }

    [Test]
    public void DiagnosticCarriesPrimaryAndRelatedLocations()
    {
        AsnSourceUnit first = new AsnSourceUnit { Name = "first.asn", Text = "First" };
        AsnSourceUnit second = new AsnSourceUnit { Name = "second.asn", Text = "Second" };
        AsnDiagnosticRelatedLocation related = new AsnDiagnosticRelatedLocation
        {
            Message = "Originally declared here.",
            Span = first.GetSpan(0, 5)
        };
        AsnDiagnostic diagnostic = new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Binding,
            Code = "ASN1001",
            Message = "Duplicate declaration.",
            Span = second.GetSpan(0, 6),
            RelatedLocations = [related]
        };
        AsnCompilationInput input = new AsnCompilationInput
        {
            SourceUnits = [first, second]
        };

        Assert.That(input.SourceUnits, Has.Length.EqualTo(2));
        Assert.That(diagnostic.Span?.Start.SourceName, Is.EqualTo("second.asn"));
        Assert.That(diagnostic.RelatedLocations, Has.Length.EqualTo(1));
        Assert.That(diagnostic.RelatedLocations[0].Span.Start.SourceName, Is.EqualTo("first.asn"));
        Assert.That(diagnostic.RelatedLocations[0].Message, Is.EqualTo("Originally declared here."));
    }

    [Test]
    public void SourceDataDefaultsAreUsable()
    {
        AsnCompilationInput input = new AsnCompilationInput();
        AsnSourcePosition position = new AsnSourcePosition();
        AsnSourceSpan span = new AsnSourceSpan();
        AsnDiagnostic diagnostic = new AsnDiagnostic();

        Assert.That(input.SourceUnits, Is.Empty);
        Assert.That(position.SourceName, Is.Empty);
        Assert.That(position.Offset, Is.Zero);
        Assert.That(position.Line, Is.EqualTo(1));
        Assert.That(position.Column, Is.EqualTo(1));
        Assert.That(span.Start, Is.Not.Null);
        Assert.That(span.End, Is.Not.Null);
        Assert.That(diagnostic.Code, Is.Empty);
        Assert.That(diagnostic.Message, Is.Empty);
        Assert.That(diagnostic.Span, Is.Null);
        Assert.That(diagnostic.RelatedLocations, Is.Empty);
    }

    private static AsnSourceUnit CreateUnicodeSource()
    {
        return new AsnSourceUnit
        {
            Name = "unicode.asn",
            Text = "A😀B\r\nC\nD\rE"
        };
    }
}
