using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CustomPropertiesTests
{
    [Test]
    public void ReadString_ParsesCommonVariantTypesAndPreservesUnknowns()
    {
        string xml = "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"2\" name=\"Text\"><vt:lpwstr>Hello</vt:lpwstr></property>" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"3\" name=\"Count\"><vt:i4>42</vt:i4></property>" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"4\" name=\"Ratio\"><vt:r8>1.5</vt:r8></property>" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"5\" name=\"Flag\"><vt:bool>true</vt:bool></property>" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"6\" name=\"When\"><vt:filetime>2026-08-09T12:34:56Z</vt:filetime></property>" +
            "<property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"7\" name=\"Raw\" x:item=\"i\"><vt:decimal>12.30</vt:decimal><x:extra /></property>" +
            "<x:future><x:child /></x:future>" +
            "</Properties>";
        XlsxCustomPropertiesReader reader = new XlsxCustomPropertiesReader();

        XlsxCustomPropertiesPart part = reader.ReadString(xml);
        IXlsxCustomPropertiesPartOperations operations = new XlsxCustomPropertiesPartOperations();

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Properties.Count, Is.EqualTo(6));
        Assert.That(operations.GetProperty(part, "Text")!.TextValue, Is.EqualTo("Hello"));
        Assert.That(part.GetProperty("Text")!.ValueType, Is.EqualTo("lpwstr"));
        Assert.That(part.GetProperty("Count")!.IntValue, Is.EqualTo(42));
        Assert.That(part.GetProperty("Ratio")!.DoubleValue, Is.EqualTo(1.5d).Within(0.000001d));
        Assert.That(part.GetProperty("Flag")!.BoolValue, Is.True);
        Assert.That(part.GetProperty("When")!.DateTimeValue.Kind, Is.EqualTo(System.DateTimeKind.Utc));
        Assert.That(part.GetProperty("Raw")!.HasRawValueXml, Is.True);
        Assert.That(part.GetProperty("Raw")!.RawValueXml, Does.Contain("vt:decimal"));
        Assert.That(part.GetProperty("Raw")!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.GetProperty("Raw")!.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesCustomPropertiesAndCanBeReadBack()
    {
        XlsxCustomPropertiesPart part = new XlsxCustomPropertiesPart();
        part.UnknownRootAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        part.Properties.Add(CreateTextProperty(2, "Text", "Hello"));
        part.Properties.Add(CreateIntProperty(3, "Count", 42));
        part.Properties.Add(CreateDoubleProperty(4, "Ratio", 1.5d));
        part.Properties.Add(CreateBoolProperty(5, "Flag", true));
        part.Properties.Add(CreateDateProperty(6, "When", new System.DateTime(2026, 8, 9, 12, 34, 56, System.DateTimeKind.Utc)));
        XlsxCustomProperty raw = new XlsxCustomProperty();
        raw.PropertyId = 7;
        raw.Name = "Raw";
        raw.RawValueXml = "<vt:decimal xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\">12.30</vt:decimal>";
        raw.HasRawValueXml = true;
        raw.UnknownAttributes.Add(CreateAttribute("urn:x", "x", "item", "i"));
        part.Properties.Add(raw);
        part.UnknownXml.Add("<x:future xmlns:x=\"urn:x\"><x:child /></x:future>");

        XlsxCustomPropertiesWriteResult result = new XlsxCustomPropertiesWriter().WriteString(part);
        XlsxCustomPropertiesPart readPart = new XlsxCustomPropertiesReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<Properties"));
        Assert.That(result.Xml, Does.Contain("xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\""));
        Assert.That(result.Xml, Does.Contain("name=\"Text\""));
        Assert.That(result.Xml, Does.Contain("<vt:lpwstr>Hello</vt:lpwstr>"));
        Assert.That(result.Xml, Does.Contain("<vt:i4>42</vt:i4>"));
        Assert.That(result.Xml, Does.Contain("<vt:r8>1.5</vt:r8>"));
        Assert.That(result.Xml, Does.Contain("<vt:bool>1</vt:bool>"));
        Assert.That(result.Xml, Does.Contain("2026-08-09T12:34:56Z"));
        Assert.That(result.Xml, Does.Contain("vt:decimal"));
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.Properties.Count, Is.EqualTo(6));
        Assert.That(readPart.GetProperty("Text")!.TextValue, Is.EqualTo("Hello"));
        Assert.That(readPart.GetProperty("Count")!.IntValue, Is.EqualTo(42));
        Assert.That(readPart.GetProperty("Flag")!.BoolValue, Is.True);
        Assert.That(readPart.GetProperty("When")!.HasDateTimeValue, Is.True);
        Assert.That(readPart.GetProperty("Raw")!.HasRawValueXml, Is.True);
        Assert.That(readPart.GetProperty("Raw")!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(readPart.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(readPart.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidValues()
    {
        XlsxCustomPropertiesReader reader = new XlsxCustomPropertiesReader();

        XlsxCustomPropertiesPart missingRoot = reader.ReadString("");
        XlsxCustomPropertiesPart invalidRoot = reader.ReadString("<Bad />");
        XlsxCustomPropertiesPart invalidValues = reader.ReadString("<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><property pid=\"bad\" name=\"BadPid\"><vt:lpwstr>x</vt:lpwstr></property><property pid=\"2\" name=\"BadInt\"><vt:i4>bad</vt:i4></property><property pid=\"3\" name=\"BadBool\"><vt:bool>maybe</vt:bool></property><property pid=\"4\" name=\"BadDate\"><vt:filetime>bad</vt:filetime></property></Properties>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "customProperties.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "customProperties.invalidRoot"), Is.True);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidValues, "xml.integer.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.dateTime.invalid"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsInvalidCustomPropertiesBeforeWriting()
    {
        XlsxCustomPropertiesPart part = new XlsxCustomPropertiesPart();
        part.Properties.Add(new XlsxCustomProperty());
        part.Properties.Add(CreateTextProperty(2, "Dup", "a"));
        part.Properties.Add(CreateTextProperty(2, "dup", "b"));
        XlsxCustomProperty missingValue = new XlsxCustomProperty();
        missingValue.PropertyId = 3;
        missingValue.Name = "MissingValue";
        part.Properties.Add(missingValue);

        XlsxCustomPropertiesWriteResult result = new XlsxCustomPropertiesWriter().WriteBytes(part);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "customProperties.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "customProperties.pidMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "customProperties.valueMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "customProperties.pidDuplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "customProperties.nameDuplicate"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsNullPart()
    {
        XlsxCustomPropertiesWriteResult? result = new XlsxCustomPropertiesWriter().WriteBytes(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "customProperties.partNull"), Is.True);
    }

    static XlsxCustomProperty CreateTextProperty(int pid, string name, string value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "lpwstr";
        property.TextValue = value;
        property.HasTextValue = true;
        return property;
    }

    static XlsxCustomProperty CreateIntProperty(int pid, string name, int value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "i4";
        property.IntValue = value;
        property.HasIntValue = true;
        return property;
    }

    static XlsxCustomProperty CreateDoubleProperty(int pid, string name, double value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "r8";
        property.DoubleValue = value;
        property.HasDoubleValue = true;
        return property;
    }

    static XlsxCustomProperty CreateBoolProperty(int pid, string name, bool value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "bool";
        property.BoolValue = value;
        property.HasBoolValue = true;
        return property;
    }

    static XlsxCustomProperty CreateDateProperty(int pid, string name, System.DateTime value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "filetime";
        property.DateTimeValue = value;
        property.HasDateTimeValue = true;
        return property;
    }

    static XlsxXmlAttribute CreateAttribute(string namespaceUri, string prefix, string localName, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = namespaceUri;
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxCustomPropertiesPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxCustomPropertiesWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
