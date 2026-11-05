namespace sdp.Tests.Api;

public class SdpDiagnosticAndComparisonDataTests
{
    [Test]
    public void SourcePath_UsesExplicitUnknownIndexesAndPreservesModelAddress()
    {
        SdpSourcePath path = new SdpSourcePath();

        Assert.Multiple(() =>
        {
            Assert.That(path.Scope, Is.EqualTo(SdpScope.Unknown));
            Assert.That(path.MediaIndex, Is.EqualTo(-1));
            Assert.That(path.MediaId, Is.Empty);
            Assert.That(path.FieldName, Is.EqualTo('\0'));
            Assert.That(path.AttributeName, Is.Empty);
            Assert.That(path.OccurrenceIndex, Is.EqualTo(-1));
            Assert.That(path.ValueIndex, Is.EqualTo(-1));
        });

        path.Scope = SdpScope.Media;
        path.MediaIndex = 2;
        path.MediaId = "video-main";
        path.FieldName = 'a';
        path.AttributeName = "rtpmap";
        path.OccurrenceIndex = 1;
        path.ValueIndex = 0;

        Assert.Multiple(() =>
        {
            Assert.That(path.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(path.MediaIndex, Is.EqualTo(2));
            Assert.That(path.MediaId, Is.EqualTo("video-main"));
            Assert.That(path.FieldName, Is.EqualTo('a'));
            Assert.That(path.AttributeName, Is.EqualTo("rtpmap"));
            Assert.That(path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(path.ValueIndex, Is.Zero);
        });
    }

    [Test]
    public void DiagnosticAndValidationIssue_KeepIndependentPathsAndLocations()
    {
        SdpDiagnostic diagnostic = new SdpDiagnostic();
        SdpValidationIssue issue = new SdpValidationIssue();
        diagnostic.Code = SdpDiagnosticCodes.ParseMalformedAttribute;
        diagnostic.Severity = SdpValidationSeverity.Error;
        diagnostic.Scope = SdpScope.Media;
        diagnostic.FieldName = 'a';
        diagnostic.AttributeName = "candidate";
        diagnostic.MediaIndex = 1;
        diagnostic.Detail = "The candidate attribute is malformed.";
        diagnostic.Path.MediaIndex = 1;
        diagnostic.Path.AttributeName = "candidate";
        diagnostic.Location.LineNumber = 12;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedAttribute));
            Assert.That(diagnostic.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(diagnostic.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(diagnostic.FieldName, Is.EqualTo('a'));
            Assert.That(diagnostic.AttributeName, Is.EqualTo("candidate"));
            Assert.That(diagnostic.MediaIndex, Is.EqualTo(1));
            Assert.That(diagnostic.Detail, Does.Not.Contain("secret"));
            Assert.That(diagnostic.Path.MediaIndex, Is.EqualTo(1));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(12));
            Assert.That(issue.Path.MediaIndex, Is.EqualTo(-1));
            Assert.That(issue.Location.LineNumber, Is.Zero);
            Assert.That(diagnostic.Path, Is.Not.SameAs(issue.Path));
            Assert.That(diagnostic.Location, Is.Not.SameAs(issue.Location));
        });
    }

    [Test]
    public void Difference_RepresentsKindPathValuesAndSensitiveRedaction()
    {
        SdpDifference changed = new SdpDifference
        {
            Kind = SdpDifferenceKind.Changed,
            OriginalValue = "sendrecv",
            ComparedValue = "recvonly",
            Detail = "The effective direction changed."
        };
        changed.Path.Scope = SdpScope.Media;
        changed.Path.MediaIndex = 0;
        changed.Path.FieldName = 'a';
        changed.Path.AttributeName = "sendrecv";

        SdpDifference redacted = new SdpDifference
        {
            Kind = SdpDifferenceKind.Changed,
            ValuesRedacted = true,
            Detail = "The encryption key changed."
        };
        redacted.Path.FieldName = 'k';
        SdpDifference empty = new SdpDifference();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Kind, Is.EqualTo(SdpDifferenceKind.Changed));
            Assert.That(changed.Path.MediaIndex, Is.Zero);
            Assert.That(changed.Path.AttributeName, Is.EqualTo("sendrecv"));
            Assert.That(changed.OriginalValue, Is.EqualTo("sendrecv"));
            Assert.That(changed.ComparedValue, Is.EqualTo("recvonly"));
            Assert.That(changed.ValuesRedacted, Is.False);
            Assert.That(redacted.ValuesRedacted, Is.True);
            Assert.That(redacted.OriginalValue, Is.Empty);
            Assert.That(redacted.ComparedValue, Is.Empty);
            Assert.That(redacted.Path.FieldName, Is.EqualTo('k'));
            Assert.That(changed.Path, Is.Not.SameAs(redacted.Path));
            Assert.That(empty.Kind, Is.EqualTo(SdpDifferenceKind.Unknown));
            Assert.That(empty.OriginalValue, Is.Empty);
            Assert.That(empty.ComparedValue, Is.Empty);
            Assert.That(empty.ValuesRedacted, Is.False);
            Assert.That(empty.Detail, Is.Empty);
            Assert.That(empty.Path, Is.Not.SameAs(changed.Path));
        });
    }

    [Test]
    public void ComparisonData_InitializesIndependentOptionsAndDifferenceCollections()
    {
        SdpComparisonResult first = new SdpComparisonResult();
        SdpComparisonResult second = new SdpComparisonResult();
        first.Equal = true;
        first.AppliedOptions.CompareSemanticValues = true;
        first.AppliedOptions.IgnoreOriginSessionVersion = true;
        first.AppliedOptions.IgnoreInsignificantOrdering = true;
        first.AppliedOptions.IgnoreRawFormatting = true;
        first.AppliedOptions.IgnoredAttributeNames.Add("ice-pwd");
        first.Differences.Add(new SdpDifference { Kind = SdpDifferenceKind.Reordered });

        Assert.Multiple(() =>
        {
            Assert.That(first.Equal, Is.True);
            Assert.That(first.AppliedOptions.CompareSemanticValues, Is.True);
            Assert.That(first.AppliedOptions.IgnoreOriginSessionVersion, Is.True);
            Assert.That(first.AppliedOptions.IgnoreInsignificantOrdering, Is.True);
            Assert.That(first.AppliedOptions.IgnoreRawFormatting, Is.True);
            Assert.That(first.AppliedOptions.IgnoredAttributeNames, Is.EqualTo(new[] { "ice-pwd" }));
            Assert.That(first.Differences[0].Kind, Is.EqualTo(SdpDifferenceKind.Reordered));
            Assert.That(second.Equal, Is.False);
            Assert.That(second.AppliedOptions.CompareSemanticValues, Is.False);
            Assert.That(second.AppliedOptions.IgnoreOriginSessionVersion, Is.False);
            Assert.That(second.AppliedOptions.IgnoreInsignificantOrdering, Is.False);
            Assert.That(second.AppliedOptions.IgnoreRawFormatting, Is.False);
            Assert.That(second.AppliedOptions.IgnoredAttributeNames, Is.Empty);
            Assert.That(second.Differences, Is.Empty);
            Assert.That(first.AppliedOptions, Is.Not.SameAs(second.AppliedOptions));
            Assert.That(first.AppliedOptions.IgnoredAttributeNames, Is.Not.SameAs(second.AppliedOptions.IgnoredAttributeNames));
            Assert.That(first.Differences, Is.Not.SameAs(second.Differences));
        });
    }

    [Test]
    public void BufferWriteResult_ReportsExactCapacityWithoutPartialSuccessAmbiguity()
    {
        SdpBufferWriteResult first = new SdpBufferWriteResult
        {
            Success = false,
            BufferTooSmall = true,
            BytesWritten = 0,
            RequiredBytes = 128,
            DiagnosticLimitReached = false
        };
        SdpBufferWriteResult second = new SdpBufferWriteResult();
        first.Diagnostics.Add(new SdpDiagnostic { Code = SdpDiagnosticCodes.WriteBufferTooSmall });

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.BufferTooSmall, Is.True);
            Assert.That(first.BytesWritten, Is.Zero);
            Assert.That(first.RequiredBytes, Is.EqualTo(128));
            Assert.That(first.DiagnosticLimitReached, Is.False);
            Assert.That(first.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteBufferTooSmall));
            Assert.That(second.Success, Is.False);
            Assert.That(second.BufferTooSmall, Is.False);
            Assert.That(second.BytesWritten, Is.Zero);
            Assert.That(second.RequiredBytes, Is.Zero);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(first.Diagnostics, Is.Not.SameAs(second.Diagnostics));
        });
    }
}
