using xml;

namespace xml.Tests;

[TestFixture]
public class ParserWorkLimiterTests
{
    [Test]
    public void EnforcesZeroBasedElementDepthAgainstTheConfiguredMaximum()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumDepth = 2;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);

        limiter.CheckXmlDepth(0);
        limiter.CheckXmlDepth(1);
        XmlLimitException exception = Assert.Throws<XmlLimitException>(() => limiter.CheckXmlDepth(2))!;

        AssertLimitException(exception, "element depth", 2, 3);
    }

    [Test]
    public void EnforcesAttributesPerElement()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumAttributesPerElement = 2;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);

        limiter.CheckXmlAttributeCount(2);
        XmlLimitException exception = Assert.Throws<XmlLimitException>(() => limiter.CheckXmlAttributeCount(3))!;

        AssertLimitException(exception, "attributes per element", 2, 3);
    }

    [Test]
    public void EnforcesNameTextAndTokenLengthsAtTheirBoundaries()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumNameLength = 4;
        limits.MaximumTextLength = 5;
        limits.MaximumTokenLength = 6;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);

        limiter.CheckXmlNameLength("name");
        limiter.CheckXmlTextLength("value");
        limiter.CheckXmlTokenLength(6);

        AssertLimitException(Assert.Throws<XmlLimitException>(() => limiter.CheckXmlNameLength("names"))!, "XML name length", 4, 5);
        AssertLimitException(Assert.Throws<XmlLimitException>(() => limiter.CheckXmlTextLength("values"))!, "text node length", 5, 6);
        AssertLimitException(Assert.Throws<XmlLimitException>(() => limiter.CheckXmlTokenLength(7))!, "lexical token length", 6, 7);
    }

    [Test]
    public void TokenObservationChecksActualLexemeAndNameLengths()
    {
        XmlSecurityLimits tokenLimits = CreateLimits();
        tokenLimits.MaximumTokenLength = 4;
        ParserWorkLimiter tokenLimiter = new ParserWorkLimiter(tokenLimits, null);
        XmlToken inconsistentLength = new XmlToken(XmlTokenKind.Text, null, "value", "value", 0, 1, 0, false, '\0', false, null);

        Assert.Throws<XmlLimitException>(() => tokenLimiter.ObserveXmlToken(inconsistentLength));

        XmlSecurityLimits nameLimits = CreateLimits();
        nameLimits.MaximumNameLength = 3;
        ParserWorkLimiter nameLimiter = new ParserWorkLimiter(nameLimits, null);
        XmlScanner scanner = new XmlScanner("<long/>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        scanner.ScanNextXmlToken();
        XmlToken name = scanner.ScanNextXmlToken();

        Assert.Throws<XmlLimitException>(() => nameLimiter.ObserveXmlToken(name));
    }

    [Test]
    public void AccumulatesInputCharactersAndBytesAtomically()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumInputCharacters = 5;
        limits.MaximumInputBytes = 6;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);

        limiter.ObserveXmlInput(3, 4);
        Assert.That(limiter.InputCharacterCount, Is.EqualTo(3));
        Assert.That(limiter.InputByteCount, Is.EqualTo(4));

        Assert.Throws<XmlLimitException>(() => limiter.ObserveXmlInput(3, 0));
        Assert.That(limiter.InputCharacterCount, Is.EqualTo(3));
        Assert.That(limiter.InputByteCount, Is.EqualTo(4));

        Assert.Throws<XmlLimitException>(() => limiter.ObserveXmlInput(2, 3));
        Assert.That(limiter.InputCharacterCount, Is.EqualTo(3));
        Assert.That(limiter.InputByteCount, Is.EqualTo(4));

        limiter.ObserveXmlInput(2, 2);
        Assert.That(limiter.InputCharacterCount, Is.EqualTo(5));
        Assert.That(limiter.InputByteCount, Is.EqualTo(6));
    }

    [Test]
    public void CumulativeChecksAvoidOverflowAndLeaveCountersUnchanged()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumInputCharacters = long.MaxValue;
        limits.MaximumInputBytes = long.MaxValue;
        limits.MaximumNodeCount = long.MaxValue;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);
        limiter.ObserveXmlInput(long.MaxValue, long.MaxValue);
        limiter.ConsumeXmlNodes(long.MaxValue);

        Assert.Throws<XmlLimitException>(() => limiter.ObserveXmlInput(1, 0));
        Assert.Throws<XmlLimitException>(() => limiter.ObserveXmlInput(0, 1));
        Assert.Throws<XmlLimitException>(() => limiter.ConsumeXmlNodes(1));
        Assert.That(limiter.InputCharacterCount, Is.EqualTo(long.MaxValue));
        Assert.That(limiter.InputByteCount, Is.EqualTo(long.MaxValue));
        Assert.That(limiter.NodeCount, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CountsNodeCreatingEventsWithoutCountingEndBoundariesTwice()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumNodeCount = 4;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);
        XmlEvent start = CreateStartElementEvent(2);
        XmlEvent text = CreateValueEvent(XmlEventKind.Text, "text");
        XmlEvent end = CreateValueEvent(XmlEventKind.EndElement, null);
        XmlEvent extra = CreateValueEvent(XmlEventKind.Comment, "comment");

        limiter.ObserveXmlEvent(start);
        limiter.ObserveXmlEvent(text);
        limiter.ObserveXmlEvent(end);

        Assert.That(limiter.NodeCount, Is.EqualTo(4));
        Assert.Throws<XmlLimitException>(() => limiter.ObserveXmlEvent(extra));
        Assert.That(limiter.NodeCount, Is.EqualTo(4));
    }

    [Test]
    public void EventObservationEnforcesTextLengthAndElementShape()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumDepth = 1;
        limits.MaximumAttributesPerElement = 1;
        limits.MaximumNameLength = 3;
        limits.MaximumTextLength = 4;

        ParserWorkLimiter depth = new ParserWorkLimiter(limits, null);
        XmlEvent deep = CreateStartElementEvent(0);
        deep.Depth = 1;
        Assert.Throws<XmlLimitException>(() => depth.ObserveXmlEvent(deep));

        ParserWorkLimiter attributes = new ParserWorkLimiter(limits, null);
        Assert.Throws<XmlLimitException>(() => attributes.ObserveXmlEvent(CreateStartElementEvent(2)));

        ParserWorkLimiter name = new ParserWorkLimiter(limits, null);
        XmlEvent longName = CreateStartElementEvent(0);
        longName.Name!.QualifiedName = "long";
        Assert.Throws<XmlLimitException>(() => name.ObserveXmlEvent(longName));

        ParserWorkLimiter text = new ParserWorkLimiter(limits, null);
        Assert.Throws<XmlLimitException>(() => text.ObserveXmlEvent(CreateValueEvent(XmlEventKind.CData, "value")));
    }

    [Test]
    public void ElementTagObservationChecksDepthAttributesAndNames()
    {
        XmlElementTag tag = ParseElementTag("<root first='1' second='2'>");

        XmlSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaximumDepth = 0;
        Assert.Throws<XmlLimitException>(() => new ParserWorkLimiter(depthLimits, null).ObserveXmlElementTag(tag));

        XmlSecurityLimits attributeLimits = CreateLimits();
        attributeLimits.MaximumAttributesPerElement = 1;
        Assert.Throws<XmlLimitException>(() => new ParserWorkLimiter(attributeLimits, null).ObserveXmlElementTag(tag));

        XmlSecurityLimits nameLimits = CreateLimits();
        nameLimits.MaximumNameLength = 5;
        Assert.Throws<XmlLimitException>(() => new ParserWorkLimiter(nameLimits, null).ObserveXmlElementTag(tag));
    }

    [Test]
    public void PollsCancellationAtEveryConfiguredWorkBoundary()
    {
        CountingCancellation cancellation = new CountingCancellation(100);
        XmlSecurityLimits limits = CreateLimits();
        limits.CancellationPollingInterval = 3;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, cancellation);

        limiter.AdvanceXmlParserWork(2);
        Assert.That(cancellation.PollCount, Is.Zero);
        Assert.That(limiter.WorkSinceCancellationPoll, Is.EqualTo(2));

        limiter.AdvanceXmlParserWork(1);
        Assert.That(cancellation.PollCount, Is.EqualTo(1));
        Assert.That(limiter.WorkSinceCancellationPoll, Is.Zero);

        limiter.AdvanceXmlParserWork(8);
        Assert.That(cancellation.PollCount, Is.EqualTo(3));
        Assert.That(limiter.WorkSinceCancellationPoll, Is.EqualTo(2));
    }

    [Test]
    public void StopsAtTheFirstPollingBoundaryThatReportsCancellation()
    {
        CountingCancellation cancellation = new CountingCancellation(2);
        XmlSecurityLimits limits = CreateLimits();
        limits.CancellationPollingInterval = 4;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, cancellation);

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => limiter.AdvanceXmlParserWork(20))!;

        Assert.That(exception.Message, Does.Contain("canceled"));
        Assert.That(cancellation.PollCount, Is.EqualTo(2));
        Assert.That(limiter.WorkSinceCancellationPoll, Is.Zero);
    }

    [Test]
    public void SupportsAnExplicitImmediateCancellationPoll()
    {
        CountingCancellation cancellation = new CountingCancellation(1);
        ParserWorkLimiter limiter = new ParserWorkLimiter(CreateLimits(), cancellation);

        Assert.Throws<OperationCanceledException>(() => limiter.PollXmlCancellation());
        Assert.That(cancellation.PollCount, Is.EqualTo(1));
    }

    [Test]
    public void TracksLargeWorkReportsWithoutOverflowWhenNoCancellationCallbackExists()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.CancellationPollingInterval = 3;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);

        limiter.AdvanceXmlParserWork(long.MaxValue);

        Assert.That(limiter.WorkSinceCancellationPoll, Is.EqualTo(long.MaxValue % 3));
    }

    [Test]
    public void SnapshotsLimitValuesAtConstruction()
    {
        XmlSecurityLimits limits = CreateLimits();
        limits.MaximumNameLength = 3;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, null);
        limits.MaximumNameLength = 100;

        Assert.Throws<XmlLimitException>(() => limiter.CheckXmlNameLength("four"));
    }

    [Test]
    public void ConstructorRejectsInvalidLimitsAndPollingIntervals()
    {
        XmlSecurityLimits depth = CreateLimits();
        depth.MaximumDepth = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(depth, null));

        XmlSecurityLimits nodes = CreateLimits();
        nodes.MaximumNodeCount = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(nodes, null));

        XmlSecurityLimits attributes = CreateLimits();
        attributes.MaximumAttributesPerElement = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(attributes, null));

        XmlSecurityLimits names = CreateLimits();
        names.MaximumNameLength = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(names, null));

        XmlSecurityLimits text = CreateLimits();
        text.MaximumTextLength = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(text, null));

        XmlSecurityLimits token = CreateLimits();
        token.MaximumTokenLength = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(token, null));

        XmlSecurityLimits characters = CreateLimits();
        characters.MaximumInputCharacters = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(characters, null));

        XmlSecurityLimits bytes = CreateLimits();
        bytes.MaximumInputBytes = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(bytes, null));

        XmlSecurityLimits polling = CreateLimits();
        polling.CancellationPollingInterval = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParserWorkLimiter(polling, null));
    }

    [Test]
    public void RejectsNegativeObservedCountsAndWork()
    {
        ParserWorkLimiter limiter = new ParserWorkLimiter(CreateLimits(), null);

        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.ObserveXmlInput(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.ObserveXmlInput(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.CheckXmlDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.CheckXmlAttributeCount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.CheckXmlTokenLength(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.ConsumeXmlNodes(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AdvanceXmlParserWork(-1));
    }

    private static XmlSecurityLimits CreateLimits()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 100;
        limits.MaximumNodeCount = 100;
        limits.MaximumAttributesPerElement = 100;
        limits.MaximumNameLength = 100;
        limits.MaximumTextLength = 100;
        limits.MaximumTokenLength = 100;
        limits.MaximumInputCharacters = 100;
        limits.MaximumInputBytes = 100;
        limits.CancellationPollingInterval = 10;
        return limits;
    }

    private static XmlEvent CreateStartElementEvent(int attributeCount)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.StartElement;
        item.NodeKind = XmlNodeKind.Element;
        item.Name = CreateName("root");
        item.Attributes = new XmlAttributeData[attributeCount];
        int index = 0;
        while (index < attributeCount)
        {
            XmlAttributeData attribute = new XmlAttributeData();
            attribute.Name = CreateName("a" + index);
            item.Attributes[index] = attribute;
            index = index + 1;
        }

        return item;
    }

    private static XmlEvent CreateValueEvent(XmlEventKind kind, string? value)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        item.Value = value;
        return item;
    }

    private static XmlName CreateName(string qualifiedName)
    {
        XmlName name = new XmlName();
        name.LocalName = qualifiedName;
        name.QualifiedName = qualifiedName;
        return name;
    }

    private static XmlElementTag ParseElementTag(string text)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        return new XmlElementTagParser(scanner, true, null).ParseNextXmlElementTag();
    }

    private static void AssertLimitException(XmlLimitException exception, string dimension, long maximum, long attempted)
    {
        Assert.That(exception.Message, Does.Contain(dimension));
        Assert.That(exception.Message, Does.Contain(maximum.ToString()));
        Assert.That(exception.Message, Does.Contain(attempted.ToString()));
        Assert.That(exception.Diagnostic, Is.Not.Null);
        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
        Assert.That(exception.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(exception.Diagnostic.Message, Is.EqualTo(exception.Message));
        Assert.That(exception.Location, Is.Null);
    }

    private class CountingCancellation : IXmlCancellation
    {
        public int CancelAtPoll;
        public int PollCount;

        public CountingCancellation(int cancelAtPoll)
        {
            CancelAtPoll = cancelAtPoll;
            PollCount = 0;
        }

        public bool IsXmlCancellationRequested()
        {
            PollCount = PollCount + 1;
            return PollCount >= CancelAtPoll;
        }
    }
}
