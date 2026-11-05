using System.Text;
using System.Xml.Linq;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetDataUriTests
{
    const string Prefix = "data:image/svg+xml;base64,";

    [Test]
    public void PlaceholderDataUrisAreDeterministicAndSelfContained()
    {
        InternetProvider first = Fake.Create(8801).Internet();
        InternetProvider second = Fake.Create(8801).Internet();

        string value = first.DataUri();
        string svg = Decode(value);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(second.DataUri()));
            Assert.That(value, Does.StartWith(Prefix));
            Assert.That(value, Does.Not.Contain("http://"));
            Assert.That(value, Does.Not.Contain("https://"));
            Assert.That(svg, Does.Contain("Synthetic placeholder"));
            Assert.That(svg, Does.Not.Contain("<script"));
            Assert.That(svg, Does.Not.Contain("<image"));
            Assert.That(svg, Does.Not.Contain("href="));
            Assert.That(svg, Does.Not.Contain("url("));
        });
    }

    [Test]
    public void PlaceholderPayloadIsWellFormedSvgWithCorrelatedDimensions()
    {
        string value = Fake.Create(8802).Internet().DataUriFromWidthAndHeight(321, 123);
        XDocument document = XDocument.Parse(Decode(value));
        XElement root = document.Root;

        Assert.Multiple(() =>
        {
            Assert.That(root.Name.LocalName, Is.EqualTo("svg"));
            Assert.That(root.Name.NamespaceName, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(root.Attribute("width").Value, Is.EqualTo("321"));
            Assert.That(root.Attribute("height").Value, Is.EqualTo("123"));
            Assert.That(root.Attribute("viewBox").Value, Is.EqualTo("0 0 321 123"));
            Assert.That(root.Elements().Count(), Is.EqualTo(3));
            Assert.That(root.Descendants(root.Name.Namespace + "rect").Count(), Is.EqualTo(1));
            Assert.That(root.Descendants(root.Name.Namespace + "path").Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ImageAndSvgAliasesGenerateTheSameLocalPlaceholderShape()
    {
        InternetProvider dataInternet = Fake.Create(8803).Internet();
        InternetProvider imageInternet = Fake.Create(8803).Internet();
        InternetProvider svgInternet = Fake.Create(8803).Internet();

        Assert.Multiple(() =>
        {
            Assert.That(imageInternet.ImageDataUri(), Is.EqualTo(dataInternet.DataUri()));
            Assert.That(svgInternet.SvgDataUri(), Is.EqualTo(Fake.Create(8803).Internet().DataUri()));
            Assert.That(
                Decode(Fake.Create(8804).Internet().ImageDataUriFromWidthAndHeight(80, 60)),
                Is.EqualTo(Decode(Fake.Create(8804).Internet().DataUriFromWidthAndHeight(80, 60))));
            Assert.That(
                Decode(Fake.Create(8805).Internet().SvgDataUriFromWidthAndHeight(40, 30)),
                Is.EqualTo(Decode(Fake.Create(8805).Internet().DataUriFromWidthAndHeight(40, 30))));
        });
    }

    [Test]
    public void AvatarDataUriIsAnAnonymousLocalSvgAtTheRequestedSize()
    {
        InternetProvider first = Fake.Create(8806).Internet();
        InternetProvider second = Fake.Create(8806).Internet();
        string value = first.AvatarDataUriFromSize(256);
        string svg = Decode(value);
        XDocument document = XDocument.Parse(svg);
        XElement root = document.Root;

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(second.AvatarDataUriFromSize(256)));
            Assert.That(value, Does.StartWith(Prefix));
            Assert.That(root.Attribute("width").Value, Is.EqualTo("256"));
            Assert.That(root.Attribute("height").Value, Is.EqualTo("256"));
            Assert.That(root.Attribute("viewBox").Value, Is.EqualTo("0 0 100 100"));
            Assert.That(root.Descendants(root.Name.Namespace + "rect").Count(), Is.EqualTo(1));
            Assert.That(root.Descendants(root.Name.Namespace + "circle").Count(), Is.EqualTo(2));
            Assert.That(svg, Does.Contain("Synthetic avatar"));
            Assert.That(svg, Does.Not.Contain("<script"));
            Assert.That(svg, Does.Not.Contain("<image"));
            Assert.That(svg, Does.Not.Contain("href="));
        });
    }

    [Test]
    public void DefaultAvatarUsesA128PixelSquare()
    {
        XDocument document = XDocument.Parse(Decode(Fake.Create(8807).Internet().AvatarDataUri()));
        XElement root = document.Root;

        Assert.That(root.Attribute("width").Value, Is.EqualTo("128"));
        Assert.That(root.Attribute("height").Value, Is.EqualTo("128"));
    }

    [Test]
    public void DataUriDimensionsEnforceDocumentedBounds()
    {
        InternetProvider internet = Fake.Create(8808).Internet();

        Assert.That(() => internet.DataUriFromWidthAndHeight(0, 10),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("width"));
        Assert.That(() => internet.ImageDataUriFromWidthAndHeight(10, 4097),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("height"));
        Assert.That(() => internet.AvatarDataUriFromSize(0),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("size"));
        Assert.That(() => internet.AvatarDataUriFromSize(4097),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("size"));
        Assert.That(() => internet.DataUriFromWidthAndHeight(1, 1), Throws.Nothing);
        Assert.That(() => internet.SvgDataUriFromWidthAndHeight(4096, 4096), Throws.Nothing);
    }

    [Test]
    public void DataUrisHonorTheConfiguredMaximumOutputSize()
    {
        string baseline = Fake.Create(8809).Internet().DataUri();
        FakeOptions exactOptions = FakeOptions.Create(8809);
        exactOptions.MaximumOutputSize = baseline.Length;
        FakeOptions shortOptions = FakeOptions.Create(8809);
        shortOptions.MaximumOutputSize = baseline.Length - 1;

        Assert.That(Fake.Create(exactOptions).Internet().DataUri(), Is.EqualTo(baseline));
        Assert.That(
            () => Fake.Create(shortOptions).Internet().DataUri(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string Decode(string value)
    {
        Assert.That(value, Does.StartWith(Prefix));
        string payload = value.Substring(Prefix.Length);
        return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }
}
