namespace barcode;

internal static class ResultCopier
{
    internal static byte[]? CopyByteArray(byte[]? source)
    {
        if (source == null)
        {
            return null;
        }

        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int[]? CopyIntArray(int[]? source)
    {
        if (source == null)
        {
            return null;
        }

        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[]? CopyStringArray(string[]? source)
    {
        if (source == null)
        {
            return null;
        }

        string[] copy = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodePayload? CopyPayload(BarcodePayload? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodePayload copy = new BarcodePayload();
        copy.Text = source.Text;
        copy.RawBytes = CopyByteArray(source.RawBytes);
        copy.CharacterEncoding = source.CharacterEncoding;
        copy.EciSegments = CopyEciSegmentArray(source.EciSegments);
        copy.IsGs1 = source.IsGs1;
        copy.StructuredAppend = CopyStructuredAppendInfo(source.StructuredAppend);
        return copy;
    }

    internal static EciSegment[]? CopyEciSegmentArray(EciSegment[]? source)
    {
        if (source == null)
        {
            return null;
        }

        EciSegment[] copy = new EciSegment[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyEciSegment(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static EciSegment CopyEciSegment(EciSegment source)
    {
        EciSegment copy = new EciSegment();
        copy.AssignmentNumber = source.AssignmentNumber;
        copy.ByteOffset = source.ByteOffset;
        copy.ByteCount = source.ByteCount;
        return copy;
    }

    internal static StructuredAppendInfo? CopyStructuredAppendInfo(StructuredAppendInfo? source)
    {
        if (source == null)
        {
            return null;
        }

        StructuredAppendInfo copy = new StructuredAppendInfo();
        copy.Format = source.Format;
        copy.Index = source.Index;
        copy.Count = source.Count;
        copy.FileIdentifier = source.FileIdentifier;
        copy.IsLastSegment = source.IsLastSegment;
        return copy;
    }

    internal static BarcodeQuietZone? CopyQuietZone(BarcodeQuietZone? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeQuietZone copy = new BarcodeQuietZone();
        copy.LeftModules = source.LeftModules;
        copy.TopModules = source.TopModules;
        copy.RightModules = source.RightModules;
        copy.BottomModules = source.BottomModules;
        copy.LeftPixels = source.LeftPixels;
        copy.TopPixels = source.TopPixels;
        copy.RightPixels = source.RightPixels;
        copy.BottomPixels = source.BottomPixels;
        return copy;
    }

    internal static BarcodeSize? CopySize(BarcodeSize? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeSize copy = new BarcodeSize();
        copy.WidthModules = source.WidthModules;
        copy.HeightModules = source.HeightModules;
        copy.WidthPixels = source.WidthPixels;
        copy.HeightPixels = source.HeightPixels;
        copy.WidthMillimetres = source.WidthMillimetres;
        copy.HeightMillimetres = source.HeightMillimetres;
        copy.DotsPerInch = source.DotsPerInch;
        copy.ModuleSizeMillimetres = source.ModuleSizeMillimetres;
        copy.QuietZone = CopyQuietZone(source.QuietZone);
        return copy;
    }

    internal static BarcodeRectangle? CopyRectangle(BarcodeRectangle? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeRectangle copy = new BarcodeRectangle();
        copy.X = source.X;
        copy.Y = source.Y;
        copy.Width = source.Width;
        copy.Height = source.Height;
        return copy;
    }

    internal static BarcodeTextRun[]? CopyTextRunArray(BarcodeTextRun[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeTextRun[] copy = new BarcodeTextRun[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyTextRun(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeTextRun CopyTextRun(BarcodeTextRun source)
    {
        BarcodeTextRun copy = new BarcodeTextRun();
        copy.Text = source.Text;
        copy.X = source.X;
        copy.BaselineY = source.BaselineY;
        copy.Width = source.Width;
        copy.Height = source.Height;
        return copy;
    }

    internal static BarcodeRectangle[]? CopyRectangleValues(
        BarcodeRectangle[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeRectangle[] copy = new BarcodeRectangle[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            BarcodeRectangle? rectangle = CopyRectangle(source[index]);
            copy[index] = rectangle == null
                ? new BarcodeRectangle()
                : rectangle;
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeTextLayout? CopyTextLayout(BarcodeTextLayout? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeTextLayout copy = new BarcodeTextLayout();
        copy.Runs = CopyTextRunArray(source.Runs);
        copy.BarExtensions = CopyRectangleValues(source.BarExtensions);
        copy.ReservedHeight = source.ReservedHeight;
        copy.Bounds = CopyRectangle(source.Bounds);
        copy.Size = CopySize(source.Size);
        return copy;
    }

    internal static BarcodeSymbol CopySymbol(BarcodeSymbol source)
    {
        BarcodeSymbol copy = new BarcodeSymbol();
        copy.Format = source.Format;
        copy.Dimension = source.Dimension;
        copy.WidthModules = source.WidthModules;
        copy.HeightModules = source.HeightModules;
        copy.Modules = CopyByteArray(source.Modules);
        copy.Payload = CopyPayload(source.Payload);
        copy.Size = CopySize(source.Size);
        copy.RequiredQuietZone = CopyQuietZone(source.RequiredQuietZone);
        copy.TextLayout = CopyTextLayout(source.TextLayout);
        copy.BearerBars = CopyRectangleValues(source.BearerBars);
        copy.SymbologyIdentifier = source.SymbologyIdentifier;
        copy.Codewords = CopyIntArray(source.Codewords);
        copy.HasChecksum = source.HasChecksum;
        copy.ChecksumValue = source.ChecksumValue;
        return copy;
    }

    internal static BarcodePoint? CopyPoint(BarcodePoint? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodePoint copy = new BarcodePoint();
        copy.X = source.X;
        copy.Y = source.Y;
        return copy;
    }

    internal static BarcodeQuadrilateral? CopyQuadrilateral(BarcodeQuadrilateral? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeQuadrilateral copy = new BarcodeQuadrilateral();
        copy.TopLeft = CopyPoint(source.TopLeft);
        copy.TopRight = CopyPoint(source.TopRight);
        copy.BottomRight = CopyPoint(source.BottomRight);
        copy.BottomLeft = CopyPoint(source.BottomLeft);
        copy.Bounds = CopyRectangle(source.Bounds);
        return copy;
    }

    internal static BarcodeDiagnostic[]? CopyDiagnosticArray(BarcodeDiagnostic[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeDiagnostic[] copy = new BarcodeDiagnostic[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyDiagnostic(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeDiagnostic CopyDiagnostic(BarcodeDiagnostic source)
    {
        BarcodeDiagnostic copy = new BarcodeDiagnostic();
        copy.Stage = source.Stage;
        copy.Code = source.Code;
        copy.Message = source.Message;
        copy.HasNumericEvidence = source.HasNumericEvidence;
        copy.Value = source.Value;
        copy.Threshold = source.Threshold;
        copy.Location = CopyPoint(source.Location);
        copy.Region = CopyRectangle(source.Region);
        return copy;
    }

    internal static BarcodeDecodeMetadata? CopyDecodeMetadata(BarcodeDecodeMetadata? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeDecodeMetadata copy = new BarcodeDecodeMetadata();
        copy.HasChecksum = source.HasChecksum;
        copy.ChecksumValid = source.ChecksumValid;
        copy.ChecksumValue = source.ChecksumValue;
        copy.ErrorCorrectionApplied = source.ErrorCorrectionApplied;
        copy.CorrectedErrorCount = source.CorrectedErrorCount;
        copy.ErasureCount = source.ErasureCount;
        copy.SymbologyIdentifier = source.SymbologyIdentifier;
        copy.Codewords = CopyIntArray(source.Codewords);
        copy.Confidence = source.Confidence;
        copy.IsGs1 = source.IsGs1;
        copy.Gs1ElementString = source.Gs1ElementString;
        copy.StructuredAppend = CopyStructuredAppendInfo(source.StructuredAppend);
        return copy;
    }

    internal static BarcodeDecodeResult CopyDecodeResult(BarcodeDecodeResult source)
    {
        BarcodeDecodeResult copy = new BarcodeDecodeResult();
        copy.Payload = CopyPayload(source.Payload);
        copy.Format = source.Format;
        copy.Dimension = source.Dimension;
        copy.Location = CopyQuadrilateral(source.Location);
        copy.Rotation = source.Rotation;
        copy.OrientationDegrees = source.OrientationDegrees;
        copy.SkewDegrees = source.SkewDegrees;
        copy.Polarity = source.Polarity;
        copy.MirrorMode = source.MirrorMode;
        copy.Metadata = CopyDecodeMetadata(source.Metadata);
        copy.Diagnostics = CopyDiagnosticArray(source.Diagnostics);
        return copy;
    }

    internal static BarcodeCandidate CopyCandidate(BarcodeCandidate source)
    {
        if (source == null)
        {
            throw new BarcodeValidationException("Barcode candidate must not be null.");
        }

        BarcodeCandidate copy = new BarcodeCandidate();
        copy.Format = source.Format;
        copy.Dimension = source.Dimension;
        copy.Location = CopyQuadrilateral(source.Location);
        copy.AngleDegrees = source.AngleDegrees;
        copy.Confidence = source.Confidence;
        copy.Polarity = source.Polarity;
        copy.MirrorMode = source.MirrorMode;
        copy.Diagnostics = CopyDiagnosticArray(source.Diagnostics);
        return copy;
    }

    internal static BarcodeCandidate[] CopyCandidateArray(BarcodeCandidate[] source)
    {
        if (source == null)
        {
            throw new BarcodeValidationException("Barcode candidate array must not be null.");
        }

        BarcodeCandidate[] copy = new BarcodeCandidate[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyCandidate(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeDecodeResult[]? CopyDecodeResultArray(BarcodeDecodeResult[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeDecodeResult[] copy = new BarcodeDecodeResult[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyDecodeResult(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeDecodeAttempt CopyDecodeAttempt(BarcodeDecodeAttempt source)
    {
        BarcodeDecodeAttempt copy = new BarcodeDecodeAttempt();
        copy.Success = source.Success;
        copy.Results = CopyDecodeResultArray(source.Results);
        copy.Diagnostics = CopyDiagnosticArray(source.Diagnostics);
        copy.FailureCode = source.FailureCode;
        copy.FailureMessage = source.FailureMessage;
        return copy;
    }

    internal static BarcodeDetectionResult CopyDetectionResult(
        BarcodeDetectionResult source)
    {
        BarcodeDetectionResult copy = new BarcodeDetectionResult();
        copy.Candidates = source.Candidates == null
            ? null
            : CopyCandidateArray(source.Candidates);
        copy.Diagnostics = CopyDiagnosticArray(source.Diagnostics);
        return copy;
    }

    internal static BarcodeRenderedImage CopyRenderedImage(
        BarcodeRenderedImage source)
    {
        BarcodeRenderedImage copy = new BarcodeRenderedImage();
        copy.Bytes = CopyByteArray(source.Bytes);
        copy.Text = source.Text;
        copy.MediaType = source.MediaType;
        copy.Format = source.Format;
        copy.WidthPixels = source.WidthPixels;
        copy.HeightPixels = source.HeightPixels;
        copy.Stride = source.Stride;
        copy.PixelFormat = source.PixelFormat;
        copy.WidthMillimetres = source.WidthMillimetres;
        copy.HeightMillimetres = source.HeightMillimetres;
        copy.DpiX = source.DpiX;
        copy.DpiY = source.DpiY;
        copy.Size = CopySize(source.Size);
        copy.TextLayout = CopyTextLayout(source.TextLayout);
        copy.Diagnostics = CopyDiagnosticArray(source.Diagnostics);
        return copy;
    }

    internal static BarcodeValidationIssue CopyValidationIssue(
        BarcodeValidationIssue source)
    {
        BarcodeValidationIssue copy = new BarcodeValidationIssue();
        copy.Severity = source.Severity;
        copy.Code = source.Code;
        copy.Message = source.Message;
        copy.Standard = source.Standard;
        copy.Field = source.Field;
        copy.Offset = source.Offset;
        copy.Expected = source.Expected;
        copy.Observed = source.Observed;
        return copy;
    }

    internal static BarcodeValidationIssue[]? CopyValidationIssueArray(
        BarcodeValidationIssue[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeValidationIssue[] copy =
            new BarcodeValidationIssue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyValidationIssue(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeQualityMetric CopyQualityMetric(
        BarcodeQualityMetric source)
    {
        BarcodeQualityMetric copy = new BarcodeQualityMetric();
        copy.Name = source.Name;
        copy.Value = source.Value;
        copy.Threshold = source.Threshold;
        copy.MeetsThreshold = source.MeetsThreshold;
        copy.IsHeuristic = source.IsHeuristic;
        copy.Interpretation = source.Interpretation;
        return copy;
    }

    internal static BarcodeQualityMetric[]? CopyQualityMetricArray(
        BarcodeQualityMetric[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeQualityMetric[] copy =
            new BarcodeQualityMetric[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyQualityMetric(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static BarcodeValidationReport CopyValidationReport(
        BarcodeValidationReport source)
    {
        BarcodeValidationReport copy = new BarcodeValidationReport();
        copy.IsConformant = source.IsConformant;
        copy.Mode = source.Mode;
        copy.Standard = source.Standard;
        copy.Profile = source.Profile;
        copy.Issues = CopyValidationIssueArray(source.Issues);
        copy.Metrics = CopyQualityMetricArray(source.Metrics);
        copy.Diagnostics = CopyDiagnosticArray(source.Diagnostics);
        return copy;
    }

    internal static BarcodeChecksumResult CopyChecksumResult(
        BarcodeChecksumResult source)
    {
        BarcodeChecksumResult copy = new BarcodeChecksumResult();
        copy.Format = source.Format;
        copy.Algorithm = source.Algorithm;
        copy.CalculatedValue = source.CalculatedValue;
        copy.HasObservedValue = source.HasObservedValue;
        copy.ObservedValue = source.ObservedValue;
        copy.IsValid = source.IsValid;
        return copy;
    }

    internal static Gs1ApplicationIdentifierInfo? CopyGs1ApplicationIdentifierInfo(
        Gs1ApplicationIdentifierInfo? source)
    {
        if (source == null)
        {
            return null;
        }

        Gs1ApplicationIdentifierInfo copy =
            new Gs1ApplicationIdentifierInfo();
        copy.Identifier = source.Identifier;
        copy.Title = source.Title;
        copy.MinimumLength = source.MinimumLength;
        copy.MaximumLength = source.MaximumLength;
        copy.CharacterSet = source.CharacterSet;
        copy.FixedLength = source.FixedLength;
        copy.HasCheckCharacter = source.HasCheckCharacter;
        copy.IsPrimaryKey = source.IsPrimaryKey;
        copy.RequiredIdentifiers = CopyStringArray(source.RequiredIdentifiers);
        copy.ExcludedIdentifiers = CopyStringArray(source.ExcludedIdentifiers);
        copy.Qualifiers = CopyStringArray(source.Qualifiers);
        return copy;
    }

    internal static Gs1Element CopyGs1Element(Gs1Element source)
    {
        Gs1Element copy = new Gs1Element();
        copy.ApplicationIdentifier = source.ApplicationIdentifier;
        copy.Value = source.Value;
        copy.Components = CopyStringArray(source.Components);
        copy.Definition = CopyGs1ApplicationIdentifierInfo(source.Definition);
        copy.SourceOffset = source.SourceOffset;
        copy.SourceLength = source.SourceLength;
        copy.VariableLength = source.VariableLength;
        return copy;
    }

    internal static Gs1Element[]? CopyGs1ElementArray(Gs1Element[]? source)
    {
        if (source == null)
        {
            return null;
        }

        Gs1Element[] copy = new Gs1Element[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyGs1Element(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static Gs1Message? CopyGs1Message(Gs1Message? source)
    {
        if (source == null)
        {
            return null;
        }

        Gs1Message copy = new Gs1Message();
        copy.Elements = CopyGs1ElementArray(source.Elements);
        copy.Syntax = source.Syntax;
        copy.SymbologyIdentifier = source.SymbologyIdentifier;
        copy.SourceText = source.SourceText;
        copy.Issues = CopyValidationIssueArray(source.Issues);
        return copy;
    }

    internal static Gs1ProcessResult CopyGs1ProcessResult(
        Gs1ProcessResult source)
    {
        Gs1ProcessResult copy = new Gs1ProcessResult();
        copy.Success = source.Success;
        copy.Message = CopyGs1Message(source.Message);
        copy.Text = source.Text;
        copy.Bytes = CopyByteArray(source.Bytes);
        copy.Issues = CopyValidationIssueArray(source.Issues);
        return copy;
    }

    internal static StructuredAppendResult CopyStructuredAppendResult(
        StructuredAppendResult source)
    {
        StructuredAppendResult copy = new StructuredAppendResult();
        copy.Complete = source.Complete;
        copy.Payload = CopyPayload(source.Payload);
        copy.Segments = CopyDecodeResultArray(source.Segments);
        copy.MissingIndices = CopyIntArray(source.MissingIndices);
        copy.Conflicts = CopyStringArray(source.Conflicts);
        copy.FileIdentifier = source.FileIdentifier;
        copy.Issues = CopyValidationIssueArray(source.Issues);
        return copy;
    }
}
