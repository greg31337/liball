using System.Text;

namespace odf.Tests;

public class DataStyleDocumentModelTests
{
    [Test]
    public void MaterializesEveryDataStyleKindAndInterpretsPatterns()
    {
        OdfDocument document = ReadFlat(DataStyleFlatXml(
            "<office:styles>"
            + "<number:number-style style:name='N' style:display-name='Number' "
            + "number:language='en' number:country='US' number:script='Latn' "
            + "number:rfc-language-tag='en-US' number:title='Numeric' "
            + "style:volatile='true' number:transliteration-format='一二三' "
            + "number:transliteration-language='ja' "
            + "number:transliteration-country='JP' "
            + "number:transliteration-style='long' ext:root='preserved'>"
            + "<style:text-properties fo:color='#112233'/><number:number "
            + "number:min-integer-digits='1' number:decimal-places='2' "
            + "number:grouping='true'><number:embedded-text "
            + "number:position='1'> kg</number:embedded-text></number:number>"
            + "<style:map style:condition='value()&lt;0' "
            + "style:apply-style-name='Negative' "
            + "style:base-cell-address='$Sheet1.$A$1' ext:map='keep'/>"
            + "</number:number-style>"
            + "<number:number-style style:name='S'><number:scientific-number "
            + "number:min-integer-digits='1' number:decimal-places='3' "
            + "number:min-exponent-digits='2' "
            + "number:forced-exponent-sign='true'/></number:number-style>"
            + "<number:number-style style:name='F'><number:fraction "
            + "number:min-integer-digits='1' number:min-numerator-digits='1' "
            + "number:denominator-value='8'/></number:number-style>"
            + "<number:percentage-style style:name='P'><number:number "
            + "number:decimal-places='1'/><number:text>%</number:text>"
            + "</number:percentage-style>"
            + "<number:currency-style style:name='C' "
            + "number:automatic-order='true'><number:currency-symbol "
            + "number:language='de' number:country='DE'>€</number:currency-symbol>"
            + "<number:number number:decimal-places='2' number:grouping='true'/>"
            + "</number:currency-style>"
            + "<number:date-style style:name='D' number:automatic-order='false' "
            + "number:format-source='fixed'><number:day number:style='long'/>"
            + "<number:text>/</number:text><number:month number:style='long'/>"
            + "<number:text>/</number:text><number:year number:style='long'/>"
            + "</number:date-style>"
            + "<number:time-style style:name='T' number:format-source='language' "
            + "number:truncate-on-overflow='false'><number:hours "
            + "number:style='long'/><number:text>:</number:text><number:minutes "
            + "number:style='long'/><number:text>:</number:text><number:seconds "
            + "number:style='long' number:decimal-places='3'/><number:text> "
            + "</number:text><number:am-pm/></number:time-style>"
            + "<number:boolean-style style:name='B'><number:boolean/>"
            + "</number:boolean-style>"
            + "<number:text-style style:name='X'><number:text-content/>"
            + "<number:text> suffix</number:text></number:text-style>"
            + "<number:number-style style:name='Negative'><number:number/>"
            + "</number:number-style>"
            + "<style:style style:name='Cell' style:family='table-cell' "
            + "style:data-style-name='N'/></office:styles>"));

        OdfDataStyle number = StyleNamed(document, "N");

        Assert.Multiple(() =>
        {
            Assert.That(document.DataStyles, Has.Length.EqualTo(10));
            Assert.That(number.Kind, Is.EqualTo(OdfDataStyleKind.Number));
            Assert.That(StyleNamed(document, "S").Kind,
                Is.EqualTo(OdfDataStyleKind.Scientific));
            Assert.That(StyleNamed(document, "F").Kind,
                Is.EqualTo(OdfDataStyleKind.Fraction));
            Assert.That(StyleNamed(document, "P").Kind,
                Is.EqualTo(OdfDataStyleKind.Percentage));
            Assert.That(StyleNamed(document, "C").Kind,
                Is.EqualTo(OdfDataStyleKind.Currency));
            Assert.That(StyleNamed(document, "D").Kind,
                Is.EqualTo(OdfDataStyleKind.Date));
            Assert.That(StyleNamed(document, "T").Kind,
                Is.EqualTo(OdfDataStyleKind.Time));
            Assert.That(StyleNamed(document, "B").Kind,
                Is.EqualTo(OdfDataStyleKind.Boolean));
            Assert.That(StyleNamed(document, "X").Kind,
                Is.EqualTo(OdfDataStyleKind.Text));
            Assert.That(number.FormatPattern, Is.EqualTo("#,## kg0.00"));
            Assert.That(StyleNamed(document, "S").FormatPattern,
                Is.EqualTo("0.000E+00"));
            Assert.That(StyleNamed(document, "F").FormatPattern,
                Is.EqualTo("0 ?/8"));
            Assert.That(StyleNamed(document, "P").FormatPattern,
                Is.EqualTo("0.0%"));
            Assert.That(StyleNamed(document, "C").FormatPattern,
                Is.EqualTo("€#,##0.00"));
            Assert.That(StyleNamed(document, "D").FormatPattern,
                Is.EqualTo("dd/MM/yyyy"));
            Assert.That(StyleNamed(document, "T").FormatPattern,
                Is.EqualTo("hh:mm:ss.000 AM/PM"));
            Assert.That(StyleNamed(document, "B").FormatPattern,
                Is.EqualTo("BOOLEAN"));
            Assert.That(StyleNamed(document, "X").FormatPattern,
                Is.EqualTo("@ suffix"));
            Assert.That(number.LanguageTag, Is.EqualTo("en-US"));
            Assert.That(number.TransliterationStyle, Is.EqualTo("long"));
            Assert.That(number.TextProperties, Has.Length.EqualTo(1));
            Assert.That(number.Components[0].Content, Has.Length.EqualTo(1));
            Assert.That(number.Maps[0].Condition, Is.EqualTo("value()<0"));
            Assert.That(number.Maps[0].BaseCellAddress,
                Is.EqualTo("$Sheet1.$A$1"));
            Assert.That(PropertyNamed(number.Attributes, "urn:example:data", "root")
                .LexicalValue, Is.EqualTo("preserved"));
            Assert.That(document.Styles.Single().DataStyle, Is.SameAs(number));
            Assert.That(document.Styles.Single().DataStyleNode,
                Is.SameAs(number.Node));
        });

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(WriteDocument(document)));
        Assert.That(StyleNamed(reread, "N").FormatPattern,
            Is.EqualTo("#,## kg0.00"));
    }

    [Test]
    public void WritesDataStyleChangesMapsMovesAndDeletion()
    {
        OdfDocument document = ReadFlat(DataStyleFlatXml(
            "<office:automatic-styles>"
            + "<number:number-style style:name='Keep'><number:number "
            + "number:decimal-places='2'/></number:number-style>"
            + "<number:text-style style:name='Remove'><number:text-content/>"
            + "</number:text-style><style:style style:name='Cell' "
            + "style:family='table-cell' style:data-style-name='Keep'/>"
            + "</office:automatic-styles>"));
        OdfDataStyle style = StyleNamed(document, "Keep");
        style.Name = "Changed";
        style.DisplayName = "Changed Scientific";
        style.Kind = OdfDataStyleKind.Scientific;
        style.IsAutomatic = false;
        style.Language = "fr";
        style.Country = "FR";
        style.Title = "Scientific";
        style.Components =
        [
            NumberComponent(
                "scientific-number",
                "",
                [
                    NumberProperty("min-integer-digits", "1"),
                    NumberProperty("decimal-places", "4"),
                    NumberProperty("min-exponent-digits", "3")
                ]),
            NumberComponent("text", " units", [])
        ];
        style.Maps =
        [
            new OdfDataStyleMap
            {
                Condition = "value()=0",
                ApplyStyleName = "Zero",
                BaseCellAddress = "$Sheet1.$B$2"
            }
        ];
        document.DataStyles = [style];
        document.Styles[0].DataStyleName = "Changed";

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(WriteDocument(document)));
        OdfDataStyle changed = reread.DataStyles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Name, Is.EqualTo("Changed"));
            Assert.That(changed.DisplayName, Is.EqualTo("Changed Scientific"));
            Assert.That(changed.Kind, Is.EqualTo(OdfDataStyleKind.Scientific));
            Assert.That(changed.IsAutomatic, Is.False);
            Assert.That(changed.Language, Is.EqualTo("fr"));
            Assert.That(changed.Country, Is.EqualTo("FR"));
            Assert.That(changed.Title, Is.EqualTo("Scientific"));
            Assert.That(changed.FormatPattern, Is.EqualTo("0.0000E000 units"));
            Assert.That(changed.Maps, Has.Length.EqualTo(1));
            Assert.That(changed.Maps[0].Condition, Is.EqualTo("value()=0"));
            Assert.That(changed.Maps[0].ApplyStyleName, Is.EqualTo("Zero"));
            Assert.That(reread.Styles.Single().DataStyle, Is.SameAs(changed));
            Assert.That(style.Node, Is.Not.Null);
            Assert.That(style.Components[0].Node, Is.Not.Null);
            Assert.That(style.Maps[0].Node, Is.Not.Null);
        });
    }

    [Test]
    public void CreatesCommonAndAutomaticDataStylesInPackageParts()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Spreadsheet);
        OdfDataStyle common = new OdfDataStyle
        {
            Name = "CommonCurrency",
            Kind = OdfDataStyleKind.Currency,
            Language = "en",
            Country = "GB",
            Components =
            [
                NumberComponent("currency-symbol", "£", []),
                NumberComponent(
                    "number",
                    "",
                    [NumberProperty("decimal-places", "2")])
            ]
        };
        OdfDataStyle automatic = new OdfDataStyle
        {
            Name = "AutoDate",
            Kind = OdfDataStyleKind.Date,
            IsAutomatic = true,
            FormatSource = "fixed",
            Components =
            [
                NumberComponent("year", "", [NumberProperty("style", "long")]),
                NumberComponent("text", "-", []),
                NumberComponent("month", "", [NumberProperty("style", "long")]),
                NumberComponent("text", "-", []),
                NumberComponent("day", "", [NumberProperty("style", "long")])
            ]
        };
        document.DataStyles = [common, automatic];

        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            WriteDocument(document),
            "data-styles.ods",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(StyleNamed(reread, "CommonCurrency").PartPath,
                Is.EqualTo(OdfPartNames.Styles));
            Assert.That(StyleNamed(reread, "CommonCurrency").FormatPattern,
                Is.EqualTo("£0.00"));
            Assert.That(StyleNamed(reread, "AutoDate").PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(StyleNamed(reread, "AutoDate").IsAutomatic, Is.True);
            Assert.That(StyleNamed(reread, "AutoDate").FormatPattern,
                Is.EqualTo("yyyy-MM-dd"));
            Assert.That(reread.Manifest?.Entries.Select(item => item.FullPath),
                Does.Contain(OdfPartNames.Styles));
        });
    }

    static OdfDataStyle StyleNamed(OdfDocument document, string name)
    {
        return document.DataStyles.Single(item => item.Name == name);
    }

    static OdfStyleProperty PropertyNamed(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        return properties.Single(item => item.Name.NamespaceName == namespaceName
            && item.Name.LocalName == localName);
    }

    static OdfDataStyleComponent NumberComponent(
        string localName,
        string text,
        OdfStyleProperty[] attributes)
    {
        return new OdfDataStyleComponent
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Number,
                LocalName = localName,
                Prefix = OdfNamespaces.NumberPrefix
            },
            Attributes = attributes,
            Text = text
        };
    }

    static OdfStyleProperty NumberProperty(
        string localName,
        string value)
    {
        return new OdfStyleProperty
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Number,
                LocalName = localName,
                Prefix = OdfNamespaces.NumberPrefix
            },
            LexicalValue = value
        };
    }

    static byte[] WriteDocument(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "data-styles.fods",
            new OdfLoadOptions());
    }

    static string DataStyleFlatXml(string sections)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:number='" + OdfNamespaces.Number + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:ext='urn:example:data'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + sections
            + "<office:body><office:spreadsheet/></office:body>"
            + "</office:document>";
    }
}
