using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffExternalOverviewTests
{
    [Test]
    public void ResolveExternalOverviewPaths_UsesExactPathAndExposesRelationship()
    {
        string path = CreateTempPath();
        TiffFile baseFile = TiffApi.Factory.OpenReadBytes(CreateFileBytes(new ulong[] { 8 }, new ulong[] { 8 }, TiffPhotometric.BlackIsZero, false), new TiffReadOptions());
        TiffExternalOverviewSet? result = null;
        try
        {
            File.WriteAllBytes(path, CreateFileBytes(new ulong[] { 4 }, new ulong[] { 4 }, TiffPhotometric.BlackIsZero, true));

            result = TiffApi.Factory.ResolveExternalOverviewPaths(baseFile.GetImage(0), new string[] { path }, new TiffReadOptions());
            TiffExternalOverviewRelationship relationship = result.GetRelationship(0);

            Assert.That(result.GetSuccess(), Is.True);
            Assert.That(result.GetIsOpen(), Is.True);
            Assert.That(result.GetSourceCount(), Is.EqualTo(1));
            Assert.That(result.GetRelationshipCount(), Is.EqualTo(1));
            Assert.That(relationship.LevelIndex, Is.EqualTo(0));
            Assert.That(relationship.BaseImageIndex, Is.EqualTo(0));
            Assert.That(relationship.SourceIndex, Is.EqualTo(0));
            Assert.That(relationship.SourceImageIndex, Is.EqualTo(0));
            Assert.That(relationship.IsPathSource, Is.True);
            Assert.That(relationship.SourcePath, Is.EqualTo(path));
            Assert.That(relationship.BaseWidth, Is.EqualTo(8));
            Assert.That(relationship.BaseLength, Is.EqualTo(8));
            Assert.That(relationship.OverviewWidth, Is.EqualTo(4));
            Assert.That(relationship.OverviewLength, Is.EqualTo(4));
            Assert.That(relationship.HorizontalReduction, Is.EqualTo(2d));
            Assert.That(relationship.VerticalReduction, Is.EqualTo(2d));
            Assert.That(relationship.IsReducedResolution, Is.True);
            Assert.That(result.GetOverviewImage(0).GetInfo().Width, Is.EqualTo(4));
            Assert.That(result.GetBaseImage().GetIndex(), Is.EqualTo(0));

            result.Close();

            Assert.That(result.GetIsOpen(), Is.False);
            Assert.That(result.GetSourceFile(0).GetIsOpen(), Is.False);
            Assert.That(baseFile.GetIsOpen(), Is.True);
        }
        finally
        {
            result?.Close();
            baseFile.Close();
            DeleteIfExists(path);
        }
    }

    [Test]
    public void ResolveExternalOverviewStreams_OrdersLevelsAndHonorsLeaveOpen()
    {
        TiffFile baseFile = TiffApi.Factory.OpenReadBytes(CreateFileBytes(new ulong[] { 8 }, new ulong[] { 8 }, TiffPhotometric.BlackIsZero, false), new TiffReadOptions());
        MemoryStream stream = new MemoryStream(CreateFileBytes(new ulong[] { 2, 4 }, new ulong[] { 2, 4 }, TiffPhotometric.BlackIsZero, true));
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffExternalOverviewSet result = TiffApi.Factory.ResolveExternalOverviewStreams(baseFile.GetImage(0), new Stream[] { stream }, options);

        Assert.That(result.GetSuccess(), Is.True);
        Assert.That(result.GetRelationshipCount(), Is.EqualTo(2));
        Assert.That(result.GetRelationship(0).OverviewWidth, Is.EqualTo(4));
        Assert.That(result.GetRelationship(0).SourceImageIndex, Is.EqualTo(1));
        Assert.That(result.GetRelationship(0).IsPathSource, Is.False);
        Assert.That(result.GetRelationship(0).SourcePath, Is.Empty);
        Assert.That(result.GetRelationship(1).OverviewWidth, Is.EqualTo(2));
        Assert.That(result.GetRelationship(1).SourceImageIndex, Is.EqualTo(0));
        Assert.That(result.GetOverviewImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(FillBytes(16, 2)));

        result.Close();

        Assert.That(stream.CanRead, Is.True);
        baseFile.Close();
        stream.Dispose();
    }

    [Test]
    public void ResolveExternalOverviewStreams_FiltersIncompatibleImagesAndOwnsStreamsByDefault()
    {
        TiffFile baseFile = TiffApi.Factory.OpenReadBytes(CreateFileBytes(new ulong[] { 8 }, new ulong[] { 8 }, TiffPhotometric.BlackIsZero, false), new TiffReadOptions());
        MemoryStream stream = new MemoryStream(CreateFileBytes(new ulong[] { 4 }, new ulong[] { 4 }, TiffPhotometric.WhiteIsZero, true));

        TiffExternalOverviewSet result = TiffApi.Factory.ResolveExternalOverviewStreams(baseFile.GetImage(0), new Stream[] { stream }, new TiffReadOptions());

        Assert.That(result.GetSuccess(), Is.True);
        Assert.That(result.GetRelationshipCount(), Is.EqualTo(0));
        Assert.That(result.GetDiagnostics().HasWarnings(), Is.True);

        result.Close();

        Assert.That(stream.CanRead, Is.False);
        baseFile.Close();
    }

    static byte[] CreateFileBytes(ulong[] widths, ulong[] lengths, TiffPhotometric photometric, bool markReduced)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int index = 0;
        while (index < widths.Length)
        {
            int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(widths[index], lengths[index], photometric, markReduced)));
            int byteCount = (int)(widths[index] * lengths[index]);
            writer.WriteRawStrip(imageIndex, 0, FillBytes(byteCount, index + 1));
            index = index + 1;
        }

        writer.Flush();
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, TiffPhotometric photometric, bool markReduced)
    {
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)photometric),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length)
        };
        if (!markReduced)
        {
            return tags;
        }

        TiffTagValue[] reducedTags = new TiffTagValue[tags.Length + 1];
        int index = 0;
        while (index < tags.Length)
        {
            reducedTags[index] = tags[index];
            index = index + 1;
        }

        reducedTags[tags.Length] = TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 1);
        return reducedTags;
    }

    static byte[] FillBytes(int count, int value)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < count)
        {
            bytes[index] = (byte)value;
            index = index + 1;
        }

        return bytes;
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        DeleteIfExists(path);
        return path;
    }

    static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
