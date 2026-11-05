namespace sdp.Tests.Api;

public class SdpOperationDataTests
{
    [Test]
    public void ParseOptions_UseStrictBoundedDefaults()
    {
        SdpParseOptions options = new SdpParseOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(SdpParseMode.Strict));
            Assert.That(options.AllowByteOrderMark, Is.False);
            Assert.That(options.AllowLfLineEndings, Is.False);
            Assert.That(options.AllowCrLineEndings, Is.False);
            Assert.That(options.PreserveRawLines, Is.False);
            Assert.That(options.PreserveMalformedLines, Is.False);
            Assert.That(options.MaximumInputCharacters, Is.EqualTo(SdpLimits.DefaultMaximumInputCharacters));
            Assert.That(options.MaximumInputBytes, Is.EqualTo(SdpLimits.DefaultMaximumInputBytes));
            Assert.That(options.MaximumLineLength, Is.EqualTo(SdpLimits.DefaultMaximumLineLength));
            Assert.That(options.MaximumLineCount, Is.EqualTo(SdpLimits.DefaultMaximumLineCount));
            Assert.That(options.MaximumMediaCount, Is.EqualTo(SdpLimits.DefaultMaximumMediaCount));
            Assert.That(options.MaximumAttributesPerScope, Is.EqualTo(SdpLimits.DefaultMaximumAttributesPerScope));
            Assert.That(options.MaximumTokenLength, Is.EqualTo(SdpLimits.DefaultMaximumTokenLength));
            Assert.That(options.MaximumCandidateCount, Is.EqualTo(SdpLimits.DefaultMaximumCandidateCount));
            Assert.That(options.MaximumDiagnosticCount, Is.EqualTo(SdpLimits.DefaultMaximumDiagnosticCount));
        });
    }

    [Test]
    public void ParseResult_RepresentsFailureAndSuccessExplicitly()
    {
        SdpParseResult first = new SdpParseResult();
        SdpParseResult second = new SdpParseResult();
        SdpSessionDescription description = new SdpSessionDescription();
        first.Success = true;
        first.Description = description;
        first.CharactersConsumed = 100;
        first.BytesConsumed = 120;
        first.LinesConsumed = 6;
        first.Diagnostics.Add(new SdpDiagnostic { Code = SdpDiagnosticCodes.ParseEmptyLine });

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Description, Is.SameAs(description));
            Assert.That(first.CharactersConsumed, Is.EqualTo(100));
            Assert.That(first.BytesConsumed, Is.EqualTo(120));
            Assert.That(first.LinesConsumed, Is.EqualTo(6));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Success, Is.False);
            Assert.That(second.Description, Is.Null);
            Assert.That(second.DiagnosticLimitReached, Is.False);
            Assert.That(second.CharactersConsumed, Is.Zero);
            Assert.That(second.BytesConsumed, Is.Zero);
            Assert.That(second.LinesConsumed, Is.Zero);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(first.Diagnostics, Is.Not.SameAs(second.Diagnostics));
            Assert.That(first.AppliedOptions, Is.Not.SameAs(second.AppliedOptions));
        });
    }

    [Test]
    public void WriteOptions_UseCanonicalBoundedDefaults()
    {
        SdpWriteOptions options = new SdpWriteOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(SdpWriteMode.Canonical));
            Assert.That(options.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(options.IncludeFinalLineEnding, Is.True);
            Assert.That(options.IncludeObsoleteFields, Is.True);
            Assert.That(options.IncludeUnknownFields, Is.True);
            Assert.That(options.MaximumOutputCharacters, Is.EqualTo(SdpLimits.DefaultMaximumOutputCharacters));
            Assert.That(options.MaximumOutputBytes, Is.EqualTo(SdpLimits.DefaultMaximumOutputBytes));
            Assert.That(options.MaximumDiagnosticCount, Is.EqualTo(SdpLimits.DefaultMaximumDiagnosticCount));
        });
    }

    [Test]
    public void WriteResult_InitializesIndependentPayloadsAndDiagnostics()
    {
        SdpWriteResult first = new SdpWriteResult();
        SdpWriteResult second = new SdpWriteResult();
        first.Success = true;
        first.Text = "v=0\r\n";
        first.Utf8Bytes = new byte[] { 118, 61, 48, 13, 10 };
        first.CharactersWritten = 5;
        first.BytesWritten = 5;
        first.Diagnostics.Add(new SdpDiagnostic { Code = SdpDiagnosticCodes.WriteLosslessDataUnavailable });

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Text, Is.EqualTo("v=0\r\n"));
            Assert.That(first.Utf8Bytes, Is.EqualTo(new byte[] { 118, 61, 48, 13, 10 }));
            Assert.That(first.CharactersWritten, Is.EqualTo(5));
            Assert.That(first.BytesWritten, Is.EqualTo(5));
            Assert.That(second.Success, Is.False);
            Assert.That(second.Text, Is.Empty);
            Assert.That(second.Utf8Bytes, Is.Empty);
            Assert.That(second.DiagnosticLimitReached, Is.False);
            Assert.That(second.CharactersWritten, Is.Zero);
            Assert.That(second.BytesWritten, Is.Zero);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(first.Utf8Bytes, Is.Not.SameAs(second.Utf8Bytes));
            Assert.That(first.Diagnostics, Is.Not.SameAs(second.Diagnostics));
            Assert.That(first.AppliedOptions, Is.Not.SameAs(second.AppliedOptions));
        });
    }

    [Test]
    public void ValidationOptions_UseCoreProfileAndIndependentSeverityLists()
    {
        SdpValidationOptions first = new SdpValidationOptions();
        SdpValidationOptions second = new SdpValidationOptions();
        first.SuppressedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationObsoleteSyntax);
        first.PromotedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationAttribute);

        Assert.Multiple(() =>
        {
            Assert.That(first.Profile, Is.EqualTo(SdpValidationProfile.Rfc8866));
            Assert.That(first.NegotiationRole, Is.EqualTo(SdpNegotiationRole.Unknown));
            Assert.That(first.OfferDescription, Is.Null);
            Assert.That(first.MinimumSeverity, Is.EqualTo(SdpValidationSeverity.Information));
            Assert.That(first.MaximumDiagnosticCount, Is.EqualTo(SdpLimits.DefaultMaximumDiagnosticCount));
            Assert.That(second.SuppressedDiagnosticCodes, Is.Empty);
            Assert.That(second.PromotedDiagnosticCodes, Is.Empty);
            Assert.That(first.SuppressedDiagnosticCodes, Is.Not.SameAs(second.SuppressedDiagnosticCodes));
            Assert.That(first.PromotedDiagnosticCodes, Is.Not.SameAs(second.PromotedDiagnosticCodes));
        });
    }

    [Test]
    public void ValidationResult_RepresentsOutcomeCountsAndCapacityExplicitly()
    {
        SdpValidationResult first = new SdpValidationResult();
        SdpValidationResult second = new SdpValidationResult();
        first.Success = false;
        first.WarningCount = 1;
        first.ErrorCount = 1;
        first.DiagnosticLimitReached = true;
        first.Issues.Add(new SdpValidationIssue
        {
            Code = SdpDiagnosticCodes.ValidationOrigin,
            Severity = SdpValidationSeverity.Error
        });

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.WarningCount, Is.EqualTo(1));
            Assert.That(first.ErrorCount, Is.EqualTo(1));
            Assert.That(first.DiagnosticLimitReached, Is.True);
            Assert.That(first.Issues, Has.Count.EqualTo(1));
            Assert.That(second.Issues, Is.Empty);
            Assert.That(second.Success, Is.False);
            Assert.That(second.DiagnosticLimitReached, Is.False);
            Assert.That(second.WarningCount, Is.Zero);
            Assert.That(second.ErrorCount, Is.Zero);
            Assert.That(first.Issues, Is.Not.SameAs(second.Issues));
            Assert.That(first.AppliedOptions, Is.Not.SameAs(second.AppliedOptions));
        });
    }

    [Test]
    public void TimingResults_ExposeNeutralDefaultsAndIndependentOccurrenceCollections()
    {
        SdpNtpConversionResult ntp = new SdpNtpConversionResult();
        SdpTypedTimeNormalizationResult normalization = new SdpTypedTimeNormalizationResult();
        SdpTimingOccurrenceResult firstOccurrences = new SdpTimingOccurrenceResult();
        SdpTimingOccurrenceResult secondOccurrences = new SdpTimingOccurrenceResult();
        firstOccurrences.Occurrences.Add(new SdpTimingOccurrence());

        Assert.Multiple(() =>
        {
            Assert.That(ntp.Success, Is.False);
            Assert.That(ntp.NtpValue, Is.Empty);
            Assert.That(ntp.DateTimeValue, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(ntp.DiagnosticCode, Is.Empty);
            Assert.That(ntp.Detail, Is.Empty);
            Assert.That(normalization.Success, Is.False);
            Assert.That(normalization.Seconds, Is.Zero);
            Assert.That(normalization.DiagnosticCode, Is.Empty);
            Assert.That(normalization.Detail, Is.Empty);
            Assert.That(secondOccurrences.Success, Is.False);
            Assert.That(secondOccurrences.Occurrences, Is.Empty);
            Assert.That(secondOccurrences.ResultLimitReached, Is.False);
            Assert.That(secondOccurrences.DiagnosticCode, Is.Empty);
            Assert.That(secondOccurrences.Detail, Is.Empty);
            Assert.That(firstOccurrences.Occurrences, Is.Not.SameAs(secondOccurrences.Occurrences));
        });
    }

    [Test]
    public void DiagnosticPayloads_HaveSafeUnknownDefaultsAndMutableLocations()
    {
        SdpDiagnostic diagnostic = new SdpDiagnostic();
        SdpValidationIssue issue = new SdpValidationIssue();
        diagnostic.Location.LineNumber = 4;
        issue.Location.LineNumber = 8;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.Empty);
            Assert.That(diagnostic.Severity, Is.EqualTo(SdpValidationSeverity.Unknown));
            Assert.That(diagnostic.Scope, Is.EqualTo(SdpScope.Unknown));
            Assert.That(diagnostic.FieldName, Is.EqualTo('\0'));
            Assert.That(diagnostic.AttributeName, Is.Empty);
            Assert.That(diagnostic.MediaIndex, Is.EqualTo(-1));
            Assert.That(diagnostic.Detail, Is.Empty);
            Assert.That(diagnostic.Path.Scope, Is.EqualTo(SdpScope.Unknown));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(4));
            Assert.That(issue.Code, Is.Empty);
            Assert.That(issue.Severity, Is.EqualTo(SdpValidationSeverity.Unknown));
            Assert.That(issue.Scope, Is.EqualTo(SdpScope.Unknown));
            Assert.That(issue.FieldName, Is.EqualTo('\0'));
            Assert.That(issue.AttributeName, Is.Empty);
            Assert.That(issue.MediaIndex, Is.EqualTo(-1));
            Assert.That(issue.Detail, Is.Empty);
            Assert.That(issue.Path.Scope, Is.EqualTo(SdpScope.Unknown));
            Assert.That(issue.Location.LineNumber, Is.EqualTo(8));
            Assert.That(diagnostic.Path, Is.Not.SameAs(issue.Path));
            Assert.That(diagnostic.Location, Is.Not.SameAs(issue.Location));
        });
    }
}
