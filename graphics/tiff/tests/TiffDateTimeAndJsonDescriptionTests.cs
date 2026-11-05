using System;
using System.Text;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDateTimeAndJsonDescriptionTests
{
    [Test]
    public void DateTimeService_ParsesAndFormatsInvariantTiffValues()
    {
        TiffDateTimeParseResult result = TiffApi.DateTime.ParseTiffDateTime("2024:02:29 23:59:58");
        string formatted = TiffApi.DateTime.FormatTiffDateTime(new DateTime(2026, 8, 26, 7, 5, 4, DateTimeKind.Utc));

        Assert.That(result.Success, Is.True);
        Assert.That(result.OriginalText, Is.EqualTo("2024:02:29 23:59:58"));
        Assert.That(result.Value, Is.EqualTo(new DateTime(2024, 2, 29, 23, 59, 58, DateTimeKind.Unspecified)));
        Assert.That(result.Value.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(formatted, Is.EqualTo("2026:08:26 07:05:04"));
    }

    [TestCase("2023:02:29 12:00:00")]
    [TestCase("2026-08-26 12:00:00")]
    [TestCase("2026:08:26 24:00:00")]
    [TestCase("2026:08:26 12:00:60")]
    [TestCase("2026:08:26")]
    public void DateTimeService_RejectsInvalidCalendarAndSyntaxValues(string text)
    {
        TiffDateTimeParseResult result = TiffApi.DateTime.ParseTiffDateTime(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.OriginalText, Is.EqualTo(text));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ReadImageDescription_RecognizesStructuredJsonAndRetainsExactSource()
    {
        string description = " \n{\"camera\":\"A\\u0020B\",\"exposure\":1.25e-2,\"flags\":[true,false,null],\"nested\":{\"x\":7}} \t";
        TiffImageDescriptionReadResult result = ReadDescription(description);
        TiffImageDescriptionMetadata metadata = result.Metadata;
        TiffJsonNode camera = FindChild(metadata.JsonRoot, "camera");
        TiffJsonNode exposure = FindChild(metadata.JsonRoot, "exposure");
        TiffJsonNode flags = FindChild(metadata.JsonRoot, "flags");
        TiffJsonNode nested = FindChild(metadata.JsonRoot, "nested");

        Assert.That(result.Success, Is.True);
        Assert.That(metadata.Text, Is.EqualTo(description));
        Assert.That(metadata.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes(description + "\0")));
        Assert.That(metadata.LooksLikeJson, Is.True);
        Assert.That(metadata.HasJson, Is.True);
        Assert.That(metadata.JsonError, Is.Empty);
        Assert.That(metadata.JsonRoot.Kind, Is.EqualTo(TiffJsonValueKind.Object));
        Assert.That(metadata.JsonRoot.RawText, Is.EqualTo(description.Trim()));
        Assert.That(camera.Kind, Is.EqualTo(TiffJsonValueKind.String));
        Assert.That(camera.Text, Is.EqualTo("A B"));
        Assert.That(camera.RawText, Is.EqualTo("\"A\\u0020B\""));
        Assert.That(exposure.Kind, Is.EqualTo(TiffJsonValueKind.Number));
        Assert.That(exposure.Text, Is.EqualTo("1.25e-2"));
        Assert.That(flags.Kind, Is.EqualTo(TiffJsonValueKind.Array));
        Assert.That(flags.Children[0].BooleanValue, Is.True);
        Assert.That(flags.Children[1].BooleanValue, Is.False);
        Assert.That(flags.Children[2].Kind, Is.EqualTo(TiffJsonValueKind.Null));
        Assert.That(FindChild(nested, "x").Text, Is.EqualTo("7"));
    }

    [Test]
    public void ReadImageDescription_DistinguishesPlainAndMalformedJsonShapedText()
    {
        TiffImageDescriptionMetadata plain = ReadDescription("plain description").Metadata;
        TiffImageDescriptionReadResult malformedResult = ReadDescription("[1, 2,]");
        TiffImageDescriptionMetadata malformed = malformedResult.Metadata;

        Assert.That(plain.LooksLikeJson, Is.False);
        Assert.That(plain.HasJson, Is.False);
        Assert.That(plain.JsonError, Is.Empty);
        Assert.That(malformedResult.Success, Is.True);
        Assert.That(malformed.Text, Is.EqualTo("[1, 2,]"));
        Assert.That(malformed.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("[1, 2,]\0")));
        Assert.That(malformed.LooksLikeJson, Is.True);
        Assert.That(malformed.HasJson, Is.False);
        Assert.That(malformed.JsonError, Does.Contain("invalid value"));
        Assert.That(malformed.JsonRoot.Kind, Is.EqualTo(TiffJsonValueKind.Array));
    }

    static TiffImageDescriptionReadResult ReadDescription(string description)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(new TiffImageDefinition());
        writer.WriteImageDescription(imageIndex, description);
        writer.Flush();
        return TiffFile.OpenMetadata(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0).ReadImageDescription();
    }

    static TiffJsonNode FindChild(TiffJsonNode node, string name)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Name == name)
            {
                return node.Children[index];
            }

            index = index + 1;
        }

        Assert.Fail("JSON child was not found: " + name);
        return new TiffJsonNode();
    }
}
