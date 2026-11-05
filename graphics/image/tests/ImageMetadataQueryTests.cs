using System.Collections.Generic;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataQueryTests
{
    [Test]
    public void Find_MatchesExactTagKeyInDocumentDirectories()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        MetadataTag make = directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        directory.AddTagWithValue("Exif.IFD0.Model", "Model", MetadataValue.FromString("R5"));

        List<MetadataTag> matches = new ImageMetadataQuery().Find(document, "Exif.IFD0.Make");
        MetadataTag first = RequiredMetadataQuery.FindFirst(document, "Exif.IFD0.Make");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0], Is.SameAs(make));
        Assert.That(first, Is.SameAs(make));
    }

    [Test]
    public void Find_MatchesFamilyGroupAndTagNamePaths()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        MetadataTag make = directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        MetadataTag model = directory.AddTagWithValue("Exif.IFD0.Model", "Model", MetadataValue.FromString("R5"));

        List<MetadataTag> slashMatches = new ImageMetadataQuery().Find(document, "Exif/IFD0/Make");
        List<MetadataTag> dottedMatches = new ImageMetadataQuery().Find(document, "Exif.IFD0.Model");

        Assert.That(slashMatches, Has.Count.EqualTo(1));
        Assert.That(slashMatches[0], Is.SameAs(make));
        Assert.That(dottedMatches, Has.Count.EqualTo(1));
        Assert.That(dottedMatches[0], Is.SameAs(model));
    }

    [Test]
    public void Find_SearchesNestedAndFrameDirectoriesInOrder()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory root = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        MetadataDirectory child = root.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS IFD");
        MetadataTag latitude = child.AddTagWithValue("Exif.GPS.Latitude", "Latitude", MetadataValue.FromDouble(45.5));
        ImageMetadataFrame frame = document.AddFrameWithDimensions(10, 20);
        MetadataDirectory frameDirectory = frame.AddDirectoryWithName(MetadataFamily.Png, "fcTL", "Frame Control");
        MetadataTag delay = frameDirectory.AddTagWithValue("Png.fcTL.Delay", "Delay", MetadataValue.FromInteger(33));

        List<MetadataTag> nestedMatches = new ImageMetadataQuery().Find(document, "Gps/GPS/Latitude");
        List<MetadataTag> frameMatches = new ImageMetadataQuery().Find(document, "Png/fcTL/Delay");

        Assert.That(nestedMatches, Has.Count.EqualTo(1));
        Assert.That(nestedMatches[0], Is.SameAs(latitude));
        Assert.That(frameMatches, Has.Count.EqualTo(1));
        Assert.That(frameMatches[0], Is.SameAs(delay));
    }

    [Test]
    public void Find_MatchesExplicitDirectoryPathWithTagNameOrKey()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "Dublin Core");
        directory.Path = "/metadata/xmp/dc/";
        MetadataTag title = directory.AddTagWithValue("dc:title", "title", MetadataValue.FromString("Sample"));

        List<MetadataTag> nameMatches = new ImageMetadataQuery().Find(document, "metadata/xmp/dc/title");
        List<MetadataTag> keyMatches = new ImageMetadataQuery().Find(document, "metadata/xmp/dc/dc:title");

        Assert.That(nameMatches, Has.Count.EqualTo(1));
        Assert.That(nameMatches[0], Is.SameAs(title));
        Assert.That(keyMatches, Has.Count.EqualTo(1));
        Assert.That(keyMatches[0], Is.SameAs(title));
    }

    [Test]
    public void Find_ReturnsEmptyAndNullForMissingOrInvalidQueries()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));

        Assert.That(new ImageMetadataQuery().Find(null, "Exif.IFD0.Make"), Is.Empty);
        Assert.That(new ImageMetadataQuery().Find(document, null), Is.Empty);
        Assert.That(new ImageMetadataQuery().Find(document, ""), Is.Empty);
        Assert.That(new ImageMetadataQuery().Find(document, "Exif.IFD0.Missing"), Is.Empty);
        Assert.That(new ImageMetadataQuery().Find(document, "exif.IFD0.Make"), Is.Empty);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Exif.IFD0.Missing"), Is.Null);
    }

    [Test]
    public void GetTagsAndDirectories_EnumerateNestedAndFrameContentInOrder()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory root = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "IFD0");
        root.Path = "metadata/exif/ifd0";
        MetadataTag rootTag = root.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        MetadataDirectory child = root.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS");
        child.Path = "metadata/exif/gps";
        MetadataTag childTag = child.AddTagWithValue("Exif.GPS.Latitude", "Latitude", MetadataValue.FromDouble(1));
        ImageMetadataFrame frame = document.AddFrameWithDimensions(10, 10);
        MetadataDirectory frameDirectory = frame.AddDirectoryWithName(MetadataFamily.Png, "fcTL", "Frame Control");
        frameDirectory.Path = "metadata/png/fctl";
        MetadataTag frameTag = frameDirectory.AddTagWithValue("Png.fcTL.Delay", "Delay", MetadataValue.FromInteger(25));
        IImageMetadataQuery query = new ImageMetadataQuery();

        List<MetadataTag> tags = query.GetTags(document);
        List<MetadataDirectory> directories = query.GetDirectories(document);

        Assert.That(tags, Is.EqualTo(new[] { rootTag, childTag, frameTag }));
        Assert.That(directories, Is.EqualTo(new[] { root, child, frameDirectory }));
        tags.Clear();
        directories.Clear();
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Directories, Has.Count.EqualTo(1));
    }

    [Test]
    public void DirectoryQueries_FilterByFamilyPathAndFrameWithoutLosingOrder()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.AddFrameWithDimensions(20, 20);
        ImageMetadataFrame secondFrame = document.AddFrameWithDimensions(10, 10);
        MetadataDirectory first = document.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "First XMP");
        first.Path = "/metadata/xmp/dc/";
        MetadataDirectory frameIndexed = document.AddDirectoryWithName(MetadataFamily.Xmp, "rights", "Frame XMP");
        frameIndexed.Path = "metadata/xmp/rights";
        frameIndexed.FrameIndex = 1;
        MetadataDirectory nested = frameIndexed.AddChildWithName(MetadataFamily.Iptc, "Application2", "IPTC");
        nested.Path = "metadata/iptc/application2";
        MetadataDirectory owned = secondFrame.AddDirectoryWithName(MetadataFamily.Xmp, "photoshop", "Owned XMP");
        owned.Path = "metadata/xmp/photoshop";
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.FindDirectoriesByFamily(document, MetadataFamily.Xmp), Is.EqualTo(new[] { first, frameIndexed, owned }));
        Assert.That(query.FindDirectoriesByPath(document, "metadata/xmp/dc"), Is.EqualTo(new[] { first }));
        Assert.That(query.FindDirectoriesByPath(document, "Iptc/Application2"), Is.EqualTo(new[] { nested }));
        Assert.That(query.GetDirectoriesForFrame(document, 1), Is.EqualTo(new[] { frameIndexed, nested, owned }));
        Assert.That(query.GetDirectoriesForFrame(document, -1), Is.Empty);
        Assert.That(query.FindDirectoriesByPath(document, null), Is.Empty);
    }

    [Test]
    public void FrameTagQueries_IncludeIndexedDocumentDirectoriesAndOwnedDirectories()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.AddFrameWithDimensions(20, 20);
        ImageMetadataFrame secondFrame = document.AddFrameWithDimensions(10, 10);
        MetadataDirectory indexed = document.AddDirectoryWithName(MetadataFamily.WebP, "ANMF", "Frame Chunk");
        indexed.FrameIndex = 1;
        MetadataTag duration = indexed.AddTagWithValue("WebP.ANMF.Duration", "Duration", MetadataValue.FromInteger(20));
        MetadataDirectory child = indexed.AddChildWithName(MetadataFamily.Xmp, "xmp", "Frame XMP");
        MetadataTag title = child.AddTagWithValue("Xmp.dc.title", "title", MetadataValue.FromString("Frame"));
        MetadataDirectory owned = secondFrame.AddDirectoryWithName(MetadataFamily.Png, "fcTL", "Frame Control");
        MetadataTag delay = owned.AddTagWithValue("Png.fcTL.Delay", "Delay", MetadataValue.FromInteger(30));
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.GetTagsForFrame(document, 1), Is.EqualTo(new[] { duration, title, delay }));
        Assert.That(query.FindTagsForFrame(document, 1, "Xmp/xmp/title"), Is.EqualTo(new[] { title }));
        Assert.That(query.FindTagsForFrame(document, 0, "Xmp/xmp/title"), Is.Empty);
        Assert.That(query.FindTagsForFrame(document, 1, null), Is.Empty);
    }

    [Test]
    public void ProfileQueries_ExposeRawPayloadsAndFilterIdentityRelationshipsAndRoles()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.AddFrameWithDimensions(100, 100);
        document.AddFrameWithDimensions(50, 50);
        MetadataProfile xmp = document.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "profile.xmp", "XMP", new byte[] { 1 });
        MetadataProfile thumbnail = document.AddProfileWithData(MetadataProfileKind.Thumbnail, MetadataFamily.Thumbnail, "profile.thumb", "Thumbnail", new byte[] { 2, 3 });
        MetadataProfile preview = new MetadataProfile();
        preview.Kind = MetadataProfileKind.Thumbnail;
        preview.Family = MetadataFamily.Thumbnail;
        preview.Key = "profile.preview";
        preview.Role = ImageMetadataRole.Preview;
        preview.SourceFrameIndex = 1;
        preview.Data = new byte[] { 4, 5, 6 };
        document.AddProfile(preview);
        MetadataProfile secondXmp = document.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "profile.xmp", "Second XMP", new byte[] { 7 });
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.GetProfiles(document), Is.EqualTo(new[] { xmp, thumbnail, preview, secondXmp }));
        Assert.That(query.FindProfilesByFamily(document, MetadataFamily.Xmp), Is.EqualTo(new[] { xmp, secondXmp }));
        Assert.That(query.FindProfilesByKind(document, MetadataProfileKind.Thumbnail), Is.EqualTo(new[] { thumbnail, preview }));
        Assert.That(query.FindProfilesByKey(document, "profile.xmp"), Is.EqualTo(new[] { xmp, secondXmp }));
        Assert.That(query.FindProfilesForFrame(document, 0), Is.EqualTo(new[] { thumbnail }));
        Assert.That(query.FindProfilesForFrame(document, 1), Is.EqualTo(new[] { preview }));
        Assert.That(query.FindProfilesByRole(document, ImageMetadataRole.Thumbnail), Is.EqualTo(new[] { thumbnail }));
        Assert.That(query.FindProfilesByRole(document, ImageMetadataRole.Preview), Is.EqualTo(new[] { preview }));
        Assert.That(query.FindRoleProfilesForFrame(document, 1, ImageMetadataRole.Preview), Is.EqualTo(new[] { preview }));
        Assert.That(query.FindProfilesByKey(document, "PROFILE.XMP"), Is.Empty);
        Assert.That(query.FindProfilesByKey(document, null), Is.Empty);
        Assert.That(query.FindProfilesByKey(document, "profile.preview")[0].Data, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void RawBlockQueries_PreserveOrderAndExposeCapturedBytes()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataRawBlock first = document.AddRawBlockWithRange(MetadataFamily.Jpeg, "APP1", "EXIF", 2, 8);
        first.Data = new byte[] { 1, 2 };
        MetadataRawBlock second = document.AddRawBlockWithRange(MetadataFamily.Jpeg, "APP13", "Photoshop", 10, 9);
        second.Data = new byte[] { 3 };
        MetadataRawBlock third = document.AddRawBlockWithRange(MetadataFamily.Xmp, "APP1", "XMP", 19, 7);
        third.Data = new byte[] { 4, 5 };
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.GetRawBlocks(document), Is.EqualTo(new[] { first, second, third }));
        Assert.That(query.FindRawBlocksByFamily(document, MetadataFamily.Jpeg), Is.EqualTo(new[] { first, second }));
        Assert.That(query.FindRawBlocksByType(document, "APP1"), Is.EqualTo(new[] { first, third }));
        Assert.That(query.FindRawBlocksByType(document, "app1"), Is.Empty);
        Assert.That(query.FindRawBlocksByType(document, null), Is.Empty);
        Assert.That(query.FindRawBlocksByType(document, "APP1")[1].Data, Is.EqualTo(new byte[] { 4, 5 }));
    }

    [Test]
    public void FrameRoleQueries_FindThumbnailsPreviewsAndParentRelationshipsInOrder()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame primary = document.AddFrameWithDimensions(100, 100);
        ImageMetadataFrame thumbnail = document.AddFrameWithDimensions(20, 20);
        thumbnail.Role = ImageMetadataRole.Thumbnail;
        thumbnail.ParentFrameIndex = primary.Index;
        ImageMetadataFrame preview = document.AddFrameWithDimensions(50, 50);
        preview.Role = ImageMetadataRole.Preview;
        preview.ParentFrameIndex = primary.Index;
        ImageMetadataFrame otherPreview = document.AddFrameWithDimensions(40, 40);
        otherPreview.Role = ImageMetadataRole.Preview;
        otherPreview.ParentFrameIndex = thumbnail.Index;
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.FindFramesByRole(document, ImageMetadataRole.Thumbnail), Is.EqualTo(new[] { thumbnail }));
        Assert.That(query.FindFramesByRole(document, ImageMetadataRole.Preview), Is.EqualTo(new[] { preview, otherPreview }));
        Assert.That(query.FindRelatedFrames(document, primary.Index, ImageMetadataRole.Thumbnail), Is.EqualTo(new[] { thumbnail }));
        Assert.That(query.FindRelatedFrames(document, primary.Index, ImageMetadataRole.Preview), Is.EqualTo(new[] { preview }));
        Assert.That(query.FindRelatedFrames(document, -1, ImageMetadataRole.Preview), Is.Empty);
    }

    [Test]
    public void ExpandedQueries_ReturnEmptyListsForNullDocuments()
    {
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.GetTags(null), Is.Empty);
        Assert.That(query.GetTagsForFrame(null, 0), Is.Empty);
        Assert.That(query.FindTagsForFrame(null, 0, "tag"), Is.Empty);
        Assert.That(query.GetDirectories(null), Is.Empty);
        Assert.That(query.FindDirectoriesByFamily(null, MetadataFamily.Exif), Is.Empty);
        Assert.That(query.FindDirectoriesByPath(null, "path"), Is.Empty);
        Assert.That(query.GetDirectoriesForFrame(null, 0), Is.Empty);
        Assert.That(query.GetProfiles(null), Is.Empty);
        Assert.That(query.FindProfilesByFamily(null, MetadataFamily.Exif), Is.Empty);
        Assert.That(query.FindProfilesByKind(null, MetadataProfileKind.Exif), Is.Empty);
        Assert.That(query.FindProfilesByKey(null, "key"), Is.Empty);
        Assert.That(query.FindProfilesForFrame(null, 0), Is.Empty);
        Assert.That(query.FindProfilesByRole(null, ImageMetadataRole.Preview), Is.Empty);
        Assert.That(query.FindRoleProfilesForFrame(null, 0, ImageMetadataRole.Preview), Is.Empty);
        Assert.That(query.GetRawBlocks(null), Is.Empty);
        Assert.That(query.FindRawBlocksByFamily(null, MetadataFamily.Exif), Is.Empty);
        Assert.That(query.FindRawBlocksByType(null, "APP1"), Is.Empty);
        Assert.That(query.FindFramesByRole(null, ImageMetadataRole.Thumbnail), Is.Empty);
        Assert.That(query.FindRelatedFrames(null, 0, ImageMetadataRole.Preview), Is.Empty);
    }
}
