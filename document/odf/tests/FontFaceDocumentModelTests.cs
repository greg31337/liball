using System.Text;

namespace odf.Tests;

public class FontFaceDocumentModelTests
{
    [Test]
    public void MaterializesFontFaceAttributesAndEverySourceKind()
    {
        string xml = FlatXml(
            "<office:font-face-decls>"
            + "<style:font-face style:name='Declared' "
            + "svg:font-family='&quot;Example Sans&quot;' "
            + "svg:font-style='italic' svg:font-variant='small-caps' "
            + "svg:font-weight='700' svg:font-stretch='condensed' "
            + "svg:font-size='12pt' svg:unicode-range='U+0000-007F' "
            + "svg:units-per-em='1000' svg:panose-1='2 11 6 4' "
            + "svg:stemv='80' svg:stemh='70' svg:slope='0' "
            + "svg:cap-height='700' svg:x-height='500' "
            + "svg:accent-height='720' svg:ascent='800' svg:descent='-200' "
            + "svg:widths='0 500' svg:bbox='0 -200 1000 800' "
            + "svg:ideographic='-100' svg:alphabetic='0' "
            + "svg:mathematical='350' svg:hanging='600' "
            + "svg:v-ideographic='900' svg:v-alphabetic='800' "
            + "svg:v-mathematical='500' svg:v-hanging='200' "
            + "svg:underline-position='-75' svg:underline-thickness='50' "
            + "svg:strikethrough-position='300' "
            + "svg:strikethrough-thickness='50' "
            + "svg:overline-position='750' svg:overline-thickness='50' "
            + "style:font-adornments='Regular' "
            + "style:font-family-generic='swiss' "
            + "style:font-pitch='variable' style:font-charset='x-symbol' "
            + "ext:token='preserved'>"
            + "<svg:font-face-src>"
            + "<svg:font-face-name svg:name='Installed Example'/>"
            + "<svg:font-face-uri xlink:type='simple' "
            + "xlink:href='https://example.test/font.woff2' "
            + "xlink:actuate='onRequest'>"
            + "<svg:font-face-format svg:string='woff2'/>"
            + "</svg:font-face-uri>"
            + "</svg:font-face-src>"
            + "<svg:definition-src xlink:type='simple' "
            + "xlink:href='https://example.test/font-definition'/>"
            + "</style:font-face>"
            + "</office:font-face-decls>");
        OdfDocument document = ReadFlat(xml);
        OdfFontFace face = document.FontFaces.Single();

        Assert.Multiple(() =>
        {
            Assert.That(face.Name, Is.EqualTo("Declared"));
            Assert.That(face.FontFamily, Is.EqualTo("\"Example Sans\""));
            Assert.That(face.FontStyle, Is.EqualTo("italic"));
            Assert.That(face.FontVariant, Is.EqualTo("small-caps"));
            Assert.That(face.FontWeight, Is.EqualTo("700"));
            Assert.That(face.FontStretch, Is.EqualTo("condensed"));
            Assert.That(face.FontSize, Is.EqualTo("12pt"));
            Assert.That(face.UnicodeRange, Is.EqualTo("U+0000-007F"));
            Assert.That(face.UnitsPerEm, Is.EqualTo("1000"));
            Assert.That(face.Panose1, Is.EqualTo("2 11 6 4"));
            Assert.That(face.StemVertical, Is.EqualTo("80"));
            Assert.That(face.StemHorizontal, Is.EqualTo("70"));
            Assert.That(face.Slope, Is.EqualTo("0"));
            Assert.That(face.CapHeight, Is.EqualTo("700"));
            Assert.That(face.XHeight, Is.EqualTo("500"));
            Assert.That(face.AccentHeight, Is.EqualTo("720"));
            Assert.That(face.Ascent, Is.EqualTo("800"));
            Assert.That(face.Descent, Is.EqualTo("-200"));
            Assert.That(face.Widths, Is.EqualTo("0 500"));
            Assert.That(face.BoundingBox, Is.EqualTo("0 -200 1000 800"));
            Assert.That(face.Ideographic, Is.EqualTo("-100"));
            Assert.That(face.Alphabetic, Is.EqualTo("0"));
            Assert.That(face.Mathematical, Is.EqualTo("350"));
            Assert.That(face.Hanging, Is.EqualTo("600"));
            Assert.That(face.VerticalIdeographic, Is.EqualTo("900"));
            Assert.That(face.VerticalAlphabetic, Is.EqualTo("800"));
            Assert.That(face.VerticalMathematical, Is.EqualTo("500"));
            Assert.That(face.VerticalHanging, Is.EqualTo("200"));
            Assert.That(face.UnderlinePosition, Is.EqualTo("-75"));
            Assert.That(face.UnderlineThickness, Is.EqualTo("50"));
            Assert.That(face.StrikethroughPosition, Is.EqualTo("300"));
            Assert.That(face.StrikethroughThickness, Is.EqualTo("50"));
            Assert.That(face.OverlinePosition, Is.EqualTo("750"));
            Assert.That(face.OverlineThickness, Is.EqualTo("50"));
            Assert.That(face.FontAdornments, Is.EqualTo("Regular"));
            Assert.That(face.GenericFamily, Is.EqualTo("swiss"));
            Assert.That(face.Pitch, Is.EqualTo("variable"));
            Assert.That(face.Charset, Is.EqualTo("x-symbol"));
            Assert.That(face.PartPath, Is.Empty);
            Assert.That(face.Node, Is.Not.Null);
            Assert.That(face.Sources, Has.Length.EqualTo(3));
            Assert.That(face.Sources[0].Name, Is.EqualTo("Installed Example"));
            Assert.That(face.Sources[0].IsEmbedded, Is.False);
            Assert.That(face.Sources[0].IsExternal, Is.False);
            Assert.That(face.Sources[1].Href, Is.EqualTo("https://example.test/font.woff2"));
            Assert.That(face.Sources[1].XLinkType, Is.EqualTo("simple"));
            Assert.That(face.Sources[1].Actuate, Is.EqualTo("onRequest"));
            Assert.That(face.Sources[1].Formats, Is.EqualTo(new[] { "woff2" }));
            Assert.That(face.Sources[1].IsExternal, Is.True);
            Assert.That(face.Sources[2].IsDefinitionSource, Is.True);
        });

        byte[] written = Write(document);
        string output = Encoding.UTF8.GetString(written);
        Assert.That(output, Does.Contain("ext:token=\"preserved\""));
    }

