using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicValidationResourceSafetyTests
{
    [Test]
    public void StrictAndPermissiveValidation_PreserveCodesAndLocationsWhileChangingSeverity()
    {
        byte[] bytes = CreateUnsupportedCompressionFile();
        TiffReadOptions strictOptions = new TiffReadOptions();
        TiffReadOptions permissiveOptions = new TiffReadOptions();
        permissiveOptions.Strict = false;

        TiffValidationResult strict = TiffApi.Validation.ValidateBytes(bytes, strictOptions);
        TiffValidationResult permissive = TiffApi.Validation.ValidateMemory(bytes, permissiveOptions);
        TiffDiagnostic strictDiagnostic = FindDiagnostic(strict.Diagnostics, TiffErrorCode.UnsupportedCompression);
        TiffDiagnostic permissiveDiagnostic = FindDiagnostic(permissive.Diagnostics, TiffErrorCode.UnsupportedCompression);

        Assert.That(strict.Success, Is.False);
        Assert.That(strict.Mode, Is.EqualTo(TiffValidationMode.Strict));
        Assert.That(strictDiagnostic.Severity, Is.EqualTo(TiffDiagnosticSeverity.Error));
        Assert.That(strictDiagnostic.Offset, Is.GreaterThanOrEqualTo(8));
        Assert.That(strictDiagnostic.ImageIndex, Is.EqualTo(0));
        Assert.That(strictDiagnostic.TagId, Is.EqualTo(TiffTagId.Compression));
        Assert.That(strictDiagnostic.Message, Is.Not.Empty);
        Assert.That(permissive.Success, Is.True);
        Assert.That(permissive.Mode, Is.EqualTo(TiffValidationMode.Permissive));
        Assert.That(permissiveDiagnostic.Severity, Is.EqualTo(TiffDiagnosticSeverity.Warning));
        Assert.That(permissiveDiagnostic.Code, Is.EqualTo(strictDiagnostic.Code));
        Assert.That(permissiveDiagnostic.Offset, Is.EqualTo(strictDiagnostic.Offset));
        Assert.That(permissiveDiagnostic.ImageIndex, Is.EqualTo(strictDiagnostic.ImageIndex));
        Assert.That(permissiveDiagnostic.TagId, Is.EqualTo(strictDiagnostic.TagId));
    }

    [TestCase("invalid-byte-order", TiffErrorCode.InvalidByteOrder, 0, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId)]
    [TestCase("invalid-version", TiffErrorCode.InvalidTiffVersion, 2, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId)]
    [TestCase("invalid-bigtiff", TiffErrorCode.InvalidBigTiffHeader, 4, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId)]
    [TestCase("invalid-offset", TiffErrorCode.InvalidOffset, 32, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId)]
    [TestCase("truncated-directory", TiffErrorCode.TruncatedStream, 8, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId)]
    [TestCase("cyclic-directory", TiffErrorCode.CyclicIfdChain, 8, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId)]
    [TestCase("invalid-entry-type", TiffErrorCode.InvalidTagType, 10, 0, TiffTagId.ImageWidth)]
    public void GeneratedMalformedInput_ReportsExpectedStructuredDiagnostic(string kind, TiffErrorCode code, long offset, int imageIndex, int tagId)
    {
        byte[] bytes = CreateMalformedBytes(kind);

        TiffValidationResult result = TiffApi.Validation.ValidateBytes(bytes, new TiffReadOptions());
        TiffDiagnostic diagnostic = FindDiagnostic(result.Diagnostics, code);

        Assert.That(result.Success, Is.False);
        Assert.That(diagnostic.Severity, Is.EqualTo(TiffDiagnosticSeverity.Error));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.ImageIndex, Is.EqualTo(imageIndex));
        Assert.That(diagnostic.TagId, Is.EqualTo(tagId));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }

    [Test]
    public void DirectoryAndTagLimits_FailAtTheDirectoryThatCrossesTheBoundary()
    {
        byte[] bytes = CreateImageFile(2, 2, 2);
        TiffReadOptions ifdOptions = new TiffReadOptions();
        ifdOptions.Limits.MaximumIfdCount = 1;
        TiffReadOptions tagOptions = new TiffReadOptions();
        tagOptions.Limits.MaximumTagCountPerIfd = 2;

        TiffValidationResult ifdResult = TiffApi.Validation.ValidateBytes(bytes, ifdOptions);
        TiffValidationResult tagResult = TiffApi.Validation.ValidateBytes(bytes, tagOptions);
        TiffDiagnostic ifdDiagnostic = FindDiagnostic(ifdResult.Diagnostics, TiffErrorCode.IfdCountLimitExceeded);
        TiffDiagnostic tagDiagnostic = FindDiagnostic(tagResult.Diagnostics, TiffErrorCode.TagCountLimitExceeded);

        Assert.That(ifdResult.Success, Is.False);
        Assert.That(ifdDiagnostic.Offset, Is.GreaterThan(8));
        Assert.That(ifdDiagnostic.Message, Is.Not.Empty);
        Assert.That(tagResult.Success, Is.False);
        Assert.That(tagDiagnostic.Offset, Is.EqualTo(8));
        Assert.That(tagDiagnostic.Message, Is.Not.Empty);
    }

    [Test]
    public void DimensionAllocationAndDecodedLimits_StopBeforeUnsafeWork()
    {
        byte[] bytes = CreateImageFile(1, 4, 4);
        TiffReadOptions dimensionOptions = new TiffReadOptions();
        dimensionOptions.Limits.MaximumImageWidth = 3;
        TiffValidationResult dimensionResult = TiffApi.Validation.ValidateBytes(bytes, dimensionOptions);
        TiffDiagnostic dimensionDiagnostic = FindDiagnostic(dimensionResult.Diagnostics, TiffErrorCode.ImageDimensionLimitExceeded);

        TiffReadOptions allocationOptions = new TiffReadOptions();
        allocationOptions.MemoryPolicy.MaximumAllocationBytes = bytes.Length - 1;
        TiffFile allocationFile = TiffApi.Factory.OpenReadBytes(bytes, allocationOptions);

        TiffReadOptions decodedOptions = new TiffReadOptions();
        decodedOptions.MemoryPolicy.MaximumDecodedBytes = 15;
        TiffImage decodedImage = TiffApi.Factory.OpenReadBytes(bytes, decodedOptions).GetImage(0);
        byte[] destination = CreateBytes(16, 99);
        TiffDiagnostics decodedDiagnostics = decodedImage.ReadSourceRasterToBuffer(destination);

        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.Limits.MaximumImageWidth = 3;
        TiffWriter writer = TiffApi.Factory.CreateWriter(writeOptions);
        int rejectedImageIndex = writer.AddConfiguredImage(CreateConfiguration(4, 4));

        Assert.That(dimensionResult.Success, Is.False);
        Assert.That(dimensionDiagnostic.Offset, Is.EqualTo(8));
        Assert.That(dimensionDiagnostic.ImageIndex, Is.EqualTo(0));
        Assert.That(allocationFile.GetSuccess(), Is.False);
        Assert.That(allocationFile.GetImageCount(), Is.EqualTo(0));
        Assert.That(allocationFile.GetDiagnostics().ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
        Assert.That(decodedDiagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
        Assert.That(destination, Is.EqualTo(CreateBytes(16, 99)));
        Assert.That(rejectedImageIndex, Is.EqualTo(-1));
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.ImageDimensionLimitExceeded), Is.True);
        allocationFile.Close();
        writer.Close();
    }

    [Test]
    public void CodecExpansionLimits_RejectDecodedSizeAndRatioBeforeCodecExecution()
    {
        CountingCodec codec = new CountingCodec();
        TiffCodecContext ratioContext = CreateCodecContext(100);
        ratioContext.Limits.MaximumCodecExpansionRatio = 10;
        TiffCodecResult ratio = TiffApi.Codec.DecodeWithCompressionCodec(ratioContext, new byte[] { 157, 0 }, codec);

        TiffCodecContext decodedContext = CreateCodecContext(100);
        decodedContext.Limits.MaximumDecodedBytes = 99;
        TiffCodecResult decoded = TiffApi.Codec.DecodeWithCompressionCodec(decodedContext, new byte[] { 157, 0 }, codec);

        Assert.That(ratio.Success, Is.False);
        Assert.That(ratio.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
        Assert.That(decoded.Success, Is.False);
        Assert.That(decoded.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
        Assert.That(codec.DecodeCallCount, Is.EqualTo(0));
    }

    [Test]
    public void CancellationAndProgress_LeaveValidationHandleReusableAfterInterruption()
    {
        TiffFile file = TiffApi.Factory.OpenReadBytes(CreateImageFile(3, 2, 2), new TiffReadOptions());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        ProgressRecorder cancelingProgress = new ProgressRecorder(cancellation, 1);

        Assert.That(() => TiffApi.Validation.ValidateFileWithCancellation(file, cancelingProgress, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
        Assert.That(file.GetIsOpen(), Is.True);
        Assert.That(cancelingProgress.Values, Has.Count.EqualTo(2));
        Assert.That(cancelingProgress.Values[0].CompletedUnits, Is.EqualTo(0));
        Assert.That(cancelingProgress.Values[1].CompletedUnits, Is.EqualTo(1));
        Assert.That(cancelingProgress.Values[1].TotalUnits, Is.EqualTo(3));

        ProgressRecorder completedProgress = new ProgressRecorder(null, -1);
        TiffValidationResult completed = TiffApi.Validation.ValidateFileWithCancellation(file, completedProgress, CancellationToken.None);

        Assert.That(completed.Success, Is.True, DiagnosticText(completed.Diagnostics));
        Assert.That(completedProgress.Values[0].CompletedUnits, Is.EqualTo(0));
        Assert.That(completedProgress.Values[completedProgress.Values.Count - 1].CompletedUnits, Is.EqualTo(3));
        AssertMonotonic(completedProgress.Values);
        file.Close();
    }

    [Test]
    public void FailedOperations_DeterministicallyReleaseOwnedStreamsAndPreserveBorrowedStreams()
    {
        MemoryStream ownedValidationStream = new MemoryStream(CreateMalformedBytes("invalid-version"));
        TiffValidationResult ownedValidation = TiffApi.Validation.ValidateStream(ownedValidationStream, new TiffReadOptions());

        MemoryStream borrowedValidationStream = new MemoryStream(CreateMalformedBytes("invalid-version"));
        TiffReadOptions borrowedReadOptions = new TiffReadOptions();
        borrowedReadOptions.LeaveOpen = true;
        TiffValidationResult borrowedValidation = TiffApi.Validation.ValidateStream(borrowedValidationStream, borrowedReadOptions);

        MemoryStream ownedWriterStream = new MemoryStream();
        TiffWriter ownedWriter = TiffApi.Factory.CreateStreamWriter(ownedWriterStream, new TiffWriteOptions());
        int ownedIndex = ownedWriter.AddConfiguredImage(CreateConfiguration(0, 1));
        ownedWriter.Close();
        ownedWriter.Close();

        MemoryStream borrowedWriterStream = new MemoryStream();
        TiffWriteOptions borrowedWriteOptions = new TiffWriteOptions();
        borrowedWriteOptions.LeaveOpen = true;
        TiffWriter borrowedWriter = TiffApi.Factory.CreateStreamWriter(borrowedWriterStream, borrowedWriteOptions);
        int borrowedIndex = borrowedWriter.AddConfiguredImage(CreateConfiguration(0, 1));
        borrowedWriter.Close();
        borrowedWriter.Close();

        Assert.That(ownedValidation.Success, Is.False);
        Assert.That(ownedValidationStream.CanRead, Is.False);
        Assert.That(borrowedValidation.Success, Is.False);
        Assert.That(borrowedValidationStream.CanRead, Is.True);
        Assert.That(ownedIndex, Is.EqualTo(-1));
        Assert.That(ownedWriter.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(ownedWriterStream.CanWrite, Is.False);
        Assert.That(borrowedIndex, Is.EqualTo(-1));
        Assert.That(borrowedWriter.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(borrowedWriterStream.CanWrite, Is.True);
        borrowedValidationStream.Dispose();
        borrowedWriterStream.Dispose();
    }

    static byte[] CreateUnsupportedCompressionFile()
    {
        TiffImageDefinition definition = CreateDefinition(2, 2, TiffCompression.Jpeg);
        TiffApi.Data.SetRawStrip(definition, 0, new byte[] { 1, 2, 3, 4 });
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        writer.AddImage(definition);
        AssertSuccess(writer.Flush());
        return writer.GetFileBytes();
    }

    static byte[] CreateImageFile(int imageCount, int width, int length)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int index = 0;
        while (index < imageCount)
        {
            int imageIndex = writer.AddConfiguredImage(CreateConfiguration(width, length));
            AssertSuccess(writer.WriteSourceRaster(imageIndex, CreateBytes(width * length, (byte)(index + 1))));
            index = index + 1;
        }

        AssertSuccess(writer.Flush());
        return writer.GetFileBytes();
    }

    static TiffWriterImageConfiguration CreateConfiguration(int width, int length)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.Layout.RowsPerStrip = 1;
        return configuration;
    }

    static TiffImageDefinition CreateDefinition(int width, int length, TiffCompression compression)
    {
        return TiffApi.Data.CreateImageDefinition(new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, (ulong)width),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, (ulong)length),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.Compression, TiffDataType.Short, (ulong)compression),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, (ulong)length)
        });
    }

    static TiffCodecContext CreateCodecContext(ulong expectedByteCount)
    {
        TiffCodecContext context = new TiffCodecContext();
        context.Info.Compression = TiffCompression.PackBits;
        context.Info.BitsPerSample = new int[] { 8 };
        context.ExpectedDecodedByteCount = expectedByteCount;
        return context;
    }

    static byte[] CreateMalformedBytes(string kind)
    {
        if (kind == "invalid-byte-order")
        {
            return new byte[8];
        }

        if (kind == "invalid-version")
        {
            byte[] invalidVersion = CreateClassicHeader(8, 8);
            WriteUInt16(invalidVersion, 2, 99);
            return invalidVersion;
        }

        if (kind == "invalid-bigtiff")
        {
            byte[] invalidBigTiff = new byte[16];
            invalidBigTiff[0] = 73;
            invalidBigTiff[1] = 73;
            WriteUInt16(invalidBigTiff, 2, 43);
            WriteUInt16(invalidBigTiff, 4, 4);
            WriteUInt16(invalidBigTiff, 6, 0);
            WriteUInt32(invalidBigTiff, 8, 16);
            return invalidBigTiff;
        }

        if (kind == "invalid-offset")
        {
            return CreateClassicHeader(8, 32);
        }

        if (kind == "truncated-directory")
        {
            byte[] truncated = CreateClassicHeader(10, 8);
            WriteUInt16(truncated, 8, 1);
            return truncated;
        }

        if (kind == "cyclic-directory")
        {
            byte[] cyclic = CreateClassicHeader(14, 8);
            WriteUInt16(cyclic, 8, 0);
            WriteUInt32(cyclic, 10, 8);
            return cyclic;
        }

        byte[] invalidType = CreateClassicHeader(26, 8);
        WriteUInt16(invalidType, 8, 1);
        WriteUInt16(invalidType, 10, TiffTagId.ImageWidth);
        WriteUInt16(invalidType, 12, 99);
        WriteUInt32(invalidType, 14, 1);
        WriteUInt32(invalidType, 18, 1);
        WriteUInt32(invalidType, 22, 0);
        return invalidType;
    }

    static byte[] CreateClassicHeader(int byteCount, uint firstIfdOffset)
    {
        byte[] bytes = new byte[byteCount];
        bytes[0] = 73;
        bytes[1] = 73;
        WriteUInt16(bytes, 2, 42);
        WriteUInt32(bytes, 4, firstIfdOffset);
        return bytes;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    static TiffDiagnostic FindDiagnostic(TiffDiagnostics diagnostics, TiffErrorCode code)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (diagnostics.Get(index).Code == code)
            {
                return diagnostics.Get(index);
            }

            index = index + 1;
        }

        Assert.Fail("Diagnostic was not found: " + code);
        return new TiffDiagnostic(TiffDiagnosticSeverity.Error, TiffErrorCode.UnknownError, "missing diagnostic");
    }

    static void AssertMonotonic(List<TiffProgress> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            Assert.That(values[index].CompletedUnits, Is.GreaterThan(values[index - 1].CompletedUnits));
            Assert.That(values[index].TotalUnits, Is.EqualTo(values[0].TotalUnits));
            index = index + 1;
        }
    }

    static byte[] CreateBytes(int count, byte value)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = value;
            index = index + 1;
        }

        return bytes;
    }

    static void AssertSuccess(TiffDiagnostics diagnostics)
    {
        Assert.That(diagnostics.HasErrors(), Is.False, DiagnosticText(diagnostics));
    }

    static string DiagnosticText(TiffDiagnostics diagnostics)
    {
        string text = "";
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (text.Length > 0)
            {
                text = text + " | ";
            }

            text = text + diagnostics.Format(diagnostics.Get(index));
            index = index + 1;
        }

        return text;
    }

    class ProgressRecorder : ITiffProgress
    {
        public List<TiffProgress> Values;
        public CancellationTokenSource? Cancellation;
        public long CancelAt;

        public ProgressRecorder(CancellationTokenSource? cancellation, long cancelAt)
        {
            Values = new List<TiffProgress>();
            Cancellation = cancellation;
            CancelAt = cancelAt;
        }

        public void Report(TiffProgress progress)
        {
            Values.Add(progress);
            if (Cancellation != null && progress.CompletedUnits == CancelAt)
            {
                Cancellation.Cancel();
            }
        }
    }

    class CountingCodec : ITiffCompressionCodec
    {
        public int DecodeCallCount;

        public CountingCodec()
        {
            DecodeCallCount = 0;
        }

        public bool CanDecode(TiffCompression compression)
        {
            return compression == TiffCompression.PackBits;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return false;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            DecodeCallCount = DecodeCallCount + 1;
            TiffCodecResult result = new TiffCodecResult();
            result.Success = true;
            result.Bytes = new byte[(int)context.ExpectedDecodedByteCount];
            return result;
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            return new TiffCodecResult();
        }
    }
}
