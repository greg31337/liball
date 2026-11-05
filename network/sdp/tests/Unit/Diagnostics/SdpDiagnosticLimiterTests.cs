namespace sdp.Tests.Unit.Diagnostics;

public class SdpDiagnosticLimiterTests
{
    [Test]
    public void DiagnosticBuilder_PreservesPayloadReferences()
    {
        SdpSourcePath path = new SdpSourcePath
        {
            Scope = SdpScope.Media,
            MediaIndex = 2,
            AttributeName = "candidate"
        };
        SdpSourceLocation location = new SdpSourceLocation
        {
            CharacterOffset = 40,
            ByteOffset = 42,
            LineNumber = 5,
            ColumnNumber = 3
        };

        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(
            SdpDiagnosticCodes.ParseMalformedAttribute,
            SdpValidationSeverity.Error,
            SdpScope.Media,
            'a',
            "candidate",
            2,
            "The candidate attribute is malformed.",
            path,
            location);
        SdpValidationIssue issue = SdpDiagnosticBuilder.CreateValidationIssue(
            SdpDiagnosticCodes.ValidationAttribute,
            SdpValidationSeverity.Warning,
            SdpScope.Media,
            'a',
            "candidate",
            2,
            "The candidate attribute is not recommended.",
            path,
            location);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedAttribute));
            Assert.That(diagnostic.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(diagnostic.FieldName, Is.EqualTo('a'));
            Assert.That(diagnostic.AttributeName, Is.EqualTo("candidate"));
            Assert.That(diagnostic.MediaIndex, Is.EqualTo(2));
            Assert.That(diagnostic.Path, Is.SameAs(path));
            Assert.That(diagnostic.Location, Is.SameAs(location));
            Assert.That(issue.Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(issue.Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(issue.Path, Is.SameAs(path));
            Assert.That(issue.Location, Is.SameAs(location));
        });
    }

    [Test]
    public void DiagnosticLimiter_ReplacesLastEntryWithOneTerminalMarkerOnOverflow()
    {
        List<SdpDiagnostic> diagnostics = new List<SdpDiagnostic>();
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(diagnostics, 3);
        SdpDiagnostic first = CreateDiagnostic("FIRST", 1);
        SdpDiagnostic second = CreateDiagnostic("SECOND", 2);
        SdpDiagnostic third = CreateDiagnostic("THIRD", 3);
        SdpDiagnostic omitted = CreateDiagnostic("OMITTED", 4);

        bool firstAdded = limiter.AddDiagnostic(first);
        bool secondAdded = limiter.AddDiagnostic(second);
        bool thirdAdded = limiter.AddDiagnostic(third);
        bool omittedAdded = limiter.AddDiagnostic(omitted);
        bool laterAdded = limiter.AddDiagnostic(CreateDiagnostic("LATER", 5));

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.True);
            Assert.That(secondAdded, Is.True);
            Assert.That(thirdAdded, Is.True);
            Assert.That(omittedAdded, Is.False);
            Assert.That(laterAdded, Is.False);
            Assert.That(limiter.DiagnosticLimitReached, Is.True);
            Assert.That(diagnostics, Has.Count.EqualTo(3));
            Assert.That(diagnostics.Select(x => x.Code), Is.EqualTo(new[]
            {
                "FIRST",
                "SECOND",
                SdpDiagnosticCodes.DiagnosticLimitReached
            }));
            Assert.That(diagnostics.Count(x => x.Code == SdpDiagnosticCodes.DiagnosticLimitReached), Is.EqualTo(1));
            Assert.That(diagnostics[2].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(diagnostics[2].Location.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[2].Path.MediaIndex, Is.EqualTo(4));
            Assert.That(diagnostics[2].Location, Is.Not.SameAs(omitted.Location));
            Assert.That(diagnostics[2].Path, Is.Not.SameAs(omitted.Path));
        });
    }

    [Test]
    public void DiagnosticLimiter_NormalizesNonpositiveCapacityToOneMarkerSlot()
    {
        List<SdpDiagnostic> diagnostics = new List<SdpDiagnostic>();
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(diagnostics, 0);

        limiter.AddDiagnostic(CreateDiagnostic("FIRST", 1));
        limiter.AddDiagnostic(CreateDiagnostic("OMITTED", 2));
        limiter.AddDiagnostic(CreateDiagnostic("LATER", 3));

        Assert.Multiple(() =>
        {
            Assert.That(limiter.MaximumDiagnosticCount, Is.EqualTo(1));
            Assert.That(limiter.DiagnosticLimitReached, Is.True);
            Assert.That(diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
        });
    }

    [Test]
    public void ValidationIssueLimiter_EmitsOneTerminalIssueAndStopsCollecting()
    {
        List<SdpValidationIssue> issues = new List<SdpValidationIssue>();
        SdpValidationIssueLimiter limiter = new SdpValidationIssueLimiter(issues, 2);
        SdpValidationIssue first = CreateIssue("FIRST", 1);
        SdpValidationIssue second = CreateIssue("SECOND", 2);
        SdpValidationIssue omitted = CreateIssue("OMITTED", 3);

        limiter.AddIssue(first);
        limiter.AddIssue(second);
        bool omittedAdded = limiter.AddIssue(omitted);
        bool laterAdded = limiter.AddIssue(CreateIssue("LATER", 4));

        Assert.Multiple(() =>
        {
            Assert.That(omittedAdded, Is.False);
            Assert.That(laterAdded, Is.False);
            Assert.That(limiter.DiagnosticLimitReached, Is.True);
            Assert.That(issues, Has.Count.EqualTo(2));
            Assert.That(issues[0], Is.SameAs(first));
            Assert.That(issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(issues[1].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(issues[1].MediaIndex, Is.EqualTo(3));
            Assert.That(issues[1].Location.LineNumber, Is.EqualTo(3));
            Assert.That(issues.Count(x => x.Code == SdpDiagnosticCodes.DiagnosticLimitReached), Is.EqualTo(1));
        });
    }

    static SdpDiagnostic CreateDiagnostic(string code, int index)
    {
        SdpDiagnostic diagnostic = new SdpDiagnostic
        {
            Code = code,
            Severity = SdpValidationSeverity.Warning,
            Scope = SdpScope.Media,
            MediaIndex = index
        };
        diagnostic.Path.MediaIndex = index;
        diagnostic.Location.LineNumber = index;
        return diagnostic;
    }

    static SdpValidationIssue CreateIssue(string code, int index)
    {
        SdpValidationIssue issue = new SdpValidationIssue
        {
            Code = code,
            Severity = SdpValidationSeverity.Warning,
            Scope = SdpScope.Media,
            MediaIndex = index
        };
        issue.Path.MediaIndex = index;
        issue.Location.LineNumber = index;
        return issue;
    }
}
