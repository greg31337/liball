using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxMarkupCompatibilityTests
{
    [Test]
    public void ReadIgnorablePrefixes_ParsesWhitespaceSeparatedDistinctPrefixes()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateIgnorableXml());

        Assert.That(reader.ReadToElement("document", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        List<string> prefixes = global::docx.DocxMarkupCompatibility.ReadIgnorablePrefixes(reader);

        Assert.That(prefixes, Is.EqualTo(new string[] { "w14", "wp14" }));
    }

    [Test]
    public void WriteIgnorablePrefixes_WritesMarkupCompatibilityAttribute()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();
        List<string> prefixes = new List<string>();

        prefixes.Add("w14");
        prefixes.Add("wp14");

        writer.StartElement("w", "document", global::docx.DocxConstants.NamespaceWordprocessingMl);
        global::docx.DocxMarkupCompatibility.WriteIgnorablePrefixes(writer, prefixes);
        writer.EndElement();

        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(writer.GetXml());

        Assert.That(reader.ReadToElement("document", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.GetAttribute("Ignorable", global::docx.DocxConstants.NamespaceMarkupCompatibility), Is.EqualTo("w14 wp14"));
    }

    [Test]
    public void CaptureAlternateContent_PreservesRawMarkupAndAdvancesReader()
    {
        string alternateContent = CreateAlternateContentXml();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root>" + alternateContent + "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" /></root>");

        Assert.That(reader.ReadToElement("AlternateContent", global::docx.DocxConstants.NamespaceMarkupCompatibility), Is.True);
        Assert.That(global::docx.DocxMarkupCompatibility.IsAlternateContent(reader), Is.True);

        global::docx.DocxXmlBuffer buffer = global::docx.DocxMarkupCompatibility.CaptureAlternateContent(reader);

        Assert.That(buffer.GetXml(), Is.EqualTo(alternateContent));
        Assert.That(reader.LocalName(), Is.EqualTo("p"));
        Assert.That(reader.NamespaceUri(), Is.EqualTo(global::docx.DocxConstants.NamespaceWordprocessingMl));
    }

    [Test]
    public void AlternateContentBuffers_WriteBackInOriginalOrder()
    {
        string first = CreateAlternateContentXml();
        string second = "<mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"><mc:Fallback><x /></mc:Fallback></mc:AlternateContent>";
        global::docx.DocxMarkupCompatibility compatibility = new global::docx.DocxMarkupCompatibility();
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        compatibility.AddAlternateContent(new global::docx.DocxXmlBuffer(first));
        compatibility.AddAlternateContent(new global::docx.DocxXmlBuffer(second));

        writer.StartElementWithoutPrefix("root", "");
        compatibility.WriteAlternateContents(writer);
        writer.EndElement();

        Assert.That(compatibility.AlternateContentCount(), Is.EqualTo(2));
        Assert.That(writer.GetXml(), Is.EqualTo("<root>" + first + second + "</root>"));
    }

    [Test]
    public void IgnorablePrefixCollection_DeduplicatesAndCountsPrefixes()
    {
        global::docx.DocxMarkupCompatibility compatibility = new global::docx.DocxMarkupCompatibility();

        compatibility.AddIgnorablePrefix("w14");
        compatibility.AddIgnorablePrefix("w14");
        compatibility.AddIgnorablePrefix("wp14");

        Assert.That(compatibility.IgnorablePrefixCount(), Is.EqualTo(2));
        Assert.That(compatibility.HasIgnorablePrefix("w14"), Is.True);
        Assert.That(global::docx.DocxMarkupCompatibility.JoinPrefixes(compatibility.IgnorablePrefixes), Is.EqualTo("w14 wp14"));
    }

    [Test]
    public void MarkupCompatibility_RejectsInvalidArguments()
    {
        global::docx.DocxMarkupCompatibility compatibility = new global::docx.DocxMarkupCompatibility();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMarkupCompatibility.IsAlternateContent(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMarkupCompatibility.ReadIgnorablePrefixes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMarkupCompatibility.ParsePrefixList(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMarkupCompatibility.WriteIgnorablePrefixes(null!, new List<string>()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMarkupCompatibility.WriteIgnorablePrefixes(writer, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMarkupCompatibility.JoinPrefixes(null!));
        Assert.Throws<ArgumentNullException>(() => compatibility.AddIgnorablePrefix(null!));
        Assert.Throws<ArgumentException>(() => compatibility.AddIgnorablePrefix(""));
        Assert.Throws<ArgumentException>(() => compatibility.AddIgnorablePrefix("bad prefix"));
        Assert.Throws<ArgumentNullException>(() => compatibility.AddAlternateContent(null!));
        Assert.Throws<ArgumentNullException>(() => compatibility.WriteAlternateContents(null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxMarkupCompatibility.CaptureAlternateContent(reader));
    }

    static string CreateIgnorableXml()
    {
        return string.Join("\n", new string[]
        {
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"",
            "            xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"",
            "            mc:Ignorable=\"w14  wp14\tw14\" />"
        });
    }

    static string CreateAlternateContentXml()
    {
        return string.Join("", new string[]
        {
            "<mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">",
            "<mc:Choice Requires=\"w14\"><w:p><w:r><w:t>Choice</w:t></w:r></w:p></mc:Choice>",
            "<mc:Fallback><w:p><w:r><w:t>Fallback</w:t></w:r></w:p></mc:Fallback>",
            "</mc:AlternateContent>"
        });
    }
}
