using raw;

namespace raw.Tests;

public class GenericRawDescriptorSuggesterTests
{
    [Test]
    public void FullyConstrainedExactLengthReturnsOneAdvisoryCandidateWithoutSelectingIt()
    {
        RawDescriptorHints hints = GrayHints(4, 3, 16, RawPacking.ByteAligned);
        hints.StoredWordBits = 16;
        hints.ByteOrderKnown = true;
        hints.ByteOrder = RawByteOrder.BigEndian;

        RawDescriptorSuggestionResult result = Suggest(24, hints);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Authoritative, Is.False);
            Assert.That(result.Truncated, Is.False);
            Assert.That(result.IsAmbiguous(), Is.False);
            Assert.That(result.Candidates[0].Descriptor.Width, Is.EqualTo(4));
            Assert.That(result.Candidates[0].Descriptor.Height, Is.EqualTo(3));
            Assert.That(result.Candidates[0].Descriptor.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
        Assert.That(result.Candidates[0].Descriptor.ValidateSourceLength(24, new RawLimits()).IsValid(), Is.True);
            Assert.That(result.Candidates[0].Reasons, Does.Contain("Exact source length"));
            Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(RawSeverity.Information));
        });
    }

    [Test]
    public void SparseHintsReturnMultipleExactCandidatesAndAnAmbiguityWarning()
    {
        RawDescriptorHints hints = new RawDescriptorHints();
        hints.Width = 4;

        RawDescriptorSuggestionResult result = Suggest(24, hints);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates.Length, Is.GreaterThan(1));
            Assert.That(result.IsAmbiguous(), Is.True);
            Assert.That(result.Authoritative, Is.False);
            Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.AmbiguousFormat), Is.True);
        Assert.That(result.Candidates.All(candidate => candidate.Descriptor.ValidateSourceLength(24, new RawLimits()).IsValid()), Is.True);
            Assert.That(result.Candidates.All(candidate => candidate.Descriptor.DataOffset + candidate.Descriptor.DataLength == 24), Is.True);
        });
    }

    [Test]
    public void WidthOrHeightCanDeriveTheOtherDimensionWhenStorageHintsAreExact()
    {
        RawDescriptorHints width = RgbHints(4, 0);
        RawDescriptorHints height = GrayHints(0, 3, 8, RawPacking.ByteAligned);
        height.StoredWordBits = 8;

        RawDescriptorSuggestionResult fromWidth = Suggest(24, width);
        RawDescriptorSuggestionResult fromHeight = Suggest(12, height);

        Assert.Multiple(() =>
        {
            Assert.That(fromWidth.Candidates, Has.Length.EqualTo(1));
            Assert.That(fromWidth.Candidates[0].Descriptor.Width, Is.EqualTo(4));
            Assert.That(fromWidth.Candidates[0].Descriptor.Height, Is.EqualTo(2));
            Assert.That(fromHeight.Candidates, Has.Length.EqualTo(1));
            Assert.That(fromHeight.Candidates[0].Descriptor.Width, Is.EqualTo(4));
            Assert.That(fromHeight.Candidates[0].Descriptor.Height, Is.EqualTo(3));
        });
    }

    [Test]
    public void OffsetAndPaddedStrideHintsMustAccountForTheExactSourceLength()
    {
        RawDescriptorHints hints = GrayHints(3, 2, 8, RawPacking.ByteAligned);
        hints.StoredWordBits = 8;
        hints.RowStrideBytes = 5;
        hints.DataOffset = 2;

        RawDescriptorSuggestionResult matching = Suggest(12, hints);
        RawDescriptorSuggestionResult wrong = Suggest(11, hints);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Candidates, Has.Length.EqualTo(1));
            Assert.That(matching.Candidates[0].Descriptor.RowStrideBytes, Is.EqualTo(5));
            Assert.That(matching.Candidates[0].Descriptor.DataOffset, Is.EqualTo(2));
            Assert.That(matching.Candidates[0].Descriptor.DataLength, Is.EqualTo(10));
            Assert.That(wrong.Candidates, Is.Empty);
            Assert.That(wrong.Diagnostics.Any(diagnostic => diagnostic.Severity == RawSeverity.Error), Is.True);
        });
    }

    [Test]
    public void CfaPhotometricHintReturnsEveryLengthIndistinguishableBayerPhase()
    {
        RawDescriptorHints hints = GrayHints(4, 2, 8, RawPacking.ByteAligned);
        hints.StoredWordBits = 8;
        hints.Photometric = RawPhotometricInterpretation.ColorFilterArray;

        RawDescriptorSuggestionResult result = Suggest(8, hints);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates, Has.Length.EqualTo(4));
            Assert.That(result.Candidates.Select(candidate => (candidate.Descriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout), Is.EqualTo(new RawCfaLayout[]
            {
                RawCfaLayout.Rggb,
                RawCfaLayout.Bggr,
                RawCfaLayout.Grbg,
                RawCfaLayout.Gbrg
            }));
            Assert.That(result.IsAmbiguous(), Is.True);
        });
    }

    [Test]
    public void ExplicitCfaSignedAndFloatingHintsConstrainNumericInterpretation()
    {
        RawDescriptorHints cfa = GrayHints(4, 2, 8, RawPacking.ByteAligned);
        cfa.StoredWordBits = 8;
        cfa.Photometric = RawPhotometricInterpretation.ColorFilterArray;
        cfa.CfaLayoutKnown = true;
        cfa.CfaLayout = RawCfaLayout.Grbg;
        RawDescriptorHints signed = GrayHints(4, 1, 12, RawPacking.PackedBits);
        signed.StoredWordBits = 12;
        signed.SampleKind = RawSampleKind.SignedInteger;
        signed.BitOrder = RawBitOrder.LeastSignificantBitFirst;
        RawDescriptorHints half = GrayHints(2, 2, 16, RawPacking.ByteAligned);
        half.StoredWordBits = 16;
        half.SampleKind = RawSampleKind.Half;

        RawDescriptorSuggestionResult cfaResult = Suggest(8, cfa);
        RawDescriptorSuggestionResult signedResult = Suggest(6, signed);
        RawDescriptorSuggestionResult halfResult = Suggest(8, half);

        Assert.Multiple(() =>
        {
            Assert.That((cfaResult.Candidates.Single().Descriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Grbg));
            Assert.That(signedResult.Candidates.Single().Descriptor.SampleKind, Is.EqualTo(RawSampleKind.SignedInteger));
            Assert.That(signedResult.Candidates.Single().Descriptor.BitOrder, Is.EqualTo(RawBitOrder.LeastSignificantBitFirst));
            Assert.That(halfResult.Candidates.Single().Descriptor.SampleKind, Is.EqualTo(RawSampleKind.Half));
        });
    }

    [Test]
    public void CandidateBoundStopsExpansionAndReportsThatMoreInterpretationsMayExist()
    {
        RawDescriptorHints hints = new RawDescriptorHints();
        hints.Width = 4;
        hints.MaxCandidates = 2;

        RawDescriptorSuggestionResult result = Suggest(24, hints);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates, Has.Length.EqualTo(2));
            Assert.That(result.Truncated, Is.True);
            Assert.That(result.IsAmbiguous(), Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.ResourceLimitExceeded), Is.True);
        });
    }

    [Test]
    public void MissingGeometryAndImpossibleHintsReturnDiagnosticsInsteadOfGuesses()
    {
        RawDescriptorSuggestionResult missing = Suggest(24, new RawDescriptorHints());
        RawDescriptorHints impossibleHints = GrayHints(5, 5, 16, RawPacking.ByteAligned);
        impossibleHints.StoredWordBits = 16;
        RawDescriptorSuggestionResult impossible = Suggest(24, impossibleHints);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Candidates, Is.Empty);
            Assert.That(missing.Authoritative, Is.False);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.InvalidDescriptor));
            Assert.That(impossible.Candidates, Is.Empty);
            Assert.That(impossible.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.InvalidDescriptor), Is.True);
        });
    }

    [Test]
    public void InvalidArgumentsLimitsAndCancellationAreRejectedBeforeCandidateGeneration()
    {
        RawDescriptorHints invalid = new RawDescriptorHints();
        invalid.Width = -1;
        RawDescriptorHints excessive = new RawDescriptorHints();
        excessive.Width = 2;
        excessive.MaxCandidates = 5;
        RawLimits candidateLimit = new RawLimits();
        candidateLimit.MaxTagCount = 4;
        RawLimits fileLimit = new RawLimits();
        fileLimit.MaxFileSizeBytes = 7;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawDescriptorHints cancellable = new RawDescriptorHints();
        cancellable.Width = 2;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => GenericRawDescriptorSuggester.Suggest(8, null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => Suggest(8, invalid));
            Assert.Throws<RawLimitException>(() => GenericRawDescriptorSuggester.Suggest(8, excessive, candidateLimit, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => GenericRawDescriptorSuggester.Suggest(8, cancellable, fileLimit, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => GenericRawDescriptorSuggester.Suggest(8, cancellable, new RawLimits(), cancellation.Token));
        });
    }

    private static RawDescriptorHints GrayHints(int width, int height, int bitDepth, RawPacking packing)
    {
        RawDescriptorHints hints = new RawDescriptorHints();
        hints.Width = width;
        hints.Height = height;
        hints.BitDepth = bitDepth;
        hints.SampleKindKnown = true;
        hints.SampleKind = RawSampleKind.UnsignedInteger;
        hints.PackingKnown = true;
        hints.Packing = packing;
        hints.ByteOrderKnown = true;
        hints.ByteOrder = RawByteOrder.LittleEndian;
        hints.BitOrderKnown = true;
        hints.BitOrder = RawBitOrder.MostSignificantBitFirst;
        hints.LayoutKnown = true;
        hints.Layout = RawSampleLayout.Interleaved;
        hints.ChannelCount = 1;
        hints.PhotometricKnown = true;
        hints.Photometric = RawPhotometricInterpretation.BlackIsZero;
        return hints;
    }

    private static RawDescriptorHints RgbHints(int width, int height)
    {
        RawDescriptorHints hints = GrayHints(width, height, 8, RawPacking.ByteAligned);
        hints.StoredWordBits = 8;
        hints.ChannelCount = 3;
        hints.Photometric = RawPhotometricInterpretation.Rgb;
        return hints;
    }

    private static RawDescriptorSuggestionResult Suggest(long length, RawDescriptorHints hints)
    {
        return GenericRawDescriptorSuggester.Suggest(length, hints, new RawLimits(), CancellationToken.None);
    }
}
