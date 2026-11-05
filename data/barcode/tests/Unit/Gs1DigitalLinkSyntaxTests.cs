namespace barcode.Tests;

public class Gs1DigitalLinkSyntaxTests
{
    [Test]
    public void ParseRecognizesCustomStemPathQualifiersAttributesAndExtensions()
    {
        string source =
            "https://brand.example.com/catalog/01/09520123456788/10/ABC%2F123/21/SER%2B1"
            + "?3103=000195&17=201225&linkType=gs1%3Atraceability#details";

        Gs1DigitalLinkParseResult result =
            Gs1DigitalLinkSyntax.ParseDigitalLink(source);
        Gs1Element[] parsedElements = result.Message.Elements!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.BaseUri,
                Is.EqualTo("https://brand.example.com/catalog"));
            Assert.That(result.Message.Syntax,
                Is.EqualTo(Gs1Syntax.DigitalLinkUri));
            Assert.That(result.Message.SourceText, Is.EqualTo(source));
            Assert.That(
                parsedElements.Select(
                    element => element.ApplicationIdentifier),
                Is.EqualTo(new string[]
                {
                    "01", "10", "21", "3103", "17"
                }));
            Assert.That(
                parsedElements.Select(element => element.Value),
                Is.EqualTo(new string[]
                {
                    "09520123456788",
                    "ABC/123",
                    "SER+1",
                    "000195",
                    "201225"
                }));
            Assert.That(result.IgnoredQueryParameters,
                Is.EqualTo(new string[]
                {
                    "linkType=gs1%3Atraceability"
                }));
        });
    }

    [Test]
    public void BuildCreatesCanonicalPathOrderAndLexicallySortedAttributes()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("3922", "0299"),
            CreateElement("21", "SER+1"),
            CreateElement("17", "201225"),
            CreateElement("10", "ABC/1?"),
            CreateElement("3103", "000195"),
            CreateElement("01", "09520123456788")
        };

        Gs1DigitalLinkBuildResult result =
            Gs1DigitalLinkSyntax.BuildDigitalLink(elements, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Text, Is.EqualTo(
                "https://id.gs1.org/01/09520123456788"
                + "/10/ABC%2F1%3F/21/SER%2B1"
                + "?17=201225&3103=000195&3922=0299"));
            Assert.That(
                result.Elements.Select(
                    element => element.ApplicationIdentifier),
                Is.EqualTo(new string[]
                {
                    "01", "10", "21", "17", "3103", "3922"
                }));
            Assert.That(
                elements.Select(element => element.ApplicationIdentifier),
                Is.EqualTo(new string[]
                {
                    "3922", "21", "17", "10", "3103", "01"
                }));
        });
    }

    [Test]
    public void BuildUsesCustomStemAndEncodesEveryReservedGs1ValueCharacter()
    {
        string symbols = "!\"%&'+,/*():;<=>?";
        Gs1DigitalLinkBuildResult result =
            Gs1DigitalLinkSyntax.BuildDigitalLink(
                new Gs1Element[]
                {
                    CreateElement("01", "09520123456788"),
                    CreateElement("10", "A" + symbols)
                },
                "http://example.com/products/");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(
                "http://example.com/products/01/09520123456788/10/A"
                + "%21%22%25%26%27%2B%2C%2F%2A%28%29%3A%3B%3C%3D%3E%3F"));
            Assert.That(Gs1DigitalLinkSyntax.EncodeUriValue(
                "AZaz09-._"), Is.EqualTo("AZaz09-._"));
            Assert.That(Gs1DigitalLinkSyntax.EncodeUriValue("#"),
                Is.EqualTo("%23"));
        });
    }

    [Test]
    public void ParseBuildParseRoundTripCanonicalizesStemQueryAndEscapes()
    {
        Gs1DigitalLinkParseResult first =
            Gs1DigitalLinkSyntax.ParseDigitalLink(
                "http://example.com/items/01/09520123456788/10/ABC%2f123"
                + "?3922=0299&campaign=spring&17=201225&3103=000195#offer");

        Gs1DigitalLinkBuildResult built =
            Gs1DigitalLinkSyntax.BuildDigitalLink(
                first.Message.Elements,
                null);
        Gs1DigitalLinkParseResult second =
            Gs1DigitalLinkSyntax.ParseDigitalLink(built.Text);
        Gs1Element[] secondElements = second.Message.Elements!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True,
                string.Join(" | ", first.Issues.Select(
                    issue => issue.Code + ":" + issue.Field
                        + ":" + issue.Expected + ":" + issue.Message)));
            Assert.That(built.Success, Is.True);
            Assert.That(built.Text, Is.EqualTo(
                "https://id.gs1.org/01/09520123456788/10/ABC%2F123"
                + "?17=201225&3103=000195&3922=0299"));
            Assert.That(second.Success, Is.True);
            Assert.That(
                secondElements.Select(
                    element => element.ApplicationIdentifier),
                Is.EqualTo(new string[]
                {
                    "01", "10", "17", "3103", "3922"
                }));
            Assert.That(
                secondElements.Select(element => element.Value),
                Is.EqualTo(new string[]
                {
                    "09520123456788",
                    "ABC/123",
                    "201225",
                    "000195",
                    "0299"
                }));
        });
    }

    [TestCase(
        "ftp://id.gs1.org/01/09520123456788",
        "GS1_DIGITAL_LINK_SCHEME_INVALID")]
    [TestCase(
        "https://id.gs1.org/catalog",
        "GS1_DIGITAL_LINK_PRIMARY_KEY_MISSING")]
    [TestCase(
        "https://id.gs1.org/01/09520123456788/10",
        "GS1_DIGITAL_LINK_PATH_PAIR_INCOMPLETE")]
    [TestCase(
        "https://id.gs1.org/01/09520123456788/10/ABC%2G",
        "GS1_DIGITAL_LINK_PERCENT_ENCODING_INVALID")]
    [TestCase(
        "https://id.gs1.org/01/09520123456788/10/ABC+123",
        "GS1_DIGITAL_LINK_VALUE_NOT_PERCENT_ENCODED")]
    public void ParseReportsMalformedUriStructure(
        string source,
        string expectedCode)
    {
        Gs1DigitalLinkParseResult result =
            Gs1DigitalLinkSyntax.ParseDigitalLink(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain(expectedCode));
            Assert.That(result.Issues, Has.All.Matches<BarcodeValidationIssue>(
                issue => issue.Message != null
                    && issue.Message.Length > 0
                    && issue.Expected != null
                    && issue.Expected.Length > 0));
        });
    }

    [TestCase(
        "https://id.gs1.org/01/09520123456788/21/SERIAL/10/LOT",
        "GS1_DIGITAL_LINK_PATH_QUALIFIERS_INVALID")]
    [TestCase(
        "https://id.gs1.org/01/09520123456788?10=LOT",
        "GS1_DIGITAL_LINK_QUALIFIER_IN_QUERY")]
    [TestCase(
        "https://id.gs1.org/01/09520123456788?7041=ABCD",
        "GS1_DIGITAL_LINK_QUERY_ATTRIBUTE_NOT_PERMITTED")]
    [TestCase(
        "https://id.gs1.org/01/09520123456788?236=VALUE",
        "GS1_DIGITAL_LINK_QUERY_AI_UNKNOWN")]
    public void ParseEnforcesPathAndQueryRoles(
        string source,
        string expectedCode)
    {
        Gs1DigitalLinkParseResult result =
            Gs1DigitalLinkSyntax.ParseDigitalLink(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain(expectedCode));
        });
    }

    [Test]
    public void BuildRejectsMixedQualifierAlternativesAndInvalidBaseUri()
    {
        Gs1DigitalLinkBuildResult mixed =
            Gs1DigitalLinkSyntax.BuildDigitalLink(
                new Gs1Element[]
                {
                    CreateElement("01", "09520123456788"),
                    CreateElement("235", "TPX"),
                    CreateElement("10", "LOT")
                },
                null);
        Gs1DigitalLinkBuildResult invalidBase =
            Gs1DigitalLinkSyntax.BuildDigitalLink(
                new Gs1Element[]
                {
                    CreateElement("01", "09520123456788")
                },
                "ftp://example.com?query=value");

        Assert.Multiple(() =>
        {
            Assert.That(mixed.Success, Is.False);
            Assert.That(mixed.Text, Is.Empty);
            Assert.That(mixed.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_DIGITAL_LINK_QUALIFIER_SET_INVALID"));
            Assert.That(invalidBase.Success, Is.False);
            Assert.That(invalidBase.Text, Is.Empty);
            Assert.That(invalidBase.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_DIGITAL_LINK_BASE_URI_INVALID"));
        });
    }

    static Gs1Element CreateElement(string identifier, string value)
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = identifier;
        element.Value = value;
        return element;
    }
}
