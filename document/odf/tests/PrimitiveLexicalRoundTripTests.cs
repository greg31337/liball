using System.Text;

namespace odf.Tests;

public class PrimitiveLexicalRoundTripTests
{
    [Test]
    public void QualifiedNamesRoundTripThroughThePublicXmlModel()
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' office:version='1.4'><office:body><office:text>"
            + "<text:p text:style-name='Body'>Value</text:p>"
            + "</office:text></office:body></office:document-content>";
        IOdfXmlReader reader = new OdfXmlReader();
        IOdfXmlWriter writer = new OdfXmlWriter();

        OdfPart first = reader.ReadXmlBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.Content,
            new OdfLoadOptions());
        byte[] serialized = writer.WriteXmlBytes(
            first,
            new OdfSaveOptions());
        OdfPart second = reader.ReadXmlBytes(
            serialized,
            OdfPartNames.Content,
            new OdfLoadOptions());
        OdfNode paragraph = FindElement(
            second.Root!,
            OdfNamespaces.Text,
            "p");
        OdfAttribute styleName = FindAttribute(
            paragraph,
            OdfNamespaces.Text,
            "style-name");
        OdfQualifiedName invalid = new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = "1bad",
            Prefix = OdfNamespaces.TextPrefix
        };

        Assert.Multiple(() =>
        {
            Assert.That(second.Root!.Name!.NamespaceName,
                Is.EqualTo(OdfNamespaces.Office));
            Assert.That(second.Root.Name.LocalName,
                Is.EqualTo("document-content"));
            Assert.That(second.Root.Name.Prefix,
                Is.EqualTo(OdfNamespaces.OfficePrefix));
            Assert.That(paragraph.Name!.NamespaceName,
                Is.EqualTo(OdfNamespaces.Text));
            Assert.That(paragraph.Name.LocalName, Is.EqualTo("p"));
            Assert.That(paragraph.Name.Prefix,
                Is.EqualTo(OdfNamespaces.TextPrefix));
            Assert.That(styleName.Name.NamespaceName,
                Is.EqualTo(OdfNamespaces.Text));
            Assert.That(styleName.Name.LocalName,
                Is.EqualTo("style-name"));
            Assert.That(styleName.Value, Is.EqualTo("Body"));
            Assert.That(OdfNameRules.IsQualifiedNameValue(invalid), Is.False);
            Assert.That(
                () => reader.ReadXmlBytes(
                    Encoding.UTF8.GetBytes(
                        "<text:1bad xmlns:text='urn:test'/>"),
                    OdfPartNames.Content,
                    new OdfLoadOptions()),
                Throws.Exception);
        });
    }

    [Test]
    public void ScalarValuesRoundTripCanonicallyAndPreserveInvalidLexicals()
    {
        AssertScalarRoundTrip(
            OdfLexicalParser.ParseBoolean,
            OdfLexicalFormatter.FormatBoolean,
            "1",
            "TRUE");
        AssertScalarRoundTrip(
            OdfLexicalParser.ParseInteger,
            OdfLexicalFormatter.FormatInteger,
            "+0042",
            "42.0");
        AssertScalarRoundTrip(
            OdfLexicalParser.ParseDecimal,
            OdfLexicalFormatter.FormatDecimal,
            "+001.2300",
            "1e2");
        AssertScalarRoundTrip(
            OdfLexicalParser.ParseFloat,
            OdfLexicalFormatter.FormatFloat,
            "-2.5e-1",
            "inf");
        AssertScalarRoundTrip(
            OdfLexicalParser.ParsePercentage,
            OdfLexicalFormatter.FormatPercentage,
            "+001.2500%",
            "1.25");
        AssertScalarRoundTrip(
            OdfLexicalParser.ParseBase64,
            OdfLexicalFormatter.FormatBase64,
            " T W F u ",
            "A===");
        AssertScalarRoundTrip(
            OdfLexicalParser.ParseColor,
            OdfLexicalFormatter.FormatColor,
            "#abcdef",
            "transparent");
    }

    [Test]
    public void VersionsRoundTripKnownAndFutureValuesAndPreserveInvalidInput()
    {
        string[] valid = ["1.0", "1.1", "1.2", "1.3", "1.4", "2.17"];
        string[] invalid = ["", "1", "01.4", "1.04", "1.4 ", "v1.4"];

        Assert.Multiple(() =>
        {
            foreach (string lexical in valid)
            {
                OdfVersionInfo parsed = OdfVersionRules.ParseVersion(lexical);
                string formatted = OdfVersionRules.FormatVersion(parsed);
                OdfVersionInfo reparsed = OdfVersionRules.ParseVersion(formatted);
                Assert.That(parsed.IsValid, Is.True, lexical);
                Assert.That(reparsed.IsValid, Is.True, lexical);
                Assert.That(reparsed.Major, Is.EqualTo(parsed.Major), lexical);
                Assert.That(reparsed.Minor, Is.EqualTo(parsed.Minor), lexical);
            }
            foreach (string lexical in invalid)
            {
                OdfVersionInfo parsed = OdfVersionRules.ParseVersion(lexical);
                Assert.That(parsed.IsValid, Is.False, lexical);
                Assert.That(
                    OdfVersionRules.FormatVersion(parsed),
                    Is.EqualTo(lexical),
                    lexical);
            }
        });
    }

    [Test]
    public void EveryUnitFormRoundTripsAndRejectsItsInvalidDomain()
    {
        AssertUnitRoundTrip(
            OdfLexicalParser.ParseLength,
            OdfLexicalFormatter.FormatMeasurement,
            "2.540cm",
            "1em");
        AssertUnitRoundTrip(
            OdfLexicalParser.ParseNonNegativeLength,
            OdfLexicalFormatter.FormatMeasurement,
            "0pt",
            "-1cm");
        AssertUnitRoundTrip(
            OdfLexicalParser.ParsePositiveLength,
            OdfLexicalFormatter.FormatMeasurement,
            "1in",
            "0cm");
        AssertUnitRoundTrip(
            OdfLexicalParser.ParsePixelLength,
            OdfLexicalFormatter.FormatMeasurement,
            "12px",
            "1pt");
        AssertUnitRoundTrip(
            OdfLexicalParser.ParseMeasurement,
            OdfLexicalFormatter.FormatMeasurement,
            "1.50em",
            "1qq");
        AssertUnitRoundTrip(
            OdfLexicalParser.ParseCoordinate,
            OdfLexicalFormatter.FormatMeasurement,
            "-2mm",
            "1%");
        AssertUnitRoundTrip(
            OdfLexicalParser.ParseAngle,
            OdfLexicalFormatter.FormatAngle,
            "1rad",
            "1turn");
    }

    [Test]
    public void DatesTimesAndDurationsRoundTripAndPreserveInvalidInput()
    {
        AssertTemporalRoundTrip(
            OdfLexicalParser.ParseDate,
            OdfLexicalFormatter.FormatDate,
            "2000-02-29+05:30",
            "2001-02-29");
        AssertTemporalRoundTrip(
            OdfLexicalParser.ParseTime,
            OdfLexicalFormatter.FormatTime,
            "23:59:59.1200Z",
            "24:00:01");
        AssertTemporalRoundTrip(
            OdfLexicalParser.ParseDateTime,
            OdfLexicalFormatter.FormatDateTime,
            "2000-02-29T23:30:00-01:00",
            "2000-01-01 12:00:00");

        OdfDurationValue duration = OdfLexicalParser.ParseDuration(
            "-P1Y2M3DT4H5M6.700S");
        string formatted = OdfLexicalFormatter.FormatDuration(duration);
        OdfDurationValue reparsed = OdfLexicalParser.ParseDuration(formatted);
        OdfDurationValue invalid = OdfLexicalParser.ParseDuration("P1YT");

        Assert.Multiple(() =>
        {
            Assert.That(duration.IsValid, Is.True);
            Assert.That(reparsed.IsValid, Is.True);
            Assert.That(
                OdfLexicalFormatter.FormatDuration(reparsed),
                Is.EqualTo(formatted));
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(
                OdfLexicalFormatter.FormatDuration(invalid),
                Is.EqualTo("P1YT"));
        });
    }

    [Test]
    public void IriAndCurieFormsValidateResolveAndRoundTrip()
    {
        OdfCurieValue curie = OdfIriResolver.ParseCurie("text:p");
        OdfCurieValue safe = OdfIriResolver.ParseSafeCurie(
            "[ex:resource/path]");
        string formattedCurie = OdfIriResolver.FormatCurie(curie);
        string formattedSafe = OdfIriResolver.FormatCurie(safe);

        Assert.Multiple(() =>
        {
            Assert.That(
                OdfIriResolver.IsAbsoluteIri("https://例え.テスト/文書"),
                Is.True);
            Assert.That(
                OdfIriResolver.IsIriReference("../Pictures/image%20one.png"),
                Is.True);
            Assert.That(
                OdfIriResolver.IsPackageRelativeReference(
                    "../Pictures/image.png"),
                Is.True);
            Assert.That(
                OdfIriResolver.ResolvePackageReference(
                    "Object%201/content.xml",
                    "../Pictures/image.png#view"),
                Is.EqualTo("Pictures/image.png#view"));
            Assert.That(OdfIriResolver.ParseCurie(formattedCurie).IsValid,
                Is.True);
            Assert.That(formattedCurie, Is.EqualTo("text:p"));
            Assert.That(OdfIriResolver.ParseSafeCurie(formattedSafe).IsValid,
                Is.True);
            Assert.That(formattedSafe, Is.EqualTo("[ex:resource/path]"));
            Assert.That(OdfIriResolver.IsAbsoluteIri("relative/文書"),
                Is.False);
            Assert.That(OdfIriResolver.IsIriReference("bad percent%2"),
                Is.False);
            Assert.That(OdfIriResolver.ParseCurie("bad prefix:value").IsValid,
                Is.False);
            Assert.That(OdfIriResolver.ParseSafeCurie("text:p").IsValid,
                Is.False);
        });
    }

    [Test]
    public void EveryCellAndRangeAddressFormRoundTripsAndPreservesInvalidInput()
    {
        OdfQuotedSheetValue sheet = OdfCellAddressParser
            .ParseQuotedSheetName("$'Bob''s Sheet'");
        string sheetText = OdfCellAddressFormatter.FormatQuotedSheetName(sheet);
        OdfCellAddressValue cell = OdfCellAddressParser.ParseCellAddress(
            "$'Bob''s Sheet'.$AA$10");
        string cellText = OdfCellAddressFormatter.FormatCellAddress(cell);
        OdfCellRangeValue range = OdfCellAddressParser.ParseCellRange(
            "Sheet.A1:Sheet.B2");
        string rangeText = OdfCellAddressFormatter.FormatCellRange(range);
        OdfCellRangeValue formula = OdfCellAddressParser
            .ParseFormulaReference("[.$A$1:.$B$2]");
        string formulaText = OdfCellAddressFormatter
            .FormatFormulaReference(formula);
        OdfCellRangeValue external = OdfCellAddressParser
            .ParseExternalReference("['../data.ods'#Sheet.A1]");
        string externalText = OdfCellAddressFormatter
            .FormatFormulaReference(external);
        OdfCellRangeListValue list = OdfCellAddressParser.ParseRangeList(
            "Sheet.A1:Sheet.B2 'My Sheet'.C3:'My Sheet'.D4");
        string listText = OdfCellAddressFormatter.FormatRangeList(list);

        Assert.Multiple(() =>
        {
            Assert.That(
                OdfCellAddressParser.ParseQuotedSheetName(sheetText).IsValid,
                Is.True);
            Assert.That(
                OdfCellAddressParser.ParseCellAddress(cellText).IsValid,
                Is.True);
            Assert.That(
                OdfCellAddressParser.ParseCellRange(rangeText).IsValid,
                Is.True);
            Assert.That(
                OdfCellAddressParser.ParseFormulaReference(formulaText).IsValid,
                Is.True);
            Assert.That(
                OdfCellAddressParser.ParseExternalReference(externalText).IsValid,
                Is.True);
            Assert.That(
                OdfCellAddressParser.ParseRangeList(listText).IsValid,
                Is.True);
            AssertInvalidAddressLexicals();
        });
    }

    static void AssertScalarRoundTrip(
        Func<string?, OdfLexicalValue> parser,
        Func<OdfLexicalValue?, string> formatter,
        string validLexical,
        string invalidLexical)
    {
        OdfLexicalValue parsed = parser(validLexical);
        string formatted = formatter(parsed);
        OdfLexicalValue reparsed = parser(formatted);
        OdfLexicalValue invalid = parser(invalidLexical);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.IsValid, Is.True, validLexical);
            Assert.That(reparsed.IsValid, Is.True, validLexical);
            Assert.That(formatter(reparsed), Is.EqualTo(formatted), validLexical);
            Assert.That(invalid.IsValid, Is.False, invalidLexical);
            Assert.That(
                formatter(invalid),
                Is.EqualTo(invalidLexical),
                invalidLexical);
        });
    }

    static void AssertUnitRoundTrip(
        Func<string?, OdfUnitValue> parser,
        Func<OdfUnitValue?, string> formatter,
        string validLexical,
        string invalidLexical)
    {
        OdfUnitValue parsed = parser(validLexical);
        string formatted = formatter(parsed);
        OdfUnitValue reparsed = parser(formatted);
        OdfUnitValue invalid = parser(invalidLexical);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.IsValid, Is.True, validLexical);
            Assert.That(reparsed.IsValid, Is.True, validLexical);
            Assert.That(formatter(reparsed), Is.EqualTo(formatted), validLexical);
            Assert.That(invalid.IsValid, Is.False, invalidLexical);
            Assert.That(
                formatter(invalid),
                Is.EqualTo(invalidLexical),
                invalidLexical);
        });
    }

    static void AssertTemporalRoundTrip(
        Func<string?, OdfTemporalValue> parser,
        Func<OdfTemporalValue?, string> formatter,
        string validLexical,
        string invalidLexical)
    {
        OdfTemporalValue parsed = parser(validLexical);
        string formatted = formatter(parsed);
        OdfTemporalValue reparsed = parser(formatted);
        OdfTemporalValue invalid = parser(invalidLexical);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.IsValid, Is.True, validLexical);
            Assert.That(reparsed.IsValid, Is.True, validLexical);
            Assert.That(formatter(reparsed), Is.EqualTo(formatted), validLexical);
            Assert.That(invalid.IsValid, Is.False, invalidLexical);
            Assert.That(
                formatter(invalid),
                Is.EqualTo(invalidLexical),
                invalidLexical);
        });
    }

    static void AssertInvalidAddressLexicals()
    {
        OdfQuotedSheetValue sheet = OdfCellAddressParser
            .ParseQuotedSheetName("'open");
        OdfCellAddressValue cell = OdfCellAddressParser.ParseCellAddress("A1");
        OdfCellRangeValue range = OdfCellAddressParser.ParseCellRange(
            ".A1:.B");
        OdfCellRangeValue formula = OdfCellAddressParser
            .ParseFormulaReference(".A1");
        OdfCellRangeValue external = OdfCellAddressParser
            .ParseExternalReference("[.A1]");
        OdfCellRangeListValue list = OdfCellAddressParser.ParseRangeList(
            ".A1 invalid");

        Assert.That(sheet.IsValid, Is.False);
        Assert.That(
            OdfCellAddressFormatter.FormatQuotedSheetName(sheet),
            Is.EqualTo("'open"));
        Assert.That(cell.IsValid, Is.False);
        Assert.That(OdfCellAddressFormatter.FormatCellAddress(cell),
            Is.EqualTo("A1"));
        Assert.That(range.IsValid, Is.False);
        Assert.That(OdfCellAddressFormatter.FormatCellRange(range),
            Is.EqualTo(".A1:.B"));
        Assert.That(formula.IsValid, Is.False);
        Assert.That(OdfCellAddressFormatter.FormatFormulaReference(formula),
            Is.EqualTo(".A1"));
        Assert.That(external.IsValid, Is.False);
        Assert.That(OdfCellAddressFormatter.FormatFormulaReference(external),
            Is.EqualTo("[.A1]"));
        Assert.That(list.IsValid, Is.False);
        Assert.That(OdfCellAddressFormatter.FormatRangeList(list),
            Is.EqualTo(".A1 invalid"));
    }

    static OdfNode FindElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        foreach (OdfNode child in node.Children)
        {
            OdfNode? result = TryFindElement(child, namespaceName, localName);
            if (result != null)
            {
                return result;
            }
        }
        throw new AssertionException("The expected element was not found.");
    }

    static OdfNode? TryFindElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        foreach (OdfNode child in node.Children)
        {
            OdfNode? result = TryFindElement(child, namespaceName, localName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    static OdfAttribute FindAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
        }
        throw new AssertionException("The expected attribute was not found.");
    }
}
