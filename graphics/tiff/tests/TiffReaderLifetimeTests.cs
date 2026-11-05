using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffReaderLifetimeTests
{
    [Test]
    public void OpenReadStream_UsesCurrentPositionAsTiffOrigin()
    {
        byte[] tiffBytes = CreateClassicFileBytes();
        byte[] prefixedBytes = new byte[tiffBytes.Length + 7];
        tiffBytes.CopyTo(prefixedBytes, 7);
        MemoryStream stream = new MemoryStream(prefixedBytes);
        stream.Position = 7;
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffFile file = TiffApi.Factory.OpenReadStream(stream, options);
        TiffRawStripReadResult strip = file.GetImage(0).ReadRawStrip(0);

        Assert.That(file.GetSuccess(), Is.True);
        Assert.That(strip.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(stream.Position, Is.EqualTo(7));
        file.Close();
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void OpenReadStream_RejectsNonSeekableStreamAndHonorsLeaveOpen()
    {
        NonSeekableStream defaultStream = new NonSeekableStream(CreateClassicFileBytes());
        NonSeekableStream retainedStream = new NonSeekableStream(CreateClassicFileBytes());
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffFile defaultFile = TiffApi.Factory.OpenReadStream(defaultStream, new TiffReadOptions());
        TiffFile retainedFile = TiffApi.Factory.OpenReadStream(retainedStream, options);

        Assert.That(defaultFile.GetSuccess(), Is.False);
        Assert.That(defaultFile.GetDiagnostics().ContainsCode(TiffErrorCode.NonSeekableStream), Is.True);
        Assert.That(defaultStream.CanRead, Is.False);
        Assert.That(retainedFile.GetSuccess(), Is.False);
        Assert.That(retainedFile.GetDiagnostics().ContainsCode(TiffErrorCode.NonSeekableStream), Is.True);
        Assert.That(retainedStream.CanRead, Is.True);
    }

    [Test]
    public void Close_ClosesOwnedMemoryEvenWhenLeaveOpenIsSet()
    {
        byte[] bytes = CreateClassicFileBytes();
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;
        TiffFile file = TiffApi.Factory.OpenReadBytes(bytes, options);
        ITiffRandomAccessSource source = file.GetSource();

        bytes[0] = 0;
        Assert.That(file.GetFileBytes()[0], Is.EqualTo((byte)0x49));

        file.Close();
        file.Close();

        Assert.That(file.GetIsOpen(), Is.False);
        Assert.That(source.GetIsOpen(), Is.False);
        Assert.That(file.GetFileByteCount(), Is.EqualTo(0));
    }

    [Test]
    public void OpenReadMemory_CopiesCallerMemoryAndOwnsTheSnapshot()
    {
        byte[] bytes = CreateClassicFileBytes();
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffFile file = TiffApi.Factory.OpenReadMemory(bytes.AsSpan(), options);
        ITiffRandomAccessSource source = file.GetSource();
        bytes[0] = 0;

        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(file.GetFileBytes()[0], Is.EqualTo((byte)0x49));
        file.Close();
        Assert.That(source.GetIsOpen(), Is.False);
    }

    [Test]
    public void Close_ClosesOwnedPathEvenWhenLeaveOpenIsSet()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        try
        {
            File.WriteAllBytes(path, CreateClassicFileBytes());
            TiffReadOptions options = new TiffReadOptions();
            options.LeaveOpen = true;
            TiffFile file = TiffApi.Factory.OpenReadPath(path, options);
            ITiffRandomAccessSource source = file.GetSource();

            file.Close();

            Assert.That(source.GetIsOpen(), Is.False);
            Assert.That(file.GetIsOpen(), Is.False);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Close_HonorsCallerProvidedStreamOwnership()
    {
        MemoryStream ownedStream = new MemoryStream(CreateClassicFileBytes());
        MemoryStream retainedStream = new MemoryStream(CreateClassicFileBytes());
        TiffReadOptions retainedOptions = new TiffReadOptions();
        retainedOptions.LeaveOpen = true;
        TiffFile ownedFile = TiffApi.Factory.OpenReadStream(ownedStream, new TiffReadOptions());
        TiffFile retainedFile = TiffApi.Factory.OpenReadStream(retainedStream, retainedOptions);

        ownedFile.Close();
        retainedFile.Close();

        Assert.That(ownedStream.CanRead, Is.False);
        Assert.That(retainedStream.CanRead, Is.True);
        Assert.That(retainedFile.GetSource().GetIsOpen(), Is.True);
    }

    [Test]
    public void Close_InvalidatesPreviouslyCreatedImageHandles()
    {
        TiffFile file = TiffApi.Factory.OpenReadBytes(CreateClassicFileBytes(), new TiffReadOptions());
        TiffImage image = file.GetImage(0);

        file.Close();
        TiffImageInfo info = image.GetInfo();
        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(image.GetIsOpen(), Is.False);
        Assert.That(image.GetCanReadImageData(), Is.False);
        Assert.That(info.Width, Is.EqualTo(0));
        Assert.That(strip.Success, Is.False);
        Assert.That(image.GetDiagnostics().ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
        Assert.That(file.GetSuccess(), Is.False);
    }

    [Test]
    public void MetadataOnly_ExposesMetadataButDisablesPayloadReads()
    {
        TiffFile file = TiffApi.Factory.OpenMetadataBytes(CreateClassicFileBytes(), new TiffReadOptions());
        TiffImage image = file.GetImage(0);

        TiffTagDefaultResult description = image.GetTagDefaulted(TiffTagId.ImageDescription);
        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(file.GetIsOpen(), Is.True);
        Assert.That(file.GetCanReadImageData(), Is.False);
        Assert.That(image.GetIsOpen(), Is.True);
        Assert.That(image.GetCanReadImageData(), Is.False);
        Assert.That(description.Found, Is.True);
        Assert.That(strip.Success, Is.False);
    }

    [Test]
    public void MetadataOnlyStream_UsesTheSameOwnershipRules()
    {
        MemoryStream ownedStream = new MemoryStream(CreateClassicFileBytes());
        MemoryStream retainedStream = new MemoryStream(CreateClassicFileBytes());
        TiffReadOptions retainedOptions = new TiffReadOptions();
        retainedOptions.LeaveOpen = true;
        TiffFile owned = TiffApi.Factory.OpenMetadataStream(ownedStream, new TiffReadOptions());
        TiffFile retained = TiffApi.Factory.OpenMetadataStream(retainedStream, retainedOptions);

        owned.Close();
        retained.Close();

        Assert.That(ownedStream.CanRead, Is.False);
        Assert.That(retainedStream.CanRead, Is.True);
    }

    [Test]
    public void FailedOpen_DeterministicallyAppliesSourceOwnership()
    {
        MemoryStream closedByReader = new MemoryStream(new byte[] { 0, 0, 0, 0 });
        MemoryStream retained = new MemoryStream(new byte[] { 0, 0, 0, 0 });
        TiffReadOptions retainedOptions = new TiffReadOptions();
        retainedOptions.LeaveOpen = true;

        TiffFile closedFile = TiffApi.Factory.OpenReadStream(closedByReader, new TiffReadOptions());
        TiffFile retainedFile = TiffApi.Factory.OpenReadStream(retained, retainedOptions);

        Assert.That(closedFile.GetSuccess(), Is.False);
        Assert.That(closedByReader.CanRead, Is.False);
        Assert.That(retainedFile.GetSuccess(), Is.False);
        Assert.That(retained.CanRead, Is.True);
    }

    [Test]
    public void TrailingData_IsLocatedAfterReferencedPayloadAndReadOnDemand()
    {
        byte[] fileBytes = CreateClassicFileBytes();
        byte[] trailingBytes = new byte[] { 90, 91, 92, 93 };
        byte[] combined = new byte[fileBytes.Length + trailingBytes.Length];
        fileBytes.CopyTo(combined, 0);
        trailingBytes.CopyTo(combined, fileBytes.Length);
        TiffFile file = TiffApi.Factory.OpenReadBytes(combined, new TiffReadOptions());

        TiffByteRange range = file.GetTrailingDataRange();
        byte[] actual = file.GetTrailingData();
        byte[] destination = new byte[trailingBytes.Length];
        TiffDiagnostics diagnostics = file.ReadTrailingData(destination);

        Assert.That(range.Offset, Is.EqualTo((ulong)fileBytes.Length));
        Assert.That(range.ByteCount, Is.EqualTo((ulong)trailingBytes.Length));
        Assert.That(range.EndOffset, Is.EqualTo((ulong)combined.Length));
        Assert.That(actual, Is.EqualTo(trailingBytes));
        Assert.That(destination, Is.EqualTo(trailingBytes));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void TrailingData_RespectsPreservationAndDestinationPolicies()
    {
        byte[] fileBytes = CreateClassicFileBytes();
        byte[] combined = new byte[fileBytes.Length + 3];
        fileBytes.CopyTo(combined, 0);
        combined[fileBytes.Length] = 7;
        combined[fileBytes.Length + 1] = 8;
        combined[fileBytes.Length + 2] = 9;
        TiffReadOptions discardedOptions = new TiffReadOptions();
        discardedOptions.PreserveTrailingData = false;
        TiffFile discarded = TiffApi.Factory.OpenReadBytes(combined, discardedOptions);
        TiffFile preserved = TiffApi.Factory.OpenReadBytes(combined, new TiffReadOptions());

        TiffDiagnostics diagnostics = preserved.ReadTrailingData(new byte[2]);

        Assert.That(discarded.GetTrailingDataRange().ByteCount, Is.EqualTo(0));
        Assert.That(discarded.GetTrailingData(), Is.Empty);
        Assert.That(discarded.GetFileByteCount(), Is.EqualTo(fileBytes.Length));
        Assert.That(discarded.GetFileBytes().Length, Is.EqualTo(fileBytes.Length));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static byte[] CreateClassicFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "reader lifetime metadata")
        };
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(tags));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    public class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] bytes) : base(bytes)
        {
        }

        public override bool CanSeek
        {
            get { return false; }
        }
    }
}
