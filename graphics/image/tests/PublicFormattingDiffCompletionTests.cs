using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicFormattingDiffCompletionTests
{
    [Test]
    public void Formatters_RepresentEveryMetadataValueKindThroughThePublicContract()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = factory.CreateDirectory(MetadataFamily.Exif, "Values", "Every value");
        MetadataDirectory nested = factory.CreateDirectory(MetadataFamily.Thumbnail, "Nested", "Nested directory");
        MetadataValue[] values = new MetadataValue[]
        {
            new MetadataValue(),
            factory.CreateIntegerValue(-7),
            factory.CreateUnsignedIntegerValue(8),
            factory.CreateRationalValue(1, 3),
            factory.CreateSignedRationalValue(-2, 5),
            factory.CreateDoubleValue(1.25),
            factory.CreateStringValue("text"),
            factory.CreateStringListValue(new string[] { "one", "two" }),
            factory.CreateBytesValue(new byte[] { 1, 2, 255 }),
            factory.CreateDateTextValue("2026:08:25 12:34:56"),
            factory.CreateXmlTextValue("<x />"),
            factory.CreateDirectoryValue(nested),
            factory.CreateBooleanValue(true),
            factory.CreateIntegerArrayValue(new long[] { -1, 2 }),
            factory.CreateUnsignedIntegerArrayValue(new ulong[] { 3, 4 }),
            factory.CreateRationalArrayValue(new long[] { 1, 3 }, new long[] { 2, 4 }),
            factory.CreateSignedRationalArrayValue(new long[] { -1 }, new long[] { 2 }),
            factory.CreateDoubleArrayValue(new double[] { 1.25, -2.5 })
        };
        string[] textValues = new string[]
        {
            "", "-7", "8", "1/3", "-2/5", "1.25", "text", "one, two", "3 bytes",
            "2026:08:25 12:34:56", "<x />", "Nested directory", "true", "-1, 2", "3, 4",
            "1/2, 3/4", "-1/2", "1.25, -2.5"
        };
        int index = 0;
        while (index < values.Length)
        {
            string key = "Value." + values[index].Kind.ToString();
            directory.Tags.Add(factory.CreateTag(key, values[index].Kind.ToString(), MetadataFamily.Exif, values[index]));
            index = index + 1;
        }
        document.Directories.Add(directory);

        string text = new ImageMetadataFormatter().ToText(document);
        string jsonText = new ImageMetadataFormatter().ToJson(document);

        index = 0;
        while (index < values.Length)
        {
            string key = "Value." + values[index].Kind.ToString();
            Assert.That(text, Does.Contain(key + " (" + values[index].Kind + ") = " + textValues[index]), values[index].Kind.ToString());
            index = index + 1;
        }

        using (JsonDocument json = JsonDocument.Parse(jsonText))
        {
            JsonElement tags = json.RootElement.GetProperty("directories")[0].GetProperty("tags");
            Assert.That(tags.GetArrayLength(), Is.EqualTo(Enum.GetValues<MetadataValueKind>().Length));
            index = 0;
            while (index < tags.GetArrayLength())
            {
                Assert.That(tags[index].GetProperty("value").GetProperty("kind").GetString(), Is.EqualTo(values[index].Kind.ToString()));
                index = index + 1;
            }

            Assert.That(tags[0].GetProperty("value").EnumerateObject().Count(), Is.EqualTo(1));
            Assert.That(tags[8].GetProperty("value").GetProperty("hex").GetString(), Is.EqualTo("0102ff"));
            Assert.That(tags[11].GetProperty("value").GetProperty("directoryName").GetString(), Is.EqualTo("Nested directory"));
            Assert.That(tags[15].GetProperty("value").GetProperty("denominators")[1].GetInt64(), Is.EqualTo(4));
        }
    }

    [Test]
    public void Formatters_RoundTripEscapedContentAndEmitUniqueJsonFrameFields()
    {
        string special = "quote \" slash \\ line\n tab\t <xml> & smile \u263a";
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format.Name = special;
        document.Summary.Title = special;
        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.SourceColorType = special;
        frame.IsInterlaced = true;
        document.Frames.Add(frame);
        MetadataDirectory directory = factory.CreateDirectory(MetadataFamily.Xmp, "dc", special);
        directory.Tags.Add(factory.CreateTag("Xmp.dc.title", special, MetadataFamily.Xmp, factory.CreateStringValue(special)));
        document.Directories.Add(directory);
        MetadataDiagnostic diagnostic = factory.CreateDiagnostic(MetadataSeverity.Warning, special, special);
        document.Diagnostics.Add(diagnostic);
        IImageMetadataFormatter formatter = new ImageMetadataFormatter();

        string jsonText = formatter.ToJson(document);
        string text = formatter.ToText(document);

        Assert.That(jsonText, Does.Contain("\\n"));
        Assert.That(jsonText, Does.Contain("\\u0022"));
        Assert.That(jsonText, Does.Contain("\\\\"));
        Assert.That(jsonText, Does.Not.Contain("line\n tab"));
        Assert.That(text, Does.Contain(special));
        using (JsonDocument json = JsonDocument.Parse(jsonText))
        {
            Assert.That(json.RootElement.GetProperty("format").GetProperty("name").GetString(), Is.EqualTo(special));
            Assert.That(json.RootElement.GetProperty("summary").GetProperty("title").GetString(), Is.EqualTo(special));
            Assert.That(json.RootElement.GetProperty("directories")[0].GetProperty("tags")[0].GetProperty("value").GetProperty("text").GetString(), Is.EqualTo(special));
            JsonElement frameJson = json.RootElement.GetProperty("frames")[0];
            string[] names = frameJson.EnumerateObject().Select(property => property.Name).ToArray();
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Length));
            Assert.That(frameJson.GetProperty("isInterlaced").GetBoolean(), Is.True);
        }
    }

    [Test]
    public void Diff_ComparesRepeatedRawBlocksByIdentityAndContentWhileIgnoringOffsets()
    {
        ImageMetadataDocument offsetLeft = new ImageMetadataDocument();
        ImageMetadataDocument offsetRight = new ImageMetadataDocument();
        offsetLeft.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP1", "EXIF", 1, 2, new byte[] { 1, 2 }));
        offsetRight.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP1", "EXIF", 99, 2, new byte[] { 1, 2 }));

        ImageMetadataDiffResult offsetResult = new ImageMetadataDiff().Compare(offsetLeft, offsetRight);

        Assert.That(offsetResult.AreEqual, Is.True);
        Assert.That(offsetResult.Differences, Is.Empty);

        ImageMetadataDocument left = new ImageMetadataDocument();
        left.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP1", "First", 0, 2, new byte[] { 1, 2 }));
        left.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP1", "Second", 2, 2, new byte[] { 3, 4 }));
        left.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP2", "ICC", 4, 1, new byte[] { 5 }));
        ImageMetadataDocument right = new ImageMetadataDocument();
        right.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP1", "First", 20, 2, new byte[] { 1, 2 }));
        right.RawBlocks.Add(RawBlock(MetadataFamily.Jpeg, "APP1", "Changed", 22, 3, new byte[] { 3, 9, 4 }));
        right.RawBlocks.Add(RawBlock(MetadataFamily.Png, "iTXt", "XMP", 25, 1, new byte[] { 6 }));

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.AreEqual, Is.False);
        Assert.That(result.Differences, Has.None.Contains("Jpeg.APP1[0]"));
        Assert.That(result.Differences, Has.Some.StartsWith("Raw block Jpeg.APP1[1].Name:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Raw block Jpeg.APP1[1].Length:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Raw block Jpeg.APP1[1].Data:").And.Contains("sha256"));
        Assert.That(result.Differences, Has.Some.EqualTo("Raw block removed: Jpeg.APP2[0]"));
        Assert.That(result.Differences, Has.Some.EqualTo("Raw block added: Png.iTXt[0]"));
    }

    [Test]
    public void Diff_ReportsEveryMutableProfileFieldAndIgnoresProfileOffsets()
    {
        ImageMetadataDocument left = new ImageMetadataDocument();
        MetadataProfile leftProfile = Profile("profile", 10);
        left.Profiles.Add(leftProfile);
        ImageMetadataDocument right = new ImageMetadataDocument();
        MetadataProfile rightProfile = Profile("profile", 900);
        right.Profiles.Add(rightProfile);

        Assert.That(new ImageMetadataDiff().Compare(left, right).AreEqual, Is.True);

        rightProfile.Role = ImageMetadataRole.Preview;
        rightProfile.ContainerItemId = 2;
        rightProfile.SourceFrameIndex = 3;
        rightProfile.Family = MetadataFamily.Exif;
        rightProfile.Name = "Changed";
        rightProfile.MimeType = "application/rdf+xml";
        rightProfile.Data = new byte[] { 9, 8 };
        rightProfile.Text = "changed";
        rightProfile.Length = 3;
        rightProfile.SequenceNumber = 2;
        rightProfile.SequenceCount = 3;

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].Role:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].ContainerItemId:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].SourceFrameIndex:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].Family:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].Name:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].MimeType:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].Data:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].Text:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].Length:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].SequenceNumber:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Xmp.profile[0].SequenceCount:"));
    }

    public static MetadataRawBlock RawBlock(MetadataFamily family, string type, string name, long offset, long length, byte[] data)
    {
        MetadataRawBlock block = new MetadataRawBlock();
        block.Family = family;
        block.BlockType = type;
        block.Name = name;
        block.Offset = offset;
        block.Length = length;
        block.Data = data;
        return block;
    }

    public static MetadataProfile Profile(string key, long offset)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = key;
        profile.Name = "Profile";
        profile.MimeType = "text/xml";
        profile.Data = new byte[] { 1, 2 };
        profile.Text = "text";
        profile.Offset = offset;
        profile.Length = 2;
        profile.SequenceNumber = 1;
        profile.SequenceCount = 1;
        return profile;
    }
}