    [Test]
    public void WritesChangesAndRemovesDeletedFacesAndSources()
    {
        string xml = FlatXml(
            "<office:font-face-decls>"
            + "<style:font-face style:name='Keep' svg:font-family='Old Family'>"
            + "<svg:font-face-src>"
            + "<svg:font-face-name svg:name='Remove Local'/>"
            + "<svg:font-face-uri xlink:type='simple' xlink:href='old.woff'>"
            + "<svg:font-face-format svg:string='woff'/>"
            + "</svg:font-face-uri>"
            + "</svg:font-face-src>"
            + "</style:font-face>"
            + "<style:font-face style:name='Remove Face' svg:font-family='Gone'/>"
            + "</office:font-face-decls>");
        OdfDocument document = ReadFlat(xml);
        OdfFontFace changed = document.FontFaces.Single(
            item => item.Name == "Keep");
        OdfFontFaceSource retained = changed.Sources[1];
        changed.Name = "Changed";
        changed.FontFamily = "New Family";
        changed.FontWeight = "bold";
        changed.UnitsPerEm = "2048";
        changed.UnderlinePosition = "-100";
        retained.Href = "new.woff2";
        retained.Formats = ["woff2"];
        OdfFontFace added = new OdfFontFace
        {
            Name = "Added",
            FontFamily = "Added Family",
            Sources =
            [
                new OdfFontFaceSource
                {
                    Href = "https://example.test/added.ttf",
                    Formats = ["truetype"]
                },
                new OdfFontFaceSource
                {
                    Href = "https://example.test/definition",
                    IsDefinitionSource = true
                }
            ]
        };
        changed.Sources = [retained];
        document.FontFaces = [changed, added];

        OdfDocument reread = ReadFlat(Encoding.UTF8.GetString(Write(document)));
        OdfFontFace updated = reread.FontFaces.Single(
            item => item.Name == "Changed");
        OdfFontFace generated = reread.FontFaces.Single(
            item => item.Name == "Added");

        Assert.Multiple(() =>
        {
            Assert.That(reread.FontFaces, Has.Length.EqualTo(2));
            Assert.That(
                reread.FontFaces.Any(item => item.Name == "Remove Face"),
                Is.False);
            Assert.That(updated.FontFamily, Is.EqualTo("New Family"));
            Assert.That(updated.FontWeight, Is.EqualTo("bold"));
            Assert.That(updated.UnitsPerEm, Is.EqualTo("2048"));
            Assert.That(updated.UnderlinePosition, Is.EqualTo("-100"));
            Assert.That(updated.Sources, Has.Length.EqualTo(1));
            Assert.That(updated.Sources[0].Name, Is.Empty);
            Assert.That(updated.Sources[0].Href, Is.EqualTo("new.woff2"));
            Assert.That(updated.Sources[0].Formats, Is.EqualTo(new[] { "woff2" }));
            Assert.That(generated.Sources, Has.Length.EqualTo(2));
            Assert.That(generated.Sources[0].XLinkType, Is.EqualTo("simple"));
            Assert.That(generated.Sources[1].IsDefinitionSource, Is.True);
            Assert.That(changed.Node, Is.Not.Null);
            Assert.That(added.Node, Is.Not.Null);
        });
    }

