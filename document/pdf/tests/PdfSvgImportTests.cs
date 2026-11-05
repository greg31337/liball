using System.Text;
using System.Xml;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfSvgImportTests
{
    [Test]
    public void Loader_ResolvesIntrinsicDimensionsAndLeavesStreamsOpen()
    {
        PdfSvgLoader loader = new PdfSvgLoader();
        PdfSvgData physical = loader.Load("<svg xmlns='http://www.w3.org/2000/svg' width='2in' height='25.4mm'/>");
        PdfSvgData viewBox = loader.Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 100'/>");
        PdfSvgData defaults = loader.Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='40' height='20'/>") );

        PdfSvgData streamed = loader.LoadFromStream(stream);

        Assert.Multiple(() =>
        {
            Assert.That(physical.Width, Is.EqualTo(144).Within(0.000001));
            Assert.That(physical.Height, Is.EqualTo(72).Within(0.000001));
            Assert.That(viewBox.Width, Is.EqualTo(150));
            Assert.That(viewBox.Height, Is.EqualTo(75));
            Assert.That(viewBox.HasViewBox, Is.True);
            Assert.That(defaults.Width, Is.EqualTo(225));
            Assert.That(defaults.Height, Is.EqualTo(112.5));
            Assert.That(streamed.Width, Is.EqualTo(30));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void Import_RendersShapesPathsStylesAndTransformsAsNativeCommands()
    {
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100' viewBox='0 0 200 100'>" +
            "<g transform='translate(2 3) scale(2) rotate(5) skewX(3)'>" +
            "<rect x='1' y='2' width='30' height='20' rx='3' ry='4' fill='#f00' stroke='rgb(0,0,255)' stroke-width='2' stroke-linecap='round' stroke-linejoin='bevel' stroke-dasharray='3,2'/>" +
            "<circle cx='50' cy='20' r='8' fill='green'/><ellipse cx='75' cy='20' rx='10' ry='5'/>" +
            "<line x1='0' y1='40' x2='30' y2='40' stroke='black'/><polyline points='35,40 45,45 55,40' fill='none' stroke='black'/>" +
            "<polygon points='60,40 70,50 80,40'/><path d='M 5 60 l 5 0 H 20 V 70 C 22 72 24 72 26 70 S 30 68 32 70 Q 35 75 38 70 T 44 70 z' fill-rule='evenodd'/>" +
            "</g></svg>";

        PdfFormXObject form = new PdfDocument().AddSvg(svg);
        string content = form.Canvas.ToPdfString();

        Assert.Multiple(() =>
        {
            Assert.That(form.Width, Is.EqualTo(150));
            Assert.That(form.Height, Is.EqualTo(75));
            Assert.That(content, Does.Contain("0.75 0 0 -0.75 0 75 cm"));
            Assert.That(content, Does.Contain("1 0 0 1 2 3 cm"));
            Assert.That(content, Does.Contain("2 0 0 2 0 0 cm"));
            Assert.That(content, Does.Contain("1 0 0.052 1 0 0 cm"));
            Assert.That(content, Does.Contain("1 0 0 rg"));
            Assert.That(content, Does.Contain("0 0 1 RG"));
            Assert.That(content, Does.Contain("2 w"));
            Assert.That(content, Does.Contain("[3 2] 0 d"));
            Assert.That(content, Does.Contain(" c"));
            Assert.That(content, Does.Contain("f*"));
            Assert.That(content, Does.Contain("S"));
        });
    }

    [Test]
    public void Import_AppliesLocalClipPathAndEvenOddRule()
    {
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='50'>" +
            "<defs><clipPath id='cut'><path d='M0 0 H40 V40 H0 Z' clip-rule='evenodd'/></clipPath></defs>" +
            "<g clip-path='url(#cut)'><rect width='100' height='50' fill='blue'/></g></svg>";

        string content = new PdfDocument().AddSvg(svg).Canvas.ToPdfString();

        Assert.That(content, Does.Contain("W*"));
        Assert.That(content, Does.Contain("n"));
        Assert.That(content, Does.Contain("0 0 1 rg"));
        Assert.That(content, Does.Contain("f"));
    }

    [Test]
    public void Import_RendersStyledTextFallbackWithSvgBaselineOrientation()
    {
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>" +
            "<text x='60' y='24' dx='2' font-size='12' font-family='sans-serif' font-weight='bold' font-style='italic' text-anchor='middle' fill='#00ff00'>Hello</text></svg>";

        string content = new PdfDocument().AddSvg(svg).Canvas.ToPdfString();

        Assert.Multiple(() =>
        {
            Assert.That(content, Does.Contain("0 1 0 rg"));
            Assert.That(content, Does.Contain("1 0 0 -1 0 48 cm"));
            Assert.That(content, Does.Contain("/F4 12 Tf"));
            Assert.That(content, Does.Contain("(Hello) Tj"));
            Assert.That(content, Does.Contain("BT"));
            Assert.That(content, Does.Contain("ET"));
        });
    }

    [Test]
    public void Document_SvgInputsRegisterReusableSerializableForms()
    {
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='10'><rect width='20' height='10'/></svg>";
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, svg);
            PdfDocument document = new PdfDocument();
            PdfSvgLoader loader = new PdfSvgLoader();
            PdfFormXObject first = document.AddLoadedSvg(loader.Load(svg));
            MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(svg));
            PdfFormXObject second = document.AddSvgFromStream(input);
            PdfFormXObject third = document.AddSvgFromFile(path);
            PdfPage page = document.AddPage(100, 100);
            page.Canvas.DrawForm(first, 5, 5, first.Width, first.Height);
            page.Canvas.DrawForm(second, 5, 25, second.Width, second.Height);
            page.Canvas.DrawForm(third, 5, 45, third.Width, third.Height);

            string pdf = Encoding.ASCII.GetString(document.ToBytes());

            Assert.Multiple(() =>
            {
                Assert.That(first.Name, Is.EqualTo("Fm1"));
                Assert.That(second.Name, Is.EqualTo("Fm2"));
                Assert.That(third.Name, Is.EqualTo("Fm3"));
                Assert.That(input.CanRead, Is.True);
                Assert.That(pdf, Does.Contain("/Subtype /Form"));
                Assert.That(pdf, Does.Contain("/Fm1 Do"));
                Assert.That(pdf, Does.Contain("/Fm2 Do"));
                Assert.That(pdf, Does.Contain("/Fm3 Do"));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><image href='data:image/png;base64,x'/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><foreignObject/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><use href='#shape'/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='10' fill='url(#gradient)'/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='10' onclick='run()'/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><g clip-path='url(https://example.test/cut)'><rect width='10' height='10'/></g></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><path d='M0 0 A5 5 0 0 0 10 10'/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><video/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='10' vector-effect='non-scaling-stroke'/></svg>")]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'><metadata><script>run()</script></metadata></svg>")]
    public void Import_RejectsUnsafeOrUnsupportedContent(string svg)
    {
        Assert.Throws<InvalidDataException>(() => new PdfDocument().AddSvg(svg));
    }

    [Test]
    public void Loader_RejectsDocumentTypesAndEntities()
    {
        string svg = "<!DOCTYPE svg [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><svg xmlns='http://www.w3.org/2000/svg'>&xxe;</svg>";
        PdfSvgLoader loader = new PdfSvgLoader();

        Assert.Throws<InvalidDataException>(() => loader.Load(svg));

        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
        Assert.Throws<XmlException>(() => loader.LoadFromStream(stream));
    }

    [Test]
    public void LoaderAndDocumentRejectInvalidPublicInputs()
    {
        PdfSvgLoader loader = new PdfSvgLoader();
        MemoryStream unreadable = new MemoryStream();
        unreadable.Dispose();
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromStream(null!));
        Assert.Throws<ArgumentException>(() => loader.LoadFromStream(unreadable));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromFile(null!));
        Assert.Throws<XmlException>(() => loader.Load("<svg"));
        Assert.Throws<InvalidDataException>(() => loader.Load("<html/>"));
        Assert.Throws<InvalidDataException>(() => loader.Load("<svg xmlns='https://example.test/svg'/>"));
        Assert.Throws<ArgumentNullException>(() => document.AddLoadedSvg(null!));
    }

    [Test]
    public void Import_RejectsInvalidIntrinsicSizingAndMissingGeometry()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<InvalidDataException>(() => document.AddSvg("<svg xmlns='http://www.w3.org/2000/svg' width='0' height='10'/>"));
        Assert.Throws<InvalidDataException>(() => document.AddSvg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 -1 10'/>"));
        Assert.Throws<InvalidDataException>(() => document.AddSvg("<svg xmlns='http://www.w3.org/2000/svg'><rect height='10'/></svg>"));
        Assert.Throws<InvalidDataException>(() => document.AddSvg("<svg xmlns='http://www.w3.org/2000/svg'><circle r='0'/></svg>"));
    }
}
