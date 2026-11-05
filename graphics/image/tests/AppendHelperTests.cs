using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class AppendHelperTests
{
    [Test]
    public void DocumentFrameHelpers_AssignIndexesAndUpdateSummary()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame first = new ImageMetadataFrame();
        first.Width = 640;
        first.Height = 480;

        ImageMetadataFrame addedFirst = document.AddFrame(first);
        ImageMetadataFrame addedSecond = document.AddFrameWithDimensions(320, 240);

        Assert.That(addedFirst, Is.SameAs(first));
        Assert.That(addedFirst.Index, Is.EqualTo(0));
        Assert.That(addedSecond.Index, Is.EqualTo(1));
        Assert.That(document.Frames, Has.Count.EqualTo(2));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(2));
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
    }

    [Test]
    public void DocumentAppendHelpers_AddDirectoriesProfilesRawBlocksAndDiagnostics()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Png, "IHDR", "Header");
        MetadataProfile profile = document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "ICC profile", new byte[] { 1, 2, 3 });
        MetadataRawBlock rawBlock = document.AddRawBlockWithRange(MetadataFamily.Png, "iTXt", "Text", 12, 24);
        MetadataDiagnostic diagnostic = document.AddDiagnosticWithSeverity(MetadataSeverity.Warning, "W001", "warning");

        Assert.That(document.Directories[0], Is.SameAs(directory));
        Assert.That(directory.Family, Is.EqualTo(MetadataFamily.Png));
        Assert.That(document.Profiles[0], Is.SameAs(profile));
        Assert.That(profile.Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(profile.Length, Is.EqualTo(3));
        Assert.That(document.RawBlocks[0], Is.SameAs(rawBlock));
        Assert.That(rawBlock.Offset, Is.EqualTo(12));
        Assert.That(rawBlock.Length, Is.EqualTo(24));
        Assert.That(document.Diagnostics[0], Is.SameAs(diagnostic));
        Assert.That(diagnostic.Code, Is.EqualTo("W001"));
    }

    [Test]
    public void FrameDirectoryHelper_PropagatesFrameIndex()
    {
        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Index = 4;

        MetadataDirectory directory = frame.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");

        Assert.That(frame.Directories, Has.Count.EqualTo(1));
        Assert.That(frame.Directories[0], Is.SameAs(directory));
        Assert.That(directory.FrameIndex, Is.EqualTo(4));
        Assert.That(directory.Group, Is.EqualTo("IFD0"));
    }

    [Test]
    public void DirectoryTagHelper_FillsGroupFamilyRepeatIndexAndValueType()
    {
        MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Exif, "IFD0", "Image File Directory 0");

        MetadataTag first = directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        MetadataTag second = directory.AddTag(MetadataTag.Create("Exif.IFD0.Model", "Model", MetadataFamily.Unknown, MetadataValue.FromString("R5")));

        Assert.That(directory.Tags, Has.Count.EqualTo(2));
        Assert.That(first.Group, Is.EqualTo("IFD0"));
        Assert.That(first.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(first.RepeatIndex, Is.EqualTo(0));
        Assert.That(first.ValueType, Is.EqualTo(MetadataValueKind.String));
        Assert.That(second.Group, Is.EqualTo("IFD0"));
        Assert.That(second.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(second.RepeatIndex, Is.EqualTo(1));
    }

    [Test]
    public void DirectoryChildHelper_PropagatesFrameIndex()
    {
        MetadataDirectory parent = MetadataDirectory.Create(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        parent.FrameIndex = 7;

        MetadataDirectory child = parent.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS IFD");

        Assert.That(parent.Children, Has.Count.EqualTo(1));
        Assert.That(parent.Children[0], Is.SameAs(child));
        Assert.That(child.FrameIndex, Is.EqualTo(7));
        Assert.That(child.Family, Is.EqualTo(MetadataFamily.Gps));
    }

    [Test]
    public void AppendHelpers_CreateDefaultsForNullInputs()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame frame = new ImageMetadataFrame();
        MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Exif, "IFD0", "Image File Directory 0");

        Assert.That(document.AddFrame(null), Is.Not.Null);
        Assert.That(document.AddDirectory(null), Is.Not.Null);
        Assert.That(document.AddProfile(null), Is.Not.Null);
        Assert.That(document.AddRawBlock(null), Is.Not.Null);
        Assert.That(document.AddDiagnostic(null), Is.Not.Null);
        Assert.That(frame.AddDirectory(null), Is.Not.Null);
        Assert.That(directory.AddTag(null), Is.Not.Null);
        Assert.That(directory.AddChild(null), Is.Not.Null);
    }
}