    [Test]
    public void CreatesPackageDeclarationsAndResolvesFontResources()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPackage package = document.Package
            ?? throw new InvalidOperationException("Editable package expected.");
        new OdfPackageWriter().AddPackageEntry(
            package,
            "Fonts/embedded.woff2",
            [1, 2, 3, 4],
            OdfCompressionMethod.Deflated);
        document.FontFaces =
        [
            new OdfFontFace
            {
                Name = "Package Face",
                FontFamily = "Package Family",
                Sources =
                [
                    new OdfFontFaceSource
                    {
                        Href = "Fonts/embedded.woff2",
                        Formats = ["woff2"]
                    },
                    new OdfFontFaceSource
                    {
                        Href = "Fonts/missing.ttf",
                        Formats = ["truetype"]
                    },
                    new OdfFontFaceSource
                    {
                        Href = "https://example.test/external.woff2",
                        Formats = ["woff2"]
                    }
                ]
            },
            new OdfFontFace
            {
                Name = "Content Face",
                FontFamily = "Content Family",
                PartPath = OdfPartNames.Content
            }
        ];

        byte[] written = Write(document);
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "fonts.odt",
            new OdfLoadOptions());
        OdfFontFace packageFace = reread.FontFaces.Single(
            item => item.Name == "Package Face");
        OdfFontFace contentFace = reread.FontFaces.Single(
            item => item.Name == "Content Face");
        OdfValidationOptions validationOptions = new OdfValidationOptions();
        validationOptions.Mode = OdfValidationMode.SemanticOnly;
        validationOptions.Version = OdfVersion.Version14;
        SemanticValidationResult validation =
            new SemanticDocumentValidator(validationOptions)
                .ValidateDocument(reread);

        Assert.Multiple(() =>
        {
            Assert.That(packageFace.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(contentFace.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(packageFace.Sources[0].IsEmbedded, Is.True);
            Assert.That(
                packageFace.Sources[0].ResolvedPath,
                Is.EqualTo("Fonts/embedded.woff2"));
            Assert.That(packageFace.Sources[0].ResourceExists, Is.True);
            Assert.That(packageFace.Sources[1].IsEmbedded, Is.True);
            Assert.That(packageFace.Sources[1].ResourceExists, Is.False);
            Assert.That(packageFace.Sources[2].IsExternal, Is.True);
            Assert.That(
                reread.Parts.Select(item => item.Path),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(
                reread.Manifest?.Entries.Select(item => item.FullPath),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(
                validation.Diagnostics.Count(
                    item => item.Code
                        == OdfDiagnosticCodes.InvalidResourceReference),
                Is.EqualTo(1));
        });
    }

    static byte[] Write(OdfDocument document)
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
            "fonts.fodt",
            new OdfLoadOptions());
    }

    static string FlatXml(string declarations)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:font-extension'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + declarations
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
