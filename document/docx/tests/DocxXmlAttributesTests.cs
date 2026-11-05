using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxXmlAttributesTests
{
    [Test]
    public void AttributeHelpers_ReadStringIntBoolColorAndMeasurementValues()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateAttributeXml());

        Assert.That(reader.ReadToElement("pPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(global::docx.DocxXmlAttributes.ReadString(reader, "style", global::docx.DocxConstants.NamespaceWordprocessingMl, ""), Is.EqualTo("Heading1"));
        Assert.That(global::docx.DocxXmlAttributes.ReadString(reader, "missing", global::docx.DocxConstants.NamespaceWordprocessingMl, "Default"), Is.EqualTo("Default"));
        Assert.That(global::docx.DocxXmlAttributes.ReadInt(reader, "count", global::docx.DocxConstants.NamespaceWordprocessingMl, 0), Is.EqualTo(42));
        Assert.That(global::docx.DocxXmlAttributes.ReadInt(reader, "missingInt", global::docx.DocxConstants.NamespaceWordprocessingMl, 7), Is.EqualTo(7));
        Assert.That(global::docx.DocxXmlAttributes.ReadBool(reader, "enabled", global::docx.DocxConstants.NamespaceWordprocessingMl, false), Is.True);
        Assert.That(global::docx.DocxXmlAttributes.ReadBool(reader, "disabled", global::docx.DocxConstants.NamespaceWordprocessingMl, true), Is.False);
        Assert.That(global::docx.DocxXmlAttributes.ReadColor(reader, "color", global::docx.DocxConstants.NamespaceWordprocessingMl, ""), Is.EqualTo("FF00AA"));
        Assert.That(global::docx.DocxXmlAttributes.ReadColor(reader, "autoColor", global::docx.DocxConstants.NamespaceWordprocessingMl, ""), Is.EqualTo("auto"));
        Assert.That(global::docx.DocxXmlAttributes.ReadMeasurement(reader, "indent", global::docx.DocxConstants.NamespaceWordprocessingMl, 0), Is.EqualTo(-120));
    }

    [Test]
    public void AttributeHelpers_WriteValuesAsXmlAttributes()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElement("w", "pPr", global::docx.DocxConstants.NamespaceWordprocessingMl);
        global::docx.DocxXmlAttributes.WriteString(writer, "w", "style", global::docx.DocxConstants.NamespaceWordprocessingMl, "Heading1");
        global::docx.DocxXmlAttributes.WriteInt(writer, "w", "count", global::docx.DocxConstants.NamespaceWordprocessingMl, 42);
        global::docx.DocxXmlAttributes.WriteBool(writer, "w", "enabled", global::docx.DocxConstants.NamespaceWordprocessingMl, true);
        global::docx.DocxXmlAttributes.WriteColor(writer, "w", "color", global::docx.DocxConstants.NamespaceWordprocessingMl, "#00aaff");
        global::docx.DocxXmlAttributes.WriteMeasurement(writer, "w", "indent", global::docx.DocxConstants.NamespaceWordprocessingMl, -120);
        writer.EndElement();

        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(writer.GetXml());

        Assert.That(reader.ReadToElement("pPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.GetAttribute("style", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("Heading1"));
        Assert.That(reader.GetAttribute("count", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("42"));
        Assert.That(reader.GetAttribute("enabled", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("1"));
        Assert.That(reader.GetAttribute("color", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("00AAFF"));
        Assert.That(reader.GetAttribute("indent", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("-120"));
    }

    [Test]
    public void BoolHelpers_AcceptCommonWordprocessingBooleanTokens()
    {
        Assert.That(global::docx.DocxXmlAttributes.ParseBool("true"), Is.True);
        Assert.That(global::docx.DocxXmlAttributes.ParseBool("1"), Is.True);
        Assert.That(global::docx.DocxXmlAttributes.ParseBool("on"), Is.True);
        Assert.That(global::docx.DocxXmlAttributes.ParseBool("false"), Is.False);
        Assert.That(global::docx.DocxXmlAttributes.ParseBool("0"), Is.False);
        Assert.That(global::docx.DocxXmlAttributes.ParseBool("off"), Is.False);
    }

    [Test]
    public void ColorHelpers_NormalizeRgbAndAutoValues()
    {
        Assert.That(global::docx.DocxXmlAttributes.NormalizeColor("#abcdef"), Is.EqualTo("ABCDEF"));
        Assert.That(global::docx.DocxXmlAttributes.NormalizeColor("123456"), Is.EqualTo("123456"));
        Assert.That(global::docx.DocxXmlAttributes.NormalizeColor("AUTO"), Is.EqualTo("auto"));
    }

    [Test]
    public void AttributeHelpers_RejectInvalidValues()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateInvalidAttributeXml());

        Assert.That(reader.ReadToElement("pPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.Throws<FormatException>(() => global::docx.DocxXmlAttributes.ReadInt(reader, "count", global::docx.DocxConstants.NamespaceWordprocessingMl, 0));
        Assert.Throws<FormatException>(() => global::docx.DocxXmlAttributes.ReadBool(reader, "enabled", global::docx.DocxConstants.NamespaceWordprocessingMl, false));
        Assert.Throws<ArgumentException>(() => global::docx.DocxXmlAttributes.ReadColor(reader, "color", global::docx.DocxConstants.NamespaceWordprocessingMl, ""));
        Assert.Throws<FormatException>(() => global::docx.DocxXmlAttributes.ParseBool("maybe"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxXmlAttributes.NormalizeColor("12345"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxXmlAttributes.NormalizeColor("12xx56"));
    }

    [Test]
    public void AttributeHelpers_RejectNullArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateAttributeXml());
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.ReadString(null!, "style", "", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.ReadString(reader, "style", "", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.ParseBool(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.NormalizeColor(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.WriteString(null!, "", "style", "", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.WriteString(writer, "", "style", "", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlAttributes.WriteColor(writer, "", "color", "", null!));
    }

    static string CreateAttributeXml()
    {
        return string.Join("\n", new string[]
        {
            "<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"",
            "       w:style=\"Heading1\" w:count=\"42\" w:enabled=\"on\" w:disabled=\"0\"",
            "       w:color=\"#ff00aa\" w:autoColor=\"auto\" w:indent=\"-120\" />"
        });
    }

    static string CreateInvalidAttributeXml()
    {
        return string.Join("\n", new string[]
        {
            "<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"",
            "       w:count=\"abc\" w:enabled=\"maybe\" w:color=\"xyz\" />"
        });
    }
}
